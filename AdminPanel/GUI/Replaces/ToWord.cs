using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GUI.Replaces;
using Novacode;

namespace DataLoader
{
    public static class ToWord
    {
        private static void Replace(IEnumerable<Paragraph> paragraphs, Dictionary<string, string> replaces)
        {
            paragraphs.ToList().ForEach(p =>
                replaces.ToList().ForEach(r =>
                    p.ReplaceText(r.Key, r.Value)
                )
            );
        }
        
        public static void SaveReplaces(IList<ReplaceItem> replaceItems, string path)
        {
            const string templatePath = "template.docx";

            var now = DateTime.Now;
            var date = now.ToString("dd.MM.yyyy");
            var dayOfWeek = DaysOfWeekConverter.Convert(now.DayOfWeek);

            var textFormatting = new Formatting
            {
                Bold = true,
                FontFamily = new FontFamily("Times New Roman")
            };

            using (var doc = DocX.Load(templatePath))
            {
                Table tplTable = null;
                foreach (var t in doc.Tables)
                {
                    var row = t.Rows[0];
                    var paragraph = row.Paragraphs[0];
                    var text = paragraph.Text;
                    if (text.StartsWith("<comment>"))
                        t.Remove();
                    if (text.StartsWith("<template>"))
                    {
                        tplTable = t;
                        tplTable.RemoveRow(0);
                    }
                }
                if (tplTable == null)
                    return;
                
                Replace(doc.Paragraphs, new Dictionary<string, string>
                {
                    {"{date}", date},
                    {"{dayOfWeek}", dayOfWeek.ToString().ToLower()}
                });

                var teacherTpl = tplTable.Rows[0];
                var replaceTpl = tplTable.Rows[1];
                tplTable.InsertRow(0);  // Чтобы всегда была как минимум одна строка 
                teacherTpl.Remove();
                replaceTpl.Remove();
                
                foreach (var replaceItem in replaceItems)
                {
                    var nameRow = tplTable.InsertRow(teacherTpl);

                    Replace(nameRow.Paragraphs, new Dictionary<string, string>
                    {
                        {"{teacherName}", replaceItem.Teacher.Name}
                    });

                    foreach (var replace in replaceItem.Replaces)
                    {
                        var replaceRow = tplTable.InsertRow(replaceTpl);

                        Replace(replaceRow.Paragraphs, new Dictionary<string, string>
                        {
                            {"{lessonNo}", replace.BeforeLesson.LessonNo.ToString()},
                            {"{classNum}", replace.Class.ClassNum.ToString()},
                            {"{classLetter}", replace.Class.ClassLetter},
                            {"{newTeacherName}", replace.AfterLesson.Teacher.Name},
                            {"{classroom}", replace.AfterLesson.Classroom.Room},
                            {
                                "{newSubject}", replace.AfterLesson.Subject.Id != replace.BeforeLesson.Subject.Id
                                    ? $"({replace.AfterLesson.Subject.Name})"
                                    : ""
                            }
                        });
                    }
                }
                tplTable.RemoveRow(0);  // Удаляет первую вспомогательную строку

                // Save
                doc.SaveAs(path);
            }
        }

        public static void OpenWord(string path)
        {
            Process.Start(path);
        }
    }
}