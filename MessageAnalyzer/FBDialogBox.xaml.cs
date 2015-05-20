using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace MessageAnalyzer
{
    public partial class FbDialogBox
    {
        readonly static Uri BaseAddress = new Uri(@"https://www.facebook.com");

        private const int MessageLimit = 5000;

        static HttpClient _client;
        private static string _accessToken;
        private static string _myId;
        private static Dictionary<string, string> _authors;

        private static Dictionary<string, string> _recentGroupThreads;
        private static Dictionary<string, string> _recentFriendThreads;

        public FbDialogBox()
        {
            InitializeComponent();

            // to persist logged in session
            var cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler { CookieContainer = cookieContainer };

            _client = new HttpClient(handler) { BaseAddress = BaseAddress };
            _client.DefaultRequestHeaders.Add("user-agent", "NCSA_Mosaic/2.0 (Windows 3.1)");

            _authors = new Dictionary<string, string>();
            _recentFriendThreads = new Dictionary<string, string>();
            _recentGroupThreads = new Dictionary<string, string>();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var buttonText = ((Button)sender).Content.ToString();

            if (buttonText == "Log in")
                Login_Click();
            else
                Message_Click();
        }

        private async void Login_Click()
        {
            var email = EmailTextBox.Text;
            var password = PasswordEntry.Password;

            StatusBlock.Text = "Logging in...";

            // simple validity checks
            if (!email.Contains("@"))
                StatusBlock.Text = "Invalid email";
            else if (password.Length < 1)
                StatusBlock.Text = "Invalid password";
            else
            {
                EmailTextBox.IsEnabled = false;
                PasswordEntry.IsEnabled = false;
                OkButton.IsEnabled = false;

                // gather session info
                var fbInfo = await FB_login(email, password, _client);
                _accessToken = fbInfo.Item1;
                _myId = fbInfo.Item2;

                if (_accessToken.Length == 0)
                {
                    StatusBlock.Text = "Login failed";
                    EmailTextBox.IsEnabled = true;
                    PasswordEntry.IsEnabled = true;
                    OkButton.IsEnabled = true;
                }
                else
                {
                    StatusBlock.Text = "Getting recent messages...";
                    OkButton.IsEnabled = true;
                    OkButton.Visibility = Visibility.Hidden;

                    // switch screens
                    AccountStackPanel.Visibility = Visibility.Collapsed;
                    OkButton.Content = "Punch it!";

                    // populate recent messages
                    var recents = await GetRecentMessages(_client);

                    OkButton.Visibility = Visibility.Visible;
                    DetailsStackPanel.Visibility = Visibility.Visible;
                    StatusBlock.Text = "Done!";

                    _authors = recents.Item1;
                    _authors[_myId] = "me";

                    var recentThreads = recents.Item2;

                    // separate threads
                    foreach (var thread in recentThreads)
                        if (_authors.ContainsKey(thread.Value))
                            _recentFriendThreads.Add(thread.Key, thread.Value);
                        else
                            _recentGroupThreads.Add(thread.Key, thread.Value);

                    RecentsPicker.ItemsSource = _recentFriendThreads.Keys;
                    RecentsPicker.SelectedIndex = 0;

                    selected_recent(null, null);
                }
            }
        }

        private void selected_recent(object sender, RoutedEventArgs e)
        {
            if (RecentsPicker.SelectedIndex >= 0)
                IdTextBox.Text = FriendRadioButton.IsChecked.GetValueOrDefault()
                    ? _recentFriendThreads[RecentsPicker.SelectedItem.ToString()]
                    : _recentGroupThreads[RecentsPicker.SelectedItem.ToString()];
        }

        private async void Message_Click()
        {
            var isGroup = !FriendRadioButton.IsChecked.GetValueOrDefault();
            var convoId = IdTextBox.Text;
            DetailsStackPanel.Visibility = Visibility.Collapsed;
            OkButton.Visibility = Visibility.Hidden;

            string convoName;

            if (isGroup)
                try
                {
                    convoName = _recentGroupThreads[convoId];
                }
                catch (KeyNotFoundException)
                {
                    convoName = await GroupNameFromFbId(convoId, _client);
                }
            else
                try
                {
                    convoName = _authors[convoId];
                }
                catch (KeyNotFoundException)
                {
                    convoName = await FriendNameFromFbId(convoId, _client);
                    _authors.Add(convoId, convoName);
                }

            StatusBlock.Text = "Getting convos with " + convoName + "...";

            // TODO: Check to make sure friend/group exists
            var got = await GetMessages(_client, _accessToken, _myId, convoId, convoName, isGroup);

            StatusBlock.Text = got;
            DetailsStackPanel.Visibility = Visibility.Visible;
            OkButton.Visibility = Visibility.Visible;
            OkButton.Content = "Punch it again!";
            CancelButton.Content = "Exit";
        }

        private async static Task<Tuple<string, string>> FB_login(string username, string password, HttpClient client)
        {
            var loginPage = await client.GetStringAsync(BaseAddress);

            const string lsdSearch = "name=\"lsd\" value=\"(.+?)\"";
            var lsd = Regex.Match(loginPage, lsdSearch).Groups[1].Value;

            var content = new FormUrlEncodedContent(
                new Dictionary<string, string> {
                {"email", username},
                {"pass", password},
                {"lsd", lsd}
            });

            var result = await client.PostAsync(BaseAddress + "login.php?login_attempt=1", content);
            result.EnsureSuccessStatusCode();

            var response = await result.Content.ReadAsStringAsync();

            const string tokenSearch = @"fb_dtsg\\"" value=\\""(\w+?)\\";
            var token = Regex.Match(response, tokenSearch).Groups[1].Value;

            const string myIdSearch = @"id=""profile_pic_header_(\d+?)""";
            var myId = Regex.Match(response, myIdSearch).Groups[1].Value;

            return new Tuple<string, string>(token, myId);
        }

        private async static Task<string> FriendNameFromFbId(string id, HttpClient client)
        {
            var personPage = await client.GetStringAsync(BaseAddress + id);

            const string nameSearch = @"pageTitle"">(.+)</title>";
            var name = Regex.Match(personPage, nameSearch).Groups[1].Value;

            return name;
        }

        private async static Task<string> GroupNameFromFbId(string id, HttpClient client)
        {
            var groupPage = await client.GetStringAsync(BaseAddress + "messages/conversation-" + id);

            var groupSearch = @"thread_fbid"":""" + id + @"(.*?)snippet";
            var groupInfo = Regex.Match(groupPage, groupSearch).ToString();

            const string nameSearch = @"name"":""(.*?)""";
            var rawName = Regex.Match(groupInfo, nameSearch).Groups[1].Value;

            // make sure file name will be valid
            var name = new string(rawName.Where(x => !Path.GetInvalidFileNameChars().Contains(x)).ToArray());

            return name;
        }

        private void radio_checked(object sender, RoutedEventArgs e)
        {
            // prevent initial NPE
            if (ConvoTypeBlock == null)
                return;

            var checkedButton = ((RadioButton)sender).Content.ToString();

            switch (checkedButton)
            {
                case "Friend":
                    ConvoTypeBlock.Text = "Friend ID";
                    RecentConversationBlock.Text = "Recent Friend Conversations";
                    RecentsPicker.ItemsSource = _recentFriendThreads.Keys;
                    break;
                case "Group":
                    ConvoTypeBlock.Text = "Conversation ID";
                    RecentConversationBlock.Text = "Recent Group Conversations";
                    RecentsPicker.ItemsSource = _recentGroupThreads.Keys;
                    break;
            }

            RecentsPicker.SelectedIndex = 0;
        }

        // friend dictionary <ID, Name>; thread dictionary <Name, ID>
        private async static Task<Tuple<Dictionary<string, string>, Dictionary<string, string>>> GetRecentMessages(HttpClient client)
        {
            var messagePage = await client.GetStringAsync(BaseAddress + "messages");

            var friends = new Dictionary<string, string>();
            var threads = new Dictionary<string, string>();

            // pull out friends
            const string friendSearch = @"""ordered_threadlists"":(.+)""unread_thread_ids";
            var friendsRaw = Regex.Match(messagePage, friendSearch).Value;
            var friendsJson = JObject.Parse("{" + friendsRaw.Remove(friendsRaw.LastIndexOf(",", StringComparison.Ordinal)) + "}");

            // populate friend dictionary
            foreach (var friend in friendsJson["participants"])
                friends[friend["fbid"].Value<string>()] = friend["name"].Value<string>();

            // pull out threads
            const string threadSearch = @"{""threads"":(.+)""ordered_threadlists";
            var messagesRaw = Regex.Match(messagePage, threadSearch).Value;
            var messagesJson = JObject.Parse(messagesRaw.Remove(messagesRaw.LastIndexOf(",", StringComparison.Ordinal)) + "}");

            // populate thread dictionary, adding in friend names
            foreach (var thread in messagesJson["threads"])
            {
                var threadId = thread["thread_fbid"].Value<string>();
                var threadName = thread["name"].Value<string>();

                if (threadName == "")
                    threadName = thread["participants"].ToList().Count > 2
                        ? thread["participants"].Aggregate(threadName, (current, person) => current + (friends[person.ToString().Split(':')[1]] + "|"))
                        : friends[threadId];

                threads[threadName] = threadId;
            }

            return new Tuple<Dictionary<string, string>, Dictionary<string, string>>(friends, threads);
        }

        private static async Task<string> GetMessages(HttpClient client, string accessToken, string myId, string convoId,
            string convoName, bool isGroup)
        {
            var offset = 0;
            var numMessages = 0;

            using (var output = new StreamWriter(convoName + @".txt"))
                while (true)
                {
                    var page = await client.GetStringAsync(ConvoUrl(myId, convoId, accessToken, offset, isGroup)).ConfigureAwait(false);

                    var messages = JObject.Parse(page.Substring(9))["payload"]["actions"].Children().ToList();
                    numMessages += messages.Count;

                    foreach (var message in messages)
                    {
                        var date = (message["timestamp"].ToString());
                        var source = string.Join(" ", message["source_tags"]).Contains("mobile") ? "mobile" : "web";

                        var authorId = message["author"].ToString().Split(':')[1];
                        string author;

                        try
                        {
                            author = _authors[authorId];
                        }
                        catch (KeyNotFoundException)
                        {
                            author = await FriendNameFromFbId(authorId, client).ConfigureAwait(false);
                            _authors.Add(authorId, author);
                        }

                        var body = message["body"] ?? message["log_message_body"];
                        body = body.ToString().Replace('\n', ' ');

                        var location = message["coordinates"].ToString() == ""
                            ? "No location"
                            : message["coordinates"]["latitude"] + "," +
                              message["coordinates"]["longitude"];

                        if (message["has_attachment"] != null)
                            body = message["attachments"].Aggregate(body, (current, attachment) => current + attachment["attach_type"].ToString());

                        // TODO: JSONify
                        await output.WriteLineAsync(string.Join(Stuff.Sepr.ToString(), date, author, body, source, location)).ConfigureAwait(false);
                    }

                    if (messages.Count < MessageLimit)
                        break;

                    offset += MessageLimit;
                }

            Stuff.SortByTime(convoName);
            return string.Format("Got {0} messages with {1}", numMessages, convoName);
        }

        private static Uri ConvoUrl(string myId, string convoId, string accessToken, int offset, bool isGroup)
        {
            var convoUrl = string.Format("https://www.facebook.com/ajax/mercury/thread_info.php?" +
                                         "&messages[user_ids][{1}][limit]={4}" +
                                         "&messages[user_ids][{1}][offset]={2}" +
                                         "&client=web_messenger&__user={0}&__a=1&fb_dtsg={3}",
                                          myId, convoId, offset, accessToken, MessageLimit);
            if (isGroup)
                convoUrl = convoUrl.Replace("user_ids", "thread_fbids");

            return new Uri(convoUrl);
        }

        private void id_focus(object sender, RoutedEventArgs e)
        {
            IdTextBox.Text = "";
        }
    }

    public class EmailValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var email = (string)value;
            if (email != null && !email.Contains("@"))
            {
                return new ValidationResult(false, "Not a valid email.");
            }

            return new ValidationResult(true, null);
        }
    }
}
