using System;
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
using Windows.UI;
using Windows.System.Profile;

namespace nanovaTest.Calibrate
{
    public sealed partial class RunCalibratePage : Page
    {
        public double FlowRate = 5; //ml/min
        private string FromSelect = null;
        //control CONFIG and STATUS button
        private int ClickStatus = 0;
        private CycleData c;
        private int total;
        private DispatcherTimer timer;
        private ObservableCollection<string> GasComboList;
        private ObservableCollection<CalibrateTestInfo> testInfoList;
        private ObservableCollection<CalibrateTestInfo> secondaryInfoList;

        private string MethodNameText;

        private int count = 0;
        private ResourceLoader loader;
        private string methodFileName;
        public ChartAdornmentInfo AdornmentInfo { get; set; }
        private ObservableCollection<string> operatorList;

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

        //VOC concentrate data
        private double StandardConcentration;
        private double VOCconcentration;
        private double VOCresponseFactor;
        private List<double> VOCconcentrationList = new List<double>();
        private string CalibrateSelected;
        private string calibrationFileName = "";

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
        private double MinY1 = 100;
        private double MinY2 = 100;

        //write into files
        private string FileNameTime = "";
        private Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
        private Windows.Storage.StorageFile Rawfile;

        public class ChartData
        {
            public double label { get; set;}
            public double text { get; set;}
        }
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

        public void Dispose()
        {
            if (null != testInfoList)
            {
                testInfoList.Clear();
            }
            if (null != operatorList)
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

        private async void initPage()
        {
            try
            {
                LoadingIndicator.IsActive = true;
                CalibrateGrid.Visibility = Visibility.Collapsed;
                testInfoList = new ObservableCollection<CalibrateTestInfo>();
                initOperator();
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
            VOCconcentrationList.Clear();
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
                    initGas(fileName);
                    ReadFromJson(fileName);
                    GasComboBox.ItemsSource = GasComboList;
                    //Hide the Gascombolist
                    GasComboBox.Visibility = Visibility.Collapsed;
                    var CalibrateString = MethodName.Text;
                    if (MethodName.Text == loader.GetString("AirQuality1"))
                    {
                        CalibrateString = "TO14";
                    }
                    GasComboBoxText.Text = "Calibration Gas: " + CalibrateString;
                    //end hide
                    UpdateRentention(FromSelect);
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
            catch (FileNotFoundException)
            {
                Debug.WriteLine("Update file not found");
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
            //var selectGas = GasComboBox.SelectedValue;
            var selectGas = " ";
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
            //save calibrition button sync with list
            SaveCalivration.Visibility = Visibility.Collapsed;
            InfoListView.Visibility = Visibility.Collapsed;
            InfoListView1.Visibility = Visibility.Collapsed;
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
                        currentconcen = currentvocarea * CalibrationFactor / ((FlowRate * Sampletimeuwp / 60.0) * ResposeFactorList[j]);
                        //handle null error
                        string currentCF = "";
                        if (VOCconcentrationList.Count == VOCNameList.Count)
                        {
                            currentCF = VOCconcentrationList[j].ToString("0.00");
                        }
                        string currentvocname = VOCNameList[j];
                        if (j == 3)
                            currentvocname = currentvocname + " & " + VOCNameList[j + 1];
                        if (j != 4)
                        {
                            Peak1DCount++;
                            testInfoList.Add(new CalibrateTestInfo
                            {
                                ID = (Peak1DCount).ToString(),
                                VOCName = currentvocname,
                                Time = currentvoctime.ToString("0.00"),
                                FWHM = FWHMvalue.ToString("0.00"),
                                Height = currentvocheight.ToString("0.00"),
                                Area = currentvocarea.ToString("0.00"),
                                Concentration = currentCF
                            });
                        }
                        //Reset other parameters to 0
                        currentvoctime = 0;
                        currentvocheight = 0;
                        currentvocarea = 0;
                        FWHMvalue = 0;
                    }
                    //update VOC
                    //updateVOC();
                    showVOC();
                    //save calibrition button sync with list
                    SaveCalivration.Visibility = Visibility;
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
                        currentconcen = currentvocarea * CalibrationFactor / ((FlowRate * Sampletimeuwp / 60.0) * ResposeFactorList[j]);
                        string currentCF = "";
                        if (VOCconcentrationList.Count == VOCNameList.Count)
                        {
                            currentCF = VOCconcentrationList[j].ToString("0.00");
                        }
                        if (Math.Abs(RetentionTimeList[j] - 0) > 0.01) //2D gas
                        {
                            Peak1DCount++;
                            testInfoList.Add(new CalibrateTestInfo
                            {
                                ID = (Peak1DCount).ToString(),
                                VOCName = VOCNameList[j],
                                Time = currentvoctime.ToString("0.00"),
                                FWHM = FWHMvalue.ToString("0.00"),
                                Height = currentvocheight.ToString("0.00"),
                                Area = currentvocarea.ToString("0.00"),
                                Concentration = currentCF
                            });
                        }
                        //Reset other parameters to 0
                        currentvoctime = 0;
                        currentvocheight = 0;
                        currentvocarea = 0;
                        FWHMvalue = 0;
                    }
                    //update VOC
                    //updateVOC();
                    showVOC();
                    //save calibrition button sync with list
                    SaveCalivration.Visibility = Visibility;
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
                                currentconcen = currentvocarea * CalibrationFactor2D / ((FlowRate * Sampletimeuwp / 60.0) * ResposeFactorList[j]);
                                secondaryInfoList.Add(new CalibrateTestInfo
                                {
                                    ID = Peak2DCount.ToString(),
                                    VOCName = VOCNameList[j],
                                    Time = currentvoctime.ToString("0.00"),
                                    FWHM = FWHMvalue.ToString("0.00"),
                                    Height = currentvocheight.ToString("0.00"),
                                    Area = currentvocarea.ToString("0.00"),
                                    Concentration = VOCconcentrationList[j].ToString("0.00")
                                });
                            }
                        }
                        //SecondaryGrid.Visibility = Visibility;
                    }
                }
                //显示表格控件
                //testInfoList.Clear();
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
                                currentconcen = currentvocarea * CalibrationFactor / ((FlowRate * Sampletimeuwp / 60.0) * ResposeFactorList[j]);
                                //handle null error
                                string currentCF = "";
                                if (VOCconcentrationList.Count == VOCNameList.Count)
                                {
                                    currentCF = VOCconcentrationList[j].ToString("0.00");
                                }
                                string currentvocname = VOCNameList[j];
                                if (j == 3)
                                    currentvocname = currentvocname + " & " + VOCNameList[j + 1];
                                if (j != 4)
                                {
                                    Peak1DCount++;
                                    testInfoList.Add(new CalibrateTestInfo
                                    {
                                        ID = (Peak1DCount).ToString(),
                                        VOCName = currentvocname,
                                        Time = currentvoctime.ToString("0.00"),
                                        FWHM = FWHMvalue.ToString("0.00"),
                                        Height = currentvocheight.ToString("0.00"),
                                        Area = currentvocarea.ToString("0.00"),
                                        Concentration = currentCF
                                    });
                                }
                                //Reset other parameters to 0
                                currentvoctime = 0;
                                currentvocheight = 0;
                                currentvocarea = 0;
                                FWHMvalue = 0;
                            }
                            //update VOC
                            //updateVOC();
                            showVOC();
                            //save calibrition button sync with list
                            SaveCalivration.Visibility = Visibility;
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
                                currentconcen = currentvocarea * CalibrationFactor / ((FlowRate * Sampletimeuwp / 60.0) * ResposeFactorList[j]);
                                string currentCF = "";
                                if (VOCconcentrationList.Count == VOCNameList.Count)
                                {
                                    currentCF = VOCconcentrationList[j].ToString("0.00");
                                }
                                if (Math.Abs(RetentionTimeList[j] - 0) > 0.01) //2D gas
                                {
                                    Peak1DCount++;
                                    testInfoList.Add(new CalibrateTestInfo
                                    {
                                        ID = (Peak1DCount).ToString(),
                                        VOCName = VOCNameList[j],
                                        Time = currentvoctime.ToString("0.00"),
                                        FWHM = FWHMvalue.ToString("0.00"),
                                        Height = currentvocheight.ToString("0.00"),
                                        Area = currentvocarea.ToString("0.00"),
                                        Concentration = currentCF
                                    });
                                }
                                //Reset other parameters to 0
                                currentvoctime = 0;
                                currentvocheight = 0;
                                currentvocarea = 0;
                                FWHMvalue = 0;
                            }
                            //update VOC
                            //updateVOC();
                            showVOC();
                            //save calibrition button sync with list
                            SaveCalivration.Visibility = Visibility;
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
                                        currentconcen = currentvocarea * CalibrationFactor2D / ((FlowRate * Sampletimeuwp / 60.0) * ResposeFactorList[j]);
                                        secondaryInfoList.Add(new CalibrateTestInfo
                                        {
                                            ID = Peak2DCount.ToString(),
                                            VOCName = VOCNameList[j],
                                            Time = currentvoctime.ToString("0.00"),
                                            FWHM = FWHMvalue.ToString("0.00"),
                                            Height = currentvocheight.ToString("0.00"),
                                            Area = currentvocarea.ToString("0.00"),
                                            Concentration = VOCconcentrationList[j].ToString("0.00")
                                    });
                                    }
                                }
                                //SecondaryGrid.Visibility = Visibility;
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
                Stream fontStream = File.OpenRead("Assets/gadugi.ttf");
                Stream textFontStream = File.OpenRead("Assets/Calibri.ttf");
                PdfFont titleFont = new PdfTrueTypeFont(fontStream, 20);
                PdfFont headerFont = new PdfTrueTypeFont(fontStream, 9);
                PdfFont logoFont = new PdfTrueTypeFont(fontStream, 30);
                PdfFont footerFont = new PdfTrueTypeFont(fontStream, 9);
                PdfFont font2 = new PdfTrueTypeFont(textFontStream, 11);
                PdfFont font = new PdfTrueTypeFont(textFontStream, 11);
                PdfFont tableFont = new PdfTrueTypeFont(textFontStream, 9);

                PdfStringFormat sf = new PdfStringFormat();
                sf.Alignment = PdfTextAlignment.Center;
                sf.LineAlignment = PdfVerticalAlignment.Middle;

                //logo
                Stream imageStream = File.OpenRead("Assets/logo.png");
                PdfBitmap image = new PdfBitmap(imageStream);
                RectangleF rf0 = new RectangleF(0, 0, 200, 45);
                graphics.DrawImage(image, rf0);

                //header
                PdfStringFormat format = new PdfStringFormat(PdfTextAlignment.Right);
                String headerText = "Nanova Environmental, Inc.";
                String headerText1 = "www.nanovaenv.com";
                String headerText2 = "+1 (573)-476-6355";
                RectangleF rfh = new RectangleF(page.Graphics.ClientSize.Width - 130, 0, 130, 20);
                RectangleF rfh1 = new RectangleF(page.Graphics.ClientSize.Width - 130, 13, 130, 20);
                RectangleF rfh2 = new RectangleF(page.Graphics.ClientSize.Width - 130, 26, 130, 20);

                graphics.DrawString(headerText, headerFont, PdfBrushes.Gray, rfh, format);
                graphics.DrawString(headerText1, headerFont, PdfBrushes.Gray, rfh1, format);
                graphics.DrawString(headerText2, headerFont, PdfBrushes.Gray, rfh2, format);



                //report title
                RectangleF rf = new RectangleF(page.Graphics.ClientSize.Width / 2 - 200, 85, 400, 30);
                document.Pages[0].Graphics.DrawString("NovaTest P100 Calibration", titleFont, PdfBrushes.Black, rf, sf);

                RectangleF rf1 = new RectangleF(0, 130, 500, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Method"), MethodName.Text), font, PdfBrushes.Black, rf1);

                RectangleF rf2 = new RectangleF(260, 130, 500, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("OperatorName1"), OperatorName.SelectedValue), font, PdfBrushes.Black, rf2);

                RectangleF rf3 = new RectangleF(0, 145, 500, 40);
                graphics.DrawString(string.Format("{0} {1}", GasComboBoxText.Text, ConcentrationName.Text + "ppb"), font, PdfBrushes.Black, rf3);


                RectangleF rf16 = new RectangleF(260, 145, 450, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("StartTime"), DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss", DateTimeFormatInfo.InvariantInfo)), font, PdfBrushes.Black, rf16);

                RectangleF rf17 = new RectangleF(0, 159, 450, 40);
                String instrumentString = "Instrument: NovaTest P100";
                document.Pages[0].Graphics.DrawString(instrumentString, font, PdfBrushes.Black, rf17);

                RectangleF rf18 = new RectangleF(260, 159, 450, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", "Calibration File", calibrationFileName), font, PdfBrushes.Black, rf18);



                RectangleF rf15 = new RectangleF(page.Graphics.ClientSize.Width / 2 - 70, 190, 400, 40);
                String parameterString = "Programming Parameters";
                //PdfStringFormat format = new PdfStringFormat(PdfTextAlignment.Right);
                document.Pages[0].Graphics.DrawString(parameterString, font, PdfBrushes.Black, rf15);

                RectangleF rf4 = new RectangleF(0, 208, 450, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("SamplingPumpingTime"), SamplingTimeText.Text), font2, PdfBrushes.Black, rf4);

                RectangleF rf5 = new RectangleF(180, 208, 450, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("WaitingTime") , WaitTimeText.Text), font2, PdfBrushes.Black, rf5);

                RectangleF rf6 = new RectangleF(360, 208, 450, 40);
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

                RectangleF rf7 = new RectangleF(0, 223, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("LowestTemp1") + "(°C)", lowestTempvalue), font2, PdfBrushes.Black, rf7);

                RectangleF rf8 = new RectangleF(180, 223, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("LowHoldingTime1"), lowestTvalue), font2, PdfBrushes.Black, rf8);

                RectangleF rf9 = new RectangleF(0, 238, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temperature11") + "(°C)", Temp1value), font2, PdfBrushes.Black, rf9);

                RectangleF rf10 = new RectangleF(180, 238, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temp1HoldigTime"), HoldT1value), font2, PdfBrushes.Black, rf10);

                RectangleF rf11 = new RectangleF(360, 238, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("RampSpeed11") + "(°C/min)", RampSpeed1value), font2, PdfBrushes.Black, rf11);

                RectangleF rf12 = new RectangleF(0, 253, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temperatures2") + "(°C)", Temp2value), font2, PdfBrushes.Black, rf12);

                RectangleF rf13 = new RectangleF(180, 253, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temp2HoldigTime"), HoldT2value), font2, PdfBrushes.Black, rf13);

                RectangleF rf14 = new RectangleF(360, 253, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("RampSpeed2") + "(°C/min)", RampSpeed2value), font2, PdfBrushes.Black, rf14);

               

                PdfPen blackPen = new PdfPen(PdfColor.Empty);
                PointF pf1 = new PointF(0, 185);
                PointF pf2 = new PointF(page.Graphics.ClientSize.Width, 185);
                graphics.DrawLine(blackPen, pf1, pf2);

                PdfPen blackPen2 = new PdfPen(PdfColor.Empty);
                PointF pf3 = new PointF(0, 205);
                PointF pf4 = new PointF(page.Graphics.ClientSize.Width, 205);
                graphics.DrawLine(blackPen, pf3, pf4);

                PdfPen blackPen3 = new PdfPen(PdfColor.Empty);
                PointF pf5 = new PointF(0, 268);
                PointF pf6 = new PointF(page.Graphics.ClientSize.Width, 268);
                graphics.DrawLine(blackPen, pf5, pf6);


                //Initializing to render to Bitmap
                var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                var renderTargetBitmap = new RenderTargetBitmap();

                //*************************hide element
                InfoListView.Visibility = Visibility.Collapsed;
                //Create the Bitmpa from xaml page
                double gridWidth = CustomGrid.ActualWidth;
                double gridHeight = CustomGrid.ActualHeight;
                await renderTargetBitmap.RenderAsync(CustomGrid, (int)gridWidth, (int)gridHeight);
                //CustomImage.Source = renderTargetBitmap;
                var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                //************************show element
                InfoListView.Visibility = Visibility.Visible;

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

                

                    PdfImage img = PdfImage.FromStream(stream.AsStream());
                    //PdfBitmap image = new PdfBitmap(renderTargetBitmap.);
                    graphics.DrawImage(img, new RectangleF(0, 275, (float)gridWidth / 1.3f, (float)gridHeight / 1.5f));
                }


                //footer

                RectangleF bounds = new RectangleF(0, 0, document.Pages[0].GetClientSize().Width, 100);
                PdfPageTemplateElement header = new PdfPageTemplateElement(bounds);

                // information about novatest

                SizeF pageSize = document.Pages[0].Size;

               
                PdfPageTemplateElement footerSpace = new PdfPageTemplateElement(bounds);
                footerSpace.Foreground = true;
                document.Template.Bottom = footerSpace;

         
                PdfPageNumberField number = new PdfPageNumberField();
                //Create page count automatic field  
                PdfPageCountField count = new PdfPageCountField();
                //Add the fields in composite field  
                PdfCompositeField compositeField1 = new PdfCompositeField(footerFont, PdfBrushes.Gray, "Copyright © 2017 Nanova Environmental, Inc.All Rights Reserved", number, count);
                compositeField1.StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Bottom);
                compositeField1.Bounds = footerSpace.Bounds;
                compositeField1.Draw(footerSpace.Graphics);

                PdfCompositeField compositeField = new PdfCompositeField(footerFont, PdfBrushes.Gray, "Page {0} of {1}", number, count);
                //Align string of "Page {0} of {1}" to center   
                compositeField.StringFormat = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Bottom);
                compositeField.Bounds = footerSpace.Bounds;
                //Draw composite field at footer space  
                compositeField.Draw(footerSpace.Graphics);



                //*************************Second page
                PdfFont font3 = new PdfCjkStandardFont(PdfCjkFontFamily.SinoTypeSongLight, 10, PdfFontStyle.Regular);
                //Add a page in the PDF document.
                PdfPage page2 = document.Pages.Add();
                //Access the PDF graphics instance of the page.
                PdfGraphics graphics2 = page2.Graphics;
                var Width = 70;
                var Length = 20;
                RectangleF p21 = new RectangleF(0, 0, Width-20, Length);
                RectangleF p22 = new RectangleF(Width-20, 0, Width+50, Length);
                RectangleF p23 = new RectangleF(2 * Width+30, 0, Width-10, Length);
                RectangleF p24 = new RectangleF(3 * Width+20, 0, Width-10, Length);
                RectangleF p25 = new RectangleF(4 * Width+10, 0, Width-10, Length);
                RectangleF p26 = new RectangleF(5 * Width, 0, Width-10, Length);
                RectangleF p27 = new RectangleF(6 * Width-10, 0, Width+10, Length);
                RectangleF p21s = new RectangleF(0 + (0.18f * Width), 0, Width, Length);
                RectangleF p22s = new RectangleF(Width + (0.25f * Width), 0, Width, Length);
                RectangleF p23s = new RectangleF(2 * Width + (0.73f * Width), 0, Width, Length);
                RectangleF p24s = new RectangleF(3 * Width + (0.4f * Width), 0, Width, Length);
                RectangleF p25s = new RectangleF(4 * Width + (0.32f * Width), 0, Width, Length);
                RectangleF p26s = new RectangleF(5 * Width + (0.3f * Width), 0, Width, Length);
                RectangleF p27s = new RectangleF(6 * Width + (0.04f * Width), 0, Width, Length);
                graphics2.DrawRectangle(PdfPens.Black, p21);
                graphics2.DrawString("Peak#", font, PdfBrushes.Black, p21s);
                graphics2.DrawRectangle(PdfPens.Black, p22);
                graphics2.DrawString("Compound", font, PdfBrushes.Black, p22s);
                graphics2.DrawRectangle(PdfPens.Black, p23);
                graphics2.DrawString("RT(s)", font, PdfBrushes.Black, p23s);
                graphics2.DrawRectangle(PdfPens.Black, p24);
                graphics2.DrawString("FWHM(s)", font, PdfBrushes.Black, p24s);
                graphics2.DrawRectangle(PdfPens.Black, p25);
                graphics2.DrawString("Height", font, PdfBrushes.Black, p25s);
                graphics2.DrawRectangle(PdfPens.Black, p26);
                graphics2.DrawString("Area", font, PdfBrushes.Black, p26s);
                graphics2.DrawRectangle(PdfPens.Black, p27);
                graphics2.DrawString("CONCN Factor", font, PdfBrushes.Black, p27s);
                for (int i = 0; i < testInfoList.Count; i++)
                {
                    Debug.WriteLine(testInfoList[i].VOCName);
                    p21 = new RectangleF(0, Length * (i + 1), Width-20, Length);
                    p22 = new RectangleF(Width-20, Length * (i + 1), Width+50, Length);
                    p23 = new RectangleF(2 * Width+30, Length * (i + 1), Width-10, Length);
                    p24 = new RectangleF(3 * Width+20, Length * (i + 1), Width-10, Length);
                    p25 = new RectangleF(4 * Width+10, Length * (i + 1), Width-10, Length);
                    p26 = new RectangleF(5 * Width, Length * (i + 1), Width-10, Length);
                    p27 = new RectangleF(6 * Width-10, Length * (i + 1), Width+10, Length);
                    p21s = new RectangleF(0 + (0.3f * Width), Length * (i + 1.2f), Width, Length);
                    p22s = new RectangleF(Width-10, Length * (i + 1.2f), Width+50, Length);
                    p23s = new RectangleF(2 * Width + (0.73f * Width), Length * (i + 1.2f), Width, Length);
                    p24s = new RectangleF(3 * Width + (0.5f * Width), Length * (i + 1.2f), Width, Length);
                    p25s = new RectangleF(4 * Width + (0.4f * Width), Length * (i + 1.2f), Width, Length);
                    p26s = new RectangleF(5 * Width + (0.35f * Width), Length * (i + 1.2f), Width, Length);
                    p27s = new RectangleF(6 * Width + (0.4f * Width), Length * (i + 1.2f), Width, Length);

                    graphics2.DrawRectangle(PdfPens.Black, p21);
                    graphics2.DrawString(testInfoList[i].ID, font2, PdfBrushes.Black, p21s);
                    graphics2.DrawRectangle(PdfPens.Black, p22);
                    graphics2.DrawString(testInfoList[i].VOCName, font2, PdfBrushes.Black, p22s);
                    graphics2.DrawRectangle(PdfPens.Black, p23);
                    graphics2.DrawString(testInfoList[i].Time, font2, PdfBrushes.Black, p23s);
                    graphics2.DrawRectangle(PdfPens.Black, p24);
                    graphics2.DrawString(testInfoList[i].FWHM, font2, PdfBrushes.Black, p24s);
                    graphics2.DrawRectangle(PdfPens.Black, p25);
                    graphics2.DrawString(testInfoList[i].Height, font2, PdfBrushes.Black, p25s);
                    graphics2.DrawRectangle(PdfPens.Black, p26);
                    graphics2.DrawString(testInfoList[i].Area, font2, PdfBrushes.Black, p26s);
                    graphics2.DrawRectangle(PdfPens.Black, p27);
                    graphics2.DrawString(testInfoList[i].Concentration, font2, PdfBrushes.Black, p27s);
                }
                //*******************************************
                //Save the Pdf document
                MemoryStream docStream = new MemoryStream();
                document.Save(docStream);
                document.Close(true);

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
            //updateVOC();
        }

        //update VOC library
        private async void updateVOC()
        {
            ////Debug.WriteLine(float.Parse(ConcentrationName.Text));
            //StandardConcentration = float.Parse(ConcentrationName.Text);
            //CalibrateSelected = GasComboBox.SelectedValue.ToString();
            ////VOCconcentration = float.Parse(ConcentrationName.Text);
            //Debug.WriteLine(CalibrateSelected);
            //for (int i = 0; i < testInfoList.Count; i++)
            //{
            //    //update every point CF based on their concentration
            //    double GasVolume = FlowRate * Sampletimeuwp;
            //    VOCconcentration = GasVolume * StandardConcentration / float.Parse(testInfoList[i].Area);
            //    VOCconcentrationList.Add(VOCconcentration);
            //    testInfoList[i].ConcentrationFactor = VOCconcentration.ToString("0.00");
                //testInfoList[i].ConcentrationFactor = "test this";
                /*
                //if there is same peak detected
                if (CalibrateSelected.Equals(testInfoList[i].VOCName))
                {
                    //Calculate the Concetration Factor
                    Debug.WriteLine(testInfoList[i].Area);
                    double GasVolume = FlowRate * Sampletimeuwp;
                    VOCconcentration = GasVolume * StandardConcentration / float.Parse(testInfoList[i].Area);
                    if (VOCconcentration > 0)
                    {

                        int index = 0;
                        //Calcilate other factor based on the Single gas, initial the list with one value and record the index of test gas
                        for (int j = 0; j < VOCNameList.Count; j++)
                        {
                            //if selected method == Name in the VOClist
                            if (VOCNameList[j] == CalibrateSelected)
                            {
                                index = j;
                                VOCresponseFactor = ResposeFactorList[j];
                                VOCconcentrationList.Add(VOCconcentration);
                            }
                            else
                            {
                                VOCconcentrationList.Add(VOCconcentration);
                            }
                        }
                        //Calculate other concentrations
                        for (int k = 0; k < VOCconcentrationList.Count; k++)
                        {
                            if (k != index)
                            {
                                VOCconcentrationList[k] *= ResposeFactorList[k] / VOCresponseFactor;
                            }
                        }
                    }
                    if (VOCconcentrationList.Count != VOCNameList.Count)
                    {
                        MessageDialog popup = new MessageDialog("Update hasn't completed properly!");
                        await popup.ShowAsync();
                    }
                }
                */
                /*
                else
                {
                    MessageDialog popup = new MessageDialog("No Peak has been found to update!");
                    await popup.ShowAsync();
                }
                */
            //}
            //if (testInfoList.Count == 0)
            //{
            //    MessageDialog popup = new MessageDialog("No Peak has been found to update!");
            //    await popup.ShowAsync();
            //}
            if (MethodNameText == "BTEX")
            {
                if (VOCNameList.Count - 1 == VOCconcentrationList.Count)
                {
                    //Create a folder: fileFloder dir calibrate -->methodFileName -->dateTimeFileName
                    StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                    StorageFolder calibrateFolder = await applicationFolder.CreateFolderAsync("calibrate_test",
                        CreationCollisionOption.OpenIfExists);
                    StorageFolder pdfFolder = await calibrateFolder.CreateFolderAsync(methodFileName,
                        CreationCollisionOption.OpenIfExists);
                    //write a raw data file
                    FileNameTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                    Rawfile = await pdfFolder.CreateFileAsync(FileNameTime + ".dat", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                    calibrationFileName = FileNameTime + ".dat";
                    //append text to the file
                    if (VOCconcentrationList.Count > 0)
                    {
                        for (int i = 0; i < VOCconcentrationList.Count - 1; i++)
                        {
                            await Windows.Storage.FileIO.AppendTextAsync(Rawfile,
                                VOCNameList[i] + ":" + VOCconcentrationList[i] + "|");
                            if (i == 3)
                            {
                                await Windows.Storage.FileIO.AppendTextAsync(Rawfile,
                                    VOCNameList[i+1] + ":" + VOCconcentrationList[i] + "|");
                            }
                        }
                        await Windows.Storage.FileIO.AppendTextAsync(Rawfile,
                            VOCNameList[VOCconcentrationList.Count] + ":" + VOCconcentrationList[VOCconcentrationList.Count - 1]);
                    }
                }
            }
            if (VOCNameList.Count == VOCconcentrationList.Count)
            {
                //Create a folder: fileFloder dir calibrate -->methodFileName -->dateTimeFileName
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder calibrateFolder = await applicationFolder.CreateFolderAsync("calibrate_test",
                    CreationCollisionOption.OpenIfExists);
                StorageFolder pdfFolder = await calibrateFolder.CreateFolderAsync(methodFileName,
                    CreationCollisionOption.OpenIfExists);
                //write a raw data file
                FileNameTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
                Rawfile = await pdfFolder.CreateFileAsync(FileNameTime + ".dat", Windows.Storage.CreationCollisionOption.ReplaceExisting);
                calibrationFileName = FileNameTime + ".dat";
                //append text to the file
                if (VOCconcentrationList.Count > 0)
                {
                    for (int i = 0; i < VOCconcentrationList.Count - 1; i++)
                    {
                        await Windows.Storage.FileIO.AppendTextAsync(Rawfile,
                            VOCNameList[i] + ":" + VOCconcentrationList[i] + "|");
                    }
                    await Windows.Storage.FileIO.AppendTextAsync(Rawfile,
                        VOCNameList[VOCconcentrationList.Count - 1] + ":" + VOCconcentrationList[VOCconcentrationList.Count - 1]);
                }
            }
            else
            {
                Debug.WriteLine("doesn't find the correct value pair");
            }
        }

        //update VOC library
        private async void showVOC()
        {
            StandardConcentration = float.Parse(ConcentrationName.Text);
            //CalibrateSelected = GasComboBox.SelectedValue.ToString();
            for (int i = 0; i < testInfoList.Count; i++)
            {
                //update every point CF based on their concentration
                double GasVolume = FlowRate * Sampletimeuwp;
                VOCconcentration = GasVolume * StandardConcentration / float.Parse(testInfoList[i].Area);
                if (float.Parse(testInfoList[i].Area) == 0)
                {
                    VOCconcentration = 0;
                }
                Debug.WriteLine(VOCconcentration);
                VOCconcentrationList.Add(VOCconcentration);
                Debug.WriteLine(VOCconcentrationList);
                testInfoList[i].Concentration = VOCconcentration.ToString("0.00");
            }
            if (testInfoList.Count == 0)
            {
                MessageDialog popup = new MessageDialog("No Peak has been found to update!");
                await popup.ShowAsync();
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
        private int constant_m = 35; // for SNIP baseline formula
        private int constant_m_end = Convert.ToInt32(1 / Math.Sqrt(2)); // for SNIP baseline formula
        private int CONSECUTIVE_SCAN_STEPS = 3;   //for peak detection
        private double THRESHOLD = 0.005f;        //for peak detection: slope
        private double THRESHOLD_peak = 0.2f;       //for peak detection: slope
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
            //change threshold for BTEX and airquality to 0.2
            if (methodFileName == "Air Qualuty" || methodFileName == "BTEX")
            {
                THRESHOLD_peak = 0.2f;
            }
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
                catch (Exception ex)
                {
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

        private void SaveCalivration_Click(object sender, RoutedEventArgs e)
        {
            //update VOC
            updateVOC();
            NotifyPopup notifyPopup = new NotifyPopup("Calibration file save successfully");
            notifyPopup.Show();
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
