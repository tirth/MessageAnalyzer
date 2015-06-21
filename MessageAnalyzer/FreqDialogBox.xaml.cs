using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;

namespace MessageAnalyzer
{
    public partial class FreqDialogBox
    {
        private Thread _thread;

        public FreqDialogBox()
        {
            InitializeComponent();

            var convoFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), @"*.json");

            // populate list of coversations
            var convos = new ArrayList();
            foreach (var convoName in convoFiles.Select(convo => convo.Split('\\'))
                        .Select(convoPath => convoPath[convoPath.Length - 1])
                        .Select(convoFile => convoFile.Split('.')[0]))
                convos.Add(convoName);

            ConvoPicker.ItemsSource = convos;
            ConvoPicker.SelectedIndex = 0;
        }

        private void AnalyzeClick(object sender, RoutedEventArgs e)
        {
            var byLen = ByLenRadio.IsChecked.GetValueOrDefault();
            var fromDate = FromDatePicker.SelectedDate.GetValueOrDefault();
            var toDate = ToDatePicker.SelectedDate.GetValueOrDefault();

            if (DateRangeRadio.IsChecked.GetValueOrDefault())
                new AnalyzedWindow(_thread, _thread.GenerateDailyFrequencies(fromDate, toDate, byLen)) { Owner = this }.Show();
            else if (TimeOfDayRadio.IsChecked.GetValueOrDefault())
                new AnalyzedWindow(_thread, _thread.GenerateHourlyFrequencies(byLen)) { Owner = this }.Show();
            else
                new AnalyzedWindow(_thread, _thread.GenerateWeekdayFrequencies(byLen)) { Owner = this }.Show();
        }

        public void display_radio(object sender, RoutedEventArgs e)
        {
            DatePickPanel.Visibility = DateRangeRadio.IsChecked.GetValueOrDefault() ? Visibility.Visible : Visibility.Collapsed;
        }

        public void convo_picked(object sender, RoutedEventArgs e)
        {
            _thread = Thread.LoadJson(ConvoPicker.SelectedItem.ToString());

            FromDatePicker.SelectedDate = _thread.Messages[0].Date.DateTime;
            ToDatePicker.SelectedDate = _thread.Messages[_thread.Size - 1].Date.DateTime;
        }
    }
}
