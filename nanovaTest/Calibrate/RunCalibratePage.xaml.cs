﻿using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Globalization;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.Data.Json;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.ApplicationModel.Resources;
using Windows.Storage.Streams;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using nanovaTest.Utils;
using nanovaTest.Models;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using Syncfusion.UI.Xaml.Charts;

namespace nanovaTest.Calibrate
{
    public sealed partial class RunCalibratePage : Page
    {
        private string FromSelect = null;
        //control CONFIG and STATUS button
        private int ClickStatus = 0;
        private CycleData c;
        private int total;
        private DispatcherTimer timer;
        private ObservableCollection<string> GasComboList;
        private ObservableCollection<CalibrateTestInfo> testInfoList;
        
        private int count;
        private ResourceLoader loader;
        private string methodFileName;

        Random random = new Random();
        //信号线数据源
        IList<Data> source;
        //基准线数据源
        IList<Data> standardSource;

        //下图信号线数据源
        IList<Data> bottomSource;
        //下图基准线数据源
        IList<Data> bottomStandardSource;

        private bool ViewBottom = false;
        //time profile from input
        private double Sampletimeuwp;
        private double Waitingtimeuwp;
        private double Analysistimeuwp;
        private double SetPressure = 5;
        private double Cleaningtimeuwp = 60;
        private DateTime StartDateTime = DateTime.Now;
        private DateTime CurrentDateTime = DateTime.Now;

        //temp profile from json file, length =18
        private List<double> JsonInputArray = new List<double>();
        private int heartcuttingNumber = 0;
        private double[] heartcuttingStartList = new double[6];   //[0] is empty
        private double[] heartcuttingEndList = new double[6];     //[0] is empty
        private List<String> VOCNameList = new List<string>();
        private List<double> RetentionTimeList = new List<double>();
        private List<double> RetentionTime2DList = new List<double>();
        private double CalibrationFactor = 1.0;
        private double CalibrationFactor2D = 1.0;
        private List<double> ResposeFactorList = new List<double>();

        //Serial communication with arduino
        private DeviceInformationCollection services;
        private DataWriter writer;
        private DataReader reader;
        private SerialDevice serialDevice;
        private CancellationTokenSource ReadCancellationTokenSource;

        //Serial input from arduino(signals)
        private string ReadInputStr = "";
        private string ExtractingStr = "";
        private string PartialStr = "";
        private double UsedTime = -5;
        private double PIDPrimary = 0;
        private double PID2D = 0;
        private double ActualTemp = 25;
        private double SetpointTemp = 25;
        private double ActualPressure = 0;
        private Boolean RunningTestFlag = false;
        private Boolean ReportSavedFlag = false;
        public List<double> x1 = new List<double>();     //1D signal
        public List<double> y1 = new List<double>();
        public List<double> x_b = new List<double>();     //baseline
        public List<double> y_b1 = new List<double>();
        public List<int> peaks1 = new List<int>();//save all the peaks
        private List<int> bottoms1 = new List<int>();//save all the bottoms
        private List<double> Area1 = new List<double>();
        private List<double> Heights1 = new List<double>();
        public List<double> x2 = new List<double>();     //2D signal
        public List<double> y2 = new List<double>();
        public List<double> y_b2 = new List<double>();   //2D baseline
        public List<double> x_p2 = new List<double>();   // 2D peakline
        public List<double> y_p2 = new List<double>();
        public List<int> peaks2 = new List<int>();      //save all the peaks
        private List<int> bottoms2 = new List<int>();   //save all the bottoms
        private List<double> Area2 = new List<double>();
        private List<double> Heights2 = new List<double>();

        //write into files
        private string FileNameTime = "";
        private Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private Windows.Storage.StorageFile Rawfile;

        public RunCalibratePage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            loader = new ResourceLoader();
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            testInfoList = new ObservableCollection<CalibrateTestInfo>();
            initPage();
        }

        private async void initPage()
        {
            try
            {
                LoadingIndicator.IsActive = true;
                CalibrateGrid.Visibility = Visibility.Collapsed;
                testInfoList = new ObservableCollection<CalibrateTestInfo>();
                initTopChart();
                devices_list();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            finally
            {
                LoadingIndicator.IsActive = false;
                CalibrateGrid.Visibility = Visibility.Visible;
                LoadingGrid.Visibility = Visibility.Collapsed;
            }
        }
        private void initialAllArray()
        {
            CurrentStepRemainTimeText.FontSize = 20;
            //initial all data from last test
            source.Clear();
            standardSource.Clear();
            bottomSource.Clear();
            bottomStandardSource.Clear();
            testInfoList.Clear();
            initTopChart();
            this.Basic_Chart.Series[0].ItemsSource = null;
            this.Basic_Chart.Series[1].ItemsSource = null;
            this.Basic_Chart1.Series[0].ItemsSource = null;
            this.Basic_Chart1.Series[1].ItemsSource = null;
            ReadInputStr = "";
            ExtractingStr = "";
            PartialStr = "";
            UsedTime = -5;
            PIDPrimary = 0;
            PID2D = 0;
            ActualTemp = 25;
            SetpointTemp = 25;
            ActualPressure = 0;
            RunningTestFlag = false;
            ReportSavedFlag = false;
            x1.Clear();
            y1.Clear();
            x_b.Clear();     //baseline
            y_b1.Clear();
            peaks1.Clear();//save all the peaks
            bottoms1.Clear(); //save all the bottoms
            Area1.Clear();
            Heights1.Clear();
            x2.Clear();     //2D signal
            y2.Clear();
            y_b2.Clear();  //2D baseline
            x_p2.Clear();    // 2D peakline
            y_p2.Clear();
            peaks2.Clear();     //save all the peaks
            bottoms2.Clear();    //save all the bottoms
            Area2.Clear();
            Heights2.Clear();
        }
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter.ToString() != "True")
            {
                GasComboList = new ObservableCollection<string>();
                FromSelect = (string)e.Parameter;
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                if (null != FromSelect)
                {
                    var fileName = "";
                    switch (FromSelect)
                    {
                        case "Cleaning":
                            MethodName.Text = loader.GetString("DeepClean1");
                            fileName = "Cleaning.json";
                            methodFileName = "Cleaning";
                            break;
                        case "TVOC":
                            MethodName.Text = "TVOC";
                            fileName = "TVOC.json";
                            methodFileName = "TVOC";
                            break;
                        case "BTEX":
                            MethodName.Text = "BTEX";
                            fileName = "BTEX.json";
                            methodFileName = "BTEX";
                            break;
                        case "MTBE":
                            MethodName.Text = "MTBE";
                            fileName = "MTBE.json";
                            methodFileName = "MTBE";
                            break;
                        case "TCE/PCE":
                            MethodName.Text = "TCE/PCE";
                            fileName = "TCEPCE.json";
                            methodFileName = "TCE&PCE";
                            break;
                        case "Malodorous":
                            MethodName.Text = loader.GetString("MalodorousGas1");
                            fileName = "Malodorous.json";
                            methodFileName = "Malodorous Gas";
                            break;
                        case "VehicleIndoor":
                            MethodName.Text = loader.GetString("Vehicle1");
                            fileName = "Vehicle.json";
                            methodFileName = "Vehicle";
                            break;
                        case "EnvironmentalAir":
                            MethodName.Text = loader.GetString("AirQuality1");
                            fileName = "AirQuality.json";
                            methodFileName = "Air Quality";
                            break;
                        case "PollutionSource":
                            MethodName.Text = loader.GetString("PollutionSource1");
                            fileName = "PollutionSource.json";
                            methodFileName = "Pollution Source";
                            break;
                        case "WaterSample-Online":
                            MethodName.Text = loader.GetString("WaterQuality1");
                            fileName = "WaterQuality.json";
                            methodFileName = "Water Quality";
                            break;
                        default:
                            break;
                    }
                    initGas(fileName);
                    GasComboBox.ItemsSource = GasComboList;
                }
            }
        }
        private async void ReadFromJson(string fileName)
        {
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var methodfile = await folder.GetFileAsync(fileName);
            using (var stream = await methodfile.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    JsonObject json = JsonObject.Parse(content);
                    heartcuttingNumber = (int)json.GetNamedNumber("heartcuttingNumber");
                    SetPressure = json.GetNamedNumber("SetPressure");
                    JsonInputArray.Add(json.GetNamedNumber("lowestTempvalue"));
                    JsonInputArray.Add(json.GetNamedNumber("lowestTvalue"));
                    JsonInputArray.Add(json.GetNamedNumber("Temp1value"));
                    JsonInputArray.Add(json.GetNamedNumber("HoldT1value"));
                    JsonInputArray.Add(json.GetNamedNumber("RampSpeed1value"));
                    JsonInputArray.Add(json.GetNamedNumber("Temp2value"));
                    JsonInputArray.Add(json.GetNamedNumber("HoldT2value"));
                    JsonInputArray.Add(json.GetNamedNumber("RampSpeed2value"));
                    JsonInputArray.Add(json.GetNamedNumber("heartcuttingNumber"));
                    JsonInputArray.Add(json.GetNamedNumber("cleaningNumber"));
                    //the first one of heartcuttig array is always empty
                    for (var index = 1; index < json.GetNamedArray("heartcuttingStartList").Count; index++)
                    {
                        var cutsecond = json.GetNamedArray("heartcuttingStartList")[index];
                        JsonInputArray.Add(cutsecond.GetNumber());
                        heartcuttingStartList[index] = cutsecond.GetNumber();
                    }
                    for (var index = 1; index < json.GetNamedArray("heartcuttingEndList").Count; index++)
                    {
                        var cutsecond = json.GetNamedArray("heartcuttingEndList")[index];
                        JsonInputArray.Add(cutsecond.GetNumber());
                        heartcuttingEndList[index] = cutsecond.GetNumber();
                    }
                    for (var index = 0; index < json.GetNamedArray("VOCList").Count; index++)
                    {
                        var cutsecond = json.GetNamedArray("VOCList")[index];
                        VOCNameList.Add(cutsecond.GetString());
                    }
                    for (var index = 0; index < json.GetNamedArray("VOCRetentionTime").Count; index++)
                    {
                        var cutsecond = json.GetNamedArray("VOCRetentionTime")[index];
                        RetentionTimeList.Add(cutsecond.GetNumber());
                    }
                    CalibrationFactor = json.GetNamedNumber("CalibrationFactor");
                    if (heartcuttingNumber > 0)
                    {
                        for (var index = 0; index < json.GetNamedArray("VOCRetentionTime2D").Count; index++)
                        {
                            var cutsecond = json.GetNamedArray("VOCRetentionTime2D")[index];
                            RetentionTime2DList.Add(cutsecond.GetNumber());
                        }
                        CalibrationFactor2D = json.GetNamedNumber("CalibrationFactor2D");
                    }
                }
            }
            //read voc response factor
            var file2 = await folder.GetFileAsync("VOCS.json");
            using (var stream = await file2.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    JsonArray vocjsonarray = JsonArray.Parse(content);
                    for (int vocjsoni = 0; vocjsoni < VOCNameList.Count; vocjsoni++)
                    {
                        foreach (var jsonValue in vocjsonarray)
                        {
                            var vocjsonvalue = JsonObject.Parse(jsonValue.Stringify());
                            string currentvocname = vocjsonvalue.GetNamedString("VOCName").ToUpper();
                            if (VOCNameList[vocjsoni].ToUpper() == currentvocname)
                            {
                                ResposeFactorList.Add(vocjsonvalue.GetNamedNumber("RF"));
                                break;
                            }
                        }
                    }
                }
            }
            if (VOCNameList.Count != RetentionTimeList.Count || VOCNameList.Count != RetentionTimeList.Count)
            {
                MessageDialog popup = new MessageDialog("There are errors in VOC database.");
                await popup.ShowAsync();
            }
        }
        private async void initGas(string fileName)
        {
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync(fileName);
            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    JsonObject json = JsonObject.Parse(content);
                    for (var index = 0; index < json.GetNamedArray("VOCList").Count; index++)
                    {
                        var vocName = json.GetNamedArray("VOCList")[index];
                        GasComboList.Add(vocName.GetString());
                    }
                }
            }
        }                    

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            //Close arduino device
            CancelReadTask();
            CloseDevice();

            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(CalibratePage), null);
        }

        //设置切换状态点击事件
        private void Config_Click(object sender, RoutedEventArgs e)
        {
            if (ClickStatus == 1)
            {
                ConfigImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/config-tab-t.png"));
                ConfigText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#007DC4"));
                StatusImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/status-tab-f.png"));
                StatusText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#808080"));
                ConfigGrid.Visibility = Visibility.Visible;
                StatusGrid.Visibility = Visibility.Collapsed;
                ClickStatus = 0;
            }
        }
        //状态切换状态点击事件
        private void Status_Click(object sender, RoutedEventArgs e)
        {
            if (ClickStatus == 0)
            {
                ConfigImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/config-tab-f.png"));
                ConfigText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#808080"));
                StatusImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/status-tab-t.png"));
                StatusText.Foreground = new SolidColorBrush(CustomUtils.GetColorFromHex("#007DC4"));
                ConfigGrid.Visibility = Visibility.Collapsed;
                StatusGrid.Visibility = Visibility.Visible;
                ClickStatus = 1;
            }
        }

        //开始按钮事件
        private async void Calculation_Click(object sender, RoutedEventArgs e)
        {
            var selectGas = GasComboBox.SelectedValue;
            if (null == selectGas)
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("SelectGasInfo"));
                notifyPopup.Show();
                return;
            }

            //根据json标志显示bottom
            if (heartcuttingNumber > 0)
            {
                BottomChartGrid.Visibility = Visibility.Visible;
                TopChartGrid.Height = 450;
            }
            else
            {
                BottomChartGrid.Visibility = Visibility.Collapsed;
                TopChartGrid.Height = 700;
            }

            if (null != timer)
                timer.Stop();
            if (null != c)
                c.Update(0, 1000);
            Value = 0;
            StartCalculation.Visibility = Visibility.Collapsed;
            StopCalculation.Visibility = Visibility.Visible;
            /**********Send profile to arduino************/
            try
            {
                if (serialDevice != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    writer = new DataWriter(serialDevice.OutputStream);

                    await WriteAsync(GenerateProfileString());
                }
                else
                {
                    //ConnectionStatusText.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                //ConnectionStatusText.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (writer != null)
                {
                    writer.DetachStream();
                    writer = null;
                }
            }
            /************************************************/
            StartCountDown();
            Status_Click(new object(), new RoutedEventArgs());
            InfoListView.Visibility = Visibility.Collapsed;
            InfoListView1.Visibility = Visibility.Collapsed;
        }


        //停止按钮事件
        private async void StopCalculation_Click(object sender, RoutedEventArgs e)
        {
            StartCalculation.Visibility = Visibility.Visible;
            StopCalculation.Visibility = Visibility.Collapsed;
            if (!ReportSavedFlag)
            {
                ReportSavedFlag = true;
                //data analysis
                if (UsedTime > 0)
                {
                    //WholeDataAnalysis(x1, y1, y_b1, peaks1, bottoms1, Area1, Heights1);
                    //if (heartcuttingNumber > 0)
                        //WholeDataAnalysis(x2, y2, y_b2, peaks2, bottoms2, Area2, Heights2);
                }
                //Add baseline
                this.Basic_Chart.Series[1].ItemsSource = null;
                for (int i = 0; i < x_b.Count && i < y_b1.Count; i++)
                {
                    standardSource.Add(new Data(x_b[i], y_b1[i]));
                }
                this.Basic_Chart.Series[1].ItemsSource = standardSource;

                //显示表格控件
                testInfoList.Clear();
                savePdf();
            }
            /**********Send profile to arduino************/
            try
            {
                if (serialDevice != null)
                {
                    // Create the DataWriter object and attach to OutputStream
                    writer = new DataWriter(serialDevice.OutputStream);

                    //Launch the WriteAsync task to perform the write
                    await WriteAsync("e");  //no cleaning
                                            //await WriteAsync("t");  //with cleaning
                }
                else
                {
                    //ConnectionStatusText.Text = "Select a device and connect";
                }
            }
            catch (Exception ex)
            {
                //ConnectionStatusText.Text = "sendTextButton_Click: " + ex.Message;
            }
            finally
            {
                // Cleanup once complete
                if (writer != null)
                {
                    writer.DetachStream();
                    writer = null;
                }
            }
            /************************************************/
        }

        private async void StartCountDown()
        {
            initialAllArray();
            total = (int)(Sampletimeuwp + Waitingtimeuwp + Analysistimeuwp + Cleaningtimeuwp); //unit:s
            c = new CycleData();
            c.data = new DoubleCollection() { 0, 1000 };
            c.i = total;
            timer = new DispatcherTimer()
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            Rount.StrokeDashArray = c.data;

            timer.Tick += Timer_Tick;
            FileNameTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            Rawfile = await storageFolder.CreateFileAsync("Cali_" + methodFileName + "_raw_" + FileNameTime + ".dat", Windows.Storage.CreationCollisionOption.ReplaceExisting);

            await Windows.Storage.FileIO.AppendTextAsync(Rawfile, "Method: " + methodFileName + "\n"
                + "Start time: " + System.DateTime.Now.ToString() + " " + System.DateTime.Now.ToString() + "\n"
                + "Sampling/Pumping time: " + Sampletimeuwp + "\n"
                + "Waiting time: " + Waitingtimeuwp + "\n"
                + "Calibration gas: " + "\n"
                + "Time,PID1,PID2,Temp,Setpoint,pressure,pwm%" + "\n");

            timer.Start();
        }

        private void Timer_Tick(object sender, object e)
        {
            if (count == 10)
            {
                c.i--;
                Value = c.i;
                c.Update((total - c.i) / (double)total * 125 / 15 * Math.PI, 1000);
                count = 0;
            }

            //更新折线图
            this.Basic_Chart.Series[0].ItemsSource = null;
            double x = UsedTime;
            double y = PIDPrimary;
            var primaryAxisMax = PrimaryAxis.Maximum;  //x
            var secondAxisMax = SecondAxis.Maximum;    //y

            if (y > Convert.ToDouble(secondAxisMax))
            {
                YaxisMax = y + 10;
            }
            if (x > Convert.ToDouble(primaryAxisMax))
            {
                XaxisMax = x + 10;
            }
            Data data = new Data(x, y);
            source.Add(data);
            this.Basic_Chart.Series[0].ItemsSource = source;


            //topDatas.Add(new ChartData() { label = total - c.i, text = 0 });

            count++;
            if (c.i == 0)
            {
                (sender as DispatcherTimer).Stop();
                StartCalculation.Visibility = Visibility.Visible;
                StopCalculation.Visibility = Visibility.Collapsed;
                this.Basic_Chart.Series[1].ItemsSource = null;
                for (int i = 0; i < 200; i++)
                {
                    standardSource.Add(new Data(random.Next(3, 120), random.Next(2, 6)));
                }
                this.Basic_Chart.Series[1].ItemsSource = standardSource;

                testInfoList.Clear();
                testInfoList.Add(new CalibrateTestInfo { ID = "1", VOCName = "Run Test", Time = "2017-02-22 17:30", Area = "aaa", Height = "eee", ConcentrationFactor = "qqqq" });
                testInfoList.Add(new CalibrateTestInfo { ID = "1", VOCName = "Run Test", Time = "2017-02-22 17:30", Area = "aaa", Height = "eee", ConcentrationFactor = "qqqq" });
                //显示表格控件
                InfoListView.Visibility = Visibility;
                savePdf();
            }
        }

        public class CycleData : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler PropertyChanged;

            private int _i;

            private DoubleCollection _data;

            public DoubleCollection data
            {
                get { return _data; }
                set { _data = value; }
            }

            public String count
            {
                get { return _i.ToString(); }
                set
                {
                    _i = Convert.ToInt32(value);

                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
                }
            }

            public int i
            {
                get { return _i; }
                set
                {
                    _i = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
                }
            }

            public void Update(double v, double p)
            {
                this._data.Clear();
                _data.Add(v);
                _data.Add(p);
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(""));
            }
        }


        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
            "Value", typeof(double), typeof(MainPage), new PropertyMetadata(default(double)));

        public double Value
        {
            get
            {
                return (double)GetValue(ValueProperty);
            }
            set
            {
                SetValue(ValueProperty, value);
            }
        }

        public static readonly DependencyProperty XaxisMaxProperty = DependencyProperty.Register(
            "XaxisMax", typeof(double), typeof(MainPage), new PropertyMetadata(default(double)));

        public double XaxisMax
        {
            get
            {
                return (double)GetValue(XaxisMaxProperty);
            }
            set
            {
                SetValue(XaxisMaxProperty, value);
            }
        }

        public static readonly DependencyProperty YaxisMaxProperty = DependencyProperty.Register(
            "YaxisMax", typeof(double), typeof(MainPage), new PropertyMetadata(default(double)));

        public double YaxisMax
        {
            get
            {
                return (double)GetValue(YaxisMaxProperty);
            }
            set
            {
                SetValue(YaxisMaxProperty, value);
            }
        }

        public static readonly DependencyProperty XaxisMaxProperty1 = DependencyProperty.Register(
            "XaxisMax1", typeof(double), typeof(MainPage), new PropertyMetadata(default(double)));

        public double XaxisMax1
        {
            get
            {
                return (double)GetValue(XaxisMaxProperty1);
            }
            set
            {
                SetValue(XaxisMaxProperty1, value);
            }
        }

        public static readonly DependencyProperty YaxisMaxProperty1 = DependencyProperty.Register(
            "YaxisMax1", typeof(double), typeof(MainPage), new PropertyMetadata(default(double)));

        public double YaxisMax1
        {
            get
            {
                return (double)GetValue(YaxisMaxProperty1);
            }
            set
            {
                SetValue(YaxisMaxProperty1, value);
            }
        }

        private void RountProgress_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void initTopChart()
        {
            source = new List<Data>();
            standardSource = new List<Data>();
            XaxisMax = 100;
            YaxisMax = 20;

            bottomSource = new List<Data>();
            bottomStandardSource = new List<Data>();
            XaxisMax1 = 100;
            YaxisMax1 = 20;
        }

        private async void savePdf()
        {
            //Create a new PDF document.
            using (PdfDocument document = new PdfDocument())
            {
                //Add a page in the PDF document.
                PdfPage page = document.Pages.Add();
                //Access the PDF graphics instance of the page.
                PdfGraphics graphics = page.Graphics;
                //Create the PDF font instance.
                //PdfFont font = new PdfStandardFont(PdfFontFamily.Helvetica, 12, PdfFontStyle.Bold);
                PdfFont font = new PdfCjkStandardFont(PdfCjkFontFamily.SinoTypeSongLight, 12 , PdfFontStyle.Bold);
                PdfStringFormat sf = new PdfStringFormat();
                sf.Alignment = PdfTextAlignment.Center;
                sf.LineAlignment = PdfVerticalAlignment.Middle;
                
                RectangleF rf = new RectangleF(page.Graphics.ClientSize.Width / 2 - 200, 0, 400, 30);
                graphics.DrawString(string.Format("NovaTest {0}({1})",loader.GetString("Report"), MethodName.Text), font, PdfBrushes.Black, rf, sf);

                RectangleF rf1 = new RectangleF(0, 35, 400, 40);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("Method"), MethodName.Text), font, PdfBrushes.Black, rf1);

                RectangleF rf2 = new RectangleF(220, 35, 400, 40);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("StartTime"), DateTime.Now.ToString("F", DateTimeFormatInfo.InvariantInfo)), font, PdfBrushes.Black, rf2);

                RectangleF rf3 = new RectangleF(0, 55, 400, 40);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("SamplingPumpingTime"), SamplingTimeText.Text), font, PdfBrushes.Black, rf3);

                RectangleF rf4 = new RectangleF(220, 55, 400, 40);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("WaitingTime"), WaitTimeText.Text), font, PdfBrushes.Black, rf4);

                RectangleF rf5 = new RectangleF(0, 75, 400, 40);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("SelectGas1"), GasComboBox.SelectedValue.ToString()), font, PdfBrushes.Black, rf5);

                RectangleF rf6 = new RectangleF(220, 75, 400, 40);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("ConcentrationPPB"), ConcentrationName.Text), font, PdfBrushes.Black, rf6);


                //Initializing to render to Bitmap
                var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var renderTargetBitmap = new RenderTargetBitmap();

                //Create the Bitmpa from xaml page
                await renderTargetBitmap.RenderAsync(CustomGrid, 510, 1600);
                //CustomImage.Source = renderTargetBitmap;
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                //Save the XAML in Bitmap image
                using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                {

                    var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                    encoder.SetPixelData(
                        BitmapPixelFormat.Bgra8,
                        BitmapAlphaMode.Ignore,
                        (uint)renderTargetBitmap.PixelWidth,
                        (uint)renderTargetBitmap.PixelHeight,
                        logicalDpi,
                        logicalDpi,
                        pixelBuffer.ToArray());

                    await encoder.FlushAsync();

                    //Load and draw the Bitmap image in PDF
                    //PdfImage img = PdfImage.FromStream(stream.AsStream());
                    //Task<IRandomAccessStream> s = GenerateImage(TopGrid);

                    PdfImage img = PdfImage.FromStream(stream.AsStream());
                    //PdfBitmap image = new PdfBitmap(renderTargetBitmap.);
                    graphics.DrawImage(img, new RectangleF(0, 105, 510, 450));
                }

                //Save the Pdf document
                MemoryStream docStream = new MemoryStream();
                document.Save(docStream);
                document.Close(true);
                //fileFloder dir calibrate -->methodFileName -->dateTimeFileName
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder calibrateFolder = await applicationFolder.CreateFolderAsync("calibrate", 
                    CreationCollisionOption.OpenIfExists);
                StorageFolder pdfFolder = await calibrateFolder.CreateFolderAsync(methodFileName,
                    CreationCollisionOption.OpenIfExists);
                StorageFile savePdfFile = await pdfFolder.CreateFileAsync(DateTime.Now.Ticks + ".pdf",
                    CreationCollisionOption.OpenIfExists);
                using (var stream = await savePdfFile.OpenAsync(FileAccessMode.ReadWrite))
                {
                    Stream st = stream.AsStreamForWrite();
                    st.Write(docStream.ToArray(), 0, (int)docStream.Length);
                    st.Flush();
                    st.Dispose();
                }
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("SaveSuccess"));
                notifyPopup.Show();
            }
        }
        //Connect to arduino
        public async void devices_list()
        {
            //string selector = SerialDevice.GetDeviceSelector("COM14");
            ushort vid = 0x1A86;
            ushort pid = 0x7523;
            //ushort pid = 0x0042;
            string selector = SerialDevice.GetDeviceSelectorFromUsbVidPid(vid, pid);
            services = await DeviceInformation.FindAllAsync(selector);

            //add other possible port
            if (selector.Length < 260)
            {
                vid = 0x2341;
                pid = 0x0010;
                selector = SerialDevice.GetDeviceSelectorFromUsbVidPid(vid, pid);
            }
            //end add other port

            if (services.Count > 0)
            {
                DeviceInformation deviceInfo = services[0];
                try
                {
                    serialDevice = await SerialDevice.FromIdAsync(deviceInfo.Id);

                    serialDevice.WriteTimeout = TimeSpan.FromMilliseconds(3000);
                    serialDevice.ReadTimeout = TimeSpan.FromMilliseconds(3000);
                    serialDevice.BaudRate = 250000;
                    serialDevice.Parity = SerialParity.None;
                    serialDevice.StopBits = SerialStopBitCount.One;
                    serialDevice.DataBits = 8;
                    serialDevice.Handshake = SerialHandshake.None;

                    // Create cancellation token object to close I/O operations when closing the device
                    ReadCancellationTokenSource = new CancellationTokenSource();
                    Listen();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
            else
            {
                MessageDialog popup = new MessageDialog(loader.GetString("NoDeviceNotice"));
                await popup.ShowAsync();
            }

        }
        /// <summary>
        /// WriteAsync: Task that asynchronously writes data from the input text box 'sendText' to the OutputStream 
        /// </summary>
        /// <returns></returns>
        private async Task WriteAsync(string outputData)   //s---start, d---double, i---int, e---end
        {
            Task<UInt32> storeAsyncTask;
            // Load the text from the sendText input text box to the dataWriter object
            writer.WriteString(outputData);

            // Launch an async task to complete the write operation
            storeAsyncTask = writer.StoreAsync().AsTask();

            UInt32 bytesWritten = await storeAsyncTask;
        }

        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <summary>
        /// - Create a DataReader object
        /// - Create an async task to read from the SerialDevice InputStream
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Listen()
        {
            try
            {
                if (serialDevice != null)
                {
                    reader = new DataReader(serialDevice.InputStream);
                    // keep reading the serial input
                    while (true)
                    {
                        await ReadAsync(ReadCancellationTokenSource.Token);
                    }
                }
            }
            catch (TaskCanceledException tce)
            {
                CloseDevice();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                // Cleanup once complete
                if (reader != null)
                {
                    reader.DetachStream();
                    reader = null;
                }
            }
        }
        private void CloseDevice()
        {
            if (serialDevice != null)
            {
                serialDevice.Dispose();
            }
            serialDevice = null;
        }
        /// <summary>
        /// ReadAsync: Task that waits on data and reads asynchronously from the serial device InputStream
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task ReadAsync(CancellationToken cancellationToken)
        {
            Task<UInt32> loadAsyncTask;

            uint ReadBufferLength = 90;

            // If task cancellation was requested, comply
            cancellationToken.ThrowIfCancellationRequested();

            // Set InputStreamOptions to complete the asynchronous read operation when one or more bytes is available
            reader.InputStreamOptions = InputStreamOptions.Partial;
            reader.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            reader.ByteOrder = Windows.Storage.Streams.ByteOrder.LittleEndian;

            using (var childCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                // Create a task object to wait for data on the serialPort.InputStream
                loadAsyncTask = reader.LoadAsync(ReadBufferLength).AsTask(childCancellationTokenSource.Token);

                // Launch the task and wait
                UInt32 bytesRead = await loadAsyncTask;
                if (bytesRead > 0)
                {
                    ReadInputStr = reader.ReadString(bytesRead);
                    ExtractSignals(ReadInputStr);
                    if (UsedTime > 0)
                    {
                        await Windows.Storage.FileIO.AppendTextAsync(Rawfile, ExtractingStr);
                    }
                    if (!Convert.ToBoolean(string.Compare(ReadInputStr, "Ready")))
                    {
                        StartTestFunction();
                    }
                }
            }
        }
        private void ExtractSignals(string BufferInputString)
        {
            if (BufferInputString.Length > 6)
            {
                int LineStart = BufferInputString.IndexOf('\n');
                if (LineStart >= 0 && LineStart < BufferInputString.Length)
                {
                    ExtractingStr = PartialStr + BufferInputString.Substring(0, LineStart);
                    PartialStr = BufferInputString.Substring(LineStart);
                }
                string[] RawNumbersStr = ExtractingStr.Split(',');
                if (RawNumbersStr.Length > 5)
                {
                    UsedTime = float.Parse(RawNumbersStr[0]);
                    PIDPrimary = Movingaverage(float.Parse(RawNumbersStr[1]) / 25, 1); //unit:V * 40
                    PID2D = float.Parse(RawNumbersStr[2]) / 25;  //unit:V * 40
                    ActualTemp = float.Parse(RawNumbersStr[3]);
                    SetpointTemp = float.Parse(RawNumbersStr[4]);
                    ActualPressure = float.Parse(RawNumbersStr[5]);
                    //End test
                    if (Math.Abs(UsedTime - (-5)) < 0.1)
                    {
                        FinishTestFunction();
                    }
                    //Add signal to list
                    if (!ReportSavedFlag && UsedTime >= 0)
                    {
                        x1.Add(UsedTime);
                        y1.Add(PIDPrimary);
                        if (heartcuttingNumber > 0)
                        {
                            PID2D = Movingaverage(PID2D, 2);
                            x2.Add(UsedTime);
                            y2.Add(PID2D);
                        }
                    }
                }
            }

        }
        /// <summary>
        /// CancelReadTask:
        /// - Uses the ReadCancellationTokenSource to cancel read operations
        /// </summary>
        private void CancelReadTask()
        {
            if (ReadCancellationTokenSource != null)
            {
                if (!ReadCancellationTokenSource.IsCancellationRequested)
                {
                    ReadCancellationTokenSource.Cancel();
                }
            }
        }

        private string GenerateProfileString()
        {
            string ProfileString = "!";
            Sampletimeuwp = float.Parse(SamplingTimeText.Text) * 60;
            Waitingtimeuwp = float.Parse(WaitTimeText.Text);
            double lowestTempvalue = JsonInputArray[0];
            double lowestTvalue = JsonInputArray[1];
            double Temp1value = JsonInputArray[2];
            double HoldT1value = JsonInputArray[3] * 60;
            double RampSpeed1value = JsonInputArray[4] / 60.0;
            double Temp2value = JsonInputArray[5];
            double HoldT2value = JsonInputArray[6] * 60;
            double RampSpeed2value = JsonInputArray[7] / 60.0;
            heartcuttingNumber = (int)JsonInputArray[8];
            Analysistimeuwp = lowestTvalue + HoldT1value + HoldT2value + (Temp1value - lowestTempvalue) / RampSpeed1value + (Temp2value - Temp1value) / RampSpeed2value; //s
            ProfileString += (float.Parse(SamplingTimeText.Text) * 60).ToString() + ",";
            ProfileString += SetPressure.ToString() + ",";
            ProfileString += float.Parse(WaitTimeText.Text).ToString() + ",";
            ProfileString += lowestTempvalue.ToString() + ",";
            ProfileString += lowestTvalue.ToString() + ",";
            ProfileString += Temp1value.ToString() + ",";
            ProfileString += HoldT1value.ToString() + ",";
            ProfileString += RampSpeed1value.ToString() + ",";
            ProfileString += Temp2value.ToString() + ",";
            ProfileString += HoldT2value.ToString() + ",";
            ProfileString += RampSpeed2value.ToString() + ",";
            ProfileString += heartcuttingNumber.ToString() + ",";
            ProfileString += heartcuttingStartList[1].ToString() + ",";
            ProfileString += heartcuttingStartList[2].ToString() + ",";
            ProfileString += heartcuttingStartList[3].ToString() + ",";
            ProfileString += heartcuttingStartList[4].ToString() + ",";
            ProfileString += heartcuttingEndList[1].ToString() + ",";
            ProfileString += heartcuttingEndList[2].ToString() + ",";
            ProfileString += heartcuttingEndList[3].ToString() + ",";
            ProfileString += heartcuttingEndList[4].ToString() + ",";
            ProfileString += lowestTempvalue.ToString() + ",";
            ProfileString += ((int)JsonInputArray[9]).ToString() + "," + '\n';
            Cleaningtimeuwp = (int)JsonInputArray[9] * 60;
            SetPressureText.Text = SetPressure.ToString();
            return ProfileString;
        }
        private async void StartTestFunction()
        {
            try
            {

                // Create the DataWriter object and attach to OutputStream
                writer = new DataWriter(serialDevice.OutputStream);

                //Launch the WriteAsync task to perform the write
                await WriteAsync("s");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("sendTextButton_Click: " + ex.Message);
            }
            finally
            {
                // Cleanup once complete
                if (writer != null)
                {
                    writer.DetachStream();
                    writer = null;
                }
            }
        }
        private void FinishTestFunction()
        {
            StartCalculation.Visibility = Visibility.Visible;
            StopCalculation.Visibility = Visibility.Collapsed;
            Config_Click(new object(), new RoutedEventArgs());
            timer.Stop();
            c.Update(0, 1000);
            Value = 0;
            RunningTestFlag = false;
            ReadInputStr = "";
            ExtractingStr = "";
            PartialStr = "";
            UsedTime = -5;
            if (heartcuttingNumber > 0)
            {
                BottomChartGrid.Visibility = Visibility.Visible;
                TopChartGrid.Height = 450;
            }
            else
            {
                BottomChartGrid.Visibility = Visibility.Collapsed;
                TopChartGrid.Height = 650;
            }
        }
        //******************************Data analysis process*******************************//
        private int constant_m = 25; // for SNIP baseline formula
        private int constant_m_end = Convert.ToInt32(1 / Math.Sqrt(2)); // for SNIP baseline formula
        private int CONSECUTIVE_SCAN_STEPS = 3;   //for peak detection
        private double THRESHOLD = 0.005f;        //for peak detection: slope
        private double THRESHOLD_peak = 3.0;       //for peak detection: slope

        public void WholeDataAnalysis(List<double> OriginalXv, List<double> OriginalYv, List<double> BaseLineYv, List<int> peaksv, List<int> bottomsv, List<double> Areav, List<double> Heightsv, double MinY)
        {
            // initialize y_b
            for (int v = 0; v < OriginalXv.Count; v++)
            {
                if (OriginalYv[v] < MinY)
                    MinY = OriginalYv[v];
                x_b.Add(OriginalXv[v]);
                BaseLineYv.Add(MinY);
            }
            SNIPBaseline(OriginalXv, OriginalYv, BaseLineYv, peaksv, bottomsv, Areav, Heightsv, MinY);
            CalculateIntegration(OriginalXv, OriginalYv, BaseLineYv, peaksv, bottomsv, Areav, Heightsv);
        }

        //SNIP Baseline algorithm
        private void SNIPBaseline(List<double> OriginalX, List<double> OriginalY, List<double> BaseLineY, List<int> peaks, List<int> bottoms, List<double> Area, List<double> Heights, double MinY)
        {
            int count = BaseLineY.Count;
            List<double> temp = new List<double>();
            //LLS
            for (int i = 0; i < count; i++)
            {
                BaseLineY[i] = Math.Log(Math.Log(Math.Sqrt(OriginalY[i] + 1) + 1) + 1);
            }
            ////iteration for 10 times
            //for (int z = 0; z < 25; z++)
            // {
            //     for (int m = constant_m; m < count - constant_m; m++)
            //     {
            //         temp.Add(Math.Min(BaseLineY[m], (BaseLineY[m - constant_m] + BaseLineY[m + constant_m]) / 2));
            //     }
            //     for (int n = constant_m; n < count - constant_m; n++)
            //     {
            //         BaseLineY[n] = temp[n - constant_m];
            //     }
            // }

            //Yuhan
            for (int p = 1; p < constant_m; p++)
            {
                for (int i = p + 1; i < count - p; i++)
                {
                    BaseLineY[i] = Math.Min(BaseLineY[i], (BaseLineY[i + p] + BaseLineY[i - p]) / 2);
                }
            }

            //LLS inverse
            for (int i = 0; i < count; i++)
            {
                BaseLineY[i] = Math.Pow((Math.Exp(Math.Exp(BaseLineY[i]) - 1) - 1), 2) - 1;
            }
        }

        ////******************************************************************************************************************************************************************************************
        ////SNIP Baseline algorithm
        //private void SNIPBaseline(List<double> OriginalX, List<double> OriginalY, List<double> BaseLineY, List<int> peaks, List<int> bottoms, List<double> Area, List<double> Heights, double MinY)
        //{
        //    int indexX = 0;
        //    double valueY = 100;
        //    int size = 0;
        //    List<double> temp = new List<double>();
        //    List<double> temp_x_b = new List<double>();
        //    List<double> temp_y_b = new List<double>();

        //    detectPeakAndBottom(OriginalX, OriginalY, peaks, bottoms, Area, Heights, MinY);
        //    size = bottoms.Count;
        //    for (int i = 0; i < size; i++)
        //    {
        //        indexX = bottoms[i];
        //        temp_x_b.Add(OriginalX[indexX]);
        //        valueY = OriginalY[indexX];
        //        temp_y_b.Add(valueY);
        //    }


        //    //smmooth baseline
        //    //iteration for 16 times from index 20
        //    for (int z = 0; z < 16; z++)
        //    {
        //        for (int m = constant_m; m < size - constant_m; m++)
        //        {
        //            temp.Add(Math.Min(temp_y_b[m], (temp_y_b[m - constant_m] + temp_y_b[m + constant_m]) / 2));

        //        }
        //        for (int n = constant_m; n < size - constant_m; n++)
        //        {
        //            temp_y_b[n] = temp[n - constant_m];

        //        }

        //    }

        //    //iteration for 8 times from index 20
        //    temp.Clear();

        //    for (int t = 0; t < 8; t++)
        //    {
        //        for (int p = constant_m; p < size - constant_m; p++)
        //        {
        //            temp.Add(Math.Min(temp_y_b[p], (temp_y_b[p - constant_m_end] + temp_y_b[p + constant_m_end]) / 2));

        //        }
        //        for (int q = constant_m; q < size - constant_m; q++)
        //        {
        //            temp_y_b[q] = temp[q - constant_m];

        //        }

        //    }
        //    //************************************************************************************************************************************************************************

        /*
        //make baseline consecutive, get all the coordination of point in between
        int bottomCount = bottoms.Count;
        double k = 0; //slope
        double b = 0; //intersection

        //calculate the points before first bottom
        int start;
        double value;
        int index;
        if (temp_x_b.Count > 0)
        {
            for (start = 0; OriginalX[start] <= temp_x_b[0]; start++)
                BaseLineY[start] = temp_y_b[0];
                ;
            //calculate the points before last bottom
            for (int a = 0; a < bottomCount - 1; a++)
            {
                k = (temp_y_b[a + 1] - temp_y_b[a]) / (temp_x_b[a + 1] - temp_x_b[a]);
                b = temp_y_b[a] - k * temp_x_b[a];

                index = 0;
                while (OriginalX[start + index] <= temp_x_b[a + 1])
                {
                    value = k * OriginalX[start + index] + b;
                    BaseLineY[start + index] = value;
                    index++;
                }
                start = start + index;

            }
            //calculate the points after last bottom
            for (int p = OriginalX.Count - 1; OriginalX[p] >= temp_x_b[(bottomCount - 1)]; p--)
            {
                BaseLineY[p] = temp_y_b[bottomCount - 1];
            }
        }*/
        //}

        // peak and bottom detection
        private void detectPeakAndBottom(List<double> OriginalX, List<double> OriginalY, List<int> peaks, List<int> bottoms, List<double> Area, List<double> Heights, double MinY)
        {
            int signalAmount = OriginalX.Count;
            List<double> slopes = new List<double>();
            int size = 0;
            int peakMax = 0; // index of maximum
            int peakStart = 0;
            int peakStop = 0;
            double slope = 0; //current slope
            List<double> values = new List<double>(); //save three consecutive slopes

            //calculate the slopes of all scans
            for (int b = 0; b < signalAmount - 1; b++)
            {
                double slopeTemp = (OriginalY[b + 1] - OriginalY[b]) / (OriginalX[b + 1] - OriginalX[b]);
                slopes.Add(slopeTemp);
            }
            slopes.Add(0);// the slope of the last scan
            size = slopes.Count;


            for (int scan = 0; scan < size - CONSECUTIVE_SCAN_STEPS; scan++)
            {
                slope = slopes[scan];
                if (slope > THRESHOLD)
                {
                    /*
                    * Get the actual and the next slope values.
                    */
                    values.Clear();
                    for (int j = 0; j < CONSECUTIVE_SCAN_STEPS; j++)
                    {
                        if (scan + j >= size) break;
                        values.Add(slopes[scan + j]);
                    }
                    //if (valuesAreGreaterThanThreshold(values) && valuesAreIncreasing(values))
                    if (valuesAreGreaterThanThreshold(values))
                    {
                        //add the first bottom 
                        if (0 == bottoms.Count)
                        {
                            bottoms.Add(scan);
                        }
                        //find peak
                        peakStart = scan;
                        peakMax = peakStart;
                        while (slopes[peakMax] > 0 && peakMax < size - 1 && (OriginalY[peakMax] - MinY) > THRESHOLD_peak)
                        {
                            peakMax++;
                        }
                        peaks.Add(peakMax);

                        //find peakStop
                        peakStop = peakMax;
                        if (peakStop == size - 1) break; //the last scan is a peak
                        while (slopes[peakStop] <= 0 && peakStop < size - 1)
                        {
                            peakStop++;
                        }
                        bottoms.Add(peakStop);
                        scan = peakStop;
                    }
                    else
                    {
                        continue;
                    }
                }
            }
        }

        //sub method for peak detection
        private bool valuesAreGreaterThanThreshold(List<double> values)
        {
            for (int i = 0; i < CONSECUTIVE_SCAN_STEPS; i++)
            {
                if (values[i] < THRESHOLD)
                    return false;
            }
            return true;
        }

        private bool valuesAreIncreasing(List<double> values)
        {
            for (int i = 0; i < CONSECUTIVE_SCAN_STEPS - 1; i++)
            {
                if (values[i] > values[i + 1])
                    return false;
            }
            return true;
        }

        //Integration
        private bool CalculateIntegration(List<double> OriginalX, List<double> OriginalY, List<double> BaseLineY, List<int> peaks, List<int> bottoms, List<double> Area, List<double> Heights)
        {
            List<double> CorrectedY = new List<double>();
            //originalY - baseline
            for (int y = 0; y < BaseLineY.Count; y++)
            {
                if (OriginalY[y] - BaseLineY[y] > 0)
                    CorrectedY.Add(OriginalY[y] - BaseLineY[y]);
                else
                    CorrectedY.Add(0);
            }
            //integration
            if (peaks != null)
            {
                for (int index = 0; index < peaks.Count; index++)
                {
                    Area.Add(0);  //initialize
                    for (int p2 = bottoms[index]; p2 < bottoms[index + 1]; p2++)
                    {
                        Area[index] = (Area[index] + CorrectedY[p2] * 0.1);
                    }
                    Heights.Add(CorrectedY[peaks[index]]);
                    //data.Add(new Item(index + 1, OriginalX[bottoms[index]], OriginalX[peaks[index]], OriginalX[bottoms[index + 1]], Heights[index], Area[index], FWHMvalue));
                }
            }
            else //no peak is found 
            {
                return false;
            }
            return true;
        }

        //Calcualte FWHM
        private double CalculateFWHM(int startposition, int peakposition, int endposition, List<double> OriginalX, List<double> OriginalY, List<double> BaseLineY)
        {
            List<double> CorrectedY = new List<double>();
            //originalY - baseline
            for (int y = 0; y < BaseLineY.Count; y++)
            {
                if (OriginalY[y] - BaseLineY[y] > 0)
                    CorrectedY.Add(OriginalY[y] - BaseLineY[y]);
                else
                    CorrectedY.Add(0);
            }
            double FWHMresult = 0;
            double HalfHeight = CorrectedY[peakposition] / 2.0;
            double StartX = 0;
            double mindistance = 2 * HalfHeight;
            double EndX = 0;
            for (int i = startposition; i < peakposition; i++)
            {
                if (Math.Abs(OriginalY[i] - HalfHeight) < mindistance)
                {
                    mindistance = Math.Abs(CorrectedY[i] - HalfHeight);
                    StartX = OriginalX[i];
                }
            }
            mindistance = 2 * HalfHeight;
            for (int i = peakposition; i < endposition; i++)
            {
                if (Math.Abs(CorrectedY[i] - HalfHeight) < mindistance)
                {
                    mindistance = Math.Abs(CorrectedY[i] - HalfHeight);
                    EndX = OriginalX[i];
                }
            }
            FWHMresult = EndX - StartX;
            return FWHMresult;
        }
        private static int MovingWindowWidth = 5;
        private double[] MovingWindow = new double[MovingWindowWidth - 1];
        private double[] MovingWindow2 = new double[MovingWindowWidth - 1];
        private double Movingaverage(double current, int OneOrTwoD)
        {
            if (OneOrTwoD == 1)
            {
                int CurrentLength = y1.Count;
                if (CurrentLength < MovingWindowWidth - 1)
                {
                    MovingWindow[CurrentLength] = current;
                    float sum = 0;
                    for (int i = 1; i <= CurrentLength; i++)
                    {
                        sum = sum + (float)MovingWindow[CurrentLength - i];
                    }
                    return (sum + current) / (CurrentLength + 1);
                }
                else
                {
                    float sum = 0;
                    for (int i = 0; i < MovingWindowWidth - 1; i++)
                    {
                        sum = sum + (float)MovingWindow[i];
                    }
                    for (int i = 0; i < MovingWindowWidth - 2; i++)
                    {
                        MovingWindow[i] = MovingWindow[i + 1];
                    }
                    MovingWindow[MovingWindowWidth - 2] = current;
                    return (sum + current) / (double)MovingWindowWidth;
                }
            }
            else
            {
                int CurrentLength = y2.Count;
                if (CurrentLength < MovingWindowWidth - 1)
                {
                    MovingWindow2[CurrentLength] = current;
                    float sum = 0;
                    for (int i = 1; i <= CurrentLength; i++)
                    {
                        sum = sum + (float)MovingWindow2[CurrentLength - i];
                    }
                    return (sum + current) / (CurrentLength + 1);
                }
                else
                {
                    float sum = 0;
                    for (int i = 0; i < MovingWindowWidth - 1; i++)
                    {
                        sum = sum + (float)MovingWindow2[i];
                    }
                    for (int i = 0; i < MovingWindowWidth - 2; i++)
                    {
                        MovingWindow2[i] = MovingWindow2[i + 1];
                    }
                    MovingWindow2[MovingWindowWidth - 2] = current;
                    return (sum + current) / (double)MovingWindowWidth;
                }
            }
        }
        //**********************************************************************************//
    }

    public class Data
    {
        public Data(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X
        {
            get;
            set;
        }

        public double Y
        {
            get;
            set;
        }
    }
}
