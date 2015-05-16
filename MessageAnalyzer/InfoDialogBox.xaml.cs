using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace MessageAnalyzer
{
    public partial class InfoDialogBox
    {
        public InfoDialogBox()
        {
            InitializeComponent();
        }

        private void OkButton_OnClick(object sender, RoutedEventArgs e)
        {
            var email = EmailTextBox.Text;

            if (email.Contains("@"))
            {
                DialogResult = true;
                Close();   
            }
            else
            {
                StatusBlock.Text = "Invalid email";
            }
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
