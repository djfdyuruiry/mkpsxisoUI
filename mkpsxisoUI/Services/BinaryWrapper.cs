using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace mkpsxisoUI.Services
{
    public class BinaryWrapper
    {
        private readonly string _installPath;
        private readonly string _mkBinPath;
        private readonly string _dumpBinPath;

        private readonly ActivityLogger _logger;
        private readonly Regex _versionRegex = new Regex(@"DUMPSXISO ([^ ]+) -");

        public BinaryWrapper(string installPath, ActivityLogger logger)
        {
            _logger = logger;
            _installPath = Path.GetFullPath(installPath);
            (_dumpBinPath, _mkBinPath) = FindBinariesInInstallPath();
        }

        private (string, string) FindBinariesInInstallPath()
        {
            var binExtension = Environment.OSVersion.Platform == PlatformID.Unix ? string.Empty : ".exe";

            var dumpBinary = $"dumpsxiso{binExtension}";
            var mkBinary = $"mkpsxiso{binExtension}";

            var dumpCandidates = Directory.GetFiles(_installPath, dumpBinary, SearchOption.AllDirectories);
            var mkCandidates = Directory.GetFiles(_installPath, mkBinary, SearchOption.AllDirectories);

            if (dumpCandidates.Length < 1 || mkCandidates.Length < 1)
            {
                throw new FileNotFoundException($"Install path does not contain mkpsxiso binaries: {_installPath}");
            }

            _logger.LogLine($"Found path to dumpsxiso: {dumpCandidates.First()}");
            _logger.LogLine($"Found path to mkpsxiso: {mkCandidates.First()}");

            return (dumpCandidates.First(), mkCandidates.First());
        }

        private async Task<List<string>> RunWithArgs(string binaryPath, params string[] args)
        {
            var argString = string.Join(' ', args.Select(a => $@"""{a}"""));
            var output = new List<string>();

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    Arguments = argString,
                    FileName = binaryPath
                }
            };

            DataReceivedEventHandler captureOutput = (_, o) =>
                output.Add(o.Data ?? string.Empty);

            process.OutputDataReceived += captureOutput;
            process.ErrorDataReceived += captureOutput;

            _logger.LogLine($"\n[Running Command] {Path.GetFileName(binaryPath)} {argString}\n");

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            var outputText = string.Join('\n', output);

            if (process.ExitCode != 0)
            {
                throw new($"{binaryPath} returned a non-zero exit code: {process.ExitCode}\n{outputText}");
            }

            // TODO: output in realtime (fix view bug not showing all output)
            _logger.LogLine(outputText);

            return output;
        }

        private async Task<List<string>> RunDumpWithArgs(params string[] args) =>
            await RunWithArgs(_dumpBinPath, args);

        private async Task<List<string>> RunMkWithArgs(params string[] args) =>
            await RunWithArgs(_mkBinPath, args);

        public async Task<string> GetVersion()
        {
            var helpTextLines = await RunDumpWithArgs("--help");
            var versionLine = helpTextLines.First();

            return _versionRegex.Match(versionLine).Groups[1].Value;
        }

        public async Task DumpIso(string inputImagePath, string outputPath, string outputXmlPath)
        {
            await RunDumpWithArgs(inputImagePath, "-x", outputPath, "-s", outputXmlPath);
        }

        public async Task BuildIso(string inputXmlPath)
        {
            await RunMkWithArgs(inputXmlPath);
        }
    }
}
