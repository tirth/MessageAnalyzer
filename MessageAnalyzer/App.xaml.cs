using System.Windows;
using System.Windows.Threading;

namespace MessageAnalyzer
{
    public partial class App
    {
        private void App_OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show("This shit went sideways, tell Tirth that: " + e.Exception.Message + "\n" + e.Exception.StackTrace, 
                "I need an adult!", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}
