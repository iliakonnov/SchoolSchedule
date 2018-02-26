using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TpsParse.Tps;

namespace DataLoader
{
    public static class IntExtension
    {
        public static int ToInt(this string value, int defaultValue = 0)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
    }

    public static class Parser
    {
        private static List<TRecord> LoadTps<TRecord>(
            string databasePath,
            string sourceTps,
            Func<string, object, Action<TRecord>> handler
        )
            where TRecord : new()
        {
            using (var logger = new StreamWriter("./log.log", true))
            using (var file = File.Open(
                Path.Combine(databasePath, sourceTps),
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read
            ))
            {
                var result = new List<TRecord>();
                // logger.WriteLine($"{sourceTps}");
                var tps = new TpsFile(file);
                var definition = tps.GetTableDefinitions()[0];
                var fields = definition.Fields.ToArray();
                foreach (var row in tps.GetDataRecords(definition))
                {
                    var i = 0;
                    var record = new TRecord();
                    foreach (var value in row.Values)
                    {
                        var field = fields[i++];
                        var name = field.FieldName.Substring(
                            field.FieldName.IndexOf(":", StringComparison.Ordinal) + 1
                        );

                        var encodedValue = Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(
                            value.ToString()
                        ));
                        //logger.WriteLine($"\t{name}: {encodedValue}; {field.Type};");

                        handler?.Invoke(name, value)(record);
                    }
                    result.Add(record);
                    //logger.WriteLine();
                }
                //logger.WriteLine($"{sourceTps}: {result.Count}");
                return result;
            }
        }

        private static List<ClassRecord> LoadClasses(string databasePath)
        {
            void Handler(string name, object value, ClassRecord record)
            {
                // Console.WriteLine($"Class: {name}; {value}");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (name)
                {
                    case "GROUPCODE":
                        record.Id = (short) value;
                        break;
                    /*
                    case "COURSE":
                        record.ClassNum = (int) value;
                        break;
                    case "LITERAL":
                        record.ClassLetter = (string) value;
                        break;
                    */
                    case "GROUPID":
                        var classSplitted = Encoding.GetEncoding(1251).GetString((byte[]) value).Split('-');
                        record.ClassNum = int.Parse(classSplitted[0].Trim(' '));
                        record.ClassLetter = classSplitted[1].Trim(' ');
                        break;
                }
            }

            return LoadTps<ClassRecord>(databasePath, "Group.tps", (n, v) => (r => Handler(n, v, r)));
        }

        private static List<TeacherRecord> LoadTeachers(string databasePath)
        {
            void Handler(string name, object value, TeacherRecord record)
            {
                // Console.WriteLine($"Teacher: {name}; {value}");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (name)
                {
                    case "TEACHERCODE":
                        record.Id = (short) value;
                        break;
                    case "TEACHERNAME":
                        record.Name = ((string) value).Trim(' ');
                        break;
                }
            }

            return LoadTps<TeacherRecord>(databasePath, "Teacher.tps", (n, v) => (r => Handler(n, v, r)));
        }

        private static List<TimetableRecord> LoadTimetable(string databasePath)
        {
            void Handler(string name, object value, TimetableRecord record)
            {
                // Console.WriteLine($"Timetable: {name}; {value}");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (name)
                {
                    case "LESSONNO":
                        record.LessonNo = int.Parse((string) value);
                        break;
                    case "BEGINING":
                        record.StartHours = (int) value / 100 / 3600;
                        record.StartMinutes = (int) value / 100 % 3600 / 60;
                        break;
                    case "ENDING":
                        record.EndHours = (int) value / 100 / 3600;
                        record.EndMinutes = (int) value / 100 % 3600 / 60;
                        break;
                }
            }

            return LoadTps<TimetableRecord>(databasePath, "LessonBell.tps", (n, v) => (r => Handler(n, v, r)));
        }

