using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MessageAnalyzer
{
    public partial class FreqDialogBox
    {
        public FreqDialogBox()
        {
            InitializeComponent();

            var convoFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), @"*.txt");

            // populate list of coversations
            var convos = new ArrayList();
            foreach (var convoName in from convo in convoFiles
                select convo.Split('\\')
                into convoPath
                select convoPath[convoPath.Length - 1]
                into convoFile
                select convoFile.Split('.')[0])
                convos.Add(convoName);

            convoPicker.ItemsSource = convos;
            convoPicker.SelectedIndex = 0;
        }

        private void AnalyzeClick(object sender, RoutedEventArgs e)
        {
            Stuff.ConvoFrequencyByDate(convoPicker.SelectedItem.ToString(), true);
            StatusBlock.Text = "Analyzed " + convoPicker.SelectedItem;
        }
    }
}
