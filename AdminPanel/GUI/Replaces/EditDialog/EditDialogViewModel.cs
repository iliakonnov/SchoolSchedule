﻿using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using DataLoader;
using GalaSoft.MvvmLight.Command;
using GalaSoft.MvvmLight.Ioc;
using GUI.Annotations;

namespace GUI.Replaces.EditDialog
{
    public class EditDialogViewModel : INotifyPropertyChanged
    {
        public EditDialogViewModel([NotNull] ReplaceItem replace)
        {
            Replace = replace;
            if (replace.Teacher != null)
            {
                CurrentTeacher = replace.Teacher;
            }
            OkCommand = new RelayCommand(() => { DialogResult = true; });
#if !DEBUG
            _dayOfWeek = DaysOfWeekConverter.Convert(DateTime.Now.DayOfWeek)
#else
            _dayOfWeek = DaysOfWeek.Вторник;
#endif
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            var filteredSchedule = generator.Data.ScheduleTemplate.Where(s => s.DayOfWeek == _dayOfWeek).ToArray();

            Teachers = new ObservableCollection<Teacher>(
                generator.ReverseTeachers.Select(kv => new Teacher(kv.Value, kv.Key)));
            Subjects = new ObservableCollection<Subject>(
                generator.ReverseSubjects.Select(kv => new Subject(kv.Value, kv.Key)));
            Classrooms =
                new ObservableCollection<Classroom>(generator.Classrooms.Select(name => new Classroom(name)));


            if (replace.Replaces == null) return;
            foreach (var replaceReplace in replace.Replaces)
            {
                replaceReplace.AfterLesson.Teacher.PropertyChanged += (s, e) => { UpdateWarnings(); };
            }
            UpdateWarnings();
        }

        public ReplaceItem Replace
        {
            get => _replace;
            set
            {
                if (_replace == value) return;
                _replace = value;
                RaisePropertyChanged();
            }
        }

        public ICommand OkCommand { get; }

        public ObservableCollection<Teacher> Teachers { get; }
        public ObservableCollection<Subject> Subjects { get; }
        public ObservableCollection<Classroom> Classrooms { get; }

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            set
            {
                if (_dialogResult == value) return;
                _dialogResult = value;
                RaisePropertyChanged();
            }
        }

        private Teacher _currentTeacher = new Teacher {Name = "Введите фамилию", IsWarning = true, Id = -1};
        private ReplaceItem _replace;

        public Teacher CurrentTeacher
        {
            get => _currentTeacher;
            set
            {
                if (_currentTeacher != null && _currentTeacher.Equals(value)) return;
                _currentTeacher = value;
                RaisePropertyChanged();
                UpdateLessons();
            }
        }

        private DaysOfWeek _dayOfWeek;

        public DaysOfWeek DayOfWeek
        {
            get => _dayOfWeek;
            set
            {
                if (_dayOfWeek == value) return;
                _dayOfWeek = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(DayOfWeekString));
                UpdateLessons();
            }
        }

        public string DayOfWeekString => _dayOfWeek.ToString();

        private Replace _currentRow;

        public Replace CurrentRow
        {
            get => _currentRow;
            set
            {
                if (_currentRow == value) return;
                _currentRow = value;
                RaisePropertyChanged();
                UpdateWarnings();
            }
        }

        private void UpdateWarnings()
        {
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            foreach (var teacher in Teachers)
            {
                teacher.SetWarning(CurrentRow.BeforeLesson.LessonNo, generator.DayOfWeek, CurrentRow.BeforeLesson.Teacher.Id);
            }
            foreach (var subject in Subjects)
            {
                subject.SetWarning(CurrentRow.AfterLesson.Teacher.Id);
            }
            foreach (var classroom in Classrooms)
            {
                classroom.SetWarning(CurrentRow.BeforeLesson.LessonNo, generator.DayOfWeek,
                    CurrentRow.BeforeLesson.Classroom.Room);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateLessons()
        {
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            if (CurrentTeacher == null || generator == null)
            {
                return;
            }

            Replace = new ReplaceItem
            {
                Teacher = CurrentTeacher,
                Replaces = generator.Generate(CurrentTeacher.Id, DayOfWeek)
            };
        }

        public ReplaceItem GetResult()
        {
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            // Convert names to ids
            var result = new ReplaceItem
            {
                Teacher = new Teacher(_replace.Teacher.Name)
                {
                    Name = _replace.Teacher.Name,
                    Id = generator.ReverseTeachers[_replace.Teacher.Name]
                },
                Replaces = _replace.Replaces.Select(r => new Replace
                {
                    AfterLesson = new Lesson(DayOfWeek)
                    {
                        Classroom = r.AfterLesson.Classroom,
                        LessonNo = r.AfterLesson.LessonNo,
                        Subject = new Subject(r.AfterLesson.Subject.Name),
                        Teacher = new Teacher(r.AfterLesson.Teacher.Name)
                    },
                    BeforeLesson = new Lesson(DayOfWeek)
                    {
                        Classroom = r.BeforeLesson.Classroom,
                        LessonNo = r.BeforeLesson.LessonNo,
                        Subject = new Subject(r.BeforeLesson.Subject.Name),
                        Teacher = new Teacher(r.BeforeLesson.Teacher.Name)
                    },
                    Class = r.Class,
                    IsEnabled = r.IsEnabled
                }).ToList()
            };
            return result;
        }
    }
}