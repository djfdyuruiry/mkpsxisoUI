using System.IO;
using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Linq;

using Avalonia.Controls;
using ReactiveUI;

using mkpsxisoUI.Services;

namespace mkpsxisoUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private const string DEFAULT_BIN_PATH = "./mkpsxiso";

        private readonly ActivityLogger _activityLogger;
        private readonly ReleaseDownloader _releaseDownloader;
        private BinaryWrapper? _binaryWrapper;

        private string? _binaryPath;
        private string _version;

        private string? _discImageInputPath;
        private string? _outputPath;
        private string? _xmlOutputPath;

        private string? _discImageOutputPath;
        private string? _xmlInputPath;

        private string? _processOutput;
        private int? _outputLength;

        public string? BinaryPath
        {
            get => _binaryPath;
            set => this.RaiseAndSetIfChanged(ref _binaryPath, value);
        }

        public string Version
        {
            get => _version;
            set => this.RaiseAndSetIfChanged(ref _version, value);
        }

        public string? DiscImageInputPath
        {
            get => _discImageInputPath;
            set => this.RaiseAndSetIfChanged(ref _discImageInputPath, value);
        }

        public string? OutputPath
        {
            get => _outputPath;
            set => this.RaiseAndSetIfChanged(ref _outputPath, value);
        }

        public string? XmlOutputPath
        {
            get => _xmlOutputPath;
            set => this.RaiseAndSetIfChanged(ref _xmlOutputPath, value);
        }

        public string? DiscImageOutputPath
        {
            get => _discImageOutputPath;
            set => this.RaiseAndSetIfChanged(ref _discImageOutputPath, value);
        }

        public string? XmlInputPath
        {
            get => _xmlInputPath;
            set => this.RaiseAndSetIfChanged(ref _xmlInputPath, value);
        }

        public string? ProcessOutput
        {
            get => _processOutput;
            set
            {
                this.RaiseAndSetIfChanged(ref _processOutput, value);
                OutputLength = _processOutput?.Length ?? 0;
            }
        }

        public int? OutputLength
        {
            get => _outputLength;
            set => this.RaiseAndSetIfChanged(ref _outputLength, value);
        }

        public ReactiveCommand<Window, Unit> PickBinaryPath { get; }

        public ReactiveCommand<Unit, Unit> GetLatestRelease { get; }

        public ReactiveCommand<Window, Unit> PickDiscImageInputPath { get; }

        public ReactiveCommand<Window, Unit> PickOutputPath { get; }

        public ReactiveCommand<Window, Unit> PickXmlOutputPath { get; }

        public ReactiveCommand<Unit, Unit> DumpIso { get; }

        public ReactiveCommand<Window, Unit> PickDiscImageOutputPath { get; }

        public ReactiveCommand<Window, Unit> PickXmlInputPath { get; }

        public ReactiveCommand<Unit, Unit> MakeIso { get; }

        public MainWindowViewModel()
        {
            _version = "???";

            _activityLogger = new(l =>
                ProcessOutput = (ProcessOutput ?? string.Empty) + l
            );
            _releaseDownloader = new(_activityLogger);

            PickBinaryPath = ReactiveCommand.CreateFromTask<Window>(
                async w => await PickFolder(w, InitBinary)
            );

            GetLatestRelease = ReactiveCommand.CreateFromTask(DoGetLatestRelease);

            PickDiscImageInputPath = ReactiveCommand.CreateFromTask<Window>(
                async w => await PickFile(w, "*", f => DiscImageInputPath = f)
            );

            PickOutputPath = ReactiveCommand.CreateFromTask<Window>(
                async w => await PickFolder(w, f => OutputPath = f)
            );

            PickXmlOutputPath = ReactiveCommand.CreateFromTask<Window>(DoPickXmlOutputPath);

            DumpIso = ReactiveCommand.CreateFromTask(() =>
            {
                ProcessOutput = string.Empty;
                return _binaryWrapper?.DumpIso(DiscImageInputPath!, OutputPath!, XmlOutputPath!) ?? Task.CompletedTask;
            });

            PickDiscImageOutputPath = ReactiveCommand.CreateFromTask<Window>(
                async w => await PickFile(w, "*", f => DiscImageOutputPath = f)
            );

            PickXmlInputPath = ReactiveCommand.CreateFromTask<Window>(
                async w => await PickFile(w, "xml", f => XmlInputPath = f)
            );

            MakeIso = ReactiveCommand.CreateFromTask(() =>
            {
                ProcessOutput = string.Empty;
                return _binaryWrapper?.BuildIso(XmlInputPath!) ?? Task.CompletedTask;
            });
        }

        private async Task DoGetLatestRelease()
        {
            var release = await _releaseDownloader.GetLatestRelease();

            await _releaseDownloader.DownloadAndInstallRelease(release, DEFAULT_BIN_PATH);

            await InitBinary(Path.GetFullPath(DEFAULT_BIN_PATH));
        }

        private async Task InitBinary(string binaryPath)
        {
            BinaryPath = binaryPath;

            _binaryWrapper = new BinaryWrapper(BinaryPath, _activityLogger);

            Version = await _binaryWrapper.GetVersion();
        }

        private async Task PickFolder(Window activeWindow, Func<string, Task> setAction)
        {
            var folderDialog = new OpenFolderDialog();

            var folderResult = await folderDialog.ShowAsync(activeWindow);

            if (folderResult is null)
            {
                return;
            }

            await setAction(folderResult);
        }

        private async Task PickFolder(Window activeWindow, Action<string> setAction) =>
            await PickFolder(activeWindow, s => {
                setAction(s);
                return Task.CompletedTask;
            });

        private async Task PickFile(Window activeWindow, string fileExtension, Action<string> setAction)
        {
            var fileDialog = new OpenFileDialog()
            {
                Filters = new()
                {
                    new()
                    {
                        Extensions = new() { fileExtension }
                    }
                }
            };

            var dialogResult = await fileDialog.ShowAsync(activeWindow);

            if ((dialogResult?.Length ?? 0) < 1)
            {
                return;
            }

            setAction(dialogResult!.First());
        }

        private async Task DoPickXmlOutputPath(Window activeWindow)
        {
            var fileDialog = new SaveFileDialog()
            {
                Filters = new()
                {
                    new()
                    {
                        Extensions = new() { "xml" }
                    }
                }
            };
            var dialogResult = await fileDialog.ShowAsync(activeWindow);

            if ((dialogResult?.Length ?? 0) < 1)
            {
                return;
            }

            XmlOutputPath = dialogResult;
        }
    }
}
