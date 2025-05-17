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
    public class ReleaseInfo
    {
        public string Version { get; set; } = "0.0.0";
        public DateTime Date { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }

    public class ReleaseStoragePlugin
    {
        private readonly string _filePath = Path.Combine(Directory.GetCurrentDirectory(), "releaseInfo.json");

        [KernelFunction]
        [Description("Gets the latest stored release version")]
        public string GetLatestRelease()
        {
            if (!File.Exists(_filePath))
                return "No release has been stored yet.";

            try
            {
                var content = File.ReadAllText(_filePath);
                var release = JsonSerializer.Deserialize<ReleaseInfo>(content);
                return release is null
                    ? "Failed to deserialize release information."
                    : $"Version: {release.Version}, Date: {release.Date:yyyy-MM-dd}, Notes: {release.Notes}";
            }
            catch (Exception ex)
            {
                return $"Error reading release file: {ex.Message}";
            }
        }

        [KernelFunction]
        [Description("Saves a new release version with optional notes")]
        public string SaveRelease([Description("The new version (e.g., 1.2.3)")] string version, [Description("Release notes or description")] string notes = "")
        {
            try
            {
                var release = new ReleaseInfo
                {
                    Version = version,
                    Date = DateTime.UtcNow,
                    Notes = notes
                };

                var json = JsonSerializer.Serialize(release, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);

                return $"Release {version} saved successfully.";
            }
            catch (Exception ex)
            {
                return $"Failed to save release: {ex.Message}";
            }
        }
    }
}
