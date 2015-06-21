using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MessageAnalyzer
{
    public partial class AnalyzedWindow
    {        
        public AnalyzedWindow(Thread thread, Tuple<Dictionary<DateTimeOffset, Dictionary<Contact, int>>, Dictionary<Contact, int>> threadData)
        {
            InitializeComponent();
            AnalzyedConvoName.Text = thread.Name;
            var freqs = threadData.Item1;
            var people = threadData.Item2;

            var stringFreqs = (people.OrderByDescending(entry => entry.Value)).Aggregate("", 
                (current, person) => current + string.Format("{0,-10} {1,-5}\n", person.Value, person.Key));

            FrequenciesBlock.Text = stringFreqs.TrimEnd();
        }

        public AnalyzedWindow(Thread thread, Dictionary<int, Dictionary<Contact, int>> freqs)
        {
            InitializeComponent();
            AnalzyedConvoName.Text = thread.Name;

            var stringFreqs = "";

            foreach (var hourlyFreq in freqs)
            {
                stringFreqs += hourlyFreq.Key + "\n";
                stringFreqs = hourlyFreq.Value.Aggregate(stringFreqs, 
                    (current, person) => current + string.Format("   {0,-3} {1, -5}\n", person.Value, person.Key));
            }

            FrequenciesBlock.Text = stringFreqs.TrimEnd();
        }

        public AnalyzedWindow(Thread thread, Dictionary<DayOfWeek, Dictionary<Contact, int>> freqs)
        {
            InitializeComponent();
            AnalzyedConvoName.Text = thread.Name;

            var stringFreqs = "";

            foreach (var dailyFreq in freqs)
            {
                stringFreqs += dailyFreq.Key + "\n";
                stringFreqs = dailyFreq.Value.Aggregate(stringFreqs, 
                    (current, person) => current + string.Format("   {0,-3} {1, -5}\n", person.Value, person.Key));
            }

            FrequenciesBlock.Text = stringFreqs.TrimEnd();
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
