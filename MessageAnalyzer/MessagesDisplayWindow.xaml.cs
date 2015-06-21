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
    public partial class MessagesDisplayWindow
    {
        private Thread _thread;

        public MessagesDisplayWindow()
        {
            InitializeComponent();

            var convoFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), @"*.json");

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

        public void convo_picked(object sender, RoutedEventArgs e)
        {
            _thread = Thread.LoadJson(ConvoPicker.SelectedItem.ToString());

            FromDatePicker.SelectedDate = _thread.Messages[0].Date.DateTime;
            ToDatePicker.SelectedDate = _thread.Messages[_thread.Size - 1].Date.DateTime;
        }

        private void DisplayButton_OnClick(object sender, RoutedEventArgs e)
        {
            var display = new List<DisplayMessage>(_thread.Size);
            display.AddRange(_thread.Select(msg => new DisplayMessage(msg)));

            DataGrid.ItemsSource = display;
        }

        private void dataGrid_GeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (e.PropertyName == "Body")
            {
//                e.Column.CellStyle = FindResource("MessageBodyTextBox") as Style;
            }
        }
    }

    public class DisplayMessage
    {
        public string Time { get; set; }
        public string From { get; set; }
        public string Body { get; set; }
        public string Source { get; set; }

        public DisplayMessage(Message message)
        {
            Time = DateTimeOffset.FromUnixTimeMilliseconds(message.Timestamp).ToLocalTime().ToString("ddd, MMM dd-yy h:mm tt");
            From = message.Author.Name.Split(' ')[0];
            Body = message.Content;
            Source = message.Source;
        }
    }
}