        private static List<ClassroomRecord> LoadClassrooms(string databasePath)
        {
            void Handler(string name, object value, ClassroomRecord record)
            {
                // Console.WriteLine($"Classroom: {name}; {value}");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (name)
                {
                    case "ROOMCODE":
                        record.Id = (int) value;
                        break;
                    case "ROOMNO":
                        record.Classroom = ((string) value).Trim(' ').Split('-')[0];
                        break;
                }
            }

            return LoadTps<ClassroomRecord>(databasePath, "Room.tps", (n, v) => (r => Handler(n, v, r)));
        }

        private static List<SubjectRecord> LoadSubjects(string databasePath)
        {
            void Handler(string name, object value, SubjectRecord record)
            {
                // Console.WriteLine($"Subject: {name}; {value}");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (name)
                {
                    case "SCIENCECODE":
                        record.Id = (short) value;
                        break;
                    case "SCIENCE":
                        record.Name = ((string) value).Trim(' ');
                        break;
                }
            }

            return LoadTps<SubjectRecord>(databasePath, "Science.tps", (n, v) => (r => Handler(n, v, r)));
        }

        private static List<ScheduleRecord> LoadSchedule(string databasePath, List<ClassRecord> classes,
            List<ClassroomRecord> classrooms)
        {
            void Handler(string name, object value, ScheduleRecord record)
            {
                // Console.WriteLine($"Schedule: {name}; {value}");
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (name)
                {
                    case "DAYSEQNUMBER":
                        record.DayOfWeek = (DaysOfWeek)(short) value;
                        break;
                    case "LESSONNO":
                        record.LessonNo = ((string) value).ToInt();
                        break;
                    case "SCIENCECODE":
                        record.SubjectId = (short) value;
                        break;
                    case "TEACHERCODE":
                        record.TeacherId = (short) value;
                        break;
                    case "GROUPCODE":
                        var matchClass = classes.Last(x => x.Id == (short) value);
                        record.ClassLetter = matchClass.ClassLetter;
                        record.ClassNum = matchClass.ClassNum;
                        break;
                    case "ROOMCODE":
                        var neededRoomId = (int) value;
                        var matchRooms = classrooms.Where(x => x.Id == neededRoomId).ToArray();
                        if (matchRooms.Length != 0)
                            record.Classroom = matchRooms[0].Classroom;
                        else
                            record.Classroom = "NOT FOUND!";
                        break;
                }
            }

            return LoadTps<ScheduleRecord>(databasePath, "Schedule.tps", (n, v) => (r => Handler(n, v, r)));
        }

        public static string DebugTables(Data data)
        {
            var text = new StringBuilder();
            text.AppendLine("[Format info]");
            text.AppendLine("# Debug section");
            text.AppendLine("Сначала идёт объявление секции. Например [Format info].");
            text.AppendLine(
                "В секции содержатся данные в формате csv и могут быть комментарии. Напрмер следующая строка -- комментарий");
            text.AppendLine("# Комментарии начинаются с решетки");
            text.AppendLine(
                "Секция обязательно завершается пустой строкой. В промежутке с конца секции и до начала следующей комментарии недопустимы.");
            text.AppendLine();

            text.Append(CreateTables(data));

            text.AppendLine("[Classrooms]");
            text.AppendLine("# Debug section");
            text.AppendLine("# Id; No");

            foreach (var classroom in data.Classrooms)
                text.Append(classroom.Id).Append(";")
                    .Append(classroom.Classroom).Append(";")
                    .AppendLine();
            text.AppendLine();

            return text.ToString();
        }

