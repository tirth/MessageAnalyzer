﻿using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Windows;

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

            ConvoPicker.ItemsSource = convos;
            ConvoPicker.SelectedIndex = 0;
        }

        private void AnalyzeClick(object sender, RoutedEventArgs e)
        {
            var byLen = ByLenRadio.IsChecked.GetValueOrDefault();

            if (DateRangeRadio.IsChecked.GetValueOrDefault())
            {
                var fromD = FromDate.SelectedDate.GetValueOrDefault();
                var toD = ToDate.SelectedDate.GetValueOrDefault();

                var data = Stuff.ConvoFrequencyByDate(ConvoPicker.SelectedItem.ToString(), true, byLen,
                    new[] { fromD.Year, fromD.Month, fromD.Day }, new[] { toD.Year, toD.Month, toD.Day });
            }
            else
            {
                var data = Stuff.ConvoFrequencyByDate(ConvoPicker.SelectedItem.ToString(), true, byLen);
            }

            StatusBlock.Text = "Saved " + ConvoPicker.SelectedItem + ".xlsx";

            //            var plot = new PlotWindow() {Owner = this};
            //            var result = plot.ShowDialog();
        }

        public void display_radio(object sender, RoutedEventArgs e)
        {
            DatePickPanel.Visibility = DateRangeRadio.IsChecked.GetValueOrDefault() ? Visibility.Visible : Visibility.Collapsed;
        }

        public void convo_picked(object sender, RoutedEventArgs e)
        {
            var messages = File.ReadAllLines(ConvoPicker.SelectedItem + ".txt");

            var fromTs = Convert.ToInt64(messages[0].Split(Stuff.Sepr)[0]);
            var fromD = DateTimeOffset.FromUnixTimeMilliseconds(fromTs).ToLocalTime().DateTime;

            var toTs = Convert.ToInt64(messages[messages.Length - 1].Split(Stuff.Sepr)[0]);
            var toD = DateTimeOffset.FromUnixTimeMilliseconds(toTs).ToLocalTime().DateTime;

            FromDate.SelectedDate = fromD;
            ToDate.SelectedDate = toD;
        }
    }
}