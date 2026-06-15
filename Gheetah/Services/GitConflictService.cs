using Gheetah.Interfaces;
using Gheetah.Models.AiModels;
using LibGit2Sharp;

namespace Gheetah.Services
{
    public class GitConflictService : IGitConflictService
    {
        private readonly ILogger<GitConflictService> _logger;

        public GitConflictService(ILogger<GitConflictService> logger)
        {
            _logger = logger;
        }

        public Task<bool> HasConflictsAsync(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                return Task.FromResult(repo.Index.Conflicts.Any());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking conflicts in {RepoPath}", repoPath);
                return Task.FromResult(false);
            }
        }

        public async Task<List<string>> GetConflictedFilesAsync(string repoPath)
        {
            try
            {
                using var repo = new Repository(repoPath);
                return repo.Index.Conflicts
                    .Select(c => c.Ours?.Path ?? c.Theirs?.Path ?? "unknown")
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error listing conflicted files in {RepoPath}", repoPath);
                return new List<string>();
            }
        }

        public async Task<List<ConflictBlock>> ParseConflictsAsync(string filePath)
        {
            if (!File.Exists(filePath)) return new List<ConflictBlock>();

            var lines = await File.ReadAllLinesAsync(filePath);
            var blocks = new List<ConflictBlock>();
            var blockIndex = 0;

            int i = 0;
            while (i < lines.Length)
            {
                if (!lines[i].StartsWith("<<<<<<<"))
                {
                    i++;
                    continue;
                }

                var startLine = i;
                var headLines = new List<string>();
                var baseLines = new List<string>();
                var theirLines = new List<string>();
                var inHead = true;

                i++; // skip <<<<<<< marker
                while (i < lines.Length && !lines[i].StartsWith(">>>>>>>"))
                {
                    if (lines[i].StartsWith("======="))
                    {
                        inHead = false;
                    }
                    else if (lines[i].StartsWith("|||||||"))
                    {
                        // diff3 format base marker — skip
                    }
                    else if (inHead)
                    {
                        headLines.Add(lines[i]);
                    }
                    else
                    {
                        theirLines.Add(lines[i]);
                    }
                    i++;
                }

                var endLine = i;
                var isBdd = filePath.EndsWith(".feature", StringComparison.OrdinalIgnoreCase)
                    && (headLines.Any(l => l.TrimStart().StartsWith("Scenario"))
                        || theirLines.Any(l => l.TrimStart().StartsWith("Scenario")));

                blocks.Add(new ConflictBlock
                {
                    BlockIndex = blockIndex++,
                    FilePath = filePath,
                    StartLine = startLine,
                    EndLine = endLine,
                    HeadContent = string.Join(Environment.NewLine, headLines),
                    IncomingContent = string.Join(Environment.NewLine, theirLines),
                    BaseContent = string.Join(Environment.NewLine, baseLines),
                    IsBddScenario = isBdd
                });

                i++; // skip >>>>>>> marker
            }

            return blocks;
        }

        public async Task ApplyResolutionsAsync(string repoPath, string filePath, List<ResolvedBlock> resolutions)
        {
            if (!File.Exists(filePath)) return;

            var lines = (await File.ReadAllLinesAsync(filePath)).ToList();
            var allBlocks = await ParseConflictsAsync(filePath);

            // Process blocks in reverse order to preserve line numbers
            foreach (var block in allBlocks.OrderByDescending(b => b.StartLine))
            {
                var resolution = resolutions.FirstOrDefault(r => r.BlockIndex == block.BlockIndex);
                if (resolution == null) continue;

                string resolvedContent = resolution.Resolution switch
                {
                    ConflictResolution.Head => block.HeadContent,
                    ConflictResolution.Incoming => block.IncomingContent,
                    ConflictResolution.Both => block.HeadContent + Environment.NewLine + block.IncomingContent,
                    ConflictResolution.Manual => resolution.CustomContent ?? block.HeadContent,
                    _ => block.HeadContent
                };

                // Replace lines from startLine to endLine (inclusive) with resolved content
                var replaceCount = block.EndLine - block.StartLine + 1;
                lines.RemoveRange(block.StartLine, Math.Min(replaceCount, lines.Count - block.StartLine));
                var resolvedLines = resolvedContent.Split(Environment.NewLine).ToList();
                lines.InsertRange(block.StartLine, resolvedLines);
            }

            await File.WriteAllLinesAsync(filePath, lines);

            // Stage the resolved file
            try
            {
                using var repo = new Repository(repoPath);
                Commands.Stage(repo, filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error staging resolved file {FilePath}", filePath);
            }
        }

        public Task CommitResolutionAsync(string repoPath, string commitMessage, string authorName, string authorEmail)
        {
            try
            {
                using var repo = new Repository(repoPath);
                var author = new Signature(authorName, authorEmail, DateTimeOffset.Now);
                repo.Commit(commitMessage, author, author);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error committing conflict resolution in {RepoPath}", repoPath);
                throw;
            }
        }
    }
}