        public static string CreateTables(Data data)
        {
            var text = new StringBuilder();

            text.AppendLine("[Teachers]");
            text.AppendLine("# ID учителя; Имя учителя");
            data.Teachers.Sort();
            foreach (var teacher in data.Teachers)
                text.Append(teacher.Id).Append(";")
                    .Append(teacher.Name)
                    .AppendLine();
            text.AppendLine();

            text.AppendLine("[Subjects]");
            text.AppendLine("# ID предмета; Название предмета");
            data.Subjects.Sort();
            foreach (var subject in data.Subjects)
                text.Append(subject.Id).Append(";")
                    .Append(subject.Name)
                    .AppendLine();
            text.AppendLine();

            text.AppendLine("[Timetable]");
            text.AppendLine("# Номер урока; Часы начала; Минуты начала; Часы конца; Минуты конца");
            data.Timetable.Sort();
            foreach (var time in data.Timetable)
                text.Append(time.LessonNo).Append(';')
                    .Append(time.StartHours).Append(';')
                    .Append(time.StartMinutes).Append(';')
                    .Append(time.EndHours).Append(';')
                    .Append(time.EndMinutes).Append(';')
                    .AppendLine();
            text.AppendLine();

            text.AppendLine("[Schedule]");
            text.AppendLine(
                "# День недели; Номер урока; Цифра класса; Буква класса; ID учителя; ID предмета; Кабинет");
            var teachersToSubjects = new Dictionary<int, HashSet<int>>();
            data.ScheduleTemplate.Sort();
            foreach (var lesson in data.ScheduleTemplate)
            {
                if (!teachersToSubjects.ContainsKey(lesson.SubjectId))
                    teachersToSubjects[lesson.SubjectId] = new HashSet<int> {lesson.TeacherId};
                else
                    teachersToSubjects[lesson.SubjectId].Add(lesson.TeacherId);

                text.Append((int)lesson.DayOfWeek).Append(";")
                    .Append(lesson.LessonNo).Append(";")
                    .Append(lesson.ClassNum).Append(";")
                    .Append(lesson.ClassLetter).Append(";")
                    .Append(lesson.TeacherId).Append(";")
                    .Append(lesson.SubjectId).Append(";")
                    .Append(lesson.Classroom).Append(";")
                    .AppendLine();
            }
            text.AppendLine();

            text.AppendLine("[Groups]");
            text.AppendLine("# ID предмета; ID учителей (несколько)");
            foreach (var item in teachersToSubjects.OrderBy(
                item =>
                {
                    var result = new List<int> {item.Key};
                    result.AddRange(item.Value.OrderBy(i => i));
                    return result;
                },
                new ListComparer()
            ))
                text.Append(item.Key).Append(";")
                    .Append(string.Join(";", item.Value))
                    .AppendLine();
            text.AppendLine();

            /*text.AppendLine("[Replaces]");
            text.AppendLine(
                "#" +
                " Цифра класса; Буква класса;" +
                " Пред. номер урока; Пред. Id учителя; Пред. Id предмета; Пред. кабинет" +
                " Новый номер урока; Новый Id учителя; Новый Id предмета; Новый кабинет");
            data.Replaces.Sort();
            foreach (var replace in data.Replaces)
                text.Append(replace.ClassNum).Append(';')
                    .Append(replace.ClassLetter).Append(';')
                    .Append(replace.OldLessonNo).Append(';')
                    .Append(replace.OldTeacherId).Append(';')
                    .Append(replace.OldSubjectId).Append(';')
                    .Append(replace.OldClassroom).Append(';')
                    .Append(replace.NewLessonNo).Append(';')
                    .Append(replace.NewTeacherId).Append(';')
                    .Append(replace.NewSubjectId).Append(';')
                    .Append(replace.NewClassroom).Append(';')
                    .AppendLine();
            text.AppendLine();*/

            text.AppendLine("[Version]");
            var schedule = text.ToString();
            var md5 = System.Security.Cryptography.MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(schedule));
            var sha1 = System.Security.Cryptography.SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(schedule));

            text.AppendLine("# Время генерации расписания; md5 расписания; sha1 расписания");
            text.Append(DateTime.Now.ToString("s")).Append(";")
                .Append(BitConverter.ToString(md5).Replace("-", "")).Append(";")
                .Append(BitConverter.ToString(sha1).Replace("-", "")).Append(";")
                .AppendLine();
            text.AppendLine();

            return text.ToString();
        }

        public static Data LoadTables(string tables)
        {
            var result = new Data();
            string currentSection = null;
            var data = new List<string>();
            foreach (var row in tables.Split('\n'))
            {
                if (row.Contains("[") && row.Contains("]"))
                {
                    currentSection = row;
                    continue;
                }
                if (string.IsNullOrWhiteSpace(row) || string.IsNullOrEmpty(row))
                {
                    switch (currentSection)
                    {
                        case "[Teachers]":
                            result.Teachers = data.Select(r => r.Split(';')).Select(r => new TeacherRecord
                            {
                                Id = int.Parse(r[0]),
                                Name = r[1]
                            }).ToList();
                            break;
                        case "[Subjects]":
                            result.Subjects = data.Select(r => r.Split(';')).Select(r => new SubjectRecord
                            {
                                Id = int.Parse(r[0]),
                                Name = r[1]
                            }).ToList();
                            break;
                        case "[Timetable]":
                            result.Timetable = data.Select(r => r.Split(';')).Select(r => new TimetableRecord
                            {
                                LessonNo = int.Parse(r[0]),
                                StartHours = int.Parse(r[1]),
                                StartMinutes = int.Parse(r[2]),
                                EndHours = int.Parse(r[3]),
                                EndMinutes = int.Parse(r[4])
                            }).ToList();
                            break;
                        case "[Schedule]":
                            result.ScheduleTemplate = data.Select(r => r.Split(';')).Select(r => new ScheduleRecord
                            {
                                DayOfWeek = (DaysOfWeek)int.Parse(r[0]),
                                LessonNo = int.Parse(r[1]),
                                ClassNum = int.Parse(r[2]),
                                ClassLetter = r[3],
                                TeacherId = int.Parse(r[4]),
                                SubjectId = int.Parse(r[5]),
                                Classroom = r[6]
                            }).ToList();
                            break;
                        default:
                            break;
                    }
                    data = new List<string>();
                    continue;
                }
                data.Add(row);
            }
            return result;
        }

        public static Data LoadData(string databasePath)
        {
            var data = new Data();

            data.Classrooms = LoadClassrooms(databasePath);
            data.Classes = LoadClasses(databasePath);
            data.Teachers = LoadTeachers(databasePath);
            data.Subjects = LoadSubjects(databasePath);
            data.Timetable = LoadTimetable(databasePath);
            data.ScheduleTemplate = LoadSchedule(databasePath, data.Classes, data.Classrooms)
                .Where(x => x.DayOfWeek != 0)
                .ToList();
            data.Replaces = LoadReplaces();
            return data;
        }

        private static List<ReplaceRecord> LoadReplaces()
        {
            return new List<ReplaceRecord>();
        }

        public class ClassRecord : IComparable<ClassRecord>
        {
            public string ClassLetter;
            public int ClassNum;
            public int Id;

            public int CompareTo(ClassRecord other)
            {
                return Id.CompareTo(other.Id);
            }

            public bool Equals(ClassRecord other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((ClassRecord)obj);
            }
        }

        public class TeacherRecord : IComparable<TeacherRecord>
        {
            public int Id;
            public string Name;

            public int CompareTo(TeacherRecord other)
            {
                return Id.CompareTo(other.Id);
            }

            public bool Equals(TeacherRecord other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((TeacherRecord)obj);
            }
        }

        public class TimetableRecord : IComparable<TimetableRecord>
        {
            public int EndHours;
            public int EndMinutes;
            public int LessonNo;
            public int StartHours;
            public int StartMinutes;

            public int CompareTo(TimetableRecord other)
            {
                var a = StartHours * 60 + StartMinutes;
                var b = other.StartHours * 60 + other.StartMinutes;

                var first = a.CompareTo(b);
                if (first == 0)
                    return (EndHours * 60 + EndMinutes - a).CompareTo(other.EndHours * 60 + other.EndMinutes - b);
                return first;
            }

            public bool Equals(TimetableRecord other)
            {
                return EndHours * 60 == other.EndHours && EndMinutes == other.EndMinutes && StartHours == other.StartHours && StartMinutes == other.StartMinutes;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((TimetableRecord)obj);
            }
        }

        public class ClassroomRecord : IComparable<ClassroomRecord>
        {
            public string Classroom;
            public int Id;

            public int CompareTo(ClassroomRecord other)
            {
                return Id.CompareTo(other.Id);
            }

            public bool Equals(ClassroomRecord other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((ClassroomRecord)obj);
            }
        }

        public class SubjectRecord : IComparable<SubjectRecord>
        {
            public int Id;
            public string Name;

            public int CompareTo(SubjectRecord other)
            {
                return Id.CompareTo(other.Id);
            }

            public bool Equals(SubjectRecord other)
            {
                return Id == other.Id;
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((SubjectRecord)obj);
            }
        }

        public class ScheduleRecord : IComparable<ScheduleRecord>
        {
            public string ClassLetter;
            public int ClassNum;
            public string Classroom;
            public DaysOfWeek DayOfWeek;
            public int IsReplace = 0; // bool (0, 1)
            public int LessonNo;
            public int SubjectId;
            public int TeacherId;

            public int CompareTo(ScheduleRecord other)
            {
                if ((int)DayOfWeek < (int)other.DayOfWeek)
                    return -1;
                if ((int)DayOfWeek > (int)other.DayOfWeek)
                    return 1;

                if (LessonNo < other.LessonNo)
                    return -1;
                if (LessonNo > other.LessonNo)
                    return 1;

                if (TeacherId < other.TeacherId)
                    return -1;
                if (TeacherId > other.TeacherId)
                    return 1;

                return 0;
            }

            public bool Equals(ScheduleRecord other)
            {
                return
                    DayOfWeek == other.DayOfWeek &&
                    LessonNo == other.LessonNo &&
                    TeacherId == other.TeacherId;  // || ClassNum == other.ClassNum && ClassLetter == other.ClassLetter
            }

            public override bool Equals(object obj)
            {
                if (obj == null || GetType() != obj.GetType())
                    return false;
                return Equals((ScheduleRecord)obj);
            }
        }

        public class Data
        {
            public List<ClassRecord> Classes;
            public List<ClassroomRecord> Classrooms;
            public List<ReplaceRecord> Replaces;
            public List<ScheduleRecord> ScheduleTemplate;
            public List<SubjectRecord> Subjects;
            public List<TeacherRecord> Teachers;
            public List<TimetableRecord> Timetable;

            public bool Equals(Data obj)
            {
                return
                    Classes.OrderBy(x => x).SequenceEqual(obj.Classes.OrderBy(x => x)) &&
                    Classrooms.OrderBy(x => x).SequenceEqual(obj.Classrooms.OrderBy(x => x)) &&
                    Replaces.OrderBy(x => x).SequenceEqual(obj.Replaces.OrderBy(x => x)) &&
                    ScheduleTemplate.OrderBy(x => x).SequenceEqual(obj.ScheduleTemplate.OrderBy(x => x)) &&
                    Subjects.OrderBy(x => x).SequenceEqual(obj.Subjects.OrderBy(x => x)) &&
                    Teachers.OrderBy(x => x).SequenceEqual(obj.Teachers.OrderBy(x => x)) &&
                    Timetable.OrderBy(x => x).SequenceEqual(obj.Timetable.OrderBy(x => x));
            }

            public override bool Equals(object obj)
            {
                if (obj == null || obj.GetType() != GetType())
                {
                    return false;
                }
                return Equals((Data) obj);
            }
        }

        public class ReplaceRecord
        {
            public int ClassNum;
            public string ClassLetter;
            public string NewClassroom;
            public int NewLessonNo;
            public int NewSubjectId;
            public int NewTeacherId;
            public string OldClassroom;
            public int OldLessonNo;
            public int OldSubjectId;
            public int OldTeacherId;
        }

        private class ListComparer : IComparer<IList<int>>
        {
            public int Compare(IList<int> x, IList<int> y)
            {
                if (x == null || y == null)
                    throw new ArgumentException();

                var byLength = x.Count.CompareTo(y.Count);
                return byLength != 0
                    ? byLength
                    : x.Select((t, i) => t.CompareTo(y[i])).FirstOrDefault(compare => compare != 0);
            }
        }
    }
}