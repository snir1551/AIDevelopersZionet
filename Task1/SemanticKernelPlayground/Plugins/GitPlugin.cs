using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Plugins
{
    public class GitPlugin
    {
        private readonly IConfiguration _config;
        private string? _repoPath;
        private const string VersionFileName = "version.txt";
        private string? _authorName;
        private string? _authorEmail;
        private string? _githubUsername = "";
        private string? _githubToken = "";
        private string VersionFilePath => string.IsNullOrEmpty(_repoPath) ? VersionFileName : Path.Combine(_repoPath, VersionFileName);

        public GitPlugin(IConfiguration config)
        {
            _config = config;

            _githubToken = _config["GitHub:Token"];
            _githubUsername = _config["GitHub:Username"];
        }

        [KernelFunction]
        [Description("Set the repository path for git operation")]
        public string SetRepository([Description("Abolute path to the git repository")] string repoPath)
        {
            if (string.IsNullOrWhiteSpace(repoPath) || !Directory.Exists(repoPath))
            {
                return $"'{repoPath}' is not a valid git repository. Please try again";
            }

            _repoPath = repoPath;

            return $"Repository set to '{repoPath}'.";
        }

        [KernelFunction]
        [Description("Get the latest commits from the currently set git repository")]
        public string GetCommits([Description("Number of commits retrieve")] int nOfCommits)
        {
            if (string.IsNullOrEmpty(_repoPath))
            {
                return "No repository defined. Please run **SetRepository** first.";
            }

            var sb = new StringBuilder();

            try
            {
                using var repo = new Repository(_repoPath);
                var repoName = Path.GetFileName(repo.Info.WorkingDirectory.TrimEnd(Path.DirectorySeparatorChar));
                sb.AppendLine($"Last {nOfCommits} commits in **{repoName}**:\n");

                foreach (var commit in repo.Commits.Take(nOfCommits))
                {
                    sb.AppendLine($"{commit.Author.Name} at {commit.Author.When.LocalDateTime}:");
                    sb.AppendLine($"    {commit.MessageShort}\n");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error while reading commits: {ex.Message}";
            }
        }


        [KernelFunction]
        [Description("Get the current branch name")]
        public string GetCurrentBranch()
        {
            if (string.IsNullOrEmpty(_repoPath))
                return "No repository defined. Please run **SetRepository** first.";

            try
            {
                using var repo = new Repository(_repoPath);
                return $"Current branch: {repo.Head.FriendlyName}";
            }
            catch (Exception ex)
            {
                return $"Error retrieving branch: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Save the given version string to persistent storage")]
        public string SaveVersion([Description("The version string to store")] string version)
        {
            try
            {
                File.WriteAllText(VersionFilePath, version);
                return $"Version '{version}' saved successfully.";
            }
            catch (Exception ex)
            {
                return $"Failed to save version: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Load the latest version string from persistent storage")]
        public string LoadVersion()
        {
            try
            {
                if (!File.Exists(VersionFilePath))
                    return "0.0.0"; 

                var version = File.ReadAllText(VersionFilePath).Trim();
                return version;
            }
            catch (Exception ex)
            {
                return "0.0.0"; 
            }
        }

        [KernelFunction]
        [Description("Increment the patch version and save it")]
        public string PatchVersion()
        {
            try
            {
                var version = File.Exists(VersionFilePath)
                    ? File.ReadAllText(VersionFilePath).Trim()
                    : "0.0.0";

                var parts = version.Split('.');
                if (parts.Length != 3)
                    return "Invalid version format. Expected format: X.Y.Z";

                int major = int.Parse(parts[0]);
                int minor = int.Parse(parts[1]);
                int patch = int.Parse(parts[2]) + 1;

                var newVersion = $"{major}.{minor}.{patch}";
                File.WriteAllText(VersionFilePath, newVersion);

                return $"Version patched to: {newVersion}";
            }
            catch (Exception ex)
            {
                return $"Failed to patch version: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Commit all staged changes with the given message")]
        public string CommitChanges([Description("Commit message")] string message)
        {
            if (string.IsNullOrEmpty(_repoPath))
                return "No repository defined. Please run **SetRepository** first.";

            try
            {
                using var repo = new Repository(_repoPath);
                Commands.Stage(repo, "*");

                var author = GetAuthorSignature();
                repo.Commit(message, author, author);

                return $"Committed: '{message}' by {author.Name} <{author.Email}> on branch '{repo.Head.FriendlyName}'";
            }
            catch (Exception ex)
            {
                return $"Failed to commit: {ex.Message}";
            }
        }


        [KernelFunction]
        [Description("Pull latest changes from the origin remote")]
        public string PullFromRemote()
        {
            if (string.IsNullOrEmpty(_repoPath))
                return "No repository defined. Please run **SetRepository** first.";

            try
            {
                using var repo = new Repository(_repoPath);


                var signature = GetAuthorSignature();
                var pullOptions = new PullOptions
                {
                    FetchOptions = new FetchOptions()
                };

                if (!string.IsNullOrWhiteSpace(_githubUsername) && !string.IsNullOrWhiteSpace(_githubToken))
                {
                    pullOptions.FetchOptions.CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _githubUsername,
                            Password = _githubToken
                        };
                }

                var result = Commands.Pull(repo, signature, pullOptions);
                return $"Pull completed: {result.Status}";
            }
            catch (Exception ex)
            {
                return $"Failed to pull: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Push current branch to origin remote")]
        public string PushToRemote()
        {
            if (string.IsNullOrEmpty(_repoPath)) return "No repository set.";

            try
            {
                using var repo = new Repository(_repoPath);
                var remote = repo.Network.Remotes["origin"];
                var pushOptions = new PushOptions();

                if (!string.IsNullOrWhiteSpace(_githubUsername) && !string.IsNullOrWhiteSpace(_githubToken))
                {
                    pushOptions.CredentialsProvider = (_url, _user, _cred) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _githubUsername,
                            Password = _githubToken
                        };
                }


                repo.Network.Push(remote, @"refs/heads/" + repo.Head.FriendlyName, pushOptions);

                return $"Pushed branch '{repo.Head.FriendlyName}' to origin.";
            }
            catch (Exception ex)
            {
                return $"Failed to push: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Set the author name and email for future commits")]
        public string SetAuthor([Description("Author name")] string name, [Description("Author email")] string email)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
            {
                return "Name and email must not be empty.";
            }

            _authorName = name;
            _authorEmail = email;

            return $"Author set to '{name} <{email}>'";
        }

        [KernelFunction]
        [Description("Set GitHub credentials")]
        public string SetGitHubCredentials([Description("GitHub username")] string username, [Description("GitHub token")] string token)
        {
            _githubUsername = username;
            _githubToken = token;
            return "GitHub credentials set successfully.";
        }



        [KernelFunction]
        [Description("Generate release notes from the last N commits and save to releaseInfo.json")]
        public async Task<string> ReleaseNotes(Kernel kernel, int commitCount = 5)
        {
            if (string.IsNullOrEmpty(_repoPath))
                return "No repository set. Please run SetRepository first.";

            var version = File.Exists(VersionFilePath)
                ? File.ReadAllText(VersionFilePath).Trim()
                : "0.0.0";

            var commits = GetCommits(commitCount);

            var promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ReleaseNotes", "skprompt.txt");
            if (!File.Exists(promptPath))
                return $"Missing prompt file: {promptPath}";

            var prompt = File.ReadAllText(promptPath);

            var result = await kernel.InvokePromptAsync(
                prompt,
                new KernelArguments
                {
                    ["version"] = version,
                    ["commits"] = commits
                });

            var notes = result.GetValue<string>() ?? "";

            try
            {
                var release = new ReleaseInfo
                {
                    Version = version,
                    Date = DateTime.UtcNow,
                    Notes = notes
                };

                var json = JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true });

                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "releaseInfo.json");
                File.WriteAllText(outputPath, json);

                return $"Release notes saved to: {outputPath}\n\n{notes}";
            }
            catch (Exception ex)
            {
                return $"Failed to save releaseInfo.json: {ex.Message}";
            }
        }

        private Signature GetAuthorSignature()
        {
            var name = string.IsNullOrWhiteSpace(_authorName) ? "" : _authorName;
            var email = string.IsNullOrWhiteSpace(_authorEmail) ? "" : _authorEmail;
            return new Signature(name, email, DateTimeOffset.Now);
        }

    }
}
