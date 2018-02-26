using System;
using System.Collections.Generic;
using System.Linq;
using DataLoader;

namespace GUI.Replaces
{
    public class DataHelper
    {
        public readonly Dictionary<int, string> Teachers;
        public readonly Dictionary<int, string> Subjects;
        public readonly Dictionary<string, int> ReverseTeachers;
        public readonly Dictionary<string, int> ReverseSubjects;
        public readonly List<string> Classrooms;
        public readonly Parser.Data Data;
        public readonly DaysOfWeek DayOfWeek;

        public DataHelper(Parser.Data data)
        {
            Teachers = data.Teachers.ToDictionary(x => x.Id, x => x.Name);
            Subjects = data.Subjects.ToDictionary(x => x.Id, x => x.Name);
            ReverseTeachers = data.Teachers.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().Id);
            ReverseSubjects = data.Subjects.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First().Id);
            Classrooms = data.Classrooms.Select(x => x.Classroom).ToList();
            Data = data;
#if DEBUG
            DayOfWeek = DaysOfWeek.Вторник;
#else
            DayOfWeek = DaysOfWeekConverter.Convert(DateTime.Now.DayOfWeek);
#endif
        }


        public Replace[] Load(int teacherId)
        {
            return Data.Replaces.Where(r => r.OldTeacherId == teacherId)
                .Select(replaceRecord => new Replace
                {
                    BeforeLesson = new Lesson(DayOfWeek)
                    {
                        Subject = new Subject(replaceRecord.OldSubjectId),
                        Teacher = new Teacher(replaceRecord.OldTeacherId),
                        Classroom = new Classroom(replaceRecord.OldClassroom),
                        LessonNo = replaceRecord.OldLessonNo
                    },
                    AfterLesson = new Lesson(DayOfWeek)
                    {
                        Subject = new Subject(replaceRecord.NewSubjectId),
                        Teacher = new Teacher(replaceRecord.NewTeacherId),
                        Classroom = new Classroom(replaceRecord.NewClassroom),
                        LessonNo = replaceRecord.OldLessonNo
                    },
                    Class = new Class
                    {
                        ClassLetter = replaceRecord.ClassLetter,
                        ClassNum = replaceRecord.ClassNum
                    }
                })
                .OrderBy(x => x.BeforeLesson.LessonNo)
                .ToArray();
        }

        public Replace[] Generate(int teacherId, DaysOfWeek dayOfWeek)
        {
            var result = Data.ScheduleTemplate.Where(r => r.TeacherId == teacherId && r.DayOfWeek == dayOfWeek)
                .Select(scheduleRecord => new Replace
                {
                    BeforeLesson = new Lesson(DayOfWeek)
                    {
                        Subject = new Subject(scheduleRecord.SubjectId),
                        Teacher = new Teacher(scheduleRecord.TeacherId),
                        Classroom = new Classroom(scheduleRecord.Classroom),
                        LessonNo = scheduleRecord.LessonNo
                    },
                    AfterLesson = new Lesson(DayOfWeek)
                    {
                        Subject = new Subject(scheduleRecord.SubjectId),
                        Teacher = new Teacher(scheduleRecord.TeacherId),
                        Classroom = new Classroom(scheduleRecord.Classroom),
                        LessonNo = scheduleRecord.LessonNo
                    },
                    Class = new Class
                    {
                        ClassLetter = scheduleRecord.ClassLetter,
                        ClassNum = scheduleRecord.ClassNum
                    }
                })
                .OrderBy(x => x.BeforeLesson.LessonNo)
                .ToArray();
            return result;
        }
    }
}