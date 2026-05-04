using Gheetah.Interfaces;
using Gheetah.Models.EditorModel;
using Gheetah.Models.ProjectModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace Gheetah.Controllers
{
public class EditorController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;

        public EditorController(IProjectService projectService, IFileService fileService)
        {
            _projectService = projectService;
            _fileService = fileService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string id)
        {
            var projects = await _projectService.GetProjectsAsync();
            var targetProject = projects?.FirstOrDefault(p => p.Id == id);

            if (targetProject == null)
                return RedirectToAction("ProjectList", "Projects");

            ViewBag.ProjectId = id;
            ViewBag.ProjectName = targetProject.Name;
            ViewBag.LanguageType = targetProject.LanguageType;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectTree(string projectId)
        {
            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return NotFound();

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);
            if (!Directory.Exists(projectPath)) return NotFound("Project folder not found.");

            string bddFramework = "None";
            if (project.LanguageType.Equals("c#", StringComparison.OrdinalIgnoreCase))
            {
                var csprojPath = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories).FirstOrDefault();
                if (csprojPath != null)
                {
                    var content = await System.IO.File.ReadAllTextAsync(csprojPath);
                    if (content.Contains("Reqnroll")) bddFramework = "Reqnroll";
                    else if (content.Contains("SpecFlow")) bddFramework = "SpecFlow";
                }
            }

            var items = GetDirectoryStructure(projectPath);
            var treeData = ConvertToJsTreeFormat(items, project.LanguageType, bddFramework);
            return Json(treeData);
        }

        [HttpGet]
        public async Task<IActionResult> GetFileContent(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return BadRequest("Path is empty");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            if (!filePath.StartsWith(clonesRoot)) return Forbid();
            if (!System.IO.File.Exists(filePath)) return NotFound();

            var content = await System.IO.File.ReadAllTextAsync(filePath);
            
            var hash = CalculateMD5(content);

            return Json(new { content = content, hash = hash });
        }

        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> SaveFileContent([FromBody] SaveFileRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.FilePath)) return BadRequest("Invalid request.");

            try
            {
                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                if (!request.FilePath.StartsWith(clonesRoot)) return Forbid();

                var currentServerContent = await System.IO.File.ReadAllTextAsync(request.FilePath);
                var currentServerHash = CalculateMD5(currentServerContent);

                if (currentServerHash != request.ClientHash)
                {
                    return Conflict(new { message = "Conflict Detected: While you were making edits, someone else updated the file on the server." });
                }

                await System.IO.File.WriteAllTextAsync(request.FilePath, request.Content);

                var newHash = CalculateMD5(request.Content);
                return Ok(new { success = true, newHash = newHash });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"The file could not be saved: {ex.Message}");
            }
        }

        private string CalculateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.UTF8.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }

        private List<FileSystemItem> GetDirectoryStructure(string path)
        {
            var items = new List<FileSystemItem>();
            var dirInfo = new DirectoryInfo(path);

            var forbiddenFolders = new[] { "bin", "obj", ".git", ".vs" };

            foreach (var dir in dirInfo.GetDirectories())
            {
                if (forbiddenFolders.Contains(dir.Name.ToLower())) continue;

                items.Add(new FileSystemItem
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    Type = "folder",
                    Children = GetDirectoryStructure(dir.FullName)
                });
            }

            foreach (var file in dirInfo.GetFiles())
            {
                items.Add(new FileSystemItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    Type = "file"
                });
            }

            return items;
        }

        private List<object> ConvertToJsTreeFormat(List<FileSystemItem> items, string language, string bddFramework)
        {
            if (items == null) return new List<object>();

            return items.Select(item => new
            {
                id = item.FullPath,
                text = item.Name,
                type = item.Type,
                icon = GetBestIcon(item, language, bddFramework),
                children = item.Children != null ? ConvertToJsTreeFormat(item.Children, language, bddFramework) : null
            }).Cast<object>().ToList();
        }
        private string GetBestIcon(FileSystemItem item, string language, string bddFramework)
        {
            if (item.Type == "folder") return "ti ti-folder";

            string fileName = item.Name.ToLower();
            if (fileName.EndsWith(".feature")) return "ti ti-brand-cucumber";
            if (fileName.EndsWith(".cs")) return "ti ti-brand-c-sharp";
            if (fileName.EndsWith(".csproj")) return "ti ti-file-code";
            if (fileName.EndsWith(".java")) return "ti ti-coffee";
            if (fileName.EndsWith(".xml") || fileName.EndsWith(".config")) return "ti ti-file-type-xml";
            if (fileName.EndsWith(".json")) return "ti ti-json";

            return "ti ti-file";
        }
    }
}