using System;
using System.Windows;

namespace MessageAnalyzer
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void FbButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new FbDialogBox { Owner = this };

            var result = dlg.ShowDialog();

            StatusBlock.Text = result == true ? "Done." : "Nope.";
        }

        private void WaButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void SmsButton_OnClick(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException();
        }

        private void FreqButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new FreqDialogBox() { Owner = this };

            var result = dlg.ShowDialog();

            StatusBlock.Text = result == true ? "Done." : "Nope.";
        }
    }
}
