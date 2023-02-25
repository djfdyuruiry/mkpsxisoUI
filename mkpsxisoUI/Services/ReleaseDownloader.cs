using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using mkpsxisoUI.Model;

namespace mkpsxisoUI.Services
{
    public class ReleaseDownloader
    {
        private const string RELEASES_PATH = "Lameguy64/mkpsxiso/releases";
        private const string RELEASES_URL = $"https://github.com/{RELEASES_PATH}";

        private readonly HttpClient _httpClient = new();
        private readonly ActivityLogger _logger;

        public ReleaseDownloader(ActivityLogger logger) => _logger = logger;

        public async Task<Release> GetLatestRelease()
        {
            _logger.LogLine($"Fetching latest mkpsxiso release from {RELEASES_URL}");

            var releasesHtml = await _httpClient.GetStringAsync(RELEASES_URL);

            var tagRegex = new Regex($@"{RELEASES_PATH}/tag/([^/""]+)");
            var tag = tagRegex.Match(releasesHtml).Groups[1].Value;

            _logger.LogLine($"Latest tag found: {tag}");

            var assestsUrl = $"{RELEASES_URL}/expanded_assets/{tag}";

            _logger.LogLine($"Fetching release list of archives from {assestsUrl}");

            var assetsHtml = await _httpClient.GetStringAsync(assestsUrl);

            var architecture = "win64";

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                architecture = "Linux";
            }

            _logger.LogLine($"Platform detected: {architecture}");

            var escapedTag = tag.Replace(".", "[.]");
            var downloadUrlRegex = new Regex($@"href=""/{RELEASES_PATH}/(download/{escapedTag}/[^""/]+-{architecture}.zip)""");
            var downloadPath = downloadUrlRegex.Match(assetsHtml).Groups[1].Value;

            var downloadUrl = $"{RELEASES_URL}/{downloadPath}";

            _logger.LogLine($"Resolved release archive URL for {tag}: {downloadUrl}");

            return new()
            {
                Version = tag,
                DownloadUrl = downloadUrl
            };
        }

        public async Task DownloadAndInstallRelease(Release release, string installPath)
        {
            _logger.LogLine($"Downloading release {release.Version} from {release.DownloadUrl}");

            var zipBytes = await _httpClient.GetByteArrayAsync(release.DownloadUrl);
            var zipTempFile = Path.GetTempFileName();
            
            await File.WriteAllBytesAsync(zipTempFile, zipBytes);

            _logger.LogLine($"Installing release {release.Version} to {installPath}");

            if (Directory.Exists(installPath))
            {
                Directory.Delete(installPath, true);
            }

            ZipFile.ExtractToDirectory(zipTempFile, installPath);

            _logger.LogLine("mkpsxiso installed!");
        }
    }
}
