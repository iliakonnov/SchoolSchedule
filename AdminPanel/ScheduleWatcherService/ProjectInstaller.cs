﻿using System.ComponentModel;
using System.Configuration.Install;

namespace ScheduleWatcherService
{
    [RunInstaller(true)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }
    }
}