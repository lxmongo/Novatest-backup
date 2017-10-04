using nanovaTest.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace nanovaTest.SelectMethod
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ChartsTest : Page
    {
        DispatcherTimer timer;
        IList<Data> source;
        Random random;
        int count = 0;
        //Basic_Chart1.Behaviors.Clear();
          //Basic_Chart.Behaviors.Clear();

        public ChartsTest()
        {
            this.InitializeComponent();
            InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            random = new Random();
            source = new List<Data>();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick1; ;
            timer.Start();
        }

        private void Timer_Tick1(object sender, object e)
        {
            this.updateChart.Series[0].ItemsSource = null;
            Data data = new Data(random.Next(3, 80), random.Next(2, 10));
            source.Add(data);
            this.updateChart.Series[0].ItemsSource = source;
            count++;
            if (count == 20000)
            {
                (sender as DispatcherTimer).Stop();
            }
        }
    }

}
