using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
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

    public class Contact : IComparable
    {
        public string Name { get; }
        public List<string> Nicknames { get; set; }
        public ConvoTypes Type { get; }

        public Contact(string name, ConvoTypes type = ConvoTypes.Person)
        {
            Name = name;
            Type = type;
            Nicknames = new List<string> { Name };
        }

        public void AddNickname(string nickname)
        {
            Nicknames.Add(nickname);
        }

        public bool IsPerson()
        {
            return Type == ConvoTypes.Person;
        }

        public override string ToString()
        {
            return Name;
        }

        public int CompareTo(object obj)
        {
            return string.Compare(Name, ((Contact)obj).Name, StringComparison.Ordinal);
        }

        protected bool Equals(Contact other)
        {
            return string.Equals(Name, other.Name) && Type == other.Type;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((Contact)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Name?.GetHashCode() ?? 0) * 397) ^ (int)Type;
            }
        }
    }

    [JsonObject]
    public class Thread : IEnumerable<Message>
    {
        public string Name { get; set; }
        public HashSet<string> People { get; set; }

        public List<Message> Messages { get; }
        public int Size => Messages.Count;

        public Thread(string name)
        {
            Name = name;
            Messages = new List<Message>();
            People = new HashSet<string>();
        }

        public void AddMessage(Message msg)
        {
            Messages.Add(msg);

            if (!People.Contains(msg.Author.Name))
                People.Add(msg.Author.Name);
        }

        public void AddParticipant(Contact participant)
        {
            People.Add(participant.Name);
        }

        public void SortByTime()
        {
            Messages.Sort();
        }

        public Tuple<Dictionary<DateTimeOffset, Dictionary<Contact, int>>, Dictionary<Contact, int>> 
            GenerateDailyFrequencies(bool byLen = true)
        {
            return GenerateDailyFrequencies(Messages[0].Date, DateTimeOffset.Now.Date, byLen);
        }

        public Tuple<Dictionary<DateTimeOffset, Dictionary<Contact, int>>, Dictionary<Contact, int>>  
            GenerateDailyFrequencies(DateTimeOffset fromDate, DateTimeOffset toDate, bool byLen = true)
        {
            var freqs = new Dictionary<DateTimeOffset, Dictionary<Contact, int>>();
            var participants = new Dictionary<Contact, int>(2);

            foreach (var msg in Messages)
            {
                if (!participants.ContainsKey(msg.Author))
                    participants.Add(msg.Author, 0);

                if (!freqs.ContainsKey(msg.Date))
                    freqs[msg.Date] = new Dictionary<Contact, int>();

                if (!freqs[msg.Date].ContainsKey(msg.Author))
                    freqs[msg.Date][msg.Author] = 0;

                freqs[msg.Date][msg.Author] += byLen ? msg.Length : 1;
                participants[msg.Author] += byLen ? msg.Length : 1;
            }

            var totalDays = (toDate - fromDate).Days;

            var allSequentialDays = new Dictionary<DateTimeOffset, Dictionary<Contact, int>>(totalDays);
            var oneDay = new TimeSpan(1, 0, 0, 0);

            // go through all elapsed days, filling in data and zeros
            var currentDay = fromDate.Date;
            for (var i = 0; i <= totalDays; i++)
            {
                allSequentialDays[currentDay] = freqs.ContainsKey(currentDay)
                    ? freqs[currentDay]
                    : new Dictionary<Contact, int>();

                foreach (var person in participants.Keys)
                    if (!allSequentialDays[currentDay].ContainsKey(person))
                        allSequentialDays[currentDay][person] = 0;

                currentDay += oneDay;
            }

            return new Tuple<Dictionary<DateTimeOffset, Dictionary<Contact, int>>, Dictionary<Contact, int>>(
                allSequentialDays, participants);
        }

        public Dictionary<DayOfWeek, Dictionary<Contact, int>> GenerateWeekdayFrequencies(bool byLen = true)
        {
            var freqs = new Dictionary<DayOfWeek, Dictionary<Contact, int>>
            {
                {DayOfWeek.Monday, new Dictionary<Contact, int>()},
                {DayOfWeek.Tuesday, new Dictionary<Contact, int>()},
                {DayOfWeek.Wednesday, new Dictionary<Contact, int>()},
                {DayOfWeek.Thursday, new Dictionary<Contact, int>()},
                {DayOfWeek.Friday, new Dictionary<Contact, int>()},
                {DayOfWeek.Saturday, new Dictionary<Contact, int>()},
                {DayOfWeek.Sunday, new Dictionary<Contact, int>()}
            };

            foreach (var msg in Messages)
            {
                var day = msg.Date.DayOfWeek;

                if (!freqs[day].ContainsKey(msg.Author))
                    freqs[day][msg.Author] = 0;

                freqs[day][msg.Author] += byLen ? msg.Length : 1;
            }

            return freqs;
        }

        public Dictionary<int, Dictionary<Contact, int>> GenerateHourlyFrequencies(bool byLen = true)
        {
            var freqs = new Dictionary<int, Dictionary<Contact, int>>();

            for (var hour = 0; hour < 24; hour++)
                freqs.Add(hour, new Dictionary<Contact, int>());

            foreach (var msg in Messages)
            {
                var hour = DateTimeOffset.FromUnixTimeMilliseconds(msg.Timestamp).Hour;

                if (!freqs[hour].ContainsKey(msg.Author))
                    freqs[hour][msg.Author] = 0;

                freqs[hour][msg.Author] += byLen ? msg.Length : 1;
            }

            return freqs;
        }

        public Dictionary<string, Dictionary<string, int>> GenerateWordFrequencies()
        {
            var peopleWords = new Dictionary<string, Dictionary<string, int>>(2);

            // very naive tokenizing
            var splitOn = new[] {' ', ',', '.', '"', '?', '!', '(', ')'};

            foreach (var person in People)
                peopleWords.Add(person, new Dictionary<string, int>());

            foreach (var msg in Messages)
            {
                var words = msg.Content.Split(splitOn, StringSplitOptions.RemoveEmptyEntries);

                foreach (var w in words.Select(word => word.ToLower()))
                    if (peopleWords[msg.Author.Name].ContainsKey(w))
                        peopleWords[msg.Author.Name][w] += 1;
                    else
                        peopleWords[msg.Author.Name].Add(w, 1);
            }

//            foreach (var person in peopleWords)
//            {
//                Console.Out.WriteLine(person.Key);
//                var thing = person.Value.OrderByDescending(entry => entry.Value);
//
//                Console.Out.WriteLine(thing.Sum(entry => entry.Value * entry.Key.Length));
//
//                foreach (var keyValuePair in thing)
//                {
//                    if (keyValuePair.Value > 100)
//                        Console.Out.WriteLine(keyValuePair.Key + ' ' + keyValuePair.Value);
//                }
//                break;
//            }

            return peopleWords;
        }

        public void Save(string fileName = null)
        {
            File.WriteAllText((fileName ?? Name) + @".json", JsonConvert.SerializeObject(this));
        }

        public static Thread LoadJson(string filename)
        {
            return JsonConvert.DeserializeObject<Thread>(File.ReadAllText(filename + @".json"));
        }

        public async void SaveTxt(string fileName = null)
        {
            using (var writer = new StreamWriter((fileName ?? Name) + @".txt"))
                foreach (var message in Messages)
                    await writer.WriteLineAsync(message.ToString());
        }

        public static async Task<Thread> LoadTxt(string fileName)
        {
            var thread = new Thread(fileName);
            using (var reader = new StreamReader(File.OpenRead(fileName + @".txt")))
            {
                while (!reader.EndOfStream)
                {
                    var message = await reader.ReadLineAsync().ConfigureAwait(false);
                    var msg = message.Split(Stuff.Sepr);
                    thread.AddMessage(new Message(msg[0], new Contact(msg[1]), msg[2], msg[3], msg[4]));
                }
            }
            return thread;
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