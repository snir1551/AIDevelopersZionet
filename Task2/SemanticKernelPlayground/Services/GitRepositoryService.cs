using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using System.Text;

namespace SemanticKernelPlayground.Services
{
    public class GitRepositoryService
    {
        private string? _repoPath;
        private readonly string? _githubUsername;
        private readonly string? _githubToken;
        private string? _authorName;
        private string? _authorEmail;

        public GitRepositoryService(IConfiguration config)
        {
            _githubUsername = config["GitHub:Username"];
            _githubToken = config["GitHub:Token"];
        }

        public void SetAuthor(string name, string email)
        {
            _authorName = name;
            _authorEmail = email;
        }

        private Signature GetSignature()
        {
            if (string.IsNullOrWhiteSpace(_authorName) || string.IsNullOrWhiteSpace(_authorEmail))
                throw new InvalidOperationException("Author name and email must be set before committing or pulling.");

            return new Signature(_authorName, _authorEmail, DateTimeOffset.Now);
        }

        public string SetRepository(string repoPath)
        {
            if (!Directory.Exists(repoPath))
                return $"'{repoPath}' is not a valid path.";

            _repoPath = repoPath;
            return $"Repository set to '{repoPath}'.";
        }

        public string GetCommits(int count)
        {
            if (string.IsNullOrEmpty(_repoPath))
                return "Repository not set.";

            var sb = new StringBuilder();
            using var repo = new Repository(_repoPath);
            foreach (var commit in repo.Commits.Take(count))
            {
                sb.AppendLine("- " + commit.MessageShort);
            }
            return sb.ToString();
        }

        public string GetCurrentBranch()
        {
            if (string.IsNullOrEmpty(_repoPath)) return "No repo set.";
            using var repo = new Repository(_repoPath);
            return repo.Head.FriendlyName;
        }

        public string Pull()
        {
            if (string.IsNullOrEmpty(_repoPath)) return "No repo set.";

            try
            {
                using var repo = new Repository(_repoPath);
                var signature = GetSignature();
                var options = new PullOptions
                {
                    FetchOptions = new FetchOptions()
                };

                if (!string.IsNullOrWhiteSpace(_githubUsername) && !string.IsNullOrWhiteSpace(_githubToken))
                {
                    options.FetchOptions.CredentialsProvider = (_, _, _) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _githubUsername,
                            Password = _githubToken
                        };
                }

                var result = Commands.Pull(repo, signature, options);
                return $"Pull completed: {result.Status}";
            }
            catch (Exception ex)
            {
                return $"Error during pull: {ex.Message}";
            }
        }

        public string Push()
        {
            if (string.IsNullOrEmpty(_repoPath)) return "No repo set.";

            try
            {
                using var repo = new Repository(_repoPath);
                var remote = repo.Network.Remotes["origin"];
                var options = new PushOptions();

                if (!string.IsNullOrWhiteSpace(_githubUsername) && !string.IsNullOrWhiteSpace(_githubToken))
                {
                    options.CredentialsProvider = (_, _, _) =>
                        new UsernamePasswordCredentials
                        {
                            Username = _githubUsername,
                            Password = _githubToken
                        };
                }

                repo.Network.Push(remote, @$"refs/heads/{repo.Head.FriendlyName}", options);
                return $"Pushed to origin/{repo.Head.FriendlyName}";
            }
            catch (Exception ex)
            {
                return $"Error during push: {ex.Message}";
            }
        }

        public string Commit(string message)
        {
            if (string.IsNullOrEmpty(_repoPath)) return "No repo set.";

            try
            {
                using var repo = new Repository(_repoPath);
                Commands.Stage(repo, "*");

                var signature = GetSignature();
                repo.Commit(message, signature, signature);

                return $"Committed: '{message}' to branch {repo.Head.FriendlyName}";
            }
            catch (Exception ex)
            {
                return $"Error during commit: {ex.Message}";
            }
        }

        public string GetRepoPath() => _repoPath ?? "";
    }
}
