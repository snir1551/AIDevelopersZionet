using LibGit2Sharp;
using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SemanticKernelPlayground.Services;
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
        private readonly GitRepositoryService _gitRepo;
        private readonly VersionService _versionService;
        private readonly ReleaseNotesService _releaseNotesService;

        public GitPlugin(GitRepositoryService gitRepo, VersionService versionService, ReleaseNotesService releaseNotesService)
        {
            _gitRepo = gitRepo;
            _versionService = versionService;
            _releaseNotesService = releaseNotesService;
        }

        [KernelFunction]
        [Description("Set the repository path to work with")]
        public string SetRepository(
            [Description("Absolute path to the local Git repository")] string path) => _gitRepo.SetRepository(path);

        [KernelFunction]
        [Description("Get the latest N commit messages from the current repository")]
        public string GetCommits(
            [Description("Number of commits to retrieve")] int count) => _gitRepo.GetCommits(count);

        [KernelFunction]
        [Description("Save a new semantic version (e.g., 1.0.1)")]
        public string SaveVersion(
            [Description("Semantic version string to save")] string version) => _versionService.SaveVersion(version);

        [KernelFunction]
        [Description("Generate and store release notes based on last N commits")]
        public async Task<string> ReleaseNotes(
            Kernel kernel,
            [Description("Number of recent commits to include in the notes")] int commitCount = 5) =>
            await _releaseNotesService.GenerateAndStoreReleaseNotesAsync(kernel, commitCount);

        [KernelFunction]
        [Description("Pull the latest changes from origin")]
        public string Pull() => _gitRepo.Pull();

        [KernelFunction]
        [Description("Push current branch to origin")]
        public string Push() => _gitRepo.Push();

        [KernelFunction]
        [Description("Stage all changes and commit with the given message")]
        public string Commit(
            [Description("Commit message to use for this commit")] string message) => _gitRepo.Commit(message);

        [KernelFunction]
        [Description("Set the author name and email used for future commits")]
        public string SetAuthor(
            [Description("Full name of the author")] string name,
            [Description("Email address of the author")] string email)
        {
            _gitRepo.SetAuthor(name, email);
            return $"Author set to: {name} <{email}>";
        }

    }
}
