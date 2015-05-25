using System;
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

    public class Message
    {
        public string Timestamp { get; set; }
        public Contact Author { get; set; }
        public string Body { get; set; }
        public string Source { get; set; }
        public string Location { get; set; }

        public Message(string timestamp, Contact author, string body, string source = null, string location = null)
        {
            Timestamp = timestamp;
            Author = author;
            Body = body.Replace('\n', ' ');
            Source = source ?? "Unknown source";
            Location = location ?? "No location";
        }

        public override string ToString()
        {
            return string.Join(Stuff.Sepr.ToString(), Timestamp, Author, Body, Source, Location);
        }

        public int CompareTo(object obj)
        {
            // use DateTime instead?
            return Int64.Parse(Timestamp).CompareTo(Int64.Parse(((Message)obj).Timestamp));
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

    public class Thread
    {
        public string Name { get; set; }
        public List<Message> Messages { get; set; }
        public HashSet<Contact> Participants { get; set; } 

        public Thread(string name)
        {
            Name = name;
            Messages = new List<Message>();
            Participants = new HashSet<Contact>();
        }

        public void AddMessage(Message msg)
        {
            Messages.Add(msg);
            AddParticipant(msg.Author);
        }

        public void AddParticipant(Contact participant)
        {
            Participants.Add(participant);
        }

        public void SortByTime()
        {
            Messages.Sort();
        }

        public void Save(string fileName = null)
        {
            File.WriteAllText(fileName ?? Name, JsonConvert.SerializeObject(this));
        }
    }
}