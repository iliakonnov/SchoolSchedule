using System;
using System.Configuration.Install;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Threading;
using DataLoader;
using Timer = System.Timers.Timer;

namespace ScheduleWatcherService
{
    [SuppressMessage("ReSharper", "LocalizableElement")]
    public partial class WatcherService : ServiceBase
    {
        public const string DbConfPath = "dbPath.conf";
        private FileSystemWatcher _configWatcher;
        private string _path;
        private Timer _timer;
        private FileSystemWatcher _watcher;

        public WatcherService()
        {
            CanPauseAndContinue = true;
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            Console.WriteLine("Start");
            InitializeWatcher();

            // ReSharper disable once AssignNullToNotNullAttribute
            _configWatcher = new FileSystemWatcher(Path.GetDirectoryName(Path.GetFullPath(DbConfPath)));
            _configWatcher.Filter = DbConfPath;
            _configWatcher.Changed += (sender, eventArgs) => InitializeWatcher();
            _configWatcher.EnableRaisingEvents = true;
        }

        private static string ReadFile(string fullPath, out int numTries)
        {
            // Based on https://stackoverflow.com/a/3677960/4079458
            for (numTries = 0; numTries < 10; numTries++)
            {
                FileStream fs = null;
                StreamReader reader = null;
                try
                {
                    fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    reader = new StreamReader(fs);
                    var text = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(text))
                        return text;
                }
                catch (IOException)
                {
                }
                fs?.Dispose();
                reader?.Dispose();
                Thread.Sleep(50);
            }

            return null;
        }

        private void InitializeWatcher()
        {
            Console.WriteLine("Initialize.");

            _path = ReadFile(DbConfPath, out var tries);
            if (_path == null)
            {
                _path = @"C:\Users\Ilia\Documents\SchoolSchedule\DataBase";
                Console.WriteLine($"\tError loading path from config. Using default '{_path}'");
            }
            else
            {
                Console.WriteLine($"\tLoaded path '{_path}' from config with {tries + 1} attempts");
            }

            if (_watcher == null)
            {
                _watcher = new FileSystemWatcher(_path);
                _watcher.Changed += (sender, eventArgs) => Upload();
                _watcher.EnableRaisingEvents = true;
            }
            else
            {
                _watcher.Path = _path;
            }
        }

        protected override void OnStop()
        {
            Console.WriteLine("Stop");
            _watcher?.Dispose();
            _watcher = null;

            _timer?.Dispose();
            _timer = null;

            _path = null;
        }

        protected override void OnPause()
        {
            Console.WriteLine("Pause");
            if (_watcher != null)
                _watcher.EnableRaisingEvents = false;
            _timer?.Start();
        }

        protected override void OnContinue()
        {
            Console.WriteLine("Continue");
            if (_watcher != null)
                _watcher.EnableRaisingEvents = true;
            _timer?.Stop();
        }

        private void Upload()
        {
            const double timerInterval = 60 * 1000;

            Console.WriteLine($"Upload with path '{_path}'");
            if (_path == null) return;

            if (_timer != null)
            {
                Console.WriteLine($"\t({DateTime.Now:HH:mm:ss.fffff}) Resetting timer");
                _timer.Interval = timerInterval;
            }
            else
            {
                Console.WriteLine($"\t({DateTime.Now:HH:mm:ss.fffff}) Creating timer");
                _timer = new Timer(timerInterval);
                _timer.Elapsed += async (sender, args) =>
                {
                    Console.WriteLine($"({DateTime.Now:HH:mm:ss.fffff}) Really uploading with path '{_path}'");
                    var data = Parser.LoadData(_path);
                    var stringData = Parser.DebugTables(data);
                    await Uploader.Upload(stringData);
                };
                _timer.AutoReset = false;
                _timer.Start();
            }
        }

        public static bool IsServiceInstalled()
        {
            return ServiceController.GetServices().Any(s => s.ServiceName == "ScheduleWatcher");
        }

        public static bool IsServiceRunning()
        {
            return IsServiceInstalled() &&
                   new ServiceController("ScheduleWatcher").Status == ServiceControllerStatus.Running;
        }

        public static void InstallService()
        {
            if (IsServiceInstalled())
                return;
            var installer = new AssemblyInstaller(
                Assembly.GetExecutingAssembly().Location,
                null
            )
            {
                UseNewContext = true
            };
            installer.Install(null);
            installer.Commit(null);
        }

        public static void UninstallService()
        {
            if (!IsServiceInstalled())
                return;

            var installer = new AssemblyInstaller(
                Assembly.GetExecutingAssembly().Location,
                null
            )
            {
                UseNewContext = true
            };
            installer.Uninstall(null);
        }

        public void RunService()
        {
            var controller = new ServiceController(ServiceName);
            if (controller.Status == ServiceControllerStatus.Stopped)
                controller.Start();
        }

        public void StopService()
        {
            var controller = new ServiceController(ServiceName);
            if (controller.Status == ServiceControllerStatus.Running)
                controller.Stop();
        }

        private static void Main(string[] args)
        {
            var service = new WatcherService();
            if (Environment.UserInteractive)
            {
                service.OnStart(args);
                Console.WriteLine("Press any key to stop program");
                Console.Read();
                service.OnStop();
            }
            else
            {
                Run(service);
            }
        }
    }
}