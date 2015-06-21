using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MessageAnalyzer
{
    public partial class WordFreqDialogBox
    {
        private Thread _thread;

        public WordFreqDialogBox()
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

        public void convo_picked(object sender, RoutedEventArgs e)
        {
            _thread = Thread.LoadJson(ConvoPicker.SelectedItem.ToString());
        }

        private void show_freqs(object sender, RoutedEventArgs e)
        {
            var words = _thread.GenerateWordFrequencies();
            var boxes = new List<TextBlock> {WordFreqBlock1, WordFreqBlock2};
            var current = 0;

            foreach (var person in words)
            {
                boxes[current].Text = person.Key + "\n\n";

                var wordList = person.Value.OrderByDescending(entry => entry.Value);

                foreach (var word in wordList)
                {
                    if (word.Value > 100)
                        boxes[current].Text += word.Key + ' ' + word.Value + '\n';
                }

                current++;
            }
        }
    }
}
