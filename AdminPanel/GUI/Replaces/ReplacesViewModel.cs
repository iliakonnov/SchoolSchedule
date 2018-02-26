using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using DataLoader;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GUI.Replaces.EditDialog;

namespace GUI.Replaces
{
    public class ReplacesViewModel : INotifyPropertyChanged
    {
        private ReplaceItem _currentReplace;

        public ReplacesViewModel(Parser.Data data)
        {
            SimpleIoc.Default.Register(() => new DataHelper(data));

            var generator = SimpleIoc.Default.GetInstance<DataHelper>();

            Replaces = new ObservableCollection<ReplaceItem>(
                data.Replaces.Select(r => r.OldTeacherId)
                    .Distinct()
                    .Select(tId => new ReplaceItem
                    {
                        Teacher = new Teacher(tId),
                        Replaces = generator.Load(tId)
                    })
            );
            _teacherNames = generator.Teachers.Values.ToArray();
            _currentReplace = Replaces.FirstOrDefault();
            EditCommand = new RelayCommand(EditCurrent);
            NewCommand = new RelayCommand(NewReplace);
            SaveCommand = new RelayCommand(SaveWord);
        }

        public ICommand EditCommand { get; }
        public ICommand NewCommand { get; }
        public ICommand SaveCommand { get; }


        private string[] _teacherNames;
        public ObservableCollection<ReplaceItem> Replaces { get; }

        public void SaveWord()
        {
            var dialog = new SaveFileDialog
            {
                Filter = @"MS Word (*.docx)|*.docx",
                DefaultExt = "docx",
                AddExtension = true,
                FileName = $"Замены-{DateTime.Now:dd.MM.yyyy}.docx"
            };

            var result = dialog.ShowDialog();
            if (result == DialogResult.OK)
            {
                var path = Path.GetFullPath(dialog.FileName);
                ToWord.SaveReplaces(Replaces.Select(
                    replace => new ReplaceItem
                    {
                        Replaces = replace.Replaces.Where(r => r.IsEnabled).ToList(),
                        Teacher = replace.Teacher
                    }
                ).ToList(), path);
                ToWord.OpenWord(path);
            }
        }

        public ReplaceItem CurrentReplace
        {
            get => _currentReplace;
            set
            {
                if (_currentReplace == value) return;
                _currentReplace = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void NewReplace()
        {
            var dialogModel = new EditDialogViewModel(new ReplaceItem());
            var dialog = new EditDialogView {DataContext = dialogModel};
            var result = dialog.ShowDialog();
            if (!dialog.DialogResult.GetValueOrDefault(false)) return;

            var newReplace = dialogModel.GetResult();
            Replaces.Add(newReplace);
            CurrentReplace = newReplace;
        }

        private void EditCurrent()
        {
            var dialogModel = new EditDialogViewModel(CurrentReplace);
            var dialog = new EditDialogView {DataContext = dialogModel};
            var result = dialog.ShowDialog();
            if (!result.GetValueOrDefault(false)) return;

            var idx = Replaces.IndexOf(CurrentReplace);
            var newReplace = dialogModel.GetResult();
            Replaces[idx] = newReplace;
            CurrentReplace = newReplace;
        }
    }
}