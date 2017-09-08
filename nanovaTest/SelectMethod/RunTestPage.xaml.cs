using System;
using System.Text;
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
using Windows.System.Profile;
using Windows.UI;
using Newtonsoft.Json.Linq;

namespace nanovaTest.SelectMethod
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class RunTestPage : Page, IDisposable
    {
        public double FlowRate = 5; //ml/min
        private string FromSelect = null;
        //control CONFIG and STATUS button
        private int ClickStatus = 0;
        private int ClickCalculationStatus = 0;
        private CycleData c;
        private int total;
        private AlertPopup popup;
        private DispatcherTimer timer;
        private ObservableCollection<SelectTestInfo> testInfoList;
        private ObservableCollection<SelectTestInfo> secondaryInfoList;

        private string MethodNameText;
        //计数，间隔100ms，则10次更新一次倒计时图
        private int count = 0;
        private ResourceLoader loader;
        private string methodFileName;
        public ChartAdornmentInfo AdornmentInfo { get; set; }
        private ObservableCollection<string> operatorList;
        Random random;
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
        private double Cleaningtimeuwp = 60;
        private double SetPressure = 5;
        private DateTime StartDateTime = DateTime.Now;
        private DateTime CurrentDateTime = DateTime.Now;

        //VOC concentration data
        private List<double> VOCconcentrationList = new List<double>();
        private string[,] newinfo;

        //temp profile from json file, length =18
        private List<double> JsonInputArray = new List<double>();
        private int heartcuttingNumber = 0;
        private double[] heartcuttingStartList = new double[6];   //[0] is empty
        private double[] heartcuttingEndList = new double[6];     //[0] is empty
        private List<String> VOCNameList = new List<string>();
        private List<double> RetentionTimeList = new List<double>();
        private double CalibrationFactor = 1.0;
        private List<double> RetentionTime2DList = new List<double>();
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
        private double MinY1 = 100;
        private double MinY2 = 100;

        //write into files
        private string FileNameTime = "";
        private Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private Windows.Storage.StorageFile Rawfile;

        //折线图数据模型
        public class ChartData
        {
            public double label { get; set; }//x
            public double text { get; set; }//y
        }
        public RunTestPage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            random = new Random();

            testInfoList = new ObservableCollection<SelectTestInfo>();
            secondaryInfoList = new ObservableCollection<SelectTestInfo>();
            initPage();
        }



        public void Dispose()
        {
            if(null != testInfoList)
            {
                testInfoList.Clear();
            }
            if(null != operatorList)
            {
                operatorList.Clear();
            }
            if (this.Basic_Chart != null)
            {
                foreach (var series in this.Basic_Chart.Series)
                    series.ClearValue(ChartSeriesBase.ItemsSourceProperty);
                this.Basic_Chart = null;
            }
        }

        private void zoomPan_Loaded(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
            ChartZoomPanBehavior zoomBehavior = new ChartZoomPanBehavior();
            zoomBehavior.EnablePanning = true;
            zoomBehavior.ZoomMode = Syncfusion.UI.Xaml.Charts.ZoomMode.XY;
            zoomBehavior.HorizontalPosition = HorizontalAlignment.Left;
            zoomBehavior.EnableZoomingToolBar = true;
            zoomBehavior.ToolBarBackground = new SolidColorBrush(Colors.LightGray);
            Basic_Chart.Behaviors.Add(zoomBehavior);

            if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
            {
                zoomBehavior.EnableSelectionZooming = true;
            }
            else
                zoomBehavior.EnableSelectionZooming = false;
        }

        private void zoomPan_Loaded1(object sender, global::Windows.UI.Xaml.RoutedEventArgs e)
        {
            ChartZoomPanBehavior zoomBehavior = new ChartZoomPanBehavior();
            zoomBehavior.EnablePanning = true;
            zoomBehavior.ZoomMode = Syncfusion.UI.Xaml.Charts.ZoomMode.XY;
            zoomBehavior.HorizontalPosition = HorizontalAlignment.Left;
            zoomBehavior.EnableZoomingToolBar = true;
            zoomBehavior.ToolBarBackground = new SolidColorBrush(Colors.LightGray);
            Basic_Chart1.Behaviors.Add(zoomBehavior);

            if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
            {
                zoomBehavior.EnableSelectionZooming = true;
            }
            else
                zoomBehavior.EnableSelectionZooming = false;
        }

        private async void initPage()
        {
            try
            {
                RunTestGrid.Visibility = Visibility.Collapsed;
                LoadingIndicator.IsActive = true;
                //添加表格数据
                testInfoList = new ObservableCollection<SelectTestInfo>();
                loader = new ResourceLoader();
                initOperator();
                initTopChart();
                devices_list();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            finally
            {
                RunTestGrid.Visibility = Visibility.Visible;
                LoadingGrid.Visibility = Visibility.Collapsed;
                LoadingIndicator.IsActive = false;

                if (!CustomUtils.CheckCalibrate())
                {
                    LoadingIndicator.IsActive = true;
                    popup = new AlertPopup(MethodNameText, TimeSpan.FromSeconds(60 * 60));
                    popup.Show();
                    this.IsHitTestVisible = false;
                    popup.UserControlButtonClicked += Popup_UserControlButtonClicked;
                }
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
            secondaryInfoList.Clear();
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
            MinY1 = 100;
            MinY2 = 100;
        }
        private async void initOperator()
        {
            StorageFile userfile = await storageFolder.CreateFileAsync("UserInfo.json", CreationCollisionOption.OpenIfExists);
            if (null != userfile)
            {
                using (var stream = await userfile.OpenStreamForReadAsync())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        if (null != content && !"".Equals(content))
                        {
                            JsonArray array = JsonArray.Parse(content);
                            operatorList = new ObservableCollection<string>();
                            foreach (var jsonValue in array)
                            {
                                var value = JsonObject.Parse(jsonValue.Stringify());
                                string LastName = value.GetNamedString("LastName");
                                string FamilyName = value.GetNamedString("FamilyName");
                                operatorList.Add(string.Format("{0}{1}{2}", FamilyName, " ", LastName));
                            }
                            if (null != operatorList && operatorList.Count > 0)
                            {
                                OperatorName.ItemsSource = operatorList;
                            }
                        }
                    }
                }
            }
        }

        private void Popup_UserControlButtonClicked(object sender, EventArgs e)
        {
            this.IsHitTestVisible = true;
            LoadingIndicator.IsActive = false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter.ToString() != "True")
            {
                FromSelect = (string)e.Parameter;
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                if (null != FromSelect)
                {
                    var fileName = "";
                    switch (FromSelect)
                    {
                        case "Cleaning":
                            MethodName.Text = loader.GetString("DeepClean1");
                            MethodNameText = "Cleaning";
                            fileName = "Cleaning.json";
                            methodFileName = "Cleaning";
                            break;
                        case "TVOC":
                            MethodName.Text = "TVOC";
                            MethodNameText = "TVOC";
                            fileName = "TVOC.json";
                            methodFileName = "TVOC";
                            break;
                        case "BTEX":
                            MethodName.Text = "BTEX";
                            MethodNameText = "BTEX";
                            fileName = "BTEX.json";
                            methodFileName = "BTEX";
                            break;
                        case "TCE/PCE":
                            MethodName.Text = "TCE/PCE";
                            MethodNameText = "TCE/PCE";
                            fileName = "TCEPCE.json";
                            methodFileName = "TCE&PCE";
                            break;
                        case "Malodorous":
                            MethodName.Text = loader.GetString("MalodorousGas1");
                            MethodNameText = "Malodorous";
                            fileName = "Malodorous.json";
                            methodFileName = "Malodorous Gas";
                            break;
                        case "VehicleIndoor":
                            MethodName.Text = loader.GetString("Vehicle1");
                            MethodNameText = "VehicleIndoor";
                            fileName = "Vehicle.json";
                            methodFileName = "Vehicle";
                            break;
                        case "EnvironmentalAir":
                            MethodName.Text = loader.GetString("AirQuality1");
                            MethodNameText = "EnvironmentalAir";
                            fileName = "AirQuality.json";
                            methodFileName = "Air Quality";
                            break;
                        case "PollutionSource":
                            MethodName.Text = loader.GetString("PollutionSource1");
                            MethodNameText = "PollutionSource";
                            fileName = "PollutionSource.json";
                            methodFileName = "Pollution Source";
                            break;
                        case "WaterSample-Online":
                            MethodName.Text = loader.GetString("WaterQuality1");
                            MethodNameText = "WaterSample-Online";
                            fileName = "WaterQuality.json";
                            methodFileName = "Water Quality";
                            break;
                        default:
                            break;
                    }
                    ReadFromJson(fileName);
                    UpdateRentention(FromSelect);
                    GetConcentrationFactor(FromSelect);
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
                    heartcuttingNumber = (int) json.GetNamedNumber("heartcuttingNumber");
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
                        if(cutsecond.ValueType == JsonValueType.Number)
                        {
                            RetentionTimeList.Add(cutsecond.GetNumber());
                        }
                    }
                    CalibrationFactor = json.GetNamedNumber("CalibrationFactor");
                    if (heartcuttingNumber > 0)
                    {
                        try
                        {
                            for (var index = 0; index < json.GetNamedArray("VOCRetentionTime2D").Count; index++)
                            {
                                var cutsecond = json.GetNamedArray("VOCRetentionTime2D")[index];
                                RetentionTime2DList.Add(cutsecond.GetNumber());
                            }
                            CalibrationFactor2D = json.GetNamedNumber("CalibrationFactor2D");
                        }
                        catch(System.Exception)
                        {

                        }
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
                                IJsonValue rfValue = vocjsonvalue.GetNamedValue("RF");
                                if(rfValue.ValueType == JsonValueType.Number)
                                {
                                    ResposeFactorList.Add(vocjsonvalue.GetNamedNumber("RF"));
                                    break;
                                }
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


        //update rentention time if there is a update txt file in specific location
        private async void UpdateRentention(string fileName)
        {
            try
            {
                //Create a folder: fileFloder dir calibrate -->methodFileName -->dateTimeFileName
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder retentionFolder = await applicationFolder.CreateFolderAsync("Retention_update",
                    CreationCollisionOption.OpenIfExists);
                StorageFolder pdfFolder = await retentionFolder.CreateFolderAsync(methodFileName,
                    CreationCollisionOption.OpenIfExists);
                //Query the file
                List<string> fileTypeFilter = new List<string>();
                fileTypeFilter.Add(".dat");
                var queryOptions = new Windows.Storage.Search.QueryOptions(Windows.Storage.Search.CommonFileQuery.OrderByName, fileTypeFilter);

                // Create query and retrieve files
                var query = pdfFolder.CreateFileQueryWithOptions(queryOptions);
                IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
                // Process results
                long maxvalue = 0;
                foreach (StorageFile file in fileList)
                {
                    // Process file
                    Debug.WriteLine(file.Name);
                    if (long.Parse(file.Name.Split('.')[0]) > maxvalue)
                    {
                        maxvalue = long.Parse(file.Name.Split('.')[0]);
                    }
                }
                Debug.WriteLine(maxvalue);

                //Get the latest file 
                string latestFilename = maxvalue.ToString() + ".dat";
                StorageFile latestFile = await pdfFolder.GetFileAsync(latestFilename);

                if (latestFile != null)
                {
                    Debug.WriteLine("Update file found");
                }

                IBuffer buffer = await FileIO.ReadBufferAsync(latestFile);
                DataReader reader = DataReader.FromBuffer(buffer);
                byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(fileContent);
                string text = GetEncoding(new byte[4] { fileContent[0], fileContent[1], fileContent[2], fileContent[3] }).GetString(fileContent);
                String[] result = text.Split(new[] { ',' });
                for (int i = 0; i < result.Length; i++)
                {
                    RetentionTimeList[i] = Math.Round(float.Parse(result[i]), 2);
                    Debug.WriteLine(RetentionTimeList[i]);
                }
            }
            catch(FileNotFoundException)
            {
                Debug.WriteLine("Update file not found");
            }
        }





        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            popup.Hide();
            //Close arduino device
            CancelReadTask();
            CloseDevice();

            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(SelectMethodPage), null);
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
            if (string.IsNullOrWhiteSpace(this.ExperienceName.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("ExperienceValidate"));
                notifyPopup.Show();
                return;
            }
            //根据json标志显示bottom
            if(heartcuttingNumber > 0)
            {
                BottomChartGrid.Visibility = Visibility.Visible;
                TopGrid.Height = 450;
            }
            else
            {
                BottomChartGrid.Visibility = Visibility.Collapsed;
                TopGrid.Height = 700;
            }

            if (null != timer)
                timer.Stop();
            if(null != c)
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
            SecondaryGrid.Visibility = Visibility.Collapsed;
        }
        
        //停止按钮事件
        private async void StopCalculation_Click(object sender, RoutedEventArgs e)
        {
            StartCalculation.Visibility = Visibility.Visible;
            StopCalculation.Visibility = Visibility.Collapsed;
            if (!ReportSavedFlag && MethodNameText != "Cleaning")
            {
                ReportSavedFlag = true;
                //data analysis
                WholeDataAnalysis(x1, y1, y_b1, peaks1, bottoms1, Area1, Heights1, MinY1);
                if (heartcuttingNumber > 0)
                    WholeDataAnalysis(x2, y2, y_b2, peaks2, bottoms2, Area2, Heights2, MinY2);
          
                //Add baseline
                this.Basic_Chart.Series[1].ItemsSource = null;
                for (int i = 0; i < x_b.Count && i < y_b1.Count; i++)
                {
                    standardSource.Add(new Data(x_b[i], y_b1[i]));
                }
                this.Basic_Chart.Series[1].ItemsSource = standardSource;

                //显示表格控件
                testInfoList.Clear();     
                double currentvoctime = 0;
                double currentvocheight = 0;
                double currentvocarea = 0;
                double FWHMvalue = 0;
                double currentconcen = 0;
                int Peak1DCount = 0;
                if (MethodNameText == "BTEX")
                {
                    int p = 0;
                    for (int j = 0; j < VOCNameList.Count; j++)
                    {
                        for (; p < peaks1.Count && p < bottoms1.Count - 1; p++)
                        {
                            if (Math.Abs(x1[peaks1[p]] - RetentionTimeList[j]) < retentionTimeThreshold)
                            {
                                currentvoctime = x1[peaks1[p]];
                                currentvocheight = Heights1[p];
                                currentvocarea = Area1[p];
                                FWHMvalue = CalculateFWHM(bottoms1[p], peaks1[p], bottoms1[p + 1], x1, y1, y_b1);
                                break;
                            }
                        }
                        CalibrationFactor = VOCconcentrationList[j];
                        currentconcen = currentvocarea * CalibrationFactor / (FlowRate * Sampletimeuwp);
                        string currentvocname = VOCNameList[j];
                        if (j == 3)
                            currentvocname = currentvocname + " & "+ VOCNameList[j + 1];
                        if (j != 4)
                        {
                            Peak1DCount++;
                            testInfoList.Add(new SelectTestInfo
                            {
                                ID = (Peak1DCount).ToString(),
                                VOCName = currentvocname,
                                Time = currentvoctime.ToString("0.00"),
                                FWHM = FWHMvalue.ToString("0.00"),
                                Height = currentvocheight.ToString("0.00"),
                                Area = currentvocarea.ToString("0.00"),
                                Concentration = currentconcen.ToString("0.00")
                            });
                        }

                    }
                    InfoListView.Visibility = Visibility;
                }
                else
                {
                    for (int j = 0; j < VOCNameList.Count; j++)
                    {
                        for (int p = 0; p < peaks1.Count && p < bottoms1.Count - 1; p++)
                        {
                            if (Math.Abs(x1[peaks1[p]] - RetentionTimeList[j]) < retentionTimeThreshold)
                            {
                                currentvoctime = x1[peaks1[p]];
                                currentvocheight = Heights1[p];
                                currentvocarea = Area1[p];
                                FWHMvalue = CalculateFWHM(bottoms1[p], peaks1[p], bottoms1[p + 1], x1, y1, y_b1);
                                break;
                            }
                        }
                        CalibrationFactor = VOCconcentrationList[j];
                        currentconcen = currentvocarea * CalibrationFactor / (FlowRate * Sampletimeuwp);
                        if (Math.Abs(RetentionTimeList[j] - 0) > 0.01) //2D gas
                        {
                            Peak1DCount++;
                            testInfoList.Add(new SelectTestInfo
                            {
                                ID = (Peak1DCount).ToString(),
                                VOCName = VOCNameList[j],
                                Time = currentvoctime.ToString("0.00"),
                                FWHM = FWHMvalue.ToString("0.00"),
                                Height = currentvocheight.ToString("0.00"),
                                Area = currentvocarea.ToString("0.00"),
                                Concentration = currentconcen.ToString("0.00")
                            });
                        }
                    }
                    InfoListView.Visibility = Visibility;
                    if (heartcuttingNumber > 0)
                    {
                        int Peak2DCount = 0;
                        secondaryInfoList.Clear();
                        for (int j = 0; j < VOCNameList.Count; j++)
                        {
                            if (Math.Abs(RetentionTimeList[j] - 0) < 0.01)
                            {
                                Peak2DCount++;
                                for (int p = 0; p < peaks2.Count && p < bottoms2.Count - 1; p++)
                                {
                                    if (Math.Abs(x2[peaks2[p]] - RetentionTime2DList[j]) < retentionTimeThreshold)
                                    {
                                        currentvoctime = x2[peaks2[p]];
                                        currentvocheight = Heights2[p];
                                        currentvocarea = Area2[p];
                                        FWHMvalue = CalculateFWHM(bottoms2[p], peaks2[p], bottoms2[p + 1], x2, y2, y_b2);
                                        break;
                                    }
                                }
                                currentconcen = currentvocarea * CalibrationFactor2D / (FlowRate * Sampletimeuwp);
                                secondaryInfoList.Add(new SelectTestInfo
                                {
                                    ID = Peak2DCount.ToString(),
                                    VOCName = VOCNameList[j],
                                    Time = currentvoctime.ToString("0.00"),
                                    FWHM = FWHMvalue.ToString("0.00"),
                                    Height = currentvocheight.ToString("0.00"),
                                    Area = currentvocarea.ToString("0.00"),
                                    Concentration = ResposeFactorList[0].ToString("0.00")
                                });
                            }
                        }
                        SecondaryGrid.Visibility = Visibility;
                    }
                }
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
            Rawfile = await storageFolder.CreateFileAsync(ExperienceName.Text + "_" + OperatorName.SelectedValue + "_raw_" + FileNameTime + ".dat", CreationCollisionOption.OpenIfExists);

            await Windows.Storage.FileIO.AppendTextAsync(Rawfile, "Experience Name: " + ExperienceName.Text + "\n"
                + "Operator Name: " + OperatorName.SelectedValue + "\n"
                + "Start time: " + System.DateTime.Now.ToString() + " " + System.DateTime.Now.ToString() + "\n"
                + "Sampling/Pumping time: " + Sampletimeuwp + "\n"
                + "Waiting time: " + Waitingtimeuwp + "\n"
                + "Time,PID1,PID2,Temp,Setpoint,pressure,pwm%" + "\n");

            timer.Start();
        }
        

        private void Timer_Tick(object sender, object e)
        {
            CurrentDateTime = DateTime.Now;
            if ((!RunningTestFlag) && Math.Abs(UsedTime - (-5)) > 0.1)
            {
                StartDateTime = DateTime.Now;
                RunningTestFlag = true;
            }
            if (total - CurrentDateTime.Subtract(StartDateTime).TotalSeconds > 0 && Math.Abs(UsedTime - (-5)) > 0.1)
            {
                c.i = total - (int)CurrentDateTime.Subtract(StartDateTime).TotalSeconds - 1;
                Value = c.i;
                c.Update((total - c.i) / (double)total * 125 / 15 * Math.PI, 1000);
            }
            if (!RunningTestFlag)
            {
                CurrentStepText.Text = loader.GetString("ConnectingStep");
                CurrentStepRemainTimeText.Text = "00:00";
            }
            //更新当前步骤
            if (RunningTestFlag)
            {
                if (CurrentDateTime.Subtract(StartDateTime).TotalSeconds < Sampletimeuwp)
                {
                    CurrentStepText.Text = loader.GetString("SamplingStep");
                    int secondsFordisplay = (int)(Sampletimeuwp - CurrentDateTime.Subtract(StartDateTime).TotalSeconds);
                    var timespan = TimeSpan.FromSeconds(secondsFordisplay);
                    CurrentStepRemainTimeText.Text = timespan.ToString(@"mm\:ss");
                }
                else if (CurrentDateTime.Subtract(StartDateTime).TotalSeconds < Sampletimeuwp + Waitingtimeuwp)
                {
                    CurrentStepText.Text = loader.GetString("WaitingStep");
                    int secondsFordisplay = (int)(Sampletimeuwp + Waitingtimeuwp - CurrentDateTime.Subtract(StartDateTime).TotalSeconds);
                    var timespan = TimeSpan.FromSeconds(secondsFordisplay);
                    CurrentStepRemainTimeText.Text = timespan.ToString(@"mm\:ss");
                }
                else if (CurrentDateTime.Subtract(StartDateTime).TotalSeconds < Sampletimeuwp + Waitingtimeuwp + Analysistimeuwp)
                {
                    CurrentStepText.Text = loader.GetString("AnalysingStep");
                    int secondsFordisplay = (int)(Sampletimeuwp + Waitingtimeuwp + Analysistimeuwp - CurrentDateTime.Subtract(StartDateTime).TotalSeconds);
                    var timespan = TimeSpan.FromSeconds(secondsFordisplay);
                    CurrentStepRemainTimeText.Text = timespan.ToString(@"mm\:ss");
                }
                else if (CurrentDateTime.Subtract(StartDateTime).TotalSeconds < Sampletimeuwp + Waitingtimeuwp + Analysistimeuwp + Cleaningtimeuwp)
                {
                    CurrentStepText.Text = loader.GetString("CleaningStep");
                    int secondsFordisplay = (int)(Sampletimeuwp + Waitingtimeuwp + Analysistimeuwp + Cleaningtimeuwp - CurrentDateTime.Subtract(StartDateTime).TotalSeconds);
                    var timespan = TimeSpan.FromSeconds(secondsFordisplay);
                    CurrentStepRemainTimeText.Text = timespan.ToString(@"mm\:ss");
                    if (!ReportSavedFlag && MethodNameText != "Cleaning")
                    {
                        ReportSavedFlag = true;
                        //data analysis
                        WholeDataAnalysis(x1, y1, y_b1, peaks1, bottoms1, Area1, Heights1, MinY1);
                        if (heartcuttingNumber > 0)
                            WholeDataAnalysis(x2, y2, y_b2, peaks2, bottoms2, Area2, Heights2, MinY2);
                       
                        //Add baseline
                        this.Basic_Chart.Series[1].ItemsSource = null;
                        for (int i = 0; i < x_b.Count && i < y_b1.Count; i++)
                        {
                            standardSource.Add(new Data(x_b[i], y_b1[i]));
                        }
                        this.Basic_Chart.Series[1].ItemsSource = standardSource;
                        //显示表格控件
                        testInfoList.Clear();
                        double currentvoctime = 0;
                        double currentvocheight = 0;
                        double currentvocarea = 0;
                        double FWHMvalue = 0;
                        double currentconcen = 0;
                        int Peak1DCount = 0;
                        if (MethodNameText == "BTEX")
                        {
                            int p = 0;
                            for (int j = 0; j < VOCNameList.Count; j++)
                            {
                                for (; p < peaks1.Count && p < bottoms1.Count - 1; p++)
                                {
                                    if (Math.Abs(x1[peaks1[p]] - RetentionTimeList[j]) < retentionTimeThreshold)
                                    {
                                        currentvoctime = x1[peaks1[p]];
                                        currentvocheight = Heights1[p];
                                        currentvocarea = Area1[p];
                                        FWHMvalue = CalculateFWHM(bottoms1[p], peaks1[p], bottoms1[p + 1], x1, y1, y_b1);
                                        break;
                                    }
                                }
                                CalibrationFactor = VOCconcentrationList[j];
                                currentconcen = currentvocarea * CalibrationFactor / (FlowRate * Sampletimeuwp);
                                string currentvocname = VOCNameList[j];
                                if (j == 3)
                                    currentvocname = currentvocname + " & " + VOCNameList[j + 1];
                                if (j != 4)
                                {
                                    Peak1DCount++;
                                    testInfoList.Add(new SelectTestInfo
                                    {
                                        ID = (Peak1DCount).ToString(),
                                        VOCName = currentvocname,
                                        Time = currentvoctime.ToString("0.00"),
                                        FWHM = FWHMvalue.ToString("0.00"),
                                        Height = currentvocheight.ToString("0.00"),
                                        Area = currentvocarea.ToString("0.00"),
                                        Concentration = currentconcen.ToString("0.00")
                                    });
                                }

                            }
                            InfoListView.Visibility = Visibility;
                        }
                        else
                        {
                            for (int j = 0; j < VOCNameList.Count; j++)
                            {
                                for (int p = 0; p < peaks1.Count && p < bottoms1.Count - 1; p++)
                                {
                                    if (Math.Abs(x1[peaks1[p]] - RetentionTimeList[j]) < retentionTimeThreshold)
                                    {
                                        currentvoctime = x1[peaks1[p]];
                                        currentvocheight = Heights1[p];
                                        currentvocarea = Area1[p];
                                        FWHMvalue = CalculateFWHM(bottoms1[p], peaks1[p], bottoms1[p + 1], x1, y1, y_b1);
                                        break;
                                    }
                                }
                                CalibrationFactor = VOCconcentrationList[j];
                                currentconcen = currentvocarea * CalibrationFactor / (FlowRate * Sampletimeuwp);
                                if (Math.Abs(RetentionTimeList[j] - 0) > 0.01) //2D gas
                                {
                                    Peak1DCount++;
                                    testInfoList.Add(new SelectTestInfo
                                    {
                                        ID = (Peak1DCount).ToString(),
                                        VOCName = VOCNameList[j],
                                        Time = currentvoctime.ToString("0.00"),
                                        FWHM = FWHMvalue.ToString("0.00"),
                                        Height = currentvocheight.ToString("0.00"),
                                        Area = currentvocarea.ToString("0.00"),
                                        Concentration = currentconcen.ToString("0.00")
                                    });
                                }
                            }
                            InfoListView.Visibility = Visibility;
                            if (heartcuttingNumber > 0)
                            {
                                int Peak2DCount = 0;
                                secondaryInfoList.Clear();
                                for (int j = 0; j < VOCNameList.Count; j++)
                                {
                                    if (Math.Abs(RetentionTimeList[j] - 0) < 0.01)
                                    {
                                        Peak2DCount++;
                                        for (int p = 0; p < peaks2.Count && p < bottoms2.Count - 1; p++)
                                        {
                                            if (Math.Abs(x2[peaks2[p]] - RetentionTime2DList[j]) < retentionTimeThreshold)
                                            {
                                                currentvoctime = x2[peaks2[p]];
                                                currentvocheight = Heights2[p];
                                                currentvocarea = Area2[p];
                                                FWHMvalue = CalculateFWHM(bottoms2[p], peaks2[p], bottoms2[p + 1], x2, y2, y_b2);
                                                break;
                                            }
                                        }
                                        currentconcen = currentvocarea * CalibrationFactor2D / (FlowRate * Sampletimeuwp);
                                        secondaryInfoList.Add(new SelectTestInfo
                                        {
                                            ID = Peak2DCount.ToString(),
                                            VOCName = VOCNameList[j],
                                            Time = currentvoctime.ToString("0.00"),
                                            FWHM = FWHMvalue.ToString("0.00"),
                                            Height = currentvocheight.ToString("0.00"),
                                            Area = currentvocarea.ToString("0.00"),
                                            Concentration = ResposeFactorList[0].ToString("0.00")
                                        });
                                    }
                                }
                                SecondaryGrid.Visibility = Visibility;
                            }
                        }
                        savePdf();
                    }
                }
                else
                {
                    CurrentStepText.Text = loader.GetString("CoolingStep");
                    CurrentStepRemainTimeText.Text = loader.GetString("CoolingNotice"); 
                    CurrentStepRemainTimeText.FontSize = 14;
                    if (Math.Abs(UsedTime - (-5)) < 0.1) //cooling is finished
                    {
                        FinishTestFunction();
                    }
                }
            }
            CurrentTempText.Text = ActualTemp.ToString("0.00");
            CurrentPressureText.Text = ActualPressure.ToString("0.00");
            //更新折线图
            if (UsedTime > 0)
            {
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
                //2D signal
                if (heartcuttingNumber > 0)
                {
                    this.Basic_Chart1.Series[0].ItemsSource = null;
                    double x2 = UsedTime;
                    double y2 = PID2D;
                    var primaryAxisMax1 = PrimaryAxis1.Maximum;  //x
                    var secondAxisMax1 = SecondAxis1.Maximum;    //y
                    if (y2 > Convert.ToDouble(secondAxisMax1))
                    {
                        YaxisMax1 = y2 + 10;
                    }
                    Data data2 = new Data(x2, y2);
                    bottomSource.Add(data2);
                    this.Basic_Chart1.Series[0].ItemsSource = bottomSource;
                }
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
<<<<<<< HEAD
                PdfFont font = new PdfStandardFont(PdfFontFamily.TimesRoman, 12, PdfFontStyle.Bold);
                PdfStringFormat sf = new PdfStringFormat();
                sf.Alignment = PdfTextAlignment.Center;
                sf.LineAlignment = PdfVerticalAlignment.Middle;
                
                RectangleF rf = new RectangleF(page.Graphics.ClientSize.Width / 2 - 200, 0, 400, 30);
                graphics.DrawString(string.Format("NovaTest {0}", loader.GetString("Report"), MethodName.Text), font, PdfBrushes.Black, rf, sf);

                RectangleF rf1 = new RectangleF(0, 30, 400, 30);
                graphics.DrawString(string.Format("{0}: {1}", loader.GetString("ExperienceName1"), ExperienceName.Text), font, PdfBrushes.Black, rf1);

                //RectangleF rf2 = new RectangleF(220, 35, 400, 40);
                RectangleF rf2 = new RectangleF(0, 40, 400, 30);
                graphics.DrawString(string.Format("{0}: {1}", loader.GetString("OperatorName1"), OperatorName.SelectedValue), font, PdfBrushes.Black, rf2);

                RectangleF rf3 = new RectangleF(350, 30, 400, 30);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("Method"), MethodName.Text), font, PdfBrushes.Black, rf3);

                RectangleF rf4 = new RectangleF(0, 50, 400, 30);
                graphics.DrawString(string.Format("{0}: {1}",loader.GetString("StartTime"), DateTime.Now.ToString("F", DateTimeFormatInfo.InvariantInfo)), font, PdfBrushes.Black, rf4);

                RectangleF rf7 = new RectangleF(0, 60, 400, 30);
                String instrumentString = "Instrument:  NovaTest P100";
                document.Pages[0].Graphics.DrawString(instrumentString, font, PdfBrushes.Black, rf7);

                RectangleF rf5 = new RectangleF(350, 50, 400, 30);
                graphics.DrawString(string.Format("{0}: {1}", loader.GetString("SamplingPumpingTime"), SamplingTimeText.Text), font, PdfBrushes.Black, rf5);
                
                RectangleF rf6 = new RectangleF(350, 70, 400, 30);
                graphics.DrawString(string.Format("{0}: {1}", loader.GetString("WaitingTime"), WaitTimeText.Text), font, PdfBrushes.Black, rf6);
=======
                PdfFont font = new PdfCjkStandardFont(PdfCjkFontFamily.SinoTypeSongLight, 12, PdfFontStyle.Regular);
                PdfFont titleFont = new PdfCjkStandardFont(PdfCjkFontFamily.SinoTypeSongLight, 16, PdfFontStyle.Regular);
                PdfFont footerFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 12, PdfFontStyle.Regular);
                PdfFont font2 = new PdfCjkStandardFont(PdfCjkFontFamily.SinoTypeSongLight, 8, PdfFontStyle.Regular);
                PdfFont logoFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 30);
                PdfStringFormat sf = new PdfStringFormat();
                sf.Alignment = PdfTextAlignment.Center;
                sf.LineAlignment = PdfVerticalAlignment.Middle;



                RectangleF rf = new RectangleF(page.Graphics.ClientSize.Width / 2 - 165, 0, 400, 30);
                document.Pages[0].Graphics.DrawString(loader.GetString("AdvanceReportTitle"), titleFont, PdfBrushes.Black, rf, sf);

                RectangleF rf1 = new RectangleF(0, 35, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("ExperienceName1"), ExperienceName.Text), font, PdfBrushes.Black, rf1);

                RectangleF rf2 = new RectangleF(0, 47, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("OperatorName1"), OperatorName.SelectedValue), font, PdfBrushes.Black, rf2);

                RectangleF rf3 = new RectangleF(0, 59, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("StartTime"), DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo)), font, PdfBrushes.Black, rf3);

                RectangleF rf16 = new RectangleF(0, 71, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Method"), MethodName.Text), font, PdfBrushes.Black, rf16);

                RectangleF rf17 = new RectangleF(0, 83, 400, 40);
                String instrumentString = "Instrument: NovaTest P100";
                document.Pages[0].Graphics.DrawString(instrumentString, font, PdfBrushes.Black, rf17);

                RectangleF rf18 = new RectangleF(0, 95, 400, 40);
                String CalibrationfileString = "Calibration file: N/A";
                document.Pages[0].Graphics.DrawString(CalibrationfileString, font, PdfBrushes.Black, rf18);

                RectangleF rf15 = new RectangleF(0, 107, 400, 40);
                String parameterString = "Parameters:";
                document.Pages[0].Graphics.DrawString(parameterString, font, PdfBrushes.Black, rf15);

                RectangleF rf4 = new RectangleF(0, 120, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("SamplingPumpingTime"), SamplingTimeText.Text), font2, PdfBrushes.Black, rf4);

                RectangleF rf5 = new RectangleF(160, 120, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("WaitingTime"), WaitTimeText.Text), font2, PdfBrushes.Black, rf5);

                RectangleF rf6 = new RectangleF(320, 120, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("PressurePDF1"), SetPressureText.Text), font2, PdfBrushes.Black, rf6);

                double lowestTempvalue = JsonInputArray[0];
                double lowestTvalue = JsonInputArray[1];
                double Temp1value = JsonInputArray[2];
                double HoldT1value = JsonInputArray[3];
                //double HoldT1value = JsonInputArray[3] * 60;
                double RampSpeed1value = JsonInputArray[4];
                //double RampSpeed1value = JsonInputArray[4] / 60.0;
                double Temp2value = JsonInputArray[5];
                double HoldT2value = JsonInputArray[6];
                //double HoldT2value = JsonInputArray[6] * 60;
                double RampSpeed2value = JsonInputArray[7];
                //double RampSpeed2value = JsonInputArray[7] / 60.0;

                RectangleF rf7 = new RectangleF(0, 130, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("LowestTemp1"), lowestTempvalue), font2, PdfBrushes.Black, rf7);

                RectangleF rf8 = new RectangleF(160, 130, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("LowHoldingTime1"), lowestTvalue), font2, PdfBrushes.Black, rf8);

                RectangleF rf9 = new RectangleF(0, 140, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temperature11"), Temp1value), font2, PdfBrushes.Black, rf9);

                RectangleF rf10 = new RectangleF(160, 140, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temp1HoldigTime"), HoldT1value), font2, PdfBrushes.Black, rf10);

                RectangleF rf11 = new RectangleF(320, 140, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("RampSpeed11"), RampSpeed1value), font2, PdfBrushes.Black, rf11);

                RectangleF rf12 = new RectangleF(0, 150, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temperatures2"), Temp2value), font2, PdfBrushes.Black, rf12);

                RectangleF rf13 = new RectangleF(160, 150, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temp2HoldigTime"), HoldT2value), font2, PdfBrushes.Black, rf13);

                RectangleF rf14 = new RectangleF(320, 150, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("RampSpeed2"), RampSpeed2value), font2, PdfBrushes.Black, rf14);

                RectangleF rf19 = new RectangleF(380, 0, 0, 0);
                document.Pages[0].Graphics.DrawString("NovaTest", logoFont, PdfBrushes.DodgerBlue, rf19);

                PdfPen blackPen = new PdfPen(PdfColor.Empty);
                PointF pf1 = new PointF(0, 29);
                PointF pf2 = new PointF(508, 29);
                graphics.DrawLine(blackPen, pf1, pf2);
>>>>>>> development

                RectangleF rf8 = new RectangleF(350, 90, 400, 30);
                String calibrationfileString = "Calibration file: N/A";
                document.Pages[0].Graphics.DrawString(calibrationfileString, font, PdfBrushes.Black, rf8);


                PdfPen bluePen = new PdfPen(PdfColor.Empty);
                PointF pf1 = new PointF(0, 29);
                PointF pf2 = new PointF(508, 29);
                graphics.DrawLine(bluePen, pf1, pf2);


                //RectangleF rf7 = new RectangleF(220, 95, 400, 40);
                //graphics.DrawString("Gc Spectrum", font, PdfBrushes.Black, new PointF(210, 95));

                //Initializing to render to Bitmap
                var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var renderTargetBitmap = new RenderTargetBitmap();

                //Create the Bitmpa from xaml page

                await renderTargetBitmap.RenderAsync(CustomGrid, 510, 1600);

                double gridWidth = CustomGrid.ActualWidth;
                double gridHeight = CustomGrid.ActualHeight;
                await renderTargetBitmap.RenderAsync(CustomGrid, (int)gridWidth, (int)gridHeight);
                Debug.WriteLine(gridWidth);
                Debug.WriteLine(gridHeight);

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
<<<<<<< HEAD
                    graphics.DrawImage(img, new RectangleF(0, 105, (float)gridWidth / 1.3f, (float)gridHeight / 1.1f));
=======
                    graphics.DrawImage(img, new RectangleF(0, 175, 510, 450));
>>>>>>> development
                }


                //footer

                RectangleF bounds = new RectangleF(0, 0, document.Pages[0].GetClientSize().Width, 100);
                PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);

                // information about novatest

                SizeF pageSize = document.Pages[0].Size;

                //Create a PdfPageTemplateElement object that will be  
                //used as footer space  
                // PdfPageTemplateElement footerSpace = new PdfPageTemplateElement(pageSize.Width, margin.Bottom);
                PdfPageTemplateElement footerSpace = new PdfPageTemplateElement(bounds);
