namespace MessageAnalyzer
{
    public class Message
    {
        public string Timestamp { get; set; }

        public string Author { get; set; }

        public string Body { get; set; }

        public string Source { get; set; }

        public string Location { get; set; }

        public Message(string timestamp, string author, string body, string source = null, string location = null)
        {
            Timestamp = timestamp;
            Author = author;
            Body = body.Replace('\n', ' ');
            Source = source ?? "";
            Location = location ?? "";
        }

        public override string ToString()
        {
            return string.Join(Stuff.Sepr.ToString(), Timestamp, Author, Body, Source, Location);
        }
    }
}