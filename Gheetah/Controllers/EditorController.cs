using Gheetah.Interfaces;
using Gheetah.Models;
using Gheetah.Models.EditorModel;
using LibGit2Sharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Gheetah.Controllers
{
    public class EditorController : Controller
    {
        private readonly IProjectService _projectService;
        private readonly IFileService _fileService;
        private readonly IUserService _userService;

        private static readonly ConcurrentDictionary<string, MergeBuildProgress> _mergeBuildProgress = new();

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
                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);

                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                
                if (project != null)
                {
                    var projectPath = Path.Combine(clonesRoot, project.Name);

                    using (var repo = new Repository(projectPath))
                    {
                        string logMessage = "";
                        var remote = repo.Network.Remotes["origin"];
            
                        if (remote != null)
                        {
                            Commands.Fetch(repo, remote.Name, new string[0], null, null);
                            logMessage = "Fetched from remote. ";
                        }

                        var upstreamBranch = repo.Head.TrackedBranch;
                        if (upstreamBranch != null)
                        {
                            var result = repo.Merge(upstreamBranch, new Signature(currentUser.FullName, currentUser.Email, DateTimeOffset.Now));
                
                            if (result.Status == MergeStatus.Conflicts)
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

                return NotFound("Project could not be found.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Pull failed: {ex.Message}");
            }
            return BadRequest("An unexpected error occurred during the pull process.");
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
                prRequest.Title = $"{prRequest.SourceBranch} → {prRequest.TargetBranch}";

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

                try
                {
                    var projects = await _projectService.GetProjectsAsync();
                    var project = projects.FirstOrDefault(p => p.Id == prRequest.ProjectId);
                    if (project != null)
                    {
                        var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                        var projectPath = Path.Combine(clonesRoot, project.Name);
                        if (Repository.IsValid(projectPath))
                        {
                            using (var repo = new Repository(projectPath))
                            {
                                var sourceBranch = repo.Branches[prRequest.SourceBranch];
                                var targetBranch = repo.Branches[prRequest.TargetBranch] ?? repo.Branches["main"];
                                if (sourceBranch != null && targetBranch != null)
                                {
                                    var changes = repo.Diff.Compare<TreeChanges>(targetBranch.Tip.Tree, sourceBranch.Tip.Tree);
                                    var changedFiles = new List<ChangedFileSnapshot>();
                                    foreach (var change in changes)
                                    {
                                        string originalContent = "", modifiedContent = "";
                                        var oldBlob = targetBranch.Tip.Tree[change.Path]?.Target as Blob;
                                        if (oldBlob != null)
                                            originalContent = oldBlob.GetContentText();
                                        var newBlob = sourceBranch.Tip.Tree[change.Path]?.Target as Blob;
                                        if (newBlob != null)
                                            modifiedContent = newBlob.GetContentText();

                                        changedFiles.Add(new ChangedFileSnapshot
                                        {
                                            FilePath = change.Path,
                                            OriginalContent = originalContent,
                                            ModifiedContent = modifiedContent
                                        });
                                    }

                                    var snapshot = new PRSnapshot
                                    {
                                        PR_Id = prRequest.PR_Id,
                                        ProjectId = prRequest.ProjectId,
                                        SourceBranch = prRequest.SourceBranch,
                                        TargetBranch = prRequest.TargetBranch,
                                        LastCommitHash = prRequest.LastCommitHash,
                                        ChangedFiles = changedFiles,
                                        CapturedAt = DateTime.UtcNow
                                    };
                                    await SavePRSnapshot(snapshot);
                                }
                            }
                        }
                    }
                }
                catch (Exception snapshotEx)
                {
                    Console.WriteLine($"Snapshot not taken: {snapshotEx.Message}");
                }

                await AddActivity(prRequest.PR_Id, "Created", currentUser.Email, $"Pull Request created from '{prRequest.SourceBranch}' to '{prRequest.TargetBranch}'");

                return Ok(new
                {
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
        public async Task<IActionResult> GetFilesBetweenBranches(string projectId, string source, string target, string prId = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(prId))
                {
                    var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new List<InternalPR>();
                    var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
                    if (pr != null && pr.Status != "Open")
                    {
                        var snapshot = await GetPRSnapshot(prId);
                        if (snapshot != null)
                        {
                            return Json(snapshot.ChangedFiles.Select(c => c.FilePath).ToList());
                        }
                    }
                }

                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);
                if (project == null) return NotFound("Project not found");

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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFileDiffBetweenBranches(string projectId, string filePath, string source, string target, string prId = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(prId))
                {
                    var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new List<InternalPR>();
                    var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
                    if (pr != null && pr.Status != "Open")
                    {
                        var snapshot = await GetPRSnapshot(prId);
                        if (snapshot != null)
                        {
                            var normalizedPath = filePath.Replace("\\", "/");
                            var fileSnapshot = snapshot.ChangedFiles.FirstOrDefault(f => f.FilePath.Replace("\\", "/") == normalizedPath);
                            if (fileSnapshot != null)
                            {
                                return Json(new { originalContent = fileSnapshot.OriginalContent, modifiedContent = fileSnapshot.ModifiedContent });
                            }
                        }
                    }
                }

                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == projectId);
                if (project == null) return NotFound();

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
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PRDetails(string id)
        {
            var prPath = "internal-pull-requests.json";
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>(prPath) ?? new List<InternalPR>();
            var pr = allPrs.FirstOrDefault(x => x.PR_Id == id);

            if (pr == null) return NotFound();

            var userEmail = User.Identity?.Name;

            var commentsPath = "internal-pr-comments.json";
            var allComments = await _fileService.LoadConfigAsync<List<PRComment>>(commentsPath) ?? new List<PRComment>();
            pr.HasActiveComments = allComments.Any(c => c.PR_Id == id && c.Status == CommentStatus.Active);

            ViewBag.IsReviewer = pr.Reviewers.Any(r => r.Equals(userEmail, StringComparison.OrdinalIgnoreCase));
            ViewBag.IsCreator = pr.CreatedByEmail.Equals(userEmail, StringComparison.OrdinalIgnoreCase);
    
            return View(pr);
        }

        [HttpPost]
        public async Task<IActionResult> ReactivateComment(string commentId)
        {
            var path = "internal-pr-comments.json";
            var allComments = await _fileService.LoadConfigAsync<List<PRComment>>(path);
            if (allComments == null) return NotFound();

            var mainComment = allComments.FirstOrDefault(c => c.Id == commentId);
            if (mainComment != null) {
                mainComment.Status = CommentStatus.Active;

                var replies = allComments.Where(c => c.ParentId == commentId).ToList();
                foreach (var reply in replies)
                {
                    reply.Status = CommentStatus.Active;
                }

                await _fileService.SaveConfigAsync(path, allComments);
                return Ok(new { success = true });
            }
            return NotFound();
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

            var userEmail = User.Identity?.Name;
            await AddActivity(prId, status, userEmail, $"PR status changed to '{status}'");

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

            var comments = await _fileService.LoadConfigAsync<List<PRComment>>("internal-pr-comments.json");
            bool hasActiveComments = comments?.Any(c => c.PR_Id == prId && c.Status == CommentStatus.Active) ?? false;

            if (hasActiveComments)
            {
                return BadRequest("All comments must be 'Resolved' before merging.");
            }

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

                    var userEmail = User.Identity?.Name;
                    await AddActivity(prId, "Merged", userEmail, $"PR merged into '{pr.TargetBranch}' and branch '{pr.SourceBranch}' deleted.");

                    repo.Branches.Remove(sourceBranch);

                    return Ok(new { success = true, message = $"PR #{pr.PR_Id} merged successfully and branch {pr.SourceBranch} deleted." });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Git Merge Error: {ex.Message}");
            }
        }

        [HttpPost]
        public async Task<IActionResult> AddPRComment([FromBody] PRComment comment)
        {
            var userEmail = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var commentsPath = "internal-pr-comments.json";
            var allComments = await _fileService.LoadConfigAsync<List<PRComment>>(commentsPath) ?? new List<PRComment>();
    
            comment.Author = userEmail;
            allComments.Add(comment);
    
            await _fileService.SaveConfigAsync(commentsPath, allComments);
            await AddActivity(comment.PR_Id, "Commented", userEmail, $"Commented on {comment.FilePath} line {comment.LineNumber}");
            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetPRComments(string prId, string filePath)
        {
            var commentsPath = "internal-pr-comments.json";
            var allComments = await _fileService.LoadConfigAsync<List<PRComment>>(commentsPath) ?? new List<PRComment>();
    
            var filtered = allComments.Where(c => c.PR_Id == prId && c.FilePath == filePath).ToList();
            return Json(filtered);
        }

        [HttpPost]
        public async Task<IActionResult> ResolveComment(string commentId)
        {
            var path = "internal-pr-comments.json";
            var allComments = await _fileService.LoadConfigAsync<List<PRComment>>(path);
            if (allComments == null) return NotFound();

            var mainComment = allComments.FirstOrDefault(c => c.Id == commentId);
            if (mainComment != null) 
            {
                mainComment.Status = CommentStatus.Resolved;

                var replies = allComments.Where(c => c.ParentId == commentId).ToList();
                foreach (var reply in replies)
                {
                    reply.Status = CommentStatus.Resolved;
                }

                await _fileService.SaveConfigAsync(path, allComments);
            }
            return Ok();
        }

        [HttpGet]
        public async Task<IActionResult> GetPRUpdates(string prId)
        {
            var snapshot = await GetPRSnapshot(prId);
            if (snapshot == null) return NotFound();
    
            return Json(snapshot.ChangedFiles.Select(f => new { f.FilePath }));
        }

        [HttpGet]
        public async Task<IActionResult> GetPRActivities(string prId)
        {
            var activities = await _fileService.LoadConfigAsync<List<PRActivity>>("internal-pr-activities.json");
            var filtered = activities?.Where(a => a.PR_Id == prId).OrderBy(a => a.Timestamp).ToList() ?? new List<PRActivity>();
            return Json(filtered);
        }

        [HttpPost]
        public async Task<IActionResult> StartMergeAndBuild(string prId)
        {
            var userEmail = User.Identity?.Name;
            var userName = User.Identity?.Name;
            if (string.IsNullOrEmpty(userEmail)) return Unauthorized();

            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
            var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
            if (pr == null) return NotFound("PR not found");

            if (pr.CreatedByEmail != userEmail && !User.IsInRole("Admin") && !User.IsInRole("Lead"))
                return Forbid();

            if (_mergeBuildProgress.TryGetValue(prId, out var existing) && !existing.IsCompleted && !existing.HasError)
                return BadRequest("Merge/build already in progress.");

            var oldResult = await GetMergeBuildResult(prId);
            if (oldResult != null)
            {
                var results = await _fileService.LoadConfigAsync<List<MergeBuildProgress>>("internal-merge-build-results.json");
                if (results != null)
                {
                    results.RemoveAll(r => r.PrId == prId);
                    await _fileService.SaveConfigAsync("internal-merge-build-results.json", results);
                }
            }

            var progress = new MergeBuildProgress { PrId = prId };
            _mergeBuildProgress[prId] = progress;

            _ = Task.Run(() => ProcessMergeAndBuildAsync(prId, progress, userEmail, userName));

            return Ok(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetMergeBuildStatus(string prId)
        {
            if (_mergeBuildProgress.TryGetValue(prId, out var progress))
                return Ok(progress);

            var saved = await GetMergeBuildResult(prId);
            if (saved != null)
                return Ok(saved);

            // Hiç ilerleme yoksa boş bir durum dön
            return Ok(new MergeBuildProgress
            {
                PrId = prId,
                SourceBuildStatus = "pending",
                MergeStatus = "pending",
                TargetBuildStatus = "pending"
            });
        }

        [HttpPost]
        public async Task<IActionResult> RetryBuild(string prId)
        {
            var userEmail = User.Identity?.Name;
            var userName = User.Identity?.Name;
            if (!_mergeBuildProgress.TryGetValue(prId, out var progress))
                return NotFound();

            if (progress.MergeStatus != "success")
                return BadRequest("Cannot retry build because merge failed. Resolve conflicts first.");

            if (progress.TargetBuildStatus == "running")
                return BadRequest("Build already running.");

            progress.TargetBuildStatus = "pending";
            progress.TargetBuildMessage = null;
            progress.TargetBuildStartTime = null;
            progress.TargetBuildEndTime = null;
            _ = Task.Run(() => BuildTargetBranchOnlyAsync(prId, progress, userEmail, userName));
            return Ok(new { success = true });
        }

        /// <summary>
        /// Switches the Git repository to the specified branch.
        /// Requires no uncommitted changes.
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> SwitchBranch([FromForm] string projectId, [FromForm] string branchName)
        {
            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return NotFound("Project not found");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);

            if (!Repository.IsValid(projectPath))
                return BadRequest("Repository not valid.");

            using (var repo = new Repository(projectPath))
            {
                var branch = repo.Branches[branchName];
                if (branch == null) return NotFound("Branch not found");

                // Prevent switching if the working directory has uncommitted changes
                if (repo.RetrieveStatus().IsDirty)
                    return Conflict("You have uncommitted changes. Commit or discard them before switching branches.");

                Commands.Checkout(repo, branch);
            }

            return Ok(new { success = true, currentBranch = branchName });
        }

        // -------------------- Conflict Resolution --------------------

        /// <summary>
        /// Returns the list of conflicted file paths (relative to repo root) for the given PR.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConflictedFiles(string prId)
        {
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
            var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
            if (pr == null) return NotFound("PR not found");

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
            if (project == null) return NotFound("Project not found");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);

            if (!Repository.IsValid(projectPath)) return Json(new List<string>());

            using (var repo = new Repository(projectPath))
            {
                if (repo.Info.CurrentOperation != CurrentOperation.Merge)
                    return Json(new List<string>());  // no merge in progress → no conflicts

                var conflictedPaths = repo.Index.Conflicts
                    .Select(c => c.Ours?.Path ?? c.Theirs?.Path ?? c.Ancestor?.Path)
                    .Distinct()
                    .ToList();
                return Json(conflictedPaths);
            }
        }

        /// <summary>
        /// Returns the content of a conflicted file from the target branch (Ours) and source branch (Theirs).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetConflictContent(string prId, string filePath)
        {
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
            var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
            if (pr == null) return NotFound("PR not found");

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
            if (project == null) return NotFound("Project not found");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);

            using (var repo = new Repository(projectPath))
            {
                var targetBranch = repo.Branches[pr.TargetBranch] ?? repo.Branches["main"];
                var sourceBranch = repo.Branches[pr.SourceBranch];
                if (sourceBranch == null || targetBranch == null)
                    return NotFound("Branch not found");

                string relativePath = filePath.Replace("\\", "/");
                string oursContent = "";
                string theirsContent = "";

                var oursBlob = targetBranch.Tip[relativePath]?.Target as Blob;
                if (oursBlob != null) oursContent = oursBlob.GetContentText();

                var theirsBlob = sourceBranch.Tip[relativePath]?.Target as Blob;
                if (theirsBlob != null) theirsContent = theirsBlob.GetContentText();

                return Json(new { oursContent, theirsContent });
            }
        }

        /// <summary>
        /// Resolves a single conflicted file by accepting either "ours" (target branch) or "theirs" (source branch).
        /// </summary>
        [HttpPost]
        [Authorize(Policy = "Dynamic_admin-perm,Dynamic_lead-perm")]
        public async Task<IActionResult> ResolveConflict(string prId, string filePath, string resolution)
        {
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
            var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
            if (pr == null) return NotFound("PR not found");

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
            if (project == null) return NotFound("Project not found");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);

            using (var repo = new Repository(projectPath))
            {
                if (repo.Info.CurrentOperation != CurrentOperation.Merge)
                    return BadRequest("No merge in progress");

                var targetBranch = repo.Branches[pr.TargetBranch] ?? repo.Branches["main"];
                var sourceBranch = repo.Branches[pr.SourceBranch];
                if (sourceBranch == null || targetBranch == null)
                    return NotFound("Branch not found");

                string relativePath = filePath.Replace("\\", "/");
                string chosenContent = "";

                if (resolution.Equals("ours", StringComparison.OrdinalIgnoreCase))
                {
                    var blob = targetBranch.Tip[relativePath]?.Target as Blob;
                    chosenContent = blob?.GetContentText() ?? "";
                }
                else if (resolution.Equals("theirs", StringComparison.OrdinalIgnoreCase))
                {
                    var blob = sourceBranch.Tip[relativePath]?.Target as Blob;
                    chosenContent = blob?.GetContentText() ?? "";
                }
                else
                {
                    return BadRequest("Resolution must be 'ours' or 'theirs'");
                }

                // Write chosen content to working directory
                var fullPath = Path.Combine(projectPath, relativePath);
                await System.IO.File.WriteAllTextAsync(fullPath, chosenContent);

                // Stage the file (this resolves the conflict in the index)
                Commands.Stage(repo, relativePath);

                // ✅ Eğer tüm conflict'ler çözüldüyse merge commit oluştur
                if (!repo.Index.Conflicts.Any())
                {
                    var userEmail = User.Identity?.Name ?? "system@gheetah.com";
                    var userName = User.Identity?.Name ?? "System";
                    var author = new Signature(userName, userEmail, DateTimeOffset.Now);
                    
                    try
                    {
                        repo.Commit(
                            $"Merge branch '{pr.SourceBranch}' into {pr.TargetBranch} - conflicts resolved",
                            author,
                            author);

                        // Branch'i sil
                        if (sourceBranch != null)
                        {
                            repo.Branches.Remove(sourceBranch);
                        }

                        // PR'ı "ConflictsResolved" ara durumuna al
                        pr.Status = "ConflictsResolved";
                        await _fileService.SaveConfigAsync("internal-pull-requests.json", allPrs);
                        await AddActivity(prId, "ConflictsResolved", userEmail,
                            $"Conflicts resolved for PR. Merge completed. Branch '{pr.SourceBranch}' deleted.");

                        // ✅ Merge/build progress'i güncelle: merge başarılı
                        if (_mergeBuildProgress.TryGetValue(prId, out var progress))
                        {
                            progress.MergeStatus = "success";
                            progress.MergeMessage = "Merge completed after conflict resolution";
                            progress.MergeEndTime = DateTime.UtcNow;
                        }

                        // ✅ Arka planda target build'i başlat
                        var targetBuildProgress = _mergeBuildProgress.TryGetValue(prId, out var existingProgress) 
                            ? existingProgress 
                            : new MergeBuildProgress { PrId = prId };
                        
                        targetBuildProgress.TargetBuildStatus = "running";
                        targetBuildProgress.TargetBuildStartTime = DateTime.UtcNow;
                        _mergeBuildProgress[prId] = targetBuildProgress;

                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                var buildOk = await BuildBranchAsync(prId, userEmail, userName, isSource: false);
                                targetBuildProgress.TargetBuildEndTime = DateTime.UtcNow;
                                
                                if (buildOk)
                                {
                                    targetBuildProgress.TargetBuildStatus = "success";
                                    targetBuildProgress.TargetBuildMessage = "Target branch build succeeded after conflict resolution";
                                    
                                    // PR'ı Merged yap
                                    var freshPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
                                    var freshPr = freshPrs.FirstOrDefault(p => p.PR_Id == prId);
                                    if (freshPr != null)
                                    {
                                        freshPr.Status = "Merged";
                                        await _fileService.SaveConfigAsync("internal-pull-requests.json", freshPrs);
                                        await AddActivity(prId, "Merged", userEmail, "PR merge completed with target build success.");
                                    }
                                }
                                else
                                {
                                    targetBuildProgress.TargetBuildStatus = "failed";
                                    targetBuildProgress.TargetBuildMessage = "Target branch build failed after conflict resolution";
                                    
                                    // PR'ı BuildFailed yap
                                    var freshPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
                                    var freshPr = freshPrs.FirstOrDefault(p => p.PR_Id == prId);
                                    if (freshPr != null)
                                    {
                                        freshPr.Status = "BuildFailed";
                                        await _fileService.SaveConfigAsync("internal-pull-requests.json", freshPrs);
                                        await AddActivity(prId, "BuildFailed", userEmail, "Target build failed after conflict resolution.");
                                    }
                                }
                                
                                await SaveMergeBuildResult(targetBuildProgress);
                            }
                            catch (Exception ex)
                            {
                                targetBuildProgress.TargetBuildStatus = "failed";
                                targetBuildProgress.TargetBuildMessage = $"Target build error: {ex.Message}";
                                await SaveMergeBuildResult(targetBuildProgress);
                            }
                        });

                        return Ok(new { success = true, allResolved = true, message = "Conflicts resolved. Target build started in background." });
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Failed to finalize merge: {ex.Message}");
                    }
                }

                return Ok(new { success = true, allResolved = false });
            }
        }

        private async Task BuildTargetBranchOnlyAsync(string prId, MergeBuildProgress progress, string userEmail, string userName)
        {
            progress.TargetBuildStatus = "running";
            progress.TargetBuildStartTime = DateTime.UtcNow;
            var ok = await BuildBranchAsync(prId, userEmail, userName, isSource: false);
            progress.TargetBuildEndTime = DateTime.UtcNow;
            if (ok)
            {
                progress.TargetBuildStatus = "success";
                progress.TargetBuildMessage = "Target branch build succeeded";
            }
            else
            {
                progress.TargetBuildStatus = "failed";
                progress.TargetBuildMessage = "Target branch build failed";
            }
            await SaveMergeBuildResult(progress);
        }

        private async Task ProcessMergeAndBuildAsync(string prId, MergeBuildProgress progress, string userEmail, string userName)
        {
            // projectPath'i metodun başında hesaplayalım
            string projectPath = null;
            try
            {
                var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
                var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
                if (pr == null) return;

                var projects = await _projectService.GetProjectsAsync();
                var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
                if (project == null) return;

                var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
                projectPath = Path.Combine(clonesRoot, project.Name);
            }
            catch
            {
                // Proje yolu alınamazsa devam edemeyiz
                return;
            }

            try
            {
                // 1️⃣ SOURCE BUILD
                progress.SourceBuildStatus = "running";
                progress.SourceBuildStartTime = DateTime.UtcNow;
                var sourceBuildOk = await BuildBranchAsync(prId, userEmail, userName, isSource: true);
                progress.SourceBuildEndTime = DateTime.UtcNow;
                
                if (sourceBuildOk)
                {
                    progress.SourceBuildStatus = "success";
                    progress.SourceBuildMessage = "Source branch build succeeded";

                    // ✅ Build sonrası çalışma dizinini temizle
                    try
                    {
                        CleanRepository(projectPath);
                    }
                    catch
                    {
                        // Temizlik başarısız olsa bile merge'i dene
                    }
                }
                else
                {
                    progress.SourceBuildStatus = "failed";
                    progress.SourceBuildMessage = "Source branch build failed";
                    await SaveMergeBuildResult(progress);
                    return;
                }

                // 2️⃣ MERGE
                progress.MergeStatus = "running";
                progress.MergeStartTime = DateTime.UtcNow;
                var mergeResult = await PerformMergeAsync(prId, userEmail, userName);
                progress.MergeEndTime = DateTime.UtcNow;
                
                if (mergeResult.Success)
                {
                    progress.MergeStatus = "success";
                    progress.MergeMessage = mergeResult.Message;
                }
                else
                {
                    progress.MergeStatus = "failed";
                    progress.MergeMessage = mergeResult.Error;
                    await SaveMergeBuildResult(progress);
                    return;
                }

                // 3️⃣ TARGET BUILD
                progress.TargetBuildStatus = "running";
                progress.TargetBuildStartTime = DateTime.UtcNow;
                var targetBuildOk = await BuildBranchAsync(prId, userEmail, userName, isSource: false);
                progress.TargetBuildEndTime = DateTime.UtcNow;
                
                if (targetBuildOk)
                {
                    progress.TargetBuildStatus = "success";
                    progress.TargetBuildMessage = "Target branch build succeeded";
                }
                else
                {
                    progress.TargetBuildStatus = "failed";
                    progress.TargetBuildMessage = "Target branch build failed";
                    await SaveMergeBuildResult(progress);
                    return;
                }

                await SaveMergeBuildResult(progress);
            }
            catch (Exception ex)
            {
                if (progress.SourceBuildStatus == "running") progress.SourceBuildStatus = "failed";
                else if (progress.MergeStatus == "running") progress.MergeStatus = "failed";
                else progress.TargetBuildStatus = "failed";
                progress.TargetBuildMessage = ex.Message;
                await SaveMergeBuildResult(progress);
            }
        }

        private async Task<bool> BuildBranchAsync(string prId, string userEmail, string userName, bool isSource)
        {
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
            var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
            if (pr == null) return false;

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
            if (project == null) return false;

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);
            if (!Directory.Exists(projectPath)) return false;

            if (isSource)
            {
                using var repo = new Repository(projectPath);
                var branch = repo.Branches[pr.SourceBranch];
                if (branch == null) return false;
                Commands.Checkout(repo, branch);
            }

            var buildResult = await _projectService.BuildProjectAsync(project.Id, project.LanguageType);

            // --- Hata/başarı mesajını progress'e yazmak için progress nesnesini alalım ---
            if (_mergeBuildProgress.TryGetValue(prId, out var progress))
            {
                if (isSource)
                {
                    progress.SourceBuildMessage = buildResult.IsSuccess
                        ? "Source branch build succeeded"
                        : $"Source branch build failed: {buildResult.Message}";
                }
                else
                {
                    progress.TargetBuildMessage = buildResult.IsSuccess
                        ? "Target branch build succeeded"
                        : $"Target branch build failed: {buildResult.Message}";
                }
            }

            return buildResult.IsSuccess;
        }

        private async Task<(bool Success, string Message, string Error)> PerformMergeAsync(string prId, string userEmail, string userName)
        {
            var allPrs = await _fileService.LoadConfigAsync<List<InternalPR>>("internal-pull-requests.json") ?? new();
            var pr = allPrs.FirstOrDefault(p => p.PR_Id == prId);
            if (pr == null) return (false, null, "PR not found");

            var projects = await _projectService.GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == pr.ProjectId);
            if (project == null) return (false, null, "Project not found");

            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(clonesRoot, project.Name);

            if (!Repository.IsValid(projectPath))
                return (false, null, "Repository not valid");

            try
            {
                using var repo = new Repository(projectPath);
                var sourceBranch = repo.Branches[pr.SourceBranch];
                var targetBranch = repo.Branches[pr.TargetBranch] ?? repo.Branches["main"];
                if (sourceBranch == null || targetBranch == null)
                    return (false, null, "Source or target branch not found");

                // Merge zaten devam ediyorsa (conflict sonrası)
                if (repo.Info.CurrentOperation == CurrentOperation.Merge)
                {
                    if (repo.Index.Conflicts.Any())
                        return (false, null, "There are still unresolved conflicts. Resolve them first in the Conflicts tab.");

                    var author = new Signature(userName, userEmail, DateTimeOffset.Now);
                    repo.Commit($"Merge branch '{pr.SourceBranch}' into {pr.TargetBranch}", author, author);
                    repo.Branches.Remove(sourceBranch);
                    pr.Status = "Merged";
                    await _fileService.SaveConfigAsync("internal-pull-requests.json", allPrs);
                    await AddActivity(prId, "Merged", userEmail, $"PR merged into '{pr.TargetBranch}' and branch '{pr.SourceBranch}' deleted.");
                    return (true, "Merge completed successfully", null);
                }

                // Normal merge
                Commands.Checkout(repo, targetBranch);
                var signature = new Signature(userName, userEmail, DateTimeOffset.Now);
                var mergeResult = repo.Merge(sourceBranch, signature);

                if (mergeResult.Status == MergeStatus.Conflicts)
                    return (false, null, "Merge conflicts detected. Please resolve in Conflicts tab.");

                if (mergeResult.Status == MergeStatus.UpToDate)
                {
                    repo.Branches.Remove(sourceBranch);
                    pr.Status = "Merged";
                    await _fileService.SaveConfigAsync("internal-pull-requests.json", allPrs);
                    await AddActivity(prId, "Merged", userEmail, $"PR merged (already up-to-date). Branch '{pr.SourceBranch}' deleted.");
                    return (true, "Already up-to-date, merge not needed.", null);
                }

                // Başarılı merge
                repo.Branches.Remove(sourceBranch);
                pr.Status = "Merged";
                await _fileService.SaveConfigAsync("internal-pull-requests.json", allPrs);
                await AddActivity(prId, "Merged", userEmail, $"PR merged into '{pr.TargetBranch}' and branch '{pr.SourceBranch}' deleted.");
                return (true, "Merge completed successfully", null);
            }
            catch (Exception ex)
            {
                return (false, null, $"Merge failed: {ex.Message}");
            }
        }

        private void CleanRepository(string projectPath)
        {
            using (var repo = new Repository(projectPath))
            {
                // 1. Çalışma dizinini HEAD'e sıfırla (git reset --hard)
                repo.Reset(ResetMode.Hard);

                // 2. İzlenmeyen tüm dosyaları sil (git clean -fd karşılığı)
                var untrackedOptions = new StatusOptions
                {
                    IncludeUntracked = true,
                    IncludeIgnored = false
                };
                var untrackedFiles = repo.RetrieveStatus(untrackedOptions)
                    .Where(e => e.State == FileStatus.NewInWorkdir);

                foreach (var entry in untrackedFiles)
                {
                    var fullPath = Path.Combine(repo.Info.WorkingDirectory, entry.FilePath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                    }
                }
            }
        }

        private async Task SaveMergeBuildResult(MergeBuildProgress progress)
        {
            var path = "internal-merge-build-results.json";
            var results = await _fileService.LoadConfigAsync<List<MergeBuildProgress>>(path) ?? new List<MergeBuildProgress>();
            results.RemoveAll(r => r.PrId == progress.PrId);
            results.Add(progress);
            await _fileService.SaveConfigAsync(path, results);
        }

        private async Task<MergeBuildProgress> GetMergeBuildResult(string prId)
        {
            var results = await _fileService.LoadConfigAsync<List<MergeBuildProgress>>("internal-merge-build-results.json");
            return results?.FirstOrDefault(r => r.PrId == prId);
        }

        private async Task AddActivity(string prId, string actionType, string actorEmail, string details)
        {
            var path = "internal-pr-activities.json";
            var activities = await _fileService.LoadConfigAsync<List<PRActivity>>(path) ?? new List<PRActivity>();
            activities.Add(new PRActivity
            {
                PR_Id = prId,
                ActionType = actionType,
                Actor = actorEmail,
                Details = details,
                Timestamp = DateTime.UtcNow
            });
            await _fileService.SaveConfigAsync(path, activities);
        }

        private async Task<PRSnapshot?> GetPRSnapshot(string prId)
        {
            var snapshots = await _fileService.LoadConfigAsync<List<PRSnapshot>>("internal-pr-snapshots.json") 
                            ?? new List<PRSnapshot>();
            return snapshots.FirstOrDefault(s => s.PR_Id == prId);
        }

        private async Task SavePRSnapshot(PRSnapshot snapshot)
        {
            var snapshots = await _fileService.LoadConfigAsync<List<PRSnapshot>>("internal-pr-snapshots.json") 
                            ?? new List<PRSnapshot>();
            snapshots.RemoveAll(s => s.PR_Id == snapshot.PR_Id);
            snapshots.Add(snapshot);
            await _fileService.SaveConfigAsync("internal-pr-snapshots.json", snapshots);
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