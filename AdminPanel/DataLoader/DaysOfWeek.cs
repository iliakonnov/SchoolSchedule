using System;

namespace DataLoader
{
    public enum DaysOfWeek
    {
        Понедельник = 1,
        Вторник = 2,
        Среда = 3,
        Четверг = 4,
        Пятница = 5,
        Суббота = 6,
        Воскресенье = 7
    }

    public static class DaysOfWeekConverter
    {
        public static DaysOfWeek Convert(DayOfWeek systemType)
        {
            switch (systemType)
            {
                case DayOfWeek.Sunday:
                    return DaysOfWeek.Воскресенье;
                case DayOfWeek.Monday:
                    return DaysOfWeek.Понедельник;
                case DayOfWeek.Tuesday:
                    return DaysOfWeek.Вторник;
                case DayOfWeek.Wednesday:
                    return DaysOfWeek.Среда;
                case DayOfWeek.Thursday:
                    return DaysOfWeek.Четверг;
                case DayOfWeek.Friday:
                    return DaysOfWeek.Пятница;
                case DayOfWeek.Saturday:
                    return DaysOfWeek.Суббота;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}