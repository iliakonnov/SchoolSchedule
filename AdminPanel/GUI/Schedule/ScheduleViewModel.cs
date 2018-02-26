﻿using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DataLoader;
using GalaSoft.MvvmLight.Command;
using ScheduleWatcherService;

namespace GUI.Schedule
{
    public class ScheduleViewModel : INotifyPropertyChanged
    {
        public string DbPath;

        private Visibility _progressBarVisiblity = Visibility.Collapsed;
        private WatcherService _watcher = new WatcherService();
        private bool _watcherRunning;
        private bool _watcherRegistered;

        public ScheduleViewModel()
        {
            DbPath = @"C:\Users\Ilia\Documents\SchoolSchedule\DataBase";
            _watcherRunning = WatcherService.IsServiceRunning();

            SaveCommand = new RelayCommand(Save);
            UploadCommand = new RelayCommand(Upload);
            RegisterWatcherCommand = new RelayCommand(RegisterWatcher);
            UnregisterWatcherCommand = new RelayCommand(UnregisterWatcher);
            StartWatcherCommand = new RelayCommand(StartWatcher);
            StopWatcherCommand = new RelayCommand(StopWatcher, () => _watcherRunning && _watcherRegistered);

            if (File.Exists(WatcherService.DbConfPath))
                DbPath = File.ReadAllText(WatcherService.DbConfPath);
            else
                File.WriteAllText(WatcherService.DbConfPath, DbPath);

            UpdateWatcherButtons();
        }

        public ICommand SaveCommand { get; }
        public ICommand UploadCommand { get; }
        
        public RelayCommand RegisterWatcherCommand { get; }
        public bool RegisterWatcherEnabled => !_watcherRegistered;
        
        public RelayCommand UnregisterWatcherCommand { get; }
        public bool UnregisterWatcherEnabled => _watcherRegistered;
        
        public RelayCommand StartWatcherCommand { get; }
        public bool StartWatcherEnabled => !_watcherRunning && _watcherRegistered;
        
        public RelayCommand StopWatcherCommand { get; }
        public bool StopWatcherEnabled => _watcherRunning && _watcherRegistered;
        
        public string InstalledText => _watcherRegistered ? "Сервис установлен" : "Сервис не установлен";
        public Brush InstalledColor => _watcherRegistered ? Brushes.GreenYellow : Brushes.OrangeRed;
        
        public string RunningText => _watcherRunning ? "Сервис работает" : "Сервис остановлен";
        public Brush RunningColor => _watcherRunning ? Brushes.GreenYellow : Brushes.OrangeRed;

        public string DatabasePath
        {
            get => DbPath;
            set
            {
                if (DbPath == value)
                    return;

                DbPath = value;
                RaisePropertyChanged();
            }
        }

        public Visibility ProgressBarVisiblity
        {
            get => _progressBarVisiblity;
            set
            {
                if (_progressBarVisiblity == value)
                    return;

                _progressBarVisiblity = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateWatcherButtons()
        {
            var watcherRunning = WatcherService.IsServiceRunning() && _watcher != null;
            var watcherRegistered = WatcherService.IsServiceInstalled();
            var changed = false;

            if (watcherRunning != _watcherRunning)
            {
                RaisePropertyChanged(nameof(RunningText));
                RaisePropertyChanged(nameof(RunningColor));
                _watcherRunning = watcherRunning;
                changed = true;
            }

            if (watcherRegistered != _watcherRegistered)
            {
                RaisePropertyChanged(nameof(RegisterWatcherEnabled));
                RaisePropertyChanged(nameof(UnregisterWatcherEnabled));
                RaisePropertyChanged(nameof(InstalledText));
                RaisePropertyChanged(nameof(InstalledColor));
                _watcherRegistered = watcherRegistered;
                changed = true;
            }

            if (changed)
            {
                RaisePropertyChanged(nameof(StartWatcherEnabled));
                RaisePropertyChanged(nameof(StopWatcherEnabled));
            }
        }

        private async void Save()
        {
            ProgressBarVisiblity = Visibility.Visible;
            await Task.Run(() =>
            {
                File.WriteAllText(WatcherService.DbConfPath, DbPath);
                UpdateWatcherButtons();
                Thread.Sleep(2000);
                UpdateWatcherButtons();
            });
            ProgressBarVisiblity = Visibility.Collapsed;
        }

        private async void RegisterWatcher()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("Для установки сервиса требуются права администратора");
                return;
            }

            ProgressBarVisiblity = Visibility.Visible;
            await Task.Run(() =>
            {
                WatcherService.InstallService();
                UpdateWatcherButtons();
                Thread.Sleep(2000);
                UpdateWatcherButtons();
            });
            ProgressBarVisiblity = Visibility.Collapsed;
        }

        private async void UnregisterWatcher()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("Для удаления сервиса требуются права администратора");
                return;
            }

            ProgressBarVisiblity = Visibility.Visible;
            await Task.Run(() =>
            {
                WatcherService.UninstallService();
                UpdateWatcherButtons();
                Thread.Sleep(2000);
                UpdateWatcherButtons();
            });
            ProgressBarVisiblity = Visibility.Collapsed;
        }

        private async void StopWatcher()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("Для остановки сервиса требуются права администратора");
                return;
            }

            ProgressBarVisiblity = Visibility.Visible;
            await Task.Run(() =>
            {
                _watcher.StopService();
                UpdateWatcherButtons();
                Thread.Sleep(2000);
                UpdateWatcherButtons();
            });
            ProgressBarVisiblity = Visibility.Collapsed;
        }

        private async void StartWatcher()
        {
            if (!new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator))
            {
                MessageBox.Show("Для запуска сервиса требуются права администратора");
                return;
            }

            ProgressBarVisiblity = Visibility.Visible;
            await Task.Run(() =>
            {
                _watcher.RunService();
                UpdateWatcherButtons();
                Thread.Sleep(2000);
                UpdateWatcherButtons();
            });
            ProgressBarVisiblity = Visibility.Collapsed;
        }

        private async void Upload()
        {
            ProgressBarVisiblity = Visibility.Visible;
            var data = Parser.LoadData(DatabasePath);
            var textData = Parser.DebugTables(data);
            await Uploader.Upload(textData);
            ProgressBarVisiblity = Visibility.Collapsed;
        }
    }
}