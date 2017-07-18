using Syncfusion.UI.Xaml.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;

namespace nanovaTest.Models
{
    public class ChartViewModel
    {

        int _ChartStripLineStartExcellent = default(int);
        int _ChartStripLineStartGood = default(int);
        int _ChartStripLineStartPoor = default(int);
        int _ChartStripLineWidthExcellent = default(int);
        int _ChartStripLineWidthGood = default(int);
        int _ChartStripLineWidthPoor = default(int);
        int _MaxChannel1 = default(int);
        int _MaxChannel2 = default(int);
        double _MaxRssi = default(double);
        int _MinChannel1 = default(int);
        int _MinChannel2 = default(int);
        double _MinRssi = default(double);
        public int ChartStripLineStartExcellent { get { return _ChartStripLineStartExcellent; } set { _ChartStripLineStartExcellent = value; } }
        public int ChartStripLineStartGood { get { return _ChartStripLineStartGood; } set { _ChartStripLineStartGood = value; } }
        public int ChartStripLineStartPoor { get { return _ChartStripLineStartPoor; } set { _ChartStripLineStartPoor = value; } }
        public int ChartStripLineWidthExcellent { get { return _ChartStripLineWidthExcellent; } set { _ChartStripLineWidthExcellent = value; } }
        public int ChartStripLineWidthGood { get { return _ChartStripLineWidthGood; } set { _ChartStripLineWidthGood = value; } }
        public int ChartStripLineWidthPoor { get { return _ChartStripLineWidthPoor; } set { _ChartStripLineWidthPoor = value; } }
        public ChartSeriesCollection Collection { get; set; } = new ChartSeriesCollection();
        public int MaxChannel1 { get { return _MaxChannel1; } set { _MaxChannel1 = value; } }
        public int MaxChannel2 { get { return _MaxChannel2; } set { _MaxChannel2 = value; } }
        public double MaxRssi { get { return _MaxRssi; } set { _MaxRssi = value; } }
        public int MinChannel1 { get { return _MinChannel1; } set { _MinChannel1 = value; } }
        public int MinChannel2 { get { return _MinChannel2; } set { _MinChannel2 = value; } }
        public double MinRssi { get { return _MinRssi; } set { _MinRssi = value; } }
        public ObservableCollection<Curve> Series { get; set; } = new ObservableCollection<Curve>();

        public void InitSampleData()
        {
            InitAux();
            //InitSeries();
            //InitCollection();
        }

        private void InitAux()
        {
            //range for first group of series
            _MinChannel1 = 0;
            _MaxChannel1 = 64;

            //range for second group of series
            _MinChannel2 = 149;
            _MaxChannel2 = 600;

            _MinRssi = 0;
            _MaxRssi = 20;

            //ChartStripLineStartExcellent = -55;
            //ChartStripLineWidthExcellent = Math.Abs(ChartStripLineStartExcellent - (int)_MaxRssi);
            //ChartStripLineStartGood = -68;
            //ChartStripLineWidthGood = Math.Abs(ChartStripLineStartGood - ChartStripLineStartExcellent);
            //ChartStripLineStartPoor = (int)_MinRssi;
            //ChartStripLineWidthPoor = Math.Abs((int)_MinRssi - ChartStripLineStartGood);
        }

        private void InitCollection()
        {
            var splineSeries = new SplineSeries();
            splineSeries.ItemsSource = Series[0].Points;
            splineSeries.XBindingPath = "x";
            splineSeries.YBindingPath = "y";
            splineSeries.Label = "";

            ChartAdornmentInfo adornments = new ChartAdornmentInfo();
            adornments.ShowLabel = true;
            adornments.HighlightOnSelection = true;
            splineSeries.AdornmentsInfo = adornments;
            splineSeries.SeriesSelectionBrush = Application.Current.Resources["SystemControlHighlightAccentBrush"] as SolidColorBrush;
            splineSeries.ShowTooltip = true;
            Collection.Add(splineSeries);
        }

        // create dummy data
        private void InitSeries()
        {
            Random random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);


            // data for the first half. x range 36-64
            for (int i = 0; i < 1; i++)
            {
                var rssiPoint = new Models.Point();
                rssiPoint.x = 38;
                rssiPoint.y = 16;

                var aPoint = new Models.Point();
                aPoint.x = 45;
                aPoint.y = 12;

                var bPoint = new Models.Point();
                bPoint.x = 55;
                bPoint.y = 10;

                var dPoint = new Models.Point();
                dPoint.x = 59;
                dPoint.y = 14;

                var ePoint = new Models.Point();
                ePoint.x = 60;
                ePoint.y = 13;

                var curve = new Curve();

                curve.Points.Add(aPoint);
                curve.Points.Add(bPoint);
                curve.Points.Add(rssiPoint);
                curve.Points.Add(dPoint);
                curve.Points.Add(ePoint);

                Series.Add(curve);
            }
            
        }

    }

    public class Series
    {
        public ObservableCollection<Curve> Curves { get; set; } = new ObservableCollection<Curve>();
    }

    public class Curve
    {
        public ObservableCollection<Point> Points { get; set; } = new ObservableCollection<Point>();
    }
    public class Point
    {
        public double x { get; set; } = default(double);
        public double y { get; set; } = default(double);
    }
}
