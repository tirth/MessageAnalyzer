using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;

namespace MessageAnalyzer
{
    public class Stuff
    {
        public const char Sepr = '|';

        public static void SortByTime(string convoName)
        {
            var messages = File.ReadAllLines(convoName + @".txt");

            Array.Sort(messages,
                (msg1, msg2) => string.CompareOrdinal(msg1.Split(Sepr)[0], msg2.Split(Sepr)[0]));

            File.WriteAllLines(convoName + @".txt", messages);
        }

        public static Tuple<Dictionary<string, Dictionary<string, int>>, Dictionary<string, int>> ConvoFrequencyByDate(
            string convoName, bool graph = false, bool byLen = true, int[] fromDate = null, int[] toDate = null)
        {
            var messages = File.ReadAllLines(convoName + @".txt");
            var people = new Dictionary<string, int>(2);
            var freq = new Dictionary<string, Dictionary<string, int>>();

            foreach (var msg in messages.Select(message => message.Split(Sepr)))
            {
                var ts = Convert.ToInt64(msg[0]);
                var sender = msg[1];
                var body = msg[2];

                var date = DateTimeOffset.FromUnixTimeMilliseconds(ts).ToLocalTime().ToString("yyyy-MM-dd");

                if (!people.ContainsKey(sender))
                    people[sender] = 0;

                if (!freq.ContainsKey(date))
                    freq[date] = new Dictionary<string, int>();

                if (!freq[date].ContainsKey(sender))
                    freq[date][sender] = 0;

                // count by message length or number of messages
                freq[date][sender] += byLen ? body.Length : 1;
                people[sender] += byLen ? body.Length : 1;
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
                foreach (var person in people.Keys.Where(person => !day.ContainsKey(person)))
                    day[person] = 0;

            if (graph)
                GraphFrequency(fullFreq, people.Keys.ToList(), convoName);

            return new Tuple<Dictionary<string, Dictionary<string, int>>, Dictionary<string, int>>(fullFreq, people);
        }

        private static void GraphFrequency(Dictionary<string, Dictionary<string, int>> freq,
            List<string> people, string convoName)
        {
            people.Sort();

            var chartData = new Dictionary<string, ArrayList> {["day"] = new ArrayList(freq.Keys.Count)};

            // prepare data dictionary
            foreach (var person in people)
                chartData[person] = new ArrayList();

            // fill dictionaries with column data
            foreach (var day in freq.Keys)
            {
                chartData["day"].Add(day);

                foreach (var person in freq[day].Keys)
                    chartData[person].Add(freq[day][person]);
            }

            // prepare worksheet
            var newFile = new FileInfo(convoName + @".xlsx");

            if (newFile.Exists)
            {
                newFile.Delete();
                newFile = new FileInfo(convoName + @".xlsx");
            }

            using (var package = new ExcelPackage(newFile))
            {
                var worksheet = package.Workbook.Worksheets.Add("Conversations");

                worksheet.Cells[1, 1].Value = "Date";  // write dates
                for (var row = 2; row < chartData["day"].Count + 2; row++)
                    worksheet.Cells[row, 1].Value = chartData["day"][row - 2];

                // write data (starts at 2 because of headings)
                for (var col = 2; col < people.Count + 2; col++)
                {
                    // participant names
                    worksheet.Cells[1, col].Value = people[col - 2];

                    // message data
                    for (var row = 2; row < chartData[people[col - 2]].Count + 2; row++)
                        worksheet.Cells[row, col].Value = chartData[people[col - 2]][row - 2];
                }

                // prepare chart
                var chart = (worksheet.Drawings.AddChart("LineChart", eChartType.Line));

                // assign series
                var dataLength = (chartData["day"].Count + 1).ToString();
                for (var i = 66; i < people.Count + 66; i++)
                    chart.Series.Add((char) i + "2:" + (char) i + dataLength, "A2:A" + dataLength)
                        .HeaderAddress = worksheet.Cells[(char) i + "1"];

                // chart formatting
                chart.SetSize(400);
                chart.Title.Text = "Conversation frequency with " + convoName;
                chart.XAxis.Title.Text = "Date";
                chart.YAxis.Title.Text = "Messages";

                chart.Style = eChartStyle.Style4;
                
                package.Save();
            }
        }
    }
}