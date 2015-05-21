using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;

namespace MessageAnalyzer
{
    public partial class AnalyzedWindow
    {
        private Dictionary<string, Dictionary<string, int>> _frequencies;
        private readonly string _convoName;
         
        public AnalyzedWindow(string convoName, Tuple<Dictionary<string, Dictionary<string, int>>, Dictionary<string, int>> data)
        {
            InitializeComponent();

            _convoName = convoName;
            AnalzyedConvoName.Text = convoName;

            _frequencies = data.Item1;
            var people = data.Item2;

            foreach (var person in (from person in people orderby person.Value descending select person))
                FrequenciesBlock.Text += String.Format("{0,-10} {1,-5}\n", person.Value, person.Key);
            FrequenciesBlock.Text = FrequenciesBlock.Text.TrimEnd();
        }

        private void OpenFileButton_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(_convoName + ".xlsx");
        }
    }
}
