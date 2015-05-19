using System;
using System.Collections.Generic;
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
using OxyPlot;
using OxyPlot.Series;

namespace MessageAnalyzer
{
    public partial class PlotWindow : Window
    {
        public PlotModel MyModel { get; private set; }

        public PlotWindow()
        {
            MyModel = new PlotModel {Title = "Test"};
            MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
        }
    }
}
