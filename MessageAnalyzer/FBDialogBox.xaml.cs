using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Text;

namespace MessageAnalyzer
{
    public partial class InfoDialogBox
    {
        readonly static Uri BaseAddress = new Uri("https://www.facebook.com");

        public InfoDialogBox()
        {
            InitializeComponent();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
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
                var fbInfo = FB_login(EmailTextBox.Text, PasswordEntry.Password);
                var token = fbInfo.Item1;
                var cookies = fbInfo.Item2;

                if (token.Length == 0)
                    StatusBlock.Text = "Login failed";
                else
                {
                    SecondStatusBlock.Text = token;

//                    SecondStatusBlock.Text = NameFromFbId("28113129", token, cookies);

                    // switch screens
                    AccountStackPanel.Visibility = Visibility.Collapsed;
                    DetailsStackPanel.Visibility = Visibility.Visible;
                    OkButton.Content = "Punch it!";
                }
//                DialogResult = true;
            }
        }

        private static Tuple<string, CookieCollection> FB_login(string username, string password)
        {
            // to persist session
            var cookies = new CookieCollection();

            // initial request
            var request = (HttpWebRequest)WebRequest.Create(BaseAddress);
            request.CookieContainer = new CookieContainer();
            request.CookieContainer.Add(cookies);

            // initial response
            var response = (HttpWebResponse)request.GetResponse();
            cookies = response.Cookies;

            // inital page code
            var initStream = new StreamReader(response.GetResponseStream());
            var initPage = initStream.ReadToEnd();
            initStream.Dispose();

            // assemble data for POST
            const string lsdSearch = "name=\"lsd\" value=\"(.+?)\"";
            var lsd = System.Text.RegularExpressions.Regex.Match(initPage, lsdSearch).Groups[1].Value;
            var postData = String.Format("email={0}&pass={1}&lsd={2}", username, password, lsd);
            var postByteArray = Encoding.ASCII.GetBytes(postData);

            // login request
            var loginRequest = (HttpWebRequest)WebRequest.Create(BaseAddress + "login.php?login_attempt=1");
            loginRequest.CookieContainer = new CookieContainer();
            loginRequest.CookieContainer.Add(cookies);
            loginRequest.Method = WebRequestMethods.Http.Post;

            // headers
            loginRequest.Referer = BaseAddress.ToString();
            loginRequest.UserAgent = "Mozilla/5.0 (Windows NT 6.3; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/43.0.2357.65 Safari/537.36";
            loginRequest.ContentType = "application/x-www-form-urlencoded";
            loginRequest.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";

            // create the connection and send data
            var newStream = loginRequest.GetRequestStream();
            newStream.Write(postByteArray, 0, postByteArray.Length);
            newStream.Close();

            // login response
            var loginResponse = (HttpWebResponse)loginRequest.GetResponse();
            cookies = loginResponse.Cookies;

            // authenticated request
            var authRequest = (HttpWebRequest)WebRequest.Create(BaseAddress);
            authRequest.CookieContainer = new CookieContainer();
            authRequest.CookieContainer.Add(cookies);

            // authenticated response
            var authResponse = (HttpWebResponse)authRequest.GetResponse();

            // authenticated page code
            var authStream = new StreamReader(authResponse.GetResponseStream());
            var authPage = authStream.ReadToEnd();
            authStream.Dispose();

            // find authentication token
            const string tokenSearch = @"fb_dtsg\\"" value=\\""(\w+?)\\";
            var token = System.Text.RegularExpressions.Regex.Match(authPage, tokenSearch).Groups[1].Value;

            return Tuple.Create(token, cookies);
        }

        private static string NameFromFbId(string id, string token, CookieCollection cookies)
        {
            var authRequest = (HttpWebRequest)WebRequest.Create(BaseAddress + id);
            authRequest.CookieContainer = new CookieContainer();
            authRequest.CookieContainer.Add(cookies);

            // authenticated response
            var authResponse = (HttpWebResponse)authRequest.GetResponse();

            // authenticated page code
            var authStream = new StreamReader(authResponse.GetResponseStream());
            var authPage = authStream.ReadToEnd();
            authStream.Dispose();

            System.IO.File.WriteAllText(@"C:\Users\Tirth\Desktop\name.txt", authPage);

            const string nameSearch = @"Yatri";
            var name = System.Text.RegularExpressions.Regex.Match(authPage, nameSearch).Groups[1].Value;

            System.Console.WriteLine(name);

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