<<<<<<< HEAD
                PdfFont footerfont = new PdfStandardFont(PdfFontFamily.TimesRoman, 12);
=======
>>>>>>> development
                footerSpace.Foreground = true;
                document.Template.Bottom = footerSpace;

                //Draw text at the center of footer space  
                // PdfTrueTypeFont fontfooter = new PdfTrueTypeFont(new Font("Arial", 9f, FontStyle.Bold), true);
                PdfStringFormat format = new PdfStringFormat(PdfTextAlignment.Center);
                String headerText = "Copyright © 2017 Nanova Environmental, Inc. www.nanovaenv.com";
                String headerText1 = "All Rights Reserved";
                //String address = "3338 Brown Station Rd, Columbia, MO, 65202";
                //String website = " ";
                float x = 255f;
                float y = 0f;
                float y1 = 15f;
                float y2 = 30f;
                float y3 = 45f;
<<<<<<< HEAD
                footerSpace.Graphics.DrawString(headerText, font, PdfBrushes.Black, x, y, format);
                footerSpace.Graphics.DrawString(headerText1, font, PdfBrushes.Black, x, y1, format);
=======
                footerSpace.Graphics.DrawString(headerText, footerFont, PdfBrushes.Black, x, y, format);
                footerSpace.Graphics.DrawString(headerText1, footerFont, PdfBrushes.Black, x, y1, format);
>>>>>>> development
                //footerSpace.Graphics.DrawString(address, font, PdfBrushes.Black, x, y2, format);
                //footerSpace.Graphics.DrawString(website, font, PdfBrushes.Black, x, y3, format);
                //Create page number automatic field  
                PdfPageNumberField number = new PdfPageNumberField();
                //Create page count automatic field  
                PdfPageCountField count = new PdfPageCountField();
                //Add the fields in composite field  
                PdfCompositeField compositeField = new PdfCompositeField(font, PdfBrushes.Black, "Page {0} of {1}", number, count);
                //Align string of "Page {0} of {1}" to center   
                compositeField.StringFormat = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Bottom);
                compositeField.Bounds = footerSpace.Bounds;
                //Draw composite field at footer space  
                compositeField.Draw(footerSpace.Graphics);

<<<<<<< HEAD
                //watermark
                PdfFont fontmarkwater = new PdfStandardFont(PdfFontFamily.TimesRoman, 20);
                PdfTilingBrush brush = new PdfTilingBrush(new SizeF(page.Graphics.ClientSize.Width / 2, page.Graphics.ClientSize.Height / 3));
                brush.Graphics.SetTransparency(0.3f);
                brush.Graphics.Save();
                brush.Graphics.TranslateTransform(brush.Size.Width / 2, brush.Size.Height / 2);
                brush.Graphics.RotateTransform(-45);
                brush.Graphics.DrawString("NovaTest", fontmarkwater, PdfBrushes.Red, 10, 10, new PdfStringFormat(PdfTextAlignment.Left));
                brush.Graphics.Restore();
                brush.Graphics.SetTransparency(1);
                page.Graphics.DrawRectangle(brush, new RectangleF(new PointF(0, 0), page.Graphics.ClientSize));



