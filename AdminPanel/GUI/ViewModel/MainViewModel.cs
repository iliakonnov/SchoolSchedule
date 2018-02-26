using System.Runtime.InteropServices;
using System.Windows.Markup;
using GalaSoft.MvvmLight;
using GUI.Replaces;
using GUI.Schedule;

namespace GUI.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// Use the <strong>mvvminpc</strong> snippet to add bindable properties to this ViewModel.
    /// </para>
    /// <para>
    /// You can also use Blend to data bind with the tool's support.
    /// </para>
    /// <para>
    /// See http://www.galasoft.ch/mvvm
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
            ////if (IsInDesignMode)
            ////{
            ////    // Code runs in Blend --> create design time data.
            ////}
            ////else
            ////{
            ////    // Code runs "for real"
            ////}
            Schedule = new ScheduleViewModel();
            var data = DataLoader.Parser.LoadData(Schedule.DatabasePath);
            Replaces = new ReplacesViewModel(data);
        }

        public ScheduleViewModel Schedule { get; }
        public ReplacesViewModel Replaces { get; }
    }
}