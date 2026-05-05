using Gheetah.Interfaces;
using Gheetah.Models.EditorModel;
using LibGit2Sharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;
using Gheetah.Models;

namespace Gheetah.Controllers
{
public class EditorController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;
        private readonly IUserService _userService;

        public EditorController(IProjectService projectService, IFileService fileService, IUserService userService)
        {
            _projectService = projectService;
            _fileService = fileService;
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string id)
        {
            var projects = await _projectService.GetProjectsAsync();
            var targetProject = projects?.FirstOrDefault(p => p.Id == id);

            if (targetProject == null)
                return RedirectToAction("ProjectList", "Projects");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, targetProject.Name);

            string currentBranch = "main"; // Fallback

            if (Directory.Exists(projectPath) && Repository.IsValid(projectPath))
            {
                using (var repo = new Repository(projectPath))
                {
                    currentBranch = repo.Head.FriendlyName;
                }
            }

            ViewBag.ProjectId = id;
            ViewBag.ProjectName = targetProject.Name;
            ViewBag.LanguageType = targetProject.LanguageType;
            ViewBag.CurrentBranch = currentBranch;

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

        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> CommitAndPush([FromForm] string projectId, [FromForm] string branchName)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized("No logged-in users found.");

            var currentUser = await _userService.GetUserByEmail(userEmail);
            if (currentUser == null) return NotFound("The user was not found in the database.");

            try
            {
                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);
                if (project == null) return NotFound("Project not found.");

                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                var projectPath = Path.Combine(clonesRoot, project.Name);

                if (!Repository.IsValid(projectPath))
                {
                    Repository.Init(projectPath);
                }

                using (var repo = new Repository(projectPath))
                {
                    // Algoritma: Mevcut branch'i BaseBranch (Origin) olarak kaydet
                    string baseBranchName = repo.Head.FriendlyName;
                    Signature author = new Signature(currentUser.FullName, currentUser.Email, DateTimeOffset.Now);

                    // Repo boşsa veya ilk commit atılıyorsa
                    if (repo.Info.IsHeadDetached || repo.Head.Tip == null)
                    {
                        Commands.Stage(repo, "*");
                        repo.Commit("Gheetah IDE: Initial commit", author, author);
                        baseBranchName = repo.Head.FriendlyName;
                    }

                    // Target branch kontrolü ve yaratımı
                    Branch targetBranch = repo.Branches[branchName];
                    if (targetBranch == null)
                    {
                        targetBranch = repo.CreateBranch(branchName);
                    }

                    // Branch'e geçiş yap ve değişiklikleri commit et
                    Commands.Checkout(repo, targetBranch);
                    Commands.Stage(repo, "*");
                    
                    var commit = repo.Commit($"Gheetah Push: {branchName}", author, author);

                    // PR yerine sadece Push History kaydı oluşturuyoruz
                    await AddToPushHistory(projectId, branchName, baseBranchName, commit.Id.Sha, currentUser);

                    return Ok(new
                    {
                        success = true,
                        message = $"Successfully pushed to '{branchName}'. History recorded.",
                        currentBranch = branchName,
                        commitHash = commit.Id.Sha.Substring(0, 7)
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during the Git operation: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> PullLatest([FromForm] string projectId)
        {
            var userEmail = User.Identity?.Name; 
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized("No logged-in users found.");
                
            var currentUser = await _userService.GetUserByEmail(userEmail);
            if (currentUser == null) return NotFound("The user was not found in the database.");
            try
            {
                var projects = await _projectService.GetProjectsAsync(); //
                var project = projects.FirstOrDefault(p => p.Id == projectId); //
        
                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json"); //
                var projectPath = Path.Combine(clonesRoot, project.Name); //

                using (var repo = new Repository(projectPath)) //
                {
                    string logMessage = "";
                    var remote = repo.Network.Remotes["origin"]; //
            
                    if (remote != null)
                    {
                        Commands.Fetch(repo, remote.Name, new string[0], null, null); //
                        logMessage = "Fetched from remote. ";
                    }

                    var upstreamBranch = repo.Head.TrackedBranch; //
                    if (upstreamBranch != null)
                    {
                        var result = repo.Merge(upstreamBranch, new Signature(currentUser.FullName, currentUser.Email, DateTimeOffset.Now)); //
                
                        if (result.Status == MergeStatus.Conflicts) //
                        {
                            return Conflict("Merge conflicts detected! Please resolve them manually.");
                        }
                        logMessage += $"Merge status: {result.Status}";
                    }
                    else
                    {
                        logMessage += "No tracked upstream branch found. Local branch is up to date.";
                    }

                    return Ok(new { success = true, message = logMessage });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Pull failed: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetModifiedFiles(string projectId)
        {
            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return NotFound();

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);

            if (!Repository.IsValid(projectPath)) return Json(new List<object>());

            using (var repo = new Repository(projectPath))
            {
                var status = repo.RetrieveStatus(new StatusOptions());
                
                var modifiedFiles = status
                    .Where(x => x.State != FileStatus.Unaltered && x.State != FileStatus.Ignored)
                    .Select(x => new
                    {
                        id = Path.Combine(projectPath, x.FilePath),
                        text = x.FilePath,
                        type = "file",
                        icon = "ti ti-file-diff diff-node-item"
                    }).ToList();

                return Json(modifiedFiles);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFileDiff(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return BadRequest("Path is empty");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            if (!filePath.StartsWith(clonesRoot)) return Forbid();

            var modifiedContent = await System.IO.File.ReadAllTextAsync(filePath);
            var originalContent = "";

            var projectFolder = FindGitRoot(filePath);

            if (!string.IsNullOrEmpty(projectFolder) && Repository.IsValid(projectFolder))
            {
                using (var repo = new Repository(projectFolder))
                {
                    string relativePath = Path.GetRelativePath(projectFolder, filePath).Replace("\\", "/");

                    var blob = repo.Head.Tip[relativePath]?.Target as Blob;
                    if (blob != null)
                    {
                        using (var content = new StreamReader(blob.GetContentStream(), Encoding.UTF8))
                        {
                            originalContent = content.ReadToEnd();
                        }
                    }
                }
            }

            return Json(new
            {
                originalContent = originalContent,
                modifiedContent = modifiedContent
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProjectBranches(string projectId)
        {
            try
            {
                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);
        
                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                var projectPath = Path.Combine(clonesRoot, project.Name);

                if (!Directory.Exists(projectPath) || !Repository.IsValid(projectPath))
                {
                    return Json(new { branches = new List<string> { "main" }, current = "main" });
                }

                using (var repo = new Repository(projectPath))
                {
                    var branches = repo.Branches.Where(b => !b.IsRemote).Select(b => b.FriendlyName).ToList();
                    return Json(new { branches = branches, current = repo.Head.FriendlyName });
                }
            }
            catch
            {
                return Json(new { branches = new List<string> { "main" }, current = "main" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPushHistory(string projectId)
        {
            try
            {
                var history = await _fileService.LoadConfigAsync<List<PushHistory>>("internal-push-history.json") ?? new List<PushHistory>();
                var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new List<InternalPR>();

                var projectHistory = history
                    .Where(h => h.ProjectId == projectId)
                    .OrderByDescending(h => h.PushedAt)
                    .Select(h => {
                        // Bu push kaydına ait bir PR var mı bulalım
                        var relatedPr = allPrs.FirstOrDefault(p => 
                            p.ProjectId == h.ProjectId && 
                            p.SourceBranch == h.BranchName && 
                            p.LastCommitHash == h.CommitHash);

                        return new {
                            h.ProjectId,
                            h.BranchName,
                            h.PushedBy,
                            h.PushedAt,
                            h.CommitHash,
                            h.HasPR,
                            prId = relatedPr?.PR_Id,       // JS tarafı için
                            prStatus = relatedPr?.Status   // JS tarafı için
                        };
                    })
                    .ToList();

                return Json(projectHistory);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Could not retrieve push history: {ex.Message}");
            }
        }

        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> CreatePRWithDetails([FromBody] InternalPR prRequest)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var currentUser = await _userService.GetUserByEmail(userEmail);
            if (currentUser == null) return NotFound("User not found!");

            if (string.IsNullOrEmpty(prRequest.TargetBranch) || string.IsNullOrEmpty(prRequest.Description))
            {
                return BadRequest("Target branch and description are required for a valid Pull Request.");
            }

            try
            {
                prRequest.PR_Id = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper(); 
                prRequest.CreatedBy = currentUser.FullName;
                prRequest.CreatedByEmail = currentUser.Email;
                prRequest.CreatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                prRequest.Status = "Open";

                await CreateInternalPullRequest(prRequest);

                var historyPath = "internal-push-history.json";
                var history = await _fileService.LoadConfigAsync<List<PushHistory>>(historyPath);
                
                if (history != null)
                {
                    var targetPush = history.FirstOrDefault(h => 
                        h.ProjectId == prRequest.ProjectId && 
                        h.BranchName == prRequest.SourceBranch && 
                        h.CommitHash == prRequest.LastCommitHash);

                    if (targetPush != null)
                    {
                        targetPush.HasPR = true;
                        await _fileService.SaveConfigAsync(historyPath, history);
                    }
                }

                return Ok(new { 
                    success = true, 
                    message = $"Pull Request #{prRequest.PR_Id} has been successfully created." 
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReviewers()
        {
            var currentUserEmail = User.Identity?.Name;

            var users = await _userService.GetAdminsAndLeadUsers();
    
            var result = users
                .Where(u => !u.Email.Equals(currentUserEmail, StringComparison.OrdinalIgnoreCase))
                .Select(u => new {
                    val = u.Email,
                    text = $"{u.FullName} ({string.Join(", ", u.Roles.Where(r => r == "Admin" || r == "Lead"))})"
                });

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetFilesBetweenBranches(string projectId, string source, string target)
        {
            try
            {
                var prPath = "internal-pull-requests.json";
                var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>(prPath);
                var isMerged = allPrs?.Any(p => p.ProjectId == projectId && p.SourceBranch == source && p.Status == "Merged") ?? false;

                if (isMerged) return Json(new List<string>());

                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);
                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                var projectPath = Path.Combine(clonesRoot, project.Name);

                using (var repo = new Repository(projectPath))
                {
                    var sourceBranch = repo.Branches[source];
                    var targetBranch = repo.Branches[target] ?? repo.Branches["main"];

                    if (sourceBranch == null) return NotFound("Source branch not found");

                    var changes = repo.Diff.Compare<TreeChanges>(targetBranch.Tip.Tree, sourceBranch.Tip.Tree);
                    var changedFiles = changes.Select(c => c.Path).ToList();
            
                    return Json(changedFiles);
                }
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet]
        public async Task<IActionResult> GetFileDiffBetweenBranches(string projectId, string filePath, string source, string target)
        {
            try
            {
                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);
                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                var projectPath = Path.Combine(clonesRoot, project.Name);

                using (var repo = new Repository(projectPath))
                {
                    var sourceBranch = repo.Branches[source];
                    var targetBranch = repo.Branches[target] ?? repo.Branches["main"];

                    string gitPath = filePath.Replace("\\", "/");

                    var oldBlob = targetBranch.Tip.Tree[gitPath]?.Target as Blob;
                    var originalContent = oldBlob != null ? oldBlob.GetContentText() : "";

                    var newBlob = sourceBranch.Tip.Tree[gitPath]?.Target as Blob;
                    var modifiedContent = newBlob != null ? newBlob.GetContentText() : "";

                    return Json(new { originalContent, modifiedContent });
                }
            }
            catch (Exception ex) { return StatusCode(500, ex.Message); }
        }

        [HttpGet]
        public async Task<IActionResult> PRDetails(string id)
        {
            var prPath = "internal-pull-requests.json";
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>(prPath) ?? new List<InternalPR>();
            var pr = allPrs.FirstOrDefault(x => x.PR_Id == id);

            if (pr == null) return NotFound();

            var userEmail = User.Identity?.Name;
            ViewBag.IsReviewer = pr.Reviewers.Any(r => r.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
            
            return View(pr);
        }

        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> UpdatePRStatus(string prId, string status)
        {
            var prPath = "internal-pull-requests.json";
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>(prPath);
            var pr = allPrs?.FirstOrDefault(x => x.PR_Id == prId);

            if (pr == null) return NotFound();

            pr.Status = status;
            await _fileService.SaveConfigAsync(prPath, allPrs);

            return Ok(new { success = true, newStatus = status });
        }

        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> CompletePR(string prId)
        {
            var prPath = "internal-pull-requests.json";
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>(prPath);
            var pr = allPrs?.FirstOrDefault(x => x.PR_Id == prId);

            if (pr == null) return NotFound("Pull Request not found.");

            bool hasReviewers = pr.Reviewers != null && pr.Reviewers.Any();
            bool isApproved = pr.Status == "Approved";
            bool isOpen = pr.Status == "Open";

            if (hasReviewers && !isApproved) 
            {
                return BadRequest("Only approved PRs can be merged when reviewers are assigned.");
            }
            
            if (!hasReviewers && !isOpen && !isApproved)
            {
                return BadRequest("PR must be in Open or Approved state to merge.");
            }

            try
            {
                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                var projectPath = Path.Combine(clonesRoot, project.Name);

                using (var repo = new Repository(projectPath))
                {
                    var sourceBranch = repo.Branches[pr.SourceBranch];
                    var targetBranch = repo.Branches[pr.TargetBranch];

                    if (sourceBranch == null || targetBranch == null)
                        return BadRequest("Source or Target branch no longer exists in the repository.");

                    Commands.Checkout(repo, targetBranch);
                    
                    var signature = new Signature(User.Identity.Name, User.Identity.Name, DateTimeOffset.Now);
                    var mergeResult = repo.Merge(sourceBranch, signature);

                    if (mergeResult.Status == MergeStatus.Conflicts)
                        return Conflict("Merge conflicts detected. Please resolve manually in the IDE.");

                    pr.Status = "Merged";
                    await _fileService.SaveConfigAsync(prPath, allPrs);

                    repo.Branches.Remove(sourceBranch);

                    return Ok(new { success = true, message = $"PR #{pr.PR_Id} merged successfully and branch {pr.SourceBranch} deleted." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Git Merge Error: {ex.Message}");
            }
        }

        private string FindGitRoot(string filePath)
        {
            var directory = new DirectoryInfo(Path.GetDirectoryName(filePath));
            while (directory != null)
            {
                if (directory.GetDirectories(".git").Any())
                    return directory.FullName;
                directory = directory.Parent;
            }
            return null;
        }

        private async Task AddToPushHistory(string projectId, string branchName, string originBranch, string commitHash, User user)
        {
            var history = await _fileService.LoadConfigAsync<List<PushHistory>>("internal-push-history.json") ?? new List<PushHistory>();

            history.Add(new PushHistory
            {
                ProjectId = projectId,
                BranchName = branchName,
                OriginBranch = originBranch,
                CommitHash = commitHash,
                PushedBy = user.FullName,
                PushedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                HasPR = false
            });

            await _fileService.SaveConfigAsync("internal-push-history.json", history);
        }

        private async Task CreateInternalPullRequest(InternalPR newPr)
        {
            var prPath = "internal-pull-requests.json";
    
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>(prPath) ?? new List<InternalPR>();

            allPrs.Add(newPr);

            await _fileService.SaveConfigAsync(prPath, allPrs);
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