using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Markup;
using DataLoader;
using GalaSoft.MvvmLight.Ioc;
using GUI.Annotations;

namespace GUI.Replaces
{
    public class ReplaceItem
    {
        public Teacher Teacher { get; set; }
        public IList<Replace> Replaces { get; set; }
    }

    public class Teacher : IEquatable<Teacher>, INotifyPropertyChanged
    {
        public Teacher()
        {
        }

        public Teacher(string name)
        {
            Name = name;
        }

        public Teacher(int id)
        {
            Id = id;
        }

        public Teacher(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetWarning(int lessonNo, DaysOfWeek dayOfWeek, int? prevTeacherId)
        {
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            IsWarning =
                Id != prevTeacherId &&
                generator.Data.ScheduleTemplate.Any(s =>
                    s.DayOfWeek == dayOfWeek && s.LessonNo == lessonNo && s.TeacherId == Id);
        }

        private bool _isWarning;

        public bool IsWarning
        {
            get => _isWarning;
            set
            {
                if (_isWarning == value) return;
                _isWarning = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                var generator = SimpleIoc.Default.GetInstance<DataHelper>();
                _name = value;
                RaisePropertyChanged();

                if (!generator.ReverseTeachers.TryGetValue(_name, out var id)) return;
                _id = id;
                RaisePropertyChanged(nameof(Id));
            }
        }

        private int _id;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                var generator = SimpleIoc.Default.GetInstance<DataHelper>();
                _id = value;
                RaisePropertyChanged();

                if (!generator.Teachers.TryGetValue(_id, out var name)) return;
                _name = name;
                RaisePropertyChanged(nameof(Name));
            }
        }

        public bool Equals(Teacher other)
        {
            return other != null && Name == other.Name && Id == other.Id;
        }

        public Teacher Clone()
        {
            return (Teacher) MemberwiseClone();
        }
    }

    public class Subject : IEquatable<Subject>, INotifyPropertyChanged
    {
        public Subject()
        {
        }

        public Subject(string name)
        {
            Name = name;
        }

        public Subject(int id)
        {
            Id = id;
        }

        public Subject(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public void SetWarning(int teacherId)
        {
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            IsWarning = !generator.Data.ScheduleTemplate.Any(s => s.SubjectId == Id && s.TeacherId == teacherId);
        }

        private bool _isWarning;

        public bool IsWarning
        {
            get => _isWarning;
            set
            {
                if (_isWarning == value) return;
                _isWarning = value;
                RaisePropertyChanged();
            }
        }

        private string _name;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                var generator = SimpleIoc.Default.GetInstance<DataHelper>();
                _name = value;

                if (!generator.ReverseTeachers.TryGetValue(_name, out var id)) return;
                _id = id;
                RaisePropertyChanged(nameof(Id));
            }
        }

        private int _id;

        public int Id
        {
            get => _id;
            set
            {
                if (_id == value) return;
                var generator = SimpleIoc.Default.GetInstance<DataHelper>();
                _id = value;
                RaisePropertyChanged();

                if (!generator.Subjects.TryGetValue(_id, out var name)) return;
                _name = name;
                RaisePropertyChanged(nameof(Name));
            }
        }

        public bool Equals(Subject other)
        {
            return other != null && Name == other.Name && Id == other.Id;
        }

        public Subject Clone()
        {
            return (Subject) MemberwiseClone();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Classroom : INotifyPropertyChanged
    {
        public Classroom()
        {
        }

        public Classroom(string room)
        {
            Room = room;
        }

        public void SetWarning(int lessonNo, DaysOfWeek dayOfWeek, string prevRoom)
        {
            var generator = SimpleIoc.Default.GetInstance<DataHelper>();
            IsWarning = generator.Data.ScheduleTemplate.Any(s =>
                s.DayOfWeek == dayOfWeek && s.LessonNo == lessonNo && s.Classroom != prevRoom && s.Classroom == Room);
        }

        private bool _isWarning;

        public bool IsWarning
        {
            get => _isWarning;
            set
            {
                if (_isWarning == value) return;
                _isWarning = value;
                RaisePropertyChanged();
            }
        }

        private string _room;

        public string Room
        {
            get => _room;
            set
            {
                if (_room == value) return;
                _room = value;
                RaisePropertyChanged();
            }
        }

        public Classroom Clone()
        {
            return (Classroom) MemberwiseClone();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Class : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public int ClassNum { get; set; }

        public string ClassLetter { get; set; }

        public string Name => $"{ClassNum}-{ClassLetter}";
    }

    public class Replace : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Class Class { get; set; }
        public Lesson BeforeLesson { get; set; }

        public Lesson AfterLesson { get; set; }

        public bool IsEnabled { get; set; } = true;

        [DependsOn(nameof(IsEnabled))]
        public bool IsDisabled
        {
            get => !IsEnabled;
            set => IsEnabled = !value;
        }
    }

    public class Lesson : INotifyPropertyChanged
    {
        private DaysOfWeek _dayOfWeek;

        public Lesson(DaysOfWeek dayOfWeek)
        {
            _dayOfWeek = dayOfWeek;
        }

        private int _lessonNo;

        public int LessonNo
        {
            get => _lessonNo;
            set
            {
                if (_lessonNo == value) return;
                _lessonNo = value;
                RaisePropertyChanged();
                // Teacher.SetWarning(LessonNo, _dayOfWeek, null);
            }
        }

        private Teacher _afterTeacher;

        public Teacher Teacher
        {
            get => _afterTeacher;
            set
            {
                if (value == null || _afterTeacher == value) return; // Not Equals because of Teacher.IsWarning
                _afterTeacher = value.Clone();
                RaisePropertyChanged();
                _afterTeacher.PropertyChanged += UpdateSubjectWarning;
            }
        }

        private void UpdateSubjectWarning(object sender, PropertyChangedEventArgs args)
        {
            if (args.PropertyName == nameof(Teacher.Id))
            {
                if (_afterTeacher != null)
                {
                    _subject?.SetWarning(_afterTeacher.Id);
                }
            }
        }

        private Subject _subject;

        public Subject Subject
        {
            get => _subject;
            set
            {
                if (value == null || _subject == value) return;
                _subject = value;
                RaisePropertyChanged();
            }
        }

        private Classroom _classroom;

        public Classroom Classroom
        {
            get => _classroom;
            set
            {
                if (value == null || _classroom == value) return;
                _classroom = value;
                RaisePropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}