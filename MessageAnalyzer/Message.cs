using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace MessageAnalyzer
{
    public enum ConvoTypes
    {
        Person,
        Group
    };

    public class Message : IComparable
    {
        public long Timestamp { get; set; }
        public DateTimeOffset Date { get; }
        public Contact Author { get; set; }
        public string Content { get; set; }
        public int Length => Content.Length;
        public string Source { get; set; }
        public string Location { get; set; }

        public Message(string timestamp, Contact author, string content, string source = null, string location = null)
        {
            Timestamp = long.Parse(timestamp);
            Date = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).ToLocalTime().Date;
            Author = author;
            Content = content.Replace('\n', ' ');
            Source = source ?? "Unknown source";
            Location = location ?? "No location";
        }

        public override string ToString()
        {
            return string.Join(Stuff.Sepr.ToString(), Timestamp, Author, Content, Source, Location);
        }

        public int CompareTo(object obj)
        {
            return Timestamp.CompareTo(((Message)obj).Timestamp);
        }
    }

    public class Contact
    {
        public string Name { get; set; }
        public ConvoTypes Type { get; set; }

        public Contact(string name, ConvoTypes type = ConvoTypes.Person)
        {
            Name = name;
            Type = type;
        }

        public bool IsPerson()
        {
            return Type == ConvoTypes.Person;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    [JsonObject]
    public class Thread : IEnumerable<Message>
    {
        public string Name { get; set; }
        public List<Message> Messages { get; }
        public Dictionary<Contact, int> Participants { get; }
        public int Size => Messages.Count;

        public Dictionary<DateTimeOffset, Dictionary<Contact, int>> DailyMessageFrequencies { get; set; }

        public Thread(string name)
        {
            Name = name;
            Messages = new List<Message>();
            Participants = new Dictionary<Contact, int>(2);
            DailyMessageFrequencies = new Dictionary<DateTimeOffset, Dictionary<Contact, int>> ();
        }

        public void AddMessage(Message msg)
        {
            Messages.Add(msg);

            if (!Participants.ContainsKey(msg.Author))
                AddParticipant(msg.Author);

            if (!DailyMessageFrequencies.ContainsKey(msg.Date))
                DailyMessageFrequencies[msg.Date] = new Dictionary<Contact, int>();

            if (!DailyMessageFrequencies[msg.Date].ContainsKey(msg.Author))
                DailyMessageFrequencies[msg.Date][msg.Author] = 0;
        }

        public void AddParticipant(Contact participant)
        {
            Participants.Add(participant, 0);
        }

        public void SortByTime()
        {
            Messages.Sort();
        }

        public void GenerateFrequencies(bool byLen = true)
        {
            GenerateFrequencies(Messages[0].Date, DateTimeOffset.Now.Date, byLen);
        }

        // dates must be dates sans times
        public void GenerateFrequencies(DateTimeOffset fromDate, DateTimeOffset toDate, bool byLen = true)
        {
            foreach (var msg in Messages)
            {
                DailyMessageFrequencies[msg.Date][msg.Author] += byLen ? msg.Length : 1;
                Participants[msg.Author] += byLen ? msg.Length : 1;
            }

            var totalDays = (toDate - fromDate).Days;

            var allSequentialDays = new Dictionary<DateTimeOffset, Dictionary<Contact, int>>(totalDays);
            var oneDay = new TimeSpan(1, 0, 0, 0);

            // go through all elapsed days, filling in data and zeros
            var currentDay = fromDate;
            for (var i = 0; i <= totalDays; i++)
            {
                allSequentialDays[currentDay] = DailyMessageFrequencies.ContainsKey(currentDay)
                    ? DailyMessageFrequencies[currentDay]
                    : new Dictionary<Contact, int>();

                foreach (var person in Participants.Keys)
                    if (!allSequentialDays[currentDay].ContainsKey(person))
                        allSequentialDays[currentDay][person] = 0;

                currentDay += oneDay;
            }
            DailyMessageFrequencies = allSequentialDays;
        }

        public void Save(string fileName = null)
        {
            File.WriteAllText((fileName ?? Name) + @".json", JsonConvert.SerializeObject(this));
        }

        public static Thread Load(string filename)
        {
            return JsonConvert.DeserializeObject<Thread>(File.ReadAllText(filename + @".json"));
        }

        public async void SaveTxt(string fileName = null)
        {
            using (var writer = new StreamWriter((fileName ?? Name) + @".txt"))
                foreach (var message in Messages)
                    await writer.WriteLineAsync(message.ToString());
        }

        public IEnumerator<Message> GetEnumerator()
        {
            return Messages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}