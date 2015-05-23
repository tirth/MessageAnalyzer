using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Windows;
using Microsoft.Win32;

namespace MessageAnalyzer
{
    class Contact
    {
        public string Name { get; set; }
        public string Type { get; set; }

        public Contact(string name, string type)
        {
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return Name;
        }
    }

    public partial class WhatsAppDialogBox
    {
        private string _wa;
        private string _msgStore;
        private List<string> _contactList;
        private Dictionary<string, Contact> _phoneBook; 

        public WhatsAppDialogBox()
        {
            InitializeComponent();
            _contactList = new List<string>();
            _phoneBook = new Dictionary<string, Contact>();
        }

        private void SelectWaDb(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                FileName = "wa",
                DefaultExt = ".db",
                Filter = "WhatsApp Database (.db)|*.db"
            };

            var result = dlg.ShowDialog();

            if (!result.GetValueOrDefault())
            {
                WaSelectButton.Content = "Invalid file";
                return;
            }

            _wa = dlg.FileName;
            WaBlock.Text = _wa;
            WaBlock.Visibility = Visibility.Visible;
            WaSelectButton.Visibility = Visibility.Collapsed;

            if (_wa != null && _msgStore != null)
                OkButton.Visibility = Visibility.Visible;
        }

        private void SelectMsgStoreDb(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                FileName = "msgstore",
                DefaultExt = ".db",
                Filter = "WhatsApp Database (.db)|*.db"
            };

            var result = dlg.ShowDialog();

            if (!result.GetValueOrDefault())
            {
                MsgStoreSelectButton.Content = "Invalid file";
                return;
            }

            _msgStore = dlg.FileName;
            MsgStoreBlock.Text = _msgStore;
            MsgStoreBlock.Visibility = Visibility.Visible;
            MsgStoreSelectButton.Visibility = Visibility.Collapsed;

            if (_wa != null && _msgStore != null)
                OkButton.Visibility = Visibility.Visible;
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            ReadContacts();

            foreach (var contact in _phoneBook)
                Trace.WriteLine(contact);

            ExtractMessages();
        }

        private async void ReadContacts()
        {
            var conn = new SQLiteConnection("Data Source=" + _wa);
            conn.Open();

            var command = new SQLiteCommand("SELECT jid, display_name FROM wa_contacts", conn);
            var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);
            
            while (await reader.ReadAsync())
            {
                var sender = reader["jid"].ToString().Split('@');
                var name = reader["display_name"].ToString();

                _phoneBook[sender[0]] = 
                    sender[1] == "g.us" ? new Contact(name, "group") : new Contact(name, "person");

                _contactList.Add(name);
            }

            conn.Close();
        }

        private async void ExtractMessages(string choice = null)
        {
            var conn = new SQLiteConnection("Data Source=" + _msgStore);
            conn.Open();

            var msgs = new List<Message>();

            var command = new SQLiteCommand(
                    "SELECT timestamp, key_from_me, key_remote_jid, remote_resource, data, media_mime_type " +
                    "FROM messages " +
                    "WHERE timestamp IS NOT 0", conn);
            var reader = await command.ExecuteReaderAsync().ConfigureAwait(false);

            while (await reader.ReadAsync())
            {
                var ts = reader["timestamp"].ToString();
                var fromMe = Convert.ToBoolean(reader["key_from_me"]);
                var jId = reader["key_remote_jid"];
                var group = reader["remote_resource"];
                var message = reader["data"];
                var media = reader["media_mime_type"];

                // figure out sender
                string sender;
                var converser = jId.ToString().Split('@')[0];

                if (fromMe)
                {
                    if (group != null)
                        continue;  // renamed conversation
                    sender = "me@" + converser;
                }
                else if (group == null || group.ToString() == "")
                    sender = converser + "@me"; // single chat
                else
                    sender = group.ToString().Split('@')[0] + "@" + converser; // group chat

                // figure out body
                string body;

                if (message != null)
                    body = message.ToString();
                else if (media != null)
                    body = media.ToString();
                else
                    continue; // calls and images

                if (choice != null)
                {

                }
            }

            conn.Close();
        }
    }
}