=======
>>>>>>> development
                //Save the Pdf document
                MemoryStream docStream = new MemoryStream();
                document.Save(docStream);
                document.Close(true);
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder runTestFolder = await applicationFolder.CreateFolderAsync("runTest",
                    CreationCollisionOption.OpenIfExists);
                StorageFolder pdfFolder = await runTestFolder.CreateFolderAsync(methodFileName,
                    CreationCollisionOption.OpenIfExists);
                StorageFile savePdfFile = await pdfFolder.CreateFileAsync(string.Format("{0}_{1}.pdf",
                    OperatorName.SelectedValue, 
                    DateTime.Now.ToString("yyyyMMddHHmmss")),
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

            //add other possible port
            if (selector.Length < 260)
            {
                vid = 0x2341;
                pid = 0x0010;
                selector = SerialDevice.GetDeviceSelectorFromUsbVidPid(vid, pid);
            }
            //end add other port

            services = await DeviceInformation.FindAllAsync(selector);
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
                    //ConnectionStatusText.Text = ex.Message;
                }
            }
            else
            {
                MessageDialog popup = new MessageDialog("Sorry, no device found.");
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
            //string sendTestString = outputData + '\n';
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

            uint ReadBufferLength = 100;

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
                    await Windows.Storage.FileIO.AppendTextAsync(Rawfile, ExtractingStr);
                    if (!Convert.ToBoolean(string.Compare(ReadInputStr, "Ready")))
                    {
                        StartTestFunction();
                    }
                }
            }
        }
        private void ExtractSignals(string BufferInputString)
        {
            if (BufferInputString.Length > 10)
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
            heartcuttingNumber = (int) JsonInputArray[8];
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
    }
        //******************************Data analysis process*******************************//
        private int constant_m = 35; // for SNIP baseline formula
        private int constant_m_end = Convert.ToInt32(1 / Math.Sqrt(2)); // for SNIP baseline formula
        private int CONSECUTIVE_SCAN_STEPS = 3;   //for peak detection
        private double THRESHOLD = 0.005f;        //for peak detection: slope
        private double THRESHOLD_peak = 2f;       //for peak detection: slope
        private double retentionTimeThreshold = 2;

        public void WholeDataAnalysis(List<double> OriginalXv, List<double> OriginalYv, List<double> BaseLineYv, List<int> peaksv, List<int> bottomsv, List<double> Areav, List<double> Heightsv, double MinY)
        {
            // initialize y_b
            x_b.Clear();
            for (int v = 0; v < OriginalXv.Count; v++)
            {
                if (OriginalYv[v] < MinY)
                    MinY = OriginalYv[v];
                x_b.Add(OriginalXv[v]);
                BaseLineYv.Add(MinY);
            }
            FindpeakRun(OriginalXv, OriginalYv, BaseLineYv, peaksv, bottomsv, Areav, Heightsv, MinY);
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

            //core iteration
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

        private void FindpeakRun(List<double> OriginalX, List<double> OriginalY, List<double> BaseLineY, List<int> peaks, List<int> bottoms, List<double> Area, List<double> Heights, double MinY)
        {
            int indexX = 0;
            double valueY = 100;
            int size = 0;
            List<double> temp = new List<double>();
            List<double> temp_x_b = new List<double>();
            List<double> temp_y_b = new List<double>();

            detectPeakAndBottom(OriginalX, OriginalY, peaks, bottoms, Area, Heights, MinY);
            size = bottoms.Count;
            for (int i = 0; i < size; i++)
            {
                indexX = bottoms[i];
                temp_x_b.Add(OriginalX[indexX]);
                valueY = OriginalY[indexX];
                temp_y_b.Add(valueY);
            }
        }


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
            List<double> values = new List<double>(); //save thre  e consecutive slopes

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
                        while (slopes[peakMax] > 0 && peakMax < size - 1)
                        {
                            peakMax++;
                        }
                        if ((OriginalY[peakMax] - MinY) > THRESHOLD_peak)
                        {
                            peaks.Add(peakMax);
                        }
                        //find peakStop
                        peakStop = peakMax;
                        if (peakStop == size - 1) break; //the last scan is a peak
                        while (slopes[peakStop] <= 0 && peakStop < size - 1)
                        {
                            peakStop++;
                        }
                        if ((OriginalY[peakMax] - MinY) > THRESHOLD_peak)
                        {
                            bottoms.Add(peakStop);
                        }
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
                try
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
                catch (Exception ex) {
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
                if (Math.Abs(CorrectedY[i] - HalfHeight) < mindistance)
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
        
        //update info for the default methods
        private async void UpdateInfo_Click(object sender, RoutedEventArgs e)
        {
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".txt");

            StorageFile UpdateInfoFile = await picker.PickSingleFileAsync();
            IBuffer buffer = await FileIO.ReadBufferAsync(UpdateInfoFile);
            DataReader reader = DataReader.FromBuffer(buffer);
            byte[] fileContent = new byte[reader.UnconsumedBufferLength];
            reader.ReadBytes(fileContent);
            string text = GetEncoding(new byte[4] { fileContent[0], fileContent[1], fileContent[2], fileContent[3] }).GetString(fileContent);
            String[] result = text.Split(new[] {'\n'});

            for (int i = 0; i < RetentionTimeList.Count; i++)
            {
                Debug.WriteLine(RetentionTimeList[i]);
            }
            var fileName = "";
            switch (MethodNameText)
            {
                case "Cleaning":
                    break;
                case "TVOC":
                    break;
                case "BTEX":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch(System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "BTEX.json";
                    break;
                case "TCE/PCE":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "TCEPCE.json";
                    break;
                case "Malodorous":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "Malodorous.json";
                    break;
                case "VehicleIndoor":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "Vehicle.json";
                    break;
                case "EnvironmentalAir":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "AirQuality.json";
                    break;
                case "PollutionSource":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch (System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "PollutionSource.json";
                    break;
                case "WaterSample-Online":
                    for (int i = 0; i < RetentionTimeList.Count; i++)
                    {
                        try
                        {
                            double num;
                            if (double.TryParse(result[i], out num))
                            {
                                RetentionTimeList[i] = num;
                            }
                        }
                        catch(System.IndexOutOfRangeException ex)
                        {
                            MessageDialog popup = new MessageDialog("the number of parameters doesn't match!");
                            await popup.ShowAsync();
                        }
                    }
                    fileName = "WaterQuality.json";
                    break;
                default:
                    break;
            }

            //write the data back to the Json file
            //Create a folder: fileFloder dir calibrate -->methodFileName -->dateTimeFileName
            StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
            StorageFolder retentionFolder = await applicationFolder.CreateFolderAsync("Retention_update",
                CreationCollisionOption.OpenIfExists);
            StorageFolder pdfFolder = await retentionFolder.CreateFolderAsync(methodFileName,
                CreationCollisionOption.OpenIfExists);
            //write a raw data file
            FileNameTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            Rawfile = await pdfFolder.CreateFileAsync(FileNameTime + ".dat", Windows.Storage.CreationCollisionOption.ReplaceExisting);

            if (RetentionTimeList.Count > 0)
            {
                for (int i = 0; i < RetentionTimeList.Count; i++)
                {
                    await Windows.Storage.FileIO.AppendTextAsync(Rawfile,
                        RetentionTimeList[i] + ",");
                }
            }
        }

        public static System.Text.Encoding GetEncoding(byte[] bom)
        {
            // Analyze the BOM
            if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76) return System.Text.Encoding.UTF7;
            if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf) return System.Text.Encoding.UTF8;
            if (bom[0] == 0xff && bom[1] == 0xfe) return System.Text.Encoding.Unicode; //UTF-16LE
            if (bom[0] == 0xfe && bom[1] == 0xff) return System.Text.Encoding.BigEndianUnicode; //UTF-16BE
            if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff) return System.Text.Encoding.UTF32;
            return System.Text.Encoding.ASCII;
        }

        //Get VOC Concentration Factor List
        private async void GetConcentrationFactor(string FileName)
        {
            try
            {
                //Create a folder: fileFloder dir calibrate -->methodFileName -->dateTimeFileName
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder retentionFolder = await applicationFolder.CreateFolderAsync("calibrate_test",
                    CreationCollisionOption.OpenIfExists);
                StorageFolder pdfFolder = await retentionFolder.CreateFolderAsync(FileName,
                    CreationCollisionOption.OpenIfExists);
                //Query the file
                List<string> fileTypeFilter = new List<string>();
                fileTypeFilter.Add(".dat");
                var queryOptions = new Windows.Storage.Search.QueryOptions(Windows.Storage.Search.CommonFileQuery.OrderByName, fileTypeFilter);

                // Create query and retrieve files
                var query = pdfFolder.CreateFileQueryWithOptions(queryOptions);
                IReadOnlyList<StorageFile> fileList = await query.GetFilesAsync();
                // Process results
                long maxvalue = 0;
                foreach (StorageFile file in fileList)
                {
                    // Process file
                    Debug.WriteLine(file.Name);
                    if (long.Parse(file.Name.Split('.')[0]) > maxvalue)
                    {
                        maxvalue = long.Parse(file.Name.Split('.')[0]);
                    }
                }
                Debug.WriteLine(maxvalue);

                //Get the latest file 
                string latestFilename = maxvalue.ToString() + ".dat";
                StorageFile latestFile = await pdfFolder.GetFileAsync(latestFilename);

                if (latestFile != null)
                {
                    Debug.WriteLine("VOC file found");
                }

                IBuffer buffer = await FileIO.ReadBufferAsync(latestFile);
                DataReader reader = DataReader.FromBuffer(buffer);
                byte[] fileContent = new byte[reader.UnconsumedBufferLength];
                reader.ReadBytes(fileContent);
                string text = GetEncoding(new byte[4] { fileContent[0], fileContent[1], fileContent[2], fileContent[3] }).GetString(fileContent);
                String[] result = text.Split(new[] { ',' });
                //get CF and method name to newinfo
                if (result.Length == VOCNameList.Count)
                {
                    newinfo = new string[result.Length + 1, 2];
                    for (int i = 0; i < result.Length; i++)
                    {
                        for (int j = 0; j < 2; j++)
                        {
                            string[] newline = result[i].Split(new[] { ':' });
                            newinfo[i, j] = newline[j];
                        }
                    }
                    newinfo[result.Length, 0] = "datetime";
                    newinfo[result.Length, 1] = maxvalue.ToString();
                }
                if (newinfo.Length > 0 && newinfo.Length / 2 - 1 == VOCNameList.Count)
                {
                    for (var index = 0; index < VOCNameList.Count; index++)
                    {
                        VOCconcentrationList.Add(double.Parse(newinfo[index, 1]));
                        //Debug.WriteLine(VOCconcentrationList[index]);
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("VOC file not found");
            }
        }

        //private void ThresholdReset_Click(object sender, RoutedEventArgs e)
        //{

        //    THRESHOLD_peak = Convert.ToDouble(ThresholdInput.Text);
        //    Debug.WriteLine(THRESHOLD_peak);
        //    detectPeakAndBottom(x1, y1, peaks1, bottoms1, Area1, Heights1, MinY1);
        //    Debug.WriteLine("new peaks");
        //    for (int i = 0; i < peaks1.Count; i++)
        //    {
        //        Debug.WriteLine(peaks1[i]);
        //    }
        //}
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
