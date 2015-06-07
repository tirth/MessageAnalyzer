using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MessageAnalyzer
{
    public partial class AnalyzedWindow
    {        
        public AnalyzedWindow(Thread thread, Dictionary<DateTimeOffset, Dictionary<Contact, int>> freqs)
        {
            InitializeComponent();
            AnalzyedConvoName.Text = thread.Name;

            foreach (var person in (from entry in thread.Participants orderby entry.Value descending select entry))
                FrequenciesBlock.Text += string.Format("{0,-10} {1,-5}\n", person.Value, person.Key);
        }

        public AnalyzedWindow(Thread thread, Dictionary<int, Dictionary<Contact, int>> freqs)
        {
            InitializeComponent();
            AnalzyedConvoName.Text = thread.Name;
        }

        public AnalyzedWindow(Thread thread, Dictionary<DayOfWeek, Dictionary<Contact, int>> freqs)
        {
            InitializeComponent();
            AnalzyedConvoName.Text = thread.Name;
        }

        private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
        {
//            Process.Start(_convoName + ".xlsx");
        }

        private void OpenPlotWindow(object sender, RoutedEventArgs e)
        {
            new PlotWindow {Owner = this}.Show();
        }
    }
}
