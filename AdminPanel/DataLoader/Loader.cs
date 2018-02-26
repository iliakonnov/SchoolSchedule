using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataLoader
{
    public static class Loader
    {
        public static async Task<string> LoadData()
        {
            var client = new HttpClient();
            return await client.GetStringAsync(
                $"https://bitbucket.org/{RepoConstants.RepoName}/raw/HEAD/{RepoConstants.DataFilename}");
        }

        public static Parser.Data ParseData(string data)
        {
            var result = new Parser.Data();
            var reg = new Regex(@"^\[(.*)\]$");
            var section = "";

            foreach (var line in data.Split('\n'))
            {
                if (line.StartsWith("#"))
                    continue;

                var groupMatch = reg.Match(line);
                if (groupMatch.Success)
                {
                    section = groupMatch.Groups[1].Value.ToLower();
                    continue;
                }

                var splitted = line.Split(';');
                // ReSharper disable once SwitchStatementMissingSomeCases
                switch (section)
                {
                    case "teachers":
                        result.Teachers.Add(new Parser.TeacherRecord
                        {
                            Id = int.Parse(splitted[0]),
                            Name = splitted[1]
                        });
                        break;
                    case "subjects":
                        result.Subjects.Add(new Parser.SubjectRecord
                        {
                            Id = int.Parse(splitted[0]),
                            Name = splitted[1]
                        });
                        break;
                    case "schedule":
                        result.ScheduleTemplate.Add(new Parser.ScheduleRecord
                        {
                            DayOfWeek = (DaysOfWeek)int.Parse(splitted[0]),
                            LessonNo = int.Parse(splitted[1]),
                            ClassNum = int.Parse(splitted[2]),
                            ClassLetter = splitted[3],
                            TeacherId = int.Parse(splitted[4]),
                            SubjectId = int.Parse(splitted[5]),
                            Classroom = splitted[6],
                            IsReplace = int.Parse(splitted[0])
                        });
                        break;
                    case "groups":
                        break;
                    case "timetable":
                        result.Timetable.Add(new Parser.TimetableRecord
                        {
                            LessonNo = int.Parse(splitted[0]),
                            StartHours = int.Parse(splitted[1]),
                            StartMinutes = int.Parse(splitted[2]),
                            EndHours = int.Parse(splitted[3]),
                            EndMinutes = int.Parse(splitted[4])
                        });
                        break;
                    case "replaces":
                        result.Replaces.Add(new Parser.ReplaceRecord
                        {
                            ClassNum = int.Parse(splitted[0]),
                            ClassLetter = splitted[1],
                            OldLessonNo = int.Parse(splitted[2]),
                            OldTeacherId = int.Parse(splitted[3]),
                            OldSubjectId = int.Parse(splitted[4]),
                            OldClassroom = splitted[5],
                            NewLessonNo = int.Parse(splitted[6]),
                            NewTeacherId = int.Parse(splitted[7]),
                            NewSubjectId = int.Parse(splitted[8]),
                            NewClassroom = splitted[9]
                        });
                        break;
                }
            }

            return result;
        }
    }
}