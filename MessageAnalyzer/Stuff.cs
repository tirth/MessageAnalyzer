using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace MessageAnalyzer
{
    public class Stuff
    {
        public const char Sepr = '|';

        public static void SortByTime(string convoName)
        {
            var messages = File.ReadAllLines(convoName + ".txt");

            Array.Sort(messages,
                (msg1, msg2) => String.CompareOrdinal(msg1.Split(Sepr)[0], msg2.Split(Sepr)[0]));

            File.WriteAllLines(convoName + ".txt", messages);
        }

        public static Tuple<Dictionary<string, Dictionary<string, int>>, List<string>> ConvoFrequencyByDate(
            string convoName, bool graph = false, bool byLen = true, int[] fromDate = null, int[] toDate = null)
        {
            var messages = File.ReadAllLines(convoName + ".txt");
            var people = new List<string>(2);
            var freq = new Dictionary<string, Dictionary<string, int>>();

            foreach (var msg in messages.Select(message => message.Split(Sepr)))
            {
                var ts = Convert.ToInt64(msg[0]);
                var sender = msg[1];
                var body = msg[2];

                var date = DateTimeOffset.FromUnixTimeMilliseconds(ts).ToLocalTime().ToString("yyyy-MM-dd");

                if (!people.Contains(sender))
                    people.Add(sender);

                if (!freq.ContainsKey(date))
                    freq[date] = new Dictionary<string, int>();

                if (!freq[date].ContainsKey(sender))
                    freq[date][sender] = 0;

                // count by message length or number of messages
                freq[date][sender] += byLen ? body.Length : 1;
            }

            // fill in empty days
            var fullFreq = new Dictionary<string, Dictionary<string, int>>();

            var days = freq.Keys.ToArray();
            Array.Sort(days);

            DateTime firstDay;
            DateTime lastDay;

            // figure out first day
            if (fromDate != null && fromDate.Length == 3)
                firstDay = new DateTime(fromDate[0], fromDate[1], fromDate[2]);
            else
            {
                var first = days[0].Split('-');
                firstDay = new DateTime(Convert.ToInt32(first[0]),
                    Convert.ToInt32(first[1]), Convert.ToInt32(first[2]));
            }

            // figure out last day
            if (toDate != null && toDate.Length == 3)
                lastDay = new DateTime(toDate[0], toDate[1], toDate[2]);
            else
            {
                var last = days[days.Length - 1].Split('-');
                lastDay = new DateTime(Convert.ToInt32(last[0]),
                    Convert.ToInt32(last[1]), Convert.ToInt32(last[2]));
            }

            var uno = new TimeSpan(1, 0, 0, 0);
            var totalDays = (lastDay - firstDay).Days;

            // go through all elapsed days, filling in data if present
            var currentDay = firstDay;
            for (var i = 0; i < totalDays + 1; i++)
            {
                var date = currentDay.ToString("u").Split(' ')[0];
                fullFreq[date] = freq.ContainsKey(date) ? freq[date] : new Dictionary<string, int>();
                currentDay += uno;
            }

            // fill in zeros
            foreach (var day in fullFreq.Values)
                foreach (var person in people.Where(person => !day.ContainsKey(person)))
                    day[person] = 0;

            if (graph)
                GraphFrequency(fullFreq, people, convoName);

            return new Tuple<Dictionary<string, Dictionary<string, int>>, List<string>>(fullFreq, people);
        }

        private static void GraphFrequency(Dictionary<string, Dictionary<string, int>> freq,
            List<string> people, string convoName)
        {
            people.Sort();

            var chartData = new Dictionary<string, ArrayList>();
            chartData["day"] = new ArrayList(freq.Keys.Count);

            var personCount = new Dictionary<string, int>();

            // prepare dictionaries
            foreach (var person in people)
            {
                chartData[person] = new ArrayList();
                personCount[person] = 0;
            }

            // fill dictionaries with column data
            foreach (var day in freq.Keys)
            {
                chartData["day"].Add(day);

                foreach (var person in freq[day].Keys)
                {
                    personCount[person] += freq[day][person];
                    chartData[person].Add(freq[day][person]);
                }
            }

            foreach (var day in chartData["me"])
            {
                Console.Out.WriteLine(day);
            }

            Console.ReadKey();

            // prepare chart
            var newFile = new FileInfo(convoName + ".xlsx");

            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(convoName + ".xlsx");
            }

            using (var package = new ExcelPackage(newFile))
            {
                var worksheet = package.Workbook.Worksheets.Add("Conversations");

                worksheet.Cells[1, 1].Value = "Date";  // write dates
                for (var row = 2; row < chartData["day"].Count + 2; row++)
                    worksheet.Cells[row, 1].Value = chartData["day"][row - 2];

                // write data
                for (var col = 2; col < people.Count + 2; col++)
                {
                    worksheet.Cells[1, col].Value = people[col - 2];
                    for (var row = 2; row < chartData[people[col - 2]].Count + 2; row++)
                        worksheet.Cells[row, col].Value = chartData[people[col - 2]][row - 2];
                }

                package.Save();
            }
        }
    }
}