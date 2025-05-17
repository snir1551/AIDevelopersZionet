using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemanticKernelPlayground.Services
{
    public class VersionService
    {
        private readonly string _versionFilePath;

        public VersionService(IConfiguration config)
        {
            var repoPath = config["RepositoryPath"] ?? Directory.GetCurrentDirectory();
            _versionFilePath = Path.Combine(repoPath, "version.txt");
        }

        public string LoadVersion()
        {
            return File.Exists(_versionFilePath)
                ? File.ReadAllText(_versionFilePath).Trim()
                : "0.0.0";
        }

        public string SaveVersion(string version)
        {
            File.WriteAllText(_versionFilePath, version);
            return $"Saved version {version}";
        }
    }
}
