using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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
                var sent = msg[0];
                var sender = msg[1];
                var body = msg[2];

                var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(sent)).ToLocalTime();
                var date = dateTime.ToString("u").Split(' ')[0];

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

        }
    }
}