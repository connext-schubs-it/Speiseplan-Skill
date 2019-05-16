using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Essensplan.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Sagt ein Datum in folgender Form: Tag. Monat --> z.B.: 1. Mai
        /// </summary>
        /// <param name="date">Das Datum, dass entsprechend gesagt werden soll.</param>
        /// <returns></returns>
        public static string SayAsDate(this DateTime date)
        {
            return $"<say-as interpret-as=\"date\">????{date.ToString("MMdd")}</say-as>";
        }

        /// <summary>
        /// Sagt ein Datum in folgende Form: Tag Monat Jahr --> z.B.: 1. Mai 2019
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static string SayAsDateYear(this DateTime date)
        {
            return $"<say-as interpret-as=\"date\">{date.ToShortDateString()}</say-as>";
        }

        /// <summary>
        /// Ermittelt die Woche, in der das Datum liegt.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static int GetWeekOfYear(this DateTime date)
        {
            var cInfo = new CultureInfo("de-DE");
            var kalender = cInfo.Calendar;
            var weekRule = cInfo.DateTimeFormat.CalendarWeekRule;
            var dayOfWeek = cInfo.DateTimeFormat.FirstDayOfWeek;            

            return kalender.GetWeekOfYear(date, weekRule, dayOfWeek);
        }

        /// <summary>
        /// Ermittelt die Anazahl an Wochen, die ein Jahr enthält.
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static int GetNumberOfWeeks(this DateTime dateTime)
        {
            var cInfo = new CultureInfo("de-DE");
            var kalender = cInfo.Calendar;
            var weekRule = cInfo.DateTimeFormat.CalendarWeekRule;
            var dayOfWeek = cInfo.DateTimeFormat.FirstDayOfWeek;
            var date = new DateTime(dateTime.Year, 12, 31);

            return kalender.GetWeekOfYear(date, weekRule, dayOfWeek);
        }

        /// <summary>
        /// Gibt für Montag bis Freitag das jeweilige Datum an.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static List<DateTime> GetDaysOfWeek(this DateTime date)
        {            
            var currentDayOfWeek = (int)date.DayOfWeek;
            var sonntag = date.AddDays(-currentDayOfWeek);
            var montag = sonntag.AddDays(1);
            
            if (currentDayOfWeek == 0)            
                montag = montag.AddDays(-7);
            
            return Enumerable.Range(0, 5).Select(d => montag.AddDays(d)).ToList();
        }
    }
}
