using Gheetah.Interfaces;
using Gheetah.Models.ProjectModel;
using Gheetah.Models.RepoSettingsModel;
using LibGit2Sharp;
using SharpCompress.Archives;
using SharpCompress.Archives.Rar;
using SharpCompress.Common;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Gheetah.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IFileService _fileService;
        private readonly ILogService _logService;
        private const string ProjectsFileName = "projects.json";
        private readonly JavaProjectConfigurator _javaProjectConfigurator;

        public ProjectService(IFileService fileService, IWebHostEnvironment env, ILogService logService, JavaProjectConfigurator javaProjectConfigurator)
        {
            _fileService = fileService;
            _logService = logService;
            _javaProjectConfigurator = javaProjectConfigurator;
        }

        public async Task LockProjectAsync(string projectId, string userId)
        {
            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project != null)
            {
                project.IsLocked = true;
                project.LockedBy = userId;
                project.LockedAt = DateTime.UtcNow;
                await SaveProjectsAsync(projects);
            }
        }

        public async Task UnlockProjectAsync(string projectId)
        {
            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project != null)
            {
                project.IsLocked = false;
                project.LockedBy = null;
                project.LockedAt = null;
                await SaveProjectsAsync(projects);
            }
        }

        public async Task<bool> IsProjectLockedAsync(string projectId)
        {
            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            return project?.IsLocked ?? false;
        }

        public async Task CheckAndReleaseStaleLocks()
        {
            var projects = await GetProjectsAsync();
            var staleProjects = projects.Where(p =>
                p.IsLocked &&
                p.LockedAt.HasValue &&
                (DateTime.UtcNow - p.LockedAt.Value) > TimeSpan.FromHours(1));

            foreach (var project in staleProjects)
            {
                project.IsLocked = false;
                project.LockedBy = null;
                project.LockedAt = null;
            }

            if (staleProjects.Any())
            {
                await SaveProjectsAsync(projects);
            }
        }

        public async Task<List<Project>> GetProjectsAsync()
        {
            var projects = await _fileService.LoadConfigAsync<List<Project>>(ProjectsFileName);
            return projects ?? new List<Project>();
        }

        public async Task SaveProjectsAsync(List<Project> projects)
        {
            await _fileService.SaveConfigAsync(ProjectsFileName, projects);
        }

        public async Task AddOrUpdateProjectAsync(Project project)
        {
            var projects = await GetProjectsAsync();
            var existing = projects.FirstOrDefault(p => p.Id == project.Id);
            if (existing != null)
            {
                projects.Remove(existing);
            }
            else if (string.IsNullOrEmpty(project.Id))
            {
                project.Id = Guid.NewGuid().ToString();
            }
            projects.Add(project);
            await SaveProjectsAsync(projects);
        }

        public async Task DeleteProjectAsync(string projectId)
        {
            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return;

            var projectsPath = await _fileService.LoadConfigAsync<string>("project-folder.json");
            var projectPath = Path.Combine(projectsPath, project.Name);
            var tempPath = Path.Combine(Path.GetTempPath(), $"delete_backup_{Guid.NewGuid()}");

            bool filesMoved = false;
            bool dbRecordRemoved = false;

            try
            {
                if (Directory.Exists(projectPath))
                {
                    Directory.Move(projectPath, tempPath);
                    filesMoved = true;
                }

                projects.Remove(project);
                await SaveProjectsAsync(projects);
                dbRecordRemoved = true;

                if (Directory.Exists(tempPath))
                {
                    await ForceDeleteDirectory(tempPath);
                }
            }
            catch (Exception ex)
            {
                if (filesMoved && !dbRecordRemoved)
                {
                    Directory.Move(tempPath, projectPath);
                }
                else if (dbRecordRemoved && !filesMoved)
                {
                    projects.Add(project);
                    await SaveProjectsAsync(projects);
                }

                throw new InvalidOperationException($"Deletion failed: {ex.Message}");
            }
        }

        private async Task ForceDeleteDirectory(string path)
        {
            await Task.Run(() =>
            {
                foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(path, recursive: true);
            });
        }
        public async Task CloneProjectAsync(string repoUrl, RepoSettingsVm repoInfo, string language, string saveDirectory)
        {
            if (string.IsNullOrEmpty(repoUrl)) throw new ArgumentNullException(nameof(repoUrl));
            if (repoInfo == null) throw new ArgumentNullException(nameof(repoInfo));
            if (string.IsNullOrEmpty(language)) throw new ArgumentNullException(nameof(language));
            if (string.IsNullOrEmpty(saveDirectory)) throw new ArgumentNullException(nameof(saveDirectory));
            if (string.IsNullOrEmpty(repoInfo.DisplayName)) throw new ArgumentNullException(nameof(repoInfo.DisplayName));

            var projectName = SanitizeFileName(repoInfo.DisplayName);
            var projectPath = Path.Combine(saveDirectory, projectName);
            bool isBddCompliant = false;

            try
            {
                if (!Directory.Exists(saveDirectory))
                {
                    Directory.CreateDirectory(saveDirectory);
                }

                if (Directory.Exists(projectPath))
                {
                    try
                    {
                        Directory.Delete(projectPath, true);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to clear existing directory {projectPath}: {ex.Message}", ex);
                    }
                }

                var cloneOptions = new CloneOptions();
                if (!string.IsNullOrEmpty(repoInfo.AccessToken))
                {
                    cloneOptions.FetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = "oauth2",
                            Password = repoInfo.AccessToken
                        };
                }

                Repository.Clone(repoUrl, projectPath, cloneOptions);

                bool hasFeatureFiles = Directory.GetFiles(projectPath, "*.feature", SearchOption.AllDirectories).Any();
                bool hasBddLibrary = language.ToLower() switch
                {
                    "c#" => CheckForCSharpBdd(projectPath),
                    "java" => CheckForJavaBdd(projectPath),
                    _ => false
                };

                if (!hasFeatureFiles || !hasBddLibrary)
                {
                    throw new InvalidOperationException("The project is not BDD compliant.");
                }

                isBddCompliant = true;

                if (language.Equals("java", StringComparison.OrdinalIgnoreCase))
                {
                    await _javaProjectConfigurator.ConfigureJavaProject(projectPath);
                }

                var newProject = new Project
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = projectName,
                    RepoUrl = repoUrl,
                    LanguageType = language,
                    UserId = "SYSTEM",
                    IsBuilt = false,
                    ClonedDate = DateTime.UtcNow
                };

                await AddOrUpdateProjectAsync(newProject);
            }
            catch (LibGit2SharpException ex)
            {
                throw new InvalidOperationException($"Git cloning failed: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cloning failed: {ex.Message}", ex);
            }
            finally
            {
                if (Directory.Exists(projectPath) && !isBddCompliant)
                {
                    try
                    {
                        Directory.Delete(projectPath, true);
                        var projects = await GetProjectsAsync();
                        var existing = projects.FirstOrDefault(p => p.Name == projectName);
                        if (existing != null)
                        {
                            projects.Remove(existing);
                            await SaveProjectsAsync(projects);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to clean up directory {projectPath}: {ex.Message}");
                    }
                }
            }
        }

        private string SanitizeFileName(string fileName)
        {
            var invalidChars = new[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
            var sanitized = fileName;
            foreach (var c in invalidChars)
            {
                sanitized = sanitized.Replace(c.ToString(), string.Empty);
            }
            sanitized = sanitized.TrimEnd('.');
            sanitized = sanitized.Replace(" ", "_");
            if (sanitized.Length > 100)
            {
                sanitized = sanitized.Substring(0, 100).TrimEnd('.');
            }
            return sanitized;
        }

        public async Task UploadLocalProjectAsync(IFormFile archiveFile, string language, string saveDirectory)
        {
            if (archiveFile == null || archiveFile.Length == 0)
                throw new InvalidOperationException("File not provided.");

            var extension = Path.GetExtension(archiveFile.FileName).ToLower();
            if (extension != ".zip" && extension != ".rar")
                throw new InvalidOperationException("Only .zip or .rar files are supported.");

            var projectName = Path.GetFileNameWithoutExtension(archiveFile.FileName);
            var projectPath = Path.Combine(saveDirectory, projectName);

            if (!Directory.Exists(saveDirectory))
                Directory.CreateDirectory(saveDirectory);

            var archivePath = Path.Combine(saveDirectory, archiveFile.FileName);

            try
            {
                using (var stream = new FileStream(archivePath, FileMode.Create))
                {
                    await archiveFile.CopyToAsync(stream);
                }

                if (extension == ".zip")
                {
                    ZipFile.ExtractToDirectory(archivePath, projectPath);
                }
                else if (extension == ".rar")
                {
                    using var archive = RarArchive.Open(archivePath);
                    foreach (var entry in archive.Entries.Where(entry => !entry.IsDirectory))
                    {
                        entry.WriteToDirectory(projectPath, new ExtractionOptions
                        {
                            ExtractFullPath = true,
                            Overwrite = true
                        });
                    }
                }

                bool hasFeatureFiles = Directory.GetFiles(projectPath, "*.feature", SearchOption.AllDirectories).Any();
                bool hasBddLibrary = language.ToLower() switch
                {
                    "c#" => CheckForCSharpBdd(projectPath),
                    "java" => CheckForJavaBdd(projectPath),
                    _ => false
                };

                if (!hasFeatureFiles || !hasBddLibrary)
                {
                    Directory.Delete(projectPath, true);
                    File.Delete(archivePath);

                    var projects = await GetProjectsAsync();
                    var existing = projects.FirstOrDefault(p => p.Name == projectName);
                    if (existing != null)
                    {
                        projects.Remove(existing);
                        await SaveProjectsAsync(projects);
                    }

                    throw new InvalidOperationException("The project is not BDD compliant. Removed.");
                }

                if (language.Equals("java", StringComparison.OrdinalIgnoreCase))
                {
                    await _javaProjectConfigurator.ConfigureJavaProject(projectPath);
                }

                var newProject = new Project
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = projectName,
                    RepoUrl = "Local Upload",
                    LanguageType = language,
                    UserId = "SYSTEM",
                    IsBuilt = false,
                    ClonedDate = DateTime.UtcNow
                };

                await AddOrUpdateProjectAsync(newProject);

                File.Delete(archivePath);
            }
            catch (Exception ex)
            {
                if (Directory.Exists(projectPath))
                    Directory.Delete(projectPath, true);
                if (File.Exists(archivePath))
                    File.Delete(archivePath);

                throw new InvalidOperationException($"Upload or clone failed: {ex.Message}");
            }
        }

        public async Task<BuildResult> BuildProjectAsync(string projectId, string languageType)
        {
            var clonesRoot = await _fileService.LoadConfigAsync<string>("project-folder.json");
            if (string.IsNullOrWhiteSpace(clonesRoot) || !Directory.Exists(clonesRoot))
            {
                return new BuildResult { IsSuccess = false, Message = "Cloned Projects folder path not found or not valid!" };
            }

            var projects = await GetProjectsAsync();
            var project = projects.FirstOrDefault(p => p.Id == projectId);
            if (project == null) return new BuildResult { IsSuccess = false, Message = "Project not found" };
            if (project.IsBuilt) return new BuildResult { IsSuccess = true, Message = "The project has already been built." };

            var projectPath = Path.Combine(clonesRoot, project.Name);
            if (!Directory.Exists(projectPath))
                return new BuildResult { IsSuccess = false, Message = "Local project folder path not found." };

            var projectInfos = new List<ProjectInfo>();
            int totalFeatures = 0;
            int totalScenarios = 0;

            try
            {
                if (languageType.Equals("c#", StringComparison.OrdinalIgnoreCase))
                {
                    var csprojs = Directory.GetFiles(projectPath, "*.csproj", SearchOption.AllDirectories);
                    foreach (var csproj in csprojs)
                    {
                        string assemblyName = GetAssemblyName(csproj);
                        if (string.IsNullOrEmpty(assemblyName))
                            throw new Exception($"AssemblyName not found: {csproj}");

                        var (restoreExit, restoreOut) = await RunProcessAsync("dotnet", $"restore \"{csproj}\"", Path.GetDirectoryName(csproj));
                        if (restoreExit != 0)
                            throw new Exception($"dotnet restore failed for {csproj}:\n{restoreOut}");

                        var (exitCode, output) = await RunProcessAsync("dotnet", $"build \"{csproj}\" --configuration Debug", Path.GetDirectoryName(csproj));
                        if (exitCode != 0)
                            throw new Exception($"dotnet restore failed for {csproj}:\n{output}");

                        var outputDir = Path.Combine(Path.GetDirectoryName(csproj), "bin", "Debug");
                        var dllFile = Directory.GetFiles(outputDir, $"{assemblyName}.dll", SearchOption.AllDirectories).FirstOrDefault()
                            ?? throw new Exception($"DLL not found: {assemblyName}.dll");

                        var featurePath = Path.Combine(Path.GetDirectoryName(csproj), "Features");
                        if (!Directory.Exists(featurePath)) featurePath = Path.GetDirectoryName(csproj);

                        var featureFiles = Directory.GetFiles(featurePath, "*.feature", SearchOption.AllDirectories);
                        if (featureFiles.Length > 0) ProcessFeatureFilesToDivideScenarios(featureFiles);

                        var (scenarios, featureCount, scenarioCount) = await ExtractFeatureScenariosAsync(featurePath);
                        totalFeatures += featureCount;
                        totalScenarios += scenarioCount;

                        projectInfos.Add(new ProjectInfo
                        {
                            ProjectName = Path.GetFileNameWithoutExtension(csproj),
                            BuildedTestFileName = Path.GetFileName(dllFile),
                            BuildedTestFileFullPath = Path.GetDirectoryName(dllFile),
                            BuildInfoFileName = Path.GetFileName(csproj),
                            BuildInfoFileFullPath = Path.GetDirectoryName(csproj),
                            FeatureFilesPath = featureCount > 0 ? featurePath : null,
                            Scenarios = scenarios
                        });
                    }
                }
                else if (languageType.Equals("java", StringComparison.OrdinalIgnoreCase))
                {
                    var pomFiles = Directory.GetFiles(projectPath, "pom.xml", SearchOption.AllDirectories);
                    var gradleFiles = Directory.GetFiles(projectPath, "build.gradle", SearchOption.AllDirectories);

                    if (!pomFiles.Any() && !gradleFiles.Any())
                    {
                        throw new Exception("No pom.xml or build.gradle found in the project directory or its subdirectories.");
                    }

                    var javaFeaturePath = Directory.GetDirectories(projectPath, "src", SearchOption.AllDirectories)
                        .Select(src => Path.Combine(src, "test", "resources"))
                        .FirstOrDefault(dir => Directory.Exists(dir)) ?? projectPath;

                    var featureFiles = Directory.GetFiles(javaFeaturePath, "*.feature", SearchOption.AllDirectories);
                    if (featureFiles.Length > 0) ProcessFeatureFilesToDivideScenarios(featureFiles);

                    var (scenarios, featureCount, scenarioCount) = await ExtractFeatureScenariosAsync(javaFeaturePath);
                    totalFeatures += featureCount;
                    totalScenarios += scenarioCount;

                    string mavenExecutable = GetMavenExecutablePath();
                    if (string.IsNullOrEmpty(mavenExecutable))
                    {
                        var mavenHome = Environment.GetEnvironmentVariable("MAVEN_HOME");
                        var path = Environment.GetEnvironmentVariable("PATH");
                        await _logService.LogAsync("SYSTEM", "BuildProjectAsync",
                            $"Maven executable file not found. MAVEN_HOME: {mavenHome}, PATH: {path}");
                        throw new Exception("Maven (mvn) is not installed or is not present in the system PATH or MAVEN_HOME.");
                    }

                    foreach (var pomFile in pomFiles)
                    {
                        var workingDir = Path.GetDirectoryName(pomFile);
                        SkipMavenTests(pomFile);
                        var (exitCode, output) = await RunProcessAsync(mavenExecutable, $"clean package -f \"{pomFile}\"", workingDir);
                        if (exitCode != 0)
                        {
                            await _logService.LogAsync("SYSTEM", "BuildProjectAsync", $"Maven build failed for {pomFile}: {output}");
                            throw new Exception($"Maven build failed for {pomFile}:\n{output}");
                        }

                        EnableMavenTests(pomFile);

                        var jarFile = Directory.GetFiles(workingDir, "*.jar", SearchOption.AllDirectories)
                            .FirstOrDefault(x => !x.EndsWith("-sources.jar") && !x.EndsWith("-javadoc.jar"))
                            ?? throw new Exception($"No Jar file found in {workingDir} for {pomFile}.");

                        projectInfos.Add(new ProjectInfo
                        {
                            ProjectName = project.Name,
                            BuildedTestFileName = Path.GetFileName(jarFile),
                            BuildedTestFileFullPath = Path.GetDirectoryName(jarFile),
                            BuildInfoFileName = "pom.xml",
                            BuildInfoFileFullPath = workingDir,
                            FeatureFilesPath = featureCount > 0 ? javaFeaturePath : null,
                            Scenarios = scenarios
                        });
                    }

                    foreach (var gradleFile in gradleFiles)
                    {
                        var workingDir = Path.GetDirectoryName(gradleFile);
                        SkipGradleTests(gradleFile);
                        var gradleExecutable = GetGradleExecutablePath();
                        if (string.IsNullOrEmpty(gradleExecutable))
                        {
                            await _logService.LogAsync("SYSTEM", "BuildProjectAsync", "Gradle executable file not found.");
                            throw new Exception("Gradle is not installed or is not present in the system PATH or GRADLE_HOME.");
                        }
                        var (exitCode, output) = await RunProcessAsync(gradleExecutable, $"clean build -p \"{workingDir}\"", workingDir);
                        if (exitCode != 0)
                        {
                            await _logService.LogAsync("SYSTEM", "BuildProjectAsync", $"Gradle build failed for {gradleFile}: {output}");
                            throw new Exception($"Gradle build failed for {gradleFile}:\n{output}");
                        }

                        EnableGradleTests(gradleFile);

                        var jarFile = Directory.GetFiles(workingDir, "*.jar", SearchOption.AllDirectories)
                            .FirstOrDefault(x => !x.EndsWith("-sources.jar") && !x.EndsWith("-javadoc.jar"))
                            ?? throw new Exception($"No Jar file found in {workingDir} for {gradleFile}.");

                        projectInfos.Add(new ProjectInfo
                        {
                            ProjectName = project.Name,
                            BuildedTestFileName = Path.GetFileName(jarFile),
                            BuildedTestFileFullPath = Path.GetDirectoryName(jarFile),
                            BuildInfoFileName = "build.gradle",
                            BuildInfoFileFullPath = workingDir,
                            FeatureFilesPath = featureCount > 0 ? javaFeaturePath : null,
                            Scenarios = scenarios
                        });
                    }
                }
                else
                {
                    return new BuildResult { IsSuccess = false, Message = $"Unsupported language: {languageType}" };
                }

                project.IsBuilt = true;
                project.LanguageType = languageType;
                project.ProjectInfos = projectInfos;
                project.FeatureFileCount = totalFeatures;
                project.ScenarioCount = totalScenarios;

                await AddOrUpdateProjectAsync(project);
                return new BuildResult { IsSuccess = true, Message = "The project was built successfully" };
            }
            catch (Exception ex)
            {
                await _logService.LogAsync("SYSTEM", "BuildProjectAsync", $"Build Error: {ex.Message}");
                return new BuildResult { IsSuccess = false, Message = $"Build Error: {ex.Message}" };
            }
        }

        private bool CheckForCSharpBdd(string path)
        {
            var allCsprojFiles = Directory.GetFiles(path, "*.csproj", SearchOption.AllDirectories);
            return allCsprojFiles.Any(file =>
            {
                var content = File.ReadAllText(file);
                return content.Contains("SpecFlow") || content.Contains("Reqnroll");
            });
        }

        private bool CheckForJavaBdd(string path)
        {
            var pomFiles = Directory.GetFiles(path, "pom.xml", SearchOption.AllDirectories);
            return pomFiles.Any(file =>
            {
                var content = File.ReadAllText(file);
                return content.Contains("cucumber") || content.Contains("jbehave");
            });
        }

        private string GetAssemblyName(string csprojPath)
        {
            var doc = XDocument.Load(csprojPath);
            var assemblyName = doc.Descendants("AssemblyName").FirstOrDefault()?.Value;
            return string.IsNullOrWhiteSpace(assemblyName)
                ? Path.GetFileNameWithoutExtension(csprojPath)
                : assemblyName;
        }

        private async Task<(List<FeatureScenarioInfo> Scenarios, int TotalFeatures, int TotalScenarios)> ExtractFeatureScenariosAsync(string projectPath)
        {
            var featureFiles = Directory.GetFiles(projectPath, "*.feature", SearchOption.AllDirectories);
            var scenarios = new List<FeatureScenarioInfo>();
            int totalScenarios = 0;

            foreach (var file in featureFiles)
            {
                var lines = await _fileService.ReadAllTextAsync(file).ContinueWith(t => t.Result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList());
                var fileName = Path.GetFileName(file);

                string currentScenarioTitle = null;
                bool isScenarioOutline = false;
                bool isInExamples = false;

                List<string> exampleHeaders = new();
                List<List<string>> exampleRows = new();

                for (int i = 0; i < lines.Count; i++)
                {
                    var line = lines[i].Trim();

                    if (Regex.IsMatch(line, @"^Scenario Outline:", RegexOptions.IgnoreCase))
                    {
                        if (isScenarioOutline && exampleRows.Count > 0)
                        {
                            foreach (var row in exampleRows)
                            {
                                var parameters = new List<string>();
                                for (int j = 0; j < exampleHeaders.Count && j < row.Count; j++)
                                {
                                    parameters.Add($"{exampleHeaders[j]} = {row[j]}");
                                }

                                scenarios.Add(new FeatureScenarioInfo
                                {
                                    FeatureFileName = fileName,
                                    ScenarioTitle = currentScenarioTitle,
                                    Parameters = parameters
                                });

                                totalScenarios++;
                            }
                        }

                        isScenarioOutline = true;
                        isInExamples = false;
                        currentScenarioTitle = line;
                        exampleHeaders = new();
                        exampleRows = new();
                    }
                    else if (Regex.IsMatch(line, @"^Scenario:", RegexOptions.IgnoreCase))
                    {
                        if (isScenarioOutline && exampleRows.Count > 0)
                        {
                            foreach (var row in exampleRows)
                            {
                                var parameters = new List<string>();
                                for (int j = 0; j < exampleHeaders.Count && j < row.Count; j++)
                                {
                                    parameters.Add($"{exampleHeaders[j]} = {row[j]}");
                                }

                                scenarios.Add(new FeatureScenarioInfo
                                {
                                    FeatureFileName = fileName,
                                    ScenarioTitle = currentScenarioTitle,
                                    Parameters = parameters
                                });

                                totalScenarios++;
                            }
                        }

                        isScenarioOutline = false;
                        isInExamples = false;

                        scenarios.Add(new FeatureScenarioInfo
                        {
                            FeatureFileName = fileName,
                            ScenarioTitle = line,
                            Parameters = ExtractQuotedParameters(line)
                        });

                        totalScenarios++;
                    }
                    else if (Regex.IsMatch(line, @"^Examples:", RegexOptions.IgnoreCase))
                    {
                        isInExamples = true;
                        exampleHeaders = new();
                        exampleRows = new();
                    }
                    else if (isInExamples && line.StartsWith("|"))
                    {
                        var values = line.Split('|', StringSplitOptions.RemoveEmptyEntries)
                                         .Select(v => v.Trim()).ToList();

                        if (exampleHeaders.Count == 0)
                        {
                            exampleHeaders = values;
                        }
                        else
                        {
                            exampleRows.Add(values);
                        }
                    }
                }

                if (isScenarioOutline && exampleRows.Count > 0)
                {
                    foreach (var row in exampleRows)
                    {
                        var parameters = new List<string>();
                        for (int j = 0; j < exampleHeaders.Count && j < row.Count; j++)
                        {
                            parameters.Add($"{exampleHeaders[j]} = {row[j]}");
                        }

                        scenarios.Add(new FeatureScenarioInfo
                        {
                            FeatureFileName = fileName,
                            ScenarioTitle = currentScenarioTitle,
                            Parameters = parameters
                        });

                        totalScenarios++;
                    }
                }
            }

            return (scenarios, featureFiles.Length, totalScenarios);
        }

        private List<string> ExtractQuotedParameters(string line)
        {
            var matches = Regex.Matches(line, "\"([^\"]+)\"");
            return matches.Select(m => m.Groups[1].Value).ToList();
        }

        private void SkipMavenTests(string pomPath)
        {
            var doc = XDocument.Load(pomPath);
            var ns = doc.Root.Name.Namespace;
            var properties = doc.Descendants(ns + "properties").FirstOrDefault() ?? new XElement(ns + "properties");
            if (properties.Parent == null)
            {
                doc.Root.Add(properties);
            }
            if (properties.Element(ns + "maven.test.skip") == null)
            {
                properties.Add(new XElement(ns + "maven.test.skip", "true"));
                doc.Save(pomPath);
            }
        }

        private void EnableMavenTests(string pomPath)
        {
            var doc = XDocument.Load(pomPath);
            var ns = doc.Root.Name.Namespace;
            var properties = doc.Descendants(ns + "properties").FirstOrDefault();
            if (properties != null)
            {
                var skipTest = properties.Element(ns + "maven.test.skip");
                if (skipTest != null)
                {
                    skipTest.Remove();
                    doc.Save(pomPath);
                }
            }
        }

        private void SkipGradleTests(string gradlePath)
        {
            var lines = File.ReadAllLines(gradlePath).ToList();
            if (!lines.Any(l => l.TrimStart().StartsWith("test {")))
            {
                lines.Add("\n");
                lines.Add("test {");
                lines.Add("    enabled = false");
                lines.Add("}");
                File.WriteAllLines(gradlePath, lines);
            }
        }

        private void EnableGradleTests(string gradlePath)
        {
            var lines = File.ReadAllLines(gradlePath).ToList();
            var testBlockIndex = lines.FindIndex(l => l.TrimStart().StartsWith("test {"));
            if (testBlockIndex >= 0)
            {
                var endIndex = lines.FindIndex(testBlockIndex, l => l.Trim() == "}");
                if (endIndex > testBlockIndex)
                {
                    lines.RemoveRange(testBlockIndex, endIndex - testBlockIndex + 1);
                    File.WriteAllLines(gradlePath, lines);
                }
            }
        }

        private string GetMavenExecutablePath()
        {
            var mavenHome = Environment.GetEnvironmentVariable("MAVEN_HOME");
            if (!string.IsNullOrEmpty(mavenHome) && Directory.Exists(mavenHome))
            {
                var mavenBin = Path.Combine(mavenHome, "bin");
                if (Directory.Exists(mavenBin))
                {
                    var possibleExecutables = new[] { "mvn.cmd", "mvn.exe", "mvn" };
                    foreach (var exe in possibleExecutables)
                    {
                        var mavenExe = Path.Combine(mavenBin, exe);
                        if (File.Exists(mavenExe))
                        {
                            _logService.LogAsync("SYSTEM", "GetMavenExecutablePath", $"Maven executable file found: {mavenExe}").GetAwaiter().GetResult();
                            return mavenExe;
                        }
                    }
                }
            }

            var path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(path))
            {
                var paths = path.Split(Path.PathSeparator);
                foreach (var p in paths)
                {
                    if (!string.IsNullOrEmpty(p) && Directory.Exists(p))
                    {
                        var possibleExecutables = new[] { "mvn.cmd", "mvn.exe", "mvn" };
                        foreach (var exe in possibleExecutables)
                        {
                            var mavenExe = Path.Combine(p, exe);
                            if (File.Exists(mavenExe))
                            {
                                _logService.LogAsync("SYSTEM", "GetMavenExecutablePath", $"Maven executable found in PATH: {mavenExe}").GetAwaiter().GetResult();
                                return mavenExe;
                            }
                        }
                    }
                }
            }
            
            _logService.LogAsync("SYSTEM", "GetMavenExecutablePath", "Maven executable file not found.").GetAwaiter().GetResult();
            return null;
        }

        private string GetGradleExecutablePath()
        {
            var gradleHome = Environment.GetEnvironmentVariable("GRADLE_HOME");
            if (!string.IsNullOrEmpty(gradleHome) && Directory.Exists(gradleHome))
            {
                var gradleBin = Path.Combine(gradleHome, "bin");
                if (Directory.Exists(gradleBin))
                {
                    var possibleExecutables = new[] { "gradle.bat", "gradle" };
                    foreach (var exe in possibleExecutables)
                    {
                        var gradleExe = Path.Combine(gradleBin, exe);
                        if (File.Exists(gradleExe))
                        {
                            return gradleExe;
                        }
                    }
                }
            }

            var path = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(path))
            {
                var paths = path.Split(Path.PathSeparator);
                foreach (var p in paths)
                {
                    if (!string.IsNullOrEmpty(p) && Directory.Exists(p))
                    {
                        var possibleExecutables = new[] { "gradle.bat", "gradle" };
                        foreach (var exe in possibleExecutables)
                        {
                            var gradleExe = Path.Combine(p, exe);
                            if (File.Exists(gradleExe))
                            {
                                return gradleExe;
                            }
                        }
                    }
                }
            }
            
            return null;
        }

        private async Task<(int ExitCode, string Output)> RunProcessAsync(string fileName, string arguments, string workingDirectory)
        {
            if (!Directory.Exists(workingDirectory))
            {
                throw new DirectoryNotFoundException($"Working directory does not exist: {workingDirectory}");
            }

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDirectory
            };

            var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            psi.EnvironmentVariables["PATH"] = currentPath;

            var mavenHome = Environment.GetEnvironmentVariable("MAVEN_HOME");
            if (!string.IsNullOrEmpty(mavenHome))
            {
                psi.EnvironmentVariables["PATH"] = $"{Path.Combine(mavenHome, "bin")};{psi.EnvironmentVariables["PATH"]}";
            }
            var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (!string.IsNullOrEmpty(javaHome))
            {
                psi.EnvironmentVariables["PATH"] = $"{Path.Combine(javaHome, "bin")};{psi.EnvironmentVariables["PATH"]}";
            }

            try
            {
                using var proc = Process.Start(psi);
                if (proc == null)
                {
                    throw new InvalidOperationException($"The process could not be started: {fileName}");
                }

                var stdOut = await proc.StandardOutput.ReadToEndAsync();
                var stdErr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();
                return (proc.ExitCode, stdOut + stdErr);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"An error occurred while trying to start the process '{fileName}' with working directory '{workingDirectory}': {ex.Message}");
            }
        }

        public static List<object> ProcessFeatureFilesToDivideScenarios(string[] featureFiles)
        {
            var processedFiles = new List<object>();

            foreach (var file in featureFiles)
            {
                var lines = File.ReadAllLines(file).ToList();
                var newLines = new List<string>();
                var i = 0;

                while (i < lines.Count)
                {
                    if (lines[i].Trim().StartsWith("Scenario Outline:"))
                    {
                        var scenarioOutlineLines = new List<string>();
                        scenarioOutlineLines.Add(lines[i]);
                        i++;

                        while (i < lines.Count && !lines[i].Trim().StartsWith("Examples:"))
                        {
                            scenarioOutlineLines.Add(lines[i]);
                            i++;
                        }

                        if (i < lines.Count && lines[i].Trim().StartsWith("Examples:"))
                        {
                            i++;
                            var examplesHeader = lines[i];
                            i++;

                            var examplesRows = new List<string>();
                            while (i < lines.Count && !string.IsNullOrWhiteSpace(lines[i]))
                            {
                                if (!lines[i].Trim().StartsWith("#"))
                                {
                                    examplesRows.Add(lines[i]);
                                }
                                i++;
                            }

                            char exampleSuffix = 'A';
                            foreach (var row in examplesRows)
                            {
                                var newScenarioOutline = new List<string>();

                                var scenarioName = scenarioOutlineLines[0].Replace("Scenario Outline:", "").Trim();
                                var newScenarioName = $"{scenarioName} Example_{exampleSuffix}";
                                newScenarioOutline.Add($"Scenario Outline: {newScenarioName}");
                                exampleSuffix++;

                                newScenarioOutline.AddRange(scenarioOutlineLines.Skip(1));

                                newScenarioOutline.Add("Examples:");
                                newScenarioOutline.Add(examplesHeader);
                                newScenarioOutline.Add(row);

                                AddScenarioTag(newScenarioOutline);

                                newLines.AddRange(newScenarioOutline);
                                newLines.Add("");
                            }
                        }
                    }
                    else if (lines[i].Trim().StartsWith("Scenario:"))
                    {
                        var scenarioLines = new List<string>();
                        scenarioLines.Add(lines[i]);
                        i++;

                        while (i < lines.Count && !lines[i].Trim().StartsWith("Scenario:") && !lines[i].Trim().StartsWith("Scenario Outline:"))
                        {
                            scenarioLines.Add(lines[i]);
                            i++;
                        }

                        AddScenarioTag(scenarioLines);
                        newLines.AddRange(scenarioLines);
                        newLines.Add("");
                    }
                    else
                    {
                        newLines.Add(lines[i]);
                        i++;
                    }
                }

                File.WriteAllLines(file, newLines);

                processedFiles.Add(new
                {
                    FileName = Path.GetFileName(file),
                    Content = newLines
                });
            }

            return processedFiles;
        }

        private static void AddScenarioTag(List<string> scenarioLines)
        {
            var scenarioLine = scenarioLines[0];
            var encodedScenarioText = EncodeText(scenarioLine);
            var tag = $"@{encodedScenarioText}_{Guid.NewGuid().ToString("N")}";

            if (scenarioLines.Count > 1 && scenarioLines[1].Trim().StartsWith("@"))
            {
                var tagLine = scenarioLines[1];
                if (tagLine.Contains("@retry"))
                {
                    scenarioLines.Insert(2, tag);
                }
            }
            else
            {
                scenarioLines.Insert(0, tag);
            }
        }

        private static string EncodeText(string scenarioLine)
        {
            var scenarioText = scenarioLine.Substring(scenarioLine.IndexOf(':') + 1).Trim();

            var words = scenarioText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                }
            }

            var encodedText = string.Join("", words);
            return encodedText.Replace("-", "").Replace("'", "").Replace(":", "").Replace("\"", "").Replace("<", "").Replace(">", "").Replace("!", "").Replace(".", "").Replace("?", "").Replace("[", "").Replace("]", "");
        }

    }
}



