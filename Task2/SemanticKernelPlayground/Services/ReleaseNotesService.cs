using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using SemanticKernelPlayground.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Services
{
    public class ReleaseNotesService
    {
        private readonly GitRepositoryService _gitRepo;
        private readonly VersionService _versionService;
        private readonly string _promptPath;

        public ReleaseNotesService(IConfiguration config, GitRepositoryService gitRepo, VersionService versionService)
        {
            _gitRepo = gitRepo;
            _versionService = versionService;
            _promptPath = Path.Combine(AppContext.BaseDirectory, "Prompts", "ReleaseNotes", "skprompt.txt");
        }

        public async Task<string> GenerateAndStoreReleaseNotesAsync(Kernel kernel, int commitCount)
        {
            if (!File.Exists(_promptPath)) return $"Missing prompt at {_promptPath}";

            var prompt = File.ReadAllText(_promptPath);
            var version = _versionService.LoadVersion();
            var commits = _gitRepo.GetCommits(commitCount);

            var result = await kernel.InvokePromptAsync(prompt, new KernelArguments
            {
                ["version"] = version,
                ["commits"] = commits
            });

            var notes = result.GetValue<string>() ?? "";

            var release = new ReleaseInfo
            {
                Version = version,
                Date = DateTime.UtcNow,
                Notes = notes
            };

            var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "releaseInfo.json");
            File.WriteAllText(outputPath, JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true }));

            return $"Release notes saved to {outputPath}\n\n{notes}";
        }

    }
}
