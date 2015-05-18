using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;

namespace MessageAnalyzer
{
    public partial class InfoDialogBox
    {
        readonly static Uri BaseAddress = new Uri("https://www.facebook.com");

        private const int MessageLimit = 5000;

        static HttpClient _client;
        private static string _accessToken;
        private static string _myId;

        public InfoDialogBox()
        {
            InitializeComponent();

            // to persist logged in session
            var cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler { CookieContainer = cookieContainer };

            _client = new HttpClient(handler) { BaseAddress = BaseAddress };
            _client.DefaultRequestHeaders.Add("user-agent", "NCSA_Mosaic/2.0 (Windows 3.1)");
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var buttonText = ((Button)sender).Content.ToString();

            if (buttonText == "Log in")
                Login_Click();
            else
                Message_Click();
        }

        private void Login_Click()
        {
            var email = EmailTextBox.Text;
            var password = PasswordEntry.Password;

            // simple validity checks
            if (!email.Contains("@"))
                StatusBlock.Text = "Invalid email";
            else if (password.Length < 1)
                StatusBlock.Text = "Invalid password";
            else
            {
                // gather session info
                var fbInfo = FB_login(email, password, _client);
                _accessToken = fbInfo.Item1;
                _myId = fbInfo.Item2;

                if (_accessToken.Length == 0)
                    StatusBlock.Text = "Login failed";
                else
                {
                    // switch screens
                    AccountStackPanel.Visibility = Visibility.Collapsed;
                    DetailsStackPanel.Visibility = Visibility.Visible;
                    OkButton.Content = "Punch it!";
                }
            }
        }

        private void Message_Click()
        {
            StatusBlock.Text = "Login successful!";

            var isGroup = !FriendRadioButton.IsChecked.GetValueOrDefault();
            var convoId = IdTextBox.Text;

            var convoName = isGroup ?
                GroupNameFromFbId(convoId, _client) : FriendNameFromFbId(convoId, _client);

            StatusBlock.Text = "Getting conversations with " + convoName;

            // TODO: Check to make sure friend/group exists
            var got = GetMessages(_client, _accessToken, _myId, convoId, convoName, isGroup, StatusBlock);

            StatusBlock.Text = got;
            OkButton.Visibility = Visibility.Hidden;
            DetailsStackPanel.Visibility = Visibility.Collapsed;
            CancelButton.Content = "Exit";
        }

        private static Tuple<string, string> FB_login(string username, string password, HttpClient client)
        {
            var loginPage = client.GetStringAsync(BaseAddress);

            const string lsdSearch = "name=\"lsd\" value=\"(.+?)\"";
            var lsd = Regex.Match(loginPage.Result, lsdSearch).Groups[1].Value;

            var content = new FormUrlEncodedContent(
                new Dictionary<string, string> {
                {"email", username},
                {"pass", password},
                {"lsd", lsd}
            });

            var result = client.PostAsync(BaseAddress + "login.php?login_attempt=1", content).Result;
            result.EnsureSuccessStatusCode();

            var response = result.Content.ReadAsStringAsync().Result;

            const string tokenSearch = @"fb_dtsg\\"" value=\\""(\w+?)\\";
            var token = Regex.Match(response, tokenSearch).Groups[1].Value;

            const string myIdSearch = @"id=""profile_pic_header_(\d+?)""";
            var myId = Regex.Match(response, myIdSearch).Groups[1].Value;

            return new Tuple<string, string>(token, myId);
        }

        private static string FriendNameFromFbId(string id, HttpClient client)
        {
            var personPage = client.GetStringAsync(BaseAddress + id).Result;

            const string nameSearch = @"pageTitle"">(.+)</title>";
            var name = Regex.Match(personPage, nameSearch).Groups[1].Value;

            return name;
        }

        private static string GroupNameFromFbId(string id, HttpClient client)
        {
            var groupPage = client.GetStringAsync(BaseAddress + "messages/conversation-" + id).Result;

            var groupSearch = @"thread_fbid"":""" + id + @"(.*?)snippet";
            var groupInfo = Regex.Match(groupPage, groupSearch).ToString();

            const string nameSearch = @"name"":""(.*?)""";
            var name = Regex.Match(groupInfo, nameSearch).Groups[1].Value;

            return name;
        }

        private void radio_checked(object sender, RoutedEventArgs e)
        {
            // prevent initial NPE
            if (ConvoTypeBlock == null)
                return;

            var checkedButton = ((RadioButton)sender).Content.ToString();

            if (checkedButton == "Friend")
                ConvoTypeBlock.Text = "Friend ID";
            else if (checkedButton == "Group")
                ConvoTypeBlock.Text = "Conversation ID";
        }

        private static string GetMessages(HttpClient client, string accessToken, string myId, string convoId,
            string convoName, bool isGroup, TextBlock statusBlock)
        {
            var offset = 0;
            var numMessages = 0;
            var authorIds = new Dictionary<string, string> { { myId, "me" } };

            using (var output = new StreamWriter(convoName + ".txt"))
                while (true)
                {
                    var page = client.GetStringAsync(ConvoUrl(myId, convoId, accessToken, offset, isGroup)).Result;

                    var messages = JObject.Parse(page.Substring(9))["payload"]["actions"].Children().ToList();
                    numMessages += messages.Count;

                    foreach (var message in messages)
                    {
                        var date = (message["timestamp"].ToString());
                        var source = String.Join(" ", message["source_tags"]).Contains("mobile") ? "mobile" : "web";

                        var authorId = message["author"].ToString().Split(':')[1];
                        string author;

                        try
                        {
                            author = authorIds[authorId];
                        }
                        catch (KeyNotFoundException)
                        {
                            author = FriendNameFromFbId(authorId, client);
                            authorIds.Add(authorId, author);
                        }

                        var body = message["body"] ?? message["log_message_body"];
                        body = body.ToString().Replace('\n', ' ');

                        var location = message["coordinates"].ToString() == ""
                            ? "No location"
                            : message["coordinates"]["latitude"] + "," +
                              message["coordinates"]["longitude"];

                        if (message["has_attachment"] != null)
                            foreach (var attachment in message["attachments"])
                                body += attachment["attach_type"].ToString();

                        // TODO: JSONify
                        output.WriteLine(String.Join(Stuff.Sepr.ToString(), date, author, body, source, location));
                    }

                    if (messages.Count < MessageLimit)
                        break;

                    offset += MessageLimit;
                    statusBlock.Text = numMessages + " so far, getting more";
                }

            return String.Format("Got {0} messages with {1}", numMessages, convoName);

//            foreach (var authorId in authorIds)
//                Console.Out.WriteLine(authorId.ToString());
        }

        private static Uri ConvoUrl(string myId, string convoId, string accessToken, int offset, bool isGroup)
        {
            var convoUrl = String.Format("https://www.facebook.com/ajax/mercury/thread_info.php?" +
                                         "&messages[user_ids][{1}][limit]={4}" +
                                         "&messages[user_ids][{1}][offset]={2}" +
                                         "&client=web_messenger&__user={0}&__a=1&fb_dtsg={3}",
                                          myId, convoId, offset, accessToken, MessageLimit);
            if (isGroup)
                convoUrl = convoUrl.Replace("user_ids", "thread_fbids");

            return new Uri(convoUrl);
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
