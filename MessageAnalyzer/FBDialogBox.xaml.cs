using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace MessageAnalyzer
{
    public partial class InfoDialogBox
    {
        readonly static Uri BaseAddress = new Uri("https://www.facebook.com");

        static HttpClient _client;
        private static string _accessToken;

        public InfoDialogBox()
        {
            InitializeComponent();

            // to persist logged in session
            var cookieContainer = new CookieContainer();

            var handler = new HttpClientHandler {CookieContainer = cookieContainer};

            _client = new HttpClient(handler) {BaseAddress = BaseAddress};
            _client.DefaultRequestHeaders.Add("user-agent", "NCSA_Mosaic/2.0 (Windows 3.1)");
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var buttonText = ((Button) sender).Content.ToString();
            SecondStatusBlock.Text = ((Button)sender).Content.ToString();

            if (buttonText == "Log in")  // first screen
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
                    _accessToken = FB_login(EmailTextBox.Text, PasswordEntry.Password, _client);

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
            else  // second screen
            {
                SecondStatusBlock.Text = NameFromFbId(IdTextBox.Text, _client);
            }
        }

        private static string FB_login(string username, string password, HttpClient client)
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

            return token;
        }

        private static string NameFromFbId(string id, HttpClient client)
        {
            var personPage = client.GetStringAsync(BaseAddress + id).Result;

            const string nameSearch = @"pageTitle"">(.+)</title>";
            var name = Regex.Match(personPage, nameSearch).Groups[1].Value;

            return name;
        }
    }

    public class EmailValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            var email = (string) value;
            if (email != null && !email.Contains("@"))
            {
                return new ValidationResult(false, "Not a valid email.");
            }

            return new ValidationResult(true, null);
        }
    }
}
