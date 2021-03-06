﻿using System;
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
            var dlg = new FbDialogBox() { Owner = this };

            var result = dlg.ShowDialog();

            //            StatusBlock.Text = result == true ? "Done." : "Nope.";
        }

        private void WaButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new WhatsAppDialogBox() { Owner = this };

            var result = dlg.ShowDialog();
        }

        private void SmsButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new UnderConstructionDialog() { Owner = this }.ShowDialog();
        }

        private void FreqButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new FreqDialogBox() { Owner = this };

            var result = dlg.ShowDialog();

            //            StatusBlock.Text = result == true ? "Done." : "Nope.";
        }

        private void WordFreqButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new WordFreqDialogBox() { Owner = this };

            var result = dlg.ShowDialog();
        }

        private void ViewButton_OnClick(object sender, RoutedEventArgs e)
        {
            var dlg = new MessagesDisplayWindow() { Owner = this };

            var result = dlg.ShowDialog();
        }
    }
}
