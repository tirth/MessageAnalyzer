using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using Newtonsoft.Json;

namespace MessageAnalyzer
{
    public partial class WhatsAppDialogBox
    {
        private string _wa;
        private string _msgStore;
        private readonly Dictionary<string, Contact> _phoneBook;
        private readonly Dictionary<string, Thread> _threads; 

        public WhatsAppDialogBox()
        {
            InitializeComponent();

            _phoneBook = new Dictionary<string, Contact> {["me"] = new Contact("me")};
            _threads = new Dictionary<string, Thread>();
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
                SubmitPanel.Visibility = Visibility.Visible;
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
                SubmitPanel.Visibility = Visibility.Visible;
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (OkButton.Content.ToString() == "OK")
            {
                ReadContacts();
                DataCollectionPanel.Visibility = Visibility.Collapsed;
                ContactSelectionPanel.Visibility = Visibility.Visible;

                ExtractMessages();

                ContactSelectionBox.ItemsSource = _threads.Keys;
                ContactSelectionBox.SelectedIndex = 0;

                OkButton.Content = "Punch it!";
            }
            else
            {
                var selected = ContactSelectionBox.SelectedItem.ToString();
                File.WriteAllLines(selected + ".txt", _threads[selected].Messages.Select(msg => msg.ToString()));
                StatusBlock.Text = "Saved " + selected + ".txt";

                //JSON
                File.WriteAllText(selected + ".json", JsonConvert.SerializeObject(_threads[selected]));
            }
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

                if (name == "")
                    continue;  // no longer a contact

                _phoneBook[sender[0]] = sender[1] == "g.us" 
                    ? new Contact(name, ConvoTypes.Group) 
                    : new Contact(name);
            }

            command.Dispose();
            conn.Close();
        }

        private async void ExtractMessages()
        {
            var conn = new SQLiteConnection("Data Source=" + _msgStore);
            conn.Open();

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
                var group = reader["remote_resource"].ToString();
                var message = reader["data"].ToString();
                var media = reader["media_mime_type"].ToString();

                // figure out sender and receiver
                Contact sender;
                Contact receiver;
                var converser = jId.ToString().Split('@')[0];

                try
                {
                    if (fromMe)
                    {
                        if (group != "")  // meta records
                            continue;  

                        sender = _phoneBook["me"];
                        receiver = _phoneBook[converser];
                    }
                    else if (group == "")  // single chat
                    {
                        sender = _phoneBook[converser]; 
                        receiver = _phoneBook["me"];
                    }
                    else  // group chat
                    {
                        sender = _phoneBook[group.Split('@')[0]]; 
                        receiver = _phoneBook[converser];
                    }
                }
                catch (KeyNotFoundException)
                {
                    Trace.WriteLine(message);
                    continue;  // unknown contact
                }

                // figure out body
                string body;

                if (message != "")
                    body = message;
                else if (media != "")
                    body = media;
                else
                    continue; // calls and images

                // figure out thread
                var thread = receiver.Type == ConvoTypes.Group || sender.Name == "me" 
                    ? receiver.Name : sender.Name;

                if (!_threads.ContainsKey(thread))
                    _threads[thread] = new Thread(thread);

                _threads[thread].AddMessage(new Message(ts, sender, body, "WhatsApp"));
            }

            command.Dispose();
            conn.Close();
        }
    }
}
