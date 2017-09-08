using nanovaTest.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using System.Globalization;
using Windows.ApplicationModel.Resources;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.SerialCommunication;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Networking.Sockets;
using Windows.System;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Syncfusion.UI.Xaml.Charts;
using Windows.System.Profile;
using Windows.UI;

namespace nanovaTest.CustomMethod
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class CustomMethodPage : Page
    {
        //control CONFIG and STATUS button
        private int ClickStatus = 0;
        //control Calculation button
        private int ClickCalculationStatus = 0;
        private bool ViewBottom = false;

        //上图信号线数据源
        IList<Data> topSource;
        //上图基准线数据源
        IList<Data> topStandardSource;

        //下图信号线数据源
        IList<Data> bottomSource;
        //下图基准线数据源
        IList<Data> bottomStandardSource;
        private ObservableCollection<string> operatorList;

        private ObservableCollection<AdvanceTestInfo> primaryInfoList;
        private ObservableCollection<AdvanceTestInfo> secondaryInfoList;
        private ResourceLoader loader;
        //折线图数据模型
        class ChartData
        {
            public double label { get; set; } //x
            public double text { get; set; } //y
        }

        private CycleData c;
        private int total;
        private DispatcherTimer timer;

        Random random = new Random();
        //time profile from input
        private double Sampletimeuwp;
        private double Waitingtimeuwp;
        private double Analysistimeuwp;
        private double Cleaningtimeuwp = 60;
        private DateTime StartDateTime = DateTime.Now;
        private DateTime CurrentDateTime = DateTime.Now;

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
        private double RoomTempvalue = 35;
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

        public CustomMethodPage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            primaryInfoList = new ObservableCollection<AdvanceTestInfo>();
            secondaryInfoList = new ObservableCollection<AdvanceTestInfo>();
            loader = new ResourceLoader();
            initPage();
        }

        private async void initPage()
        {
            try
            {
                LoadingIndicator.IsActive = true;
                MethodGrid.Visibility = Visibility.Collapsed;
                initTopChart();
                initBottomChart();
                initOperator();
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                LoadingIndicator.IsActive = false;
                devices_list();
            }
            finally
            {
                MethodGrid.Visibility = Visibility.Visible;
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
            topSource.Clear();
            topStandardSource.Clear();
            bottomSource.Clear();
            bottomStandardSource.Clear();
            primaryInfoList.Clear();
            secondaryInfoList.Clear();
            initTopChart();
            initBottomChart();
            MinY1 = 100;
            MinY2 = 100;
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
            RoomTempvalue = 35;
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
            peaks2.Clear();     //save all the peaks
            bottoms2.Clear();    //save all the bottoms
            Area2.Clear(); 
            Heights2.Clear(); 
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
                PdfFont font = new PdfCjkStandardFont(PdfCjkFontFamily.SinoTypeSongLight, 12, PdfFontStyle.Bold);
                PdfStringFormat sf = new PdfStringFormat();
                sf.Alignment = PdfTextAlignment.Center;
                sf.LineAlignment = PdfVerticalAlignment.Middle;

                RectangleF rf = new RectangleF(page.Graphics.ClientSize.Width / 2 - 200, 0, 400, 30);
                document.Pages[0].Graphics.DrawString(loader.GetString("AdvanceReportTitle"), font, PdfBrushes.Black, rf, sf);

                RectangleF rf1 = new RectangleF(0, 35, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("ExperienceName1"), ExperienceName.Text), font, PdfBrushes.Black, rf1);

                RectangleF rf2 = new RectangleF(220, 35, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("OperatorName1"), OperatorName.SelectedValue), font, PdfBrushes.Black, rf2);

                RectangleF rf3 = new RectangleF(0, 55, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("StartTime"), DateTime.Now.ToString("F", DateTimeFormatInfo.InvariantInfo)), font, PdfBrushes.Black, rf3);

                RectangleF rf4 = new RectangleF(0, 75, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("SamplingPumpingTime"), SamplingTimeText.Text), font, PdfBrushes.Black, rf4);

                RectangleF rf5 = new RectangleF(220, 75, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("WaitingTime"), WaitTimeText.Text), font, PdfBrushes.Black, rf5);

                RectangleF rf6 = new RectangleF(0, 95, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("PressurePDF1"), SetPressureText.Text), font, PdfBrushes.Black, rf6);

                RectangleF rf7 = new RectangleF(0, 115, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("LowestTemp1"), LowestTempText.Text), font, PdfBrushes.Black, rf7);

                RectangleF rf8 = new RectangleF(220, 115, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("LowHoldingTime1"), LowHoldingTimeText.Text), font, PdfBrushes.Black, rf8);

                RectangleF rf9 = new RectangleF(0, 135, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temperature11"), Temp1Text.Text), font, PdfBrushes.Black, rf9);

                RectangleF rf10 = new RectangleF(220, 135, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temp1HoldigTime"), Hold1Text.Text), font, PdfBrushes.Black, rf10);

                RectangleF rf11 = new RectangleF(0, 155, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("RampSpeed11"), RampSpeed1Text.Text), font, PdfBrushes.Black, rf11);

                RectangleF rf12 = new RectangleF(220, 155, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temperatures2"), Temp2Text.Text), font, PdfBrushes.Black, rf12);

                RectangleF rf13 = new RectangleF(0, 175, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("Temp2HoldigTime"), Hold2Text.Text), font, PdfBrushes.Black, rf13);

                RectangleF rf14 = new RectangleF(220, 175, 400, 40);
                document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("RampSpeed2"), RampSpeed2Text.Text), font, PdfBrushes.Black, rf14);
                

                if (heartcuttingNumber > 0)
                {
                    /*RectangleF rf2dperiod = new RectangleF(0, 195, 400, 40);
                    document.Pages[0].Graphics.DrawString(string.Format("{0}:", loader.GetString("HeartCuttingPeriodspdf")), font, PdfBrushes.Black, rf2dperiod);
                    int picturestartposition = 205;
                    for (int pdf2d = 0; pdf2d < heartcuttingNumber; pdf2d++)
                    {
                        RectangleF rf2dstart = new RectangleF(0, 205 + pdf2d * 20, 400, 40);
                        document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("StartTime"), heartcuttingStartList[pdf2d + 1].ToString()), font, PdfBrushes.Black, rf2dstart);
                        RectangleF rf2dend = new RectangleF(220, 205 + pdf2d * 20, 400, 40);
                        document.Pages[0].Graphics.DrawString(string.Format("{0}: {1}", loader.GetString("EndTime1"), heartcuttingEndList[pdf2d + 1].ToString()), font, PdfBrushes.Black, rf2dend);
                        picturestartposition = picturestartposition + 20;
                    }*/
                    //Initializing to render to Bitmap
                    var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var renderTargetBitmap = new RenderTargetBitmap();

                    //Create the Bitmpa from xaml page
                    await renderTargetBitmap.RenderAsync(CustomTopGrid, 700, 900);
                    //CustomImage.Source = renderTargetBitmap;
                    var pixelBuffer = await renderTargetBitmap.GetPixelsAsync();

                    //Save the XAML in Bitmap image
                    using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                    {

                        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream);
                        encoder.SetPixelData(
                            BitmapPixelFormat.Yuy2,
                            BitmapAlphaMode.Premultiplied,
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
                        document.Pages[0].Graphics.DrawImage(img, new RectangleF(0, 195, 500, 400));
                    }

                    document.Pages.Add();

                    //Initializing to render to Bitmap
                    var logicalDpi1 = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var renderTargetBitmap1 = new RenderTargetBitmap();

                    //Create the Bitmpa from xaml page
                    await renderTargetBitmap1.RenderAsync(CustomBottomGrid, 510, 1600);
                    //CustomImage.Source = renderTargetBitmap;
                    var pixelBuffer1 = await renderTargetBitmap1.GetPixelsAsync();

                    //Save the XAML in Bitmap image
                    using (var stream1 = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                    {

                        var encoder1 = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, stream1);
                        encoder1.SetPixelData(
                            BitmapPixelFormat.Bgra8,
                            BitmapAlphaMode.Ignore,
                            (uint)renderTargetBitmap1.PixelWidth,
                            (uint)renderTargetBitmap1.PixelHeight,
                            logicalDpi1,
                            logicalDpi1,
                            pixelBuffer1.ToArray());

                        await encoder1.FlushAsync();

                        //Load and draw the Bitmap image in PDF
                        //PdfImage img = PdfImage.FromStream(stream.AsStream());
                        //Task<IRandomAccessStream> s = GenerateImage(TopGrid);

                        PdfImage img = PdfImage.FromStream(stream1.AsStream());
                        //PdfBitmap image = new PdfBitmap(renderTargetBitmap.);
                        document.Pages[1].Graphics.DrawImage(img, new RectangleF(0, 0, 510, 450));
                    }
                }
                else
                {
                    
                    //Initializing to render to Bitmap
                    var logicalDpi = DisplayInformation.GetForCurrentView().LogicalDpi;
                    var renderTargetBitmap = new RenderTargetBitmap();

                    //Create the Bitmpa from xaml page
                    await renderTargetBitmap.RenderAsync(CustomTopGrid, 510, 1600);
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
                        
                        document.Pages[0].Graphics.DrawImage(img, new RectangleF(0, 195, 510, 450));
                    }
                }

                //Save the Pdf document
                MemoryStream docStream = new MemoryStream();
                document.Save(docStream);
                document.Close(true);
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder runTestFolder = await applicationFolder.CreateFolderAsync("runTest",
                    CreationCollisionOption.OpenIfExists);
                StorageFolder pdfFolder = await runTestFolder.CreateFolderAsync("Advance Test",
                    CreationCollisionOption.OpenIfExists);
                StorageFile savePdfFile = await pdfFolder.CreateFileAsync(string.Format("{0}_{1}.pdf",
                    OperatorName.SelectedValue,
                    FileNameTime),
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

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Debug.WriteLine("Back Event!");
            //Close arduino device
            CancelReadTask();
            CloseDevice();

            Frame rootFrame = Window.Current.Content as Frame;
            if (rootFrame == null)
                return;
            rootFrame.Navigate(typeof(MainPage), null);
            //if (rootFrame.CanGoBack && e.Handled == false)
            //{
            //    e.Handled = true;
            //    rootFrame.GoBack();
            //}
        }

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

        //设置和状态切换状态点击事件
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

        //开始和停止按钮点击方法
        private async void Calculation_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(this.ExperienceName.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("ExperienceValidate"));
                notifyPopup.Show();
                return;
            }
            /*if (null != this.OperatorName.SelectedValue)
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("OperatorNameValidate"));
                notifyPopup.Show();
                return;
            }*/
            if (string.IsNullOrWhiteSpace(this.SamplingTimeText.Text) || IsNotNumeric(this.SamplingTimeText.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("SamplingTimeValidate"));
                notifyPopup.Show();
                return;
            }

            if (string.IsNullOrWhiteSpace(this.WaitTimeText.Text) || IsNotNumeric(this.WaitTimeText.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("WaitTimeValidate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.LowestTempText.Text) || IsNotNumeric(this.LowestTempText.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("LowestTempValidate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.LowHoldingTimeText.Text) || IsNotNumeric(this.LowHoldingTimeText.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("LowHoldingTimeValidate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.Temp1Text.Text) || IsNotNumeric(this.Temp1Text.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("Temp1Validate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.Hold1Text.Text) || IsNotNumeric(this.Hold1Text.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("Temp1HoldingTimeValidate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.RampSpeed1Text.Text) || IsNotNumeric(this.RampSpeed1Text.Text) || this.RampSpeed1Text.Text == "0")
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("RampSpeed1Validate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.Temp2Text.Text) || IsNotNumeric(this.Temp2Text.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("Temp2Validate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.Hold2Text.Text) || IsNotNumeric(this.Hold2Text.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("Temp2HoldingTimeValidate"));
                notifyPopup.Show();
                return;
            }
            if (string.IsNullOrWhiteSpace(this.RampSpeed2Text.Text) || IsNotNumeric(this.RampSpeed2Text.Text))
            {
                NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("RampSpeed2Validate"));
                notifyPopup.Show();
                return;
            }
            if (!string.IsNullOrWhiteSpace(BeginTimeText1.Text))
            {
                if (IsNotNumeric(BeginTimeText1.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("BeginTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(BeginTimeText2.Text))
            {
                if (IsNotNumeric(BeginTimeText2.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("BeginTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(BeginTimeText3.Text))
            {
                if (IsNotNumeric(BeginTimeText3.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("BeginTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(BeginTimeText4.Text))
            {
                if (IsNotNumeric(BeginTimeText4.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("BeginTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EndTimeText1.Text))
            {
                if (IsNotNumeric(EndTimeText1.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("EndTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EndTimeText4.Text))
            {
                if (IsNotNumeric(EndTimeText4.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("EndTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EndTimeText2.Text))
            {
                if (IsNotNumeric(EndTimeText2.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("EndTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(EndTimeText3.Text))
            {
                if (IsNotNumeric(EndTimeText3.Text))
                {
                    NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("EndTimeValidate"));
                    notifyPopup.Show();
                    return;
                }
            }
            if (ClickCalculationStatus == 0)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                CalcButtonImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/stop-button.png"));
                CalcTestText.Text = loader.GetString("Stop1");
                ClickCalculationStatus = 1;
                if (null != timer)
                    timer.Stop();
                if (null != c)
                    c.Update(0, 1000);
                Value = 0;
                /**********Send profile to arduino************/
                heartcuttingNumber = 0;
                try
                {
                    if (serialDevice != null)
                    {
                        // Create the DataWriter object and attach to OutputStream
                        writer = new DataWriter(serialDevice.OutputStream);
                        if (HeartcuttingInputCheck())
                        {
                            //Launch the WriteAsync task to perform the write
                            await WriteAsync(GenerateProfileString());
                            Debug.WriteLine(GenerateProfileString());
                        }
                        else
                        {
                            Debug.WriteLine("Please check the heartcutting input.");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Select a device and connect");
                    }
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
                /************************************************/
                StartCountDown();
                Status_Click(new object(), new RoutedEventArgs());
                PrimaryGrid.Visibility = Visibility.Collapsed;
                SecondaryGrid.Visibility = Visibility.Collapsed;
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
            else
            {
                CalcButtonImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/start-button.png"));
                CalcTestText.Text = loader.GetString("Start1");
                ClickCalculationStatus = 0;
                if (!ReportSavedFlag)
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
                        topStandardSource.Add(new Data(x_b[i], y_b1[i]));
                    }
                    this.Basic_Chart.Series[1].ItemsSource = topStandardSource;
                    if (heartcuttingNumber > 0)
                    {
                        this.Basic_Chart1.Series[1].ItemsSource = null;
                        for (int i = 0; i < x_b.Count && i < y_b2.Count; i++)
                        {
                            bottomStandardSource.Add(new Data(x_b[i], y_b2[i]));
                        }
                        this.Basic_Chart1.Series[1].ItemsSource = bottomStandardSource;
                    }
                    //显示表格控件
                    primaryInfoList.Clear();
                    for (int j = 0; j < peaks1.Count && j < (bottoms1.Count - 1); j++)
                    {
                        double FWHMvalue = CalculateFWHM(bottoms1[j], peaks1[j], bottoms1[j + 1], x1, y1, y_b1);
                        if (y1[peaks1[j]] >= 100)
                        {
                            MessageDialog popup = new MessageDialog(loader.GetString("SaturationNotice"));
                            await popup.ShowAsync();
                        }
                        primaryInfoList.Add(new AdvanceTestInfo
                        {
                            ID = (j + 1).ToString(),
                            RetentionTime = x1[peaks1[j]].ToString("0.00"),
                            FWHM = FWHMvalue.ToString("0.00"),
                            Height = Heights1[j].ToString("0.00"),
                            Area = Area1[j].ToString("0.00")
                        });
                    }
                    PrimaryGrid.Visibility = Visibility;

                    if(heartcuttingNumber > 0)
                    {
                        secondaryInfoList.Clear();
                        for (int j = 0; j < peaks2.Count && j < (bottoms2.Count - 1); j++)
                        {
                            double FWHMvalue = CalculateFWHM(bottoms2[j], peaks2[j], bottoms2[j + 1], x2, y2, y_b2);
                            if (y2[peaks2[j]] >= 100)
                            {
                                MessageDialog popup = new MessageDialog(loader.GetString("SaturationNotice"));
                                await popup.ShowAsync();
                            }
                            secondaryInfoList.Add(new AdvanceTestInfo
                            {
                                ID = (j + 1).ToString(),
                                RetentionTime = x2[peaks2[j]].ToString("0.00"),
                                FWHM = FWHMvalue.ToString("0.00"),
                                Height = Heights2[j].ToString("0.00"),
                                Area = Area2[j].ToString("0.00")
                            });
                        }
                        SecondaryGrid.Visibility = Visibility;
                    }
                    savePdf();
                    //hide button for a while
                    CalcButtonImage.Visibility = Visibility.Collapsed;
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
                        Debug.WriteLine("Select a device and connect");
                    }
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
                /************************************************/
                //show button after 5 secondes
                await Task.Delay(TimeSpan.FromMilliseconds(5000));
                CalcButtonImage.Visibility = Visibility.Visible;

            }

        }

        private static bool IsNotNumeric(string test)
        {
            double number = 0;
            return !double.TryParse(test, out number);
        }

        private async void StartCountDown()
        {
            //总的计算剩余时间，现暂支持秒
            //total = 100;
            initialAllArray();
            total = (int)(Sampletimeuwp + Waitingtimeuwp + Analysistimeuwp + Cleaningtimeuwp); //unit:s
            c = new CycleData();
            c.data = new DoubleCollection() { 0, 1000 };
            c.i = total;
            if (timer == null)
            {
                timer = new DispatcherTimer()
                {
                    Interval = TimeSpan.FromMilliseconds(100)
                };
            }
            Rount.StrokeDashArray = c.data;
            timer.Tick += Timer_Tick;
            FileNameTime = System.DateTime.Now.ToString("yyyyMMddHHmmss");
            Rawfile = await storageFolder.CreateFileAsync(ExperienceName.Text + "_" + OperatorName.SelectedValue + "_raw_" + FileNameTime + ".dat", CreationCollisionOption.OpenIfExists);

            await Windows.Storage.FileIO.AppendTextAsync(Rawfile, "Experience Name: " + ExperienceName.Text + "\n"
                + "Operator Name: " + OperatorName.SelectedValue + "\n"
                + "Start time: " + System.DateTime.Now.ToString()+ " " + System.DateTime.Now.ToString() + "\n"
                + "Sampling/Pumping time: " + Sampletimeuwp + "\n"
                + "Waiting time: " + Waitingtimeuwp + "\n"
                + "Lowest Temperature: " + LowestTempText.Text + "\n"
                + "Low holding time: " + LowHoldingTimeText.Text + "\n"
                + "Temperature 1: " + Temp1Text.Text + "\n"
                + "Temp1 holding time: " + Hold1Text.Text + "\n"
                + "Ramping speed 1: " + RampSpeed1Text.Text + "\n"
                + "Temperature2: " + Temp2Text.Text + "\n"
                + "Temp2 holding time: " + Hold2Text.Text + "\n"
                + "Ramping speed 2: " + RampSpeed2Text.Text + "\n"
                + "Time,PID1,PID2,Temp,Setpoint,pressure,pwm%" + "\n");

            timer.Start();
        }

        private async void Timer_Tick(object sender, object e)
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
                    if (!ReportSavedFlag)
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
                            topStandardSource.Add(new Data(x_b[i], y_b1[i]));
                        }
                        this.Basic_Chart.Series[1].ItemsSource = topStandardSource;
                        if (heartcuttingNumber > 0)
                        {
                            this.Basic_Chart1.Series[1].ItemsSource = null;
                            for (int i = 0; i < x_b.Count && i < y_b2.Count; i++)
                            {
                                bottomStandardSource.Add(new Data(x_b[i], y_b2[i]));
                            }
                            this.Basic_Chart1.Series[1].ItemsSource = bottomStandardSource;
                        }

                        //显示表格控件
                        primaryInfoList.Clear();
                        for (int j = 0; j < peaks1.Count && j < (bottoms1.Count - 1); j++)
                        {
                            double FWHMvalue = CalculateFWHM(bottoms1[j], peaks1[j], bottoms1[j + 1], x1, y1, y_b1);
                            if (y1[peaks1[j]] >= 100)
                            {
                                MessageDialog popup = new MessageDialog(loader.GetString("SaturationNotice"));
                                await popup.ShowAsync();
                            }
                            primaryInfoList.Add(new AdvanceTestInfo
                            {
                                ID = (j + 1).ToString(),
                                RetentionTime = x1[peaks1[j]].ToString("0.00"),
                                FWHM = FWHMvalue.ToString("0.00"),
                                Height = Heights1[j].ToString("0.00"),
                                Area = Area1[j].ToString("0.00")
                            });
                        }
                        PrimaryGrid.Visibility = Visibility;

                        if (heartcuttingNumber > 0)
                        {
                            secondaryInfoList.Clear();
                            for (int j = 0; j < peaks2.Count && j < (bottoms2.Count - 1); j++)
                            {
                                double FWHMvalue = CalculateFWHM(bottoms2[j], peaks2[j], bottoms2[j + 1], x2, y2,y_b2);
                                if (y2[peaks2[j]] >= 100)
                                {
                                    MessageDialog popup = new MessageDialog(loader.GetString("SaturationNotice"));
                                    await popup.ShowAsync();
                                }
                                secondaryInfoList.Add(new AdvanceTestInfo
                                {
                                    ID = (j + 1).ToString(),
                                    RetentionTime = x2[peaks2[j]].ToString("0.00"),
                                    FWHM = FWHMvalue.ToString("0.00"),
                                    Height = Heights2[j].ToString("0.00"),
                                    Area = Area2[j].ToString("0.00")
                                });
                            }
                            SecondaryGrid.Visibility = Visibility;
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
            //更新图像数据源
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
                topSource.Add(data);
                this.Basic_Chart.Series[0].ItemsSource = topSource;
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
            CurrentTempText.Text = ActualTemp.ToString("0.00");
            SetpointText.Text = SetpointTemp.ToString("0.00");
            CurrentPressureText.Text = ActualPressure.ToString("0.00");
            /*if (float.Parse(SetPressureText.Text) - ActualPressure > 0.5)
            {
                MessageDialog popup = new MessageDialog(loader.GetString("LowPressureNotice"));
                await popup.ShowAsync();
            }*/
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
            get { return (double)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
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
            topSource = new List<Data>();
            topStandardSource = new List<Data>();
            XaxisMax = 100;
            YaxisMax = 20;
        }

        private void initBottomChart()
        {
            bottomSource = new List<Data>();
            bottomStandardSource = new List<Data>();
            XaxisMax1 = 300;
            YaxisMax1 = 100;
        }

        private void BeginTimeText1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BeginTimeText1.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText1.Text))
            {
                double BeginTime1 = double.Parse(BeginTimeText1.Text);
                double EndTime1 = double.Parse(EndTimeText1.Text);
                if (BeginTime1 < EndTime1 && EndTime1 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }

        }

        private void BeginTimeText2_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BeginTimeText2.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText2.Text))
            {
                double BeginTime2 = double.Parse(BeginTimeText2.Text);
                double EndTime2 = double.Parse(EndTimeText2.Text);
                if (BeginTime2 < EndTime2 && EndTime2 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }
        }

        private void BeginTimeText3_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(BeginTimeText3.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText3.Text))
            {
                double BeginTime3 = double.Parse(BeginTimeText3.Text);
                double EndTime3 = double.Parse(EndTimeText3.Text);
                if (BeginTime3 < EndTime3 && EndTime3 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }
        }

        private void BeginTimeText4_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(BeginTimeText4.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText4.Text))
            {
                double BeginTime4 = double.Parse(BeginTimeText4.Text);
                double EndTime4 = double.Parse(EndTimeText4.Text);
                if (BeginTime4 < EndTime4 && EndTime4 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }
        }

        private void EndTimeText1_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BeginTimeText1.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText1.Text))
            {
                double BeginTime1 = double.Parse(BeginTimeText1.Text);
                double EndTime1 = double.Parse(EndTimeText1.Text);
                if (BeginTime1 < EndTime1 && EndTime1 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }

        }

        private void EndTimeText2_TextChanged(object sender, TextChangedEventArgs e)
        {

            if (!string.IsNullOrWhiteSpace(BeginTimeText2.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText2.Text))
            {
                double BeginTime2 = double.Parse(BeginTimeText2.Text);
                double EndTime2 = double.Parse(EndTimeText2.Text);
                if (BeginTime2 < EndTime2 && EndTime2 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }

        }

        private void EndTimeText3_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BeginTimeText3.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText3.Text))
            {
                double BeginTime3 = double.Parse(BeginTimeText3.Text);
                double EndTime3 = double.Parse(EndTimeText3.Text);
                if (BeginTime3 < EndTime3 && EndTime3 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }
        }

        private void EndTimeText4_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(BeginTimeText4.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText4.Text))
            {
                double BeginTime4 = double.Parse(BeginTimeText4.Text);
                double EndTime4 = double.Parse(EndTimeText4.Text);
                if (BeginTime4 < EndTime4 && EndTime4 > 0)
                {
                    BottomChartGrid.Visibility = Visibility.Visible;
                    TopChartGrid.Height = 450;
                    ViewBottom = true;
                }
                else
                {
                    BottomChartGrid.Visibility = Visibility.Collapsed;
                    TopChartGrid.Height = 650;
                }
            }
        }
        //Connect to arduino
        public async void devices_list()
        {
            //string selector = SerialDevice.GetDeviceSelector("COM14");
            //ushort vid = 0x2341;
            //ushort pid = 0x0010;
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
                    //Debug.WriteLine(ActualTemp);
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

        private int heartcuttingNumber = 0;
        private double[] heartcuttingStartList = new double[6];   //[0] is empty
        private double[] heartcuttingEndList = new double[6];     //[0] is empty

        private string GenerateProfileString()
        {
            string ProfileString = "!";
            Sampletimeuwp = float.Parse(SamplingTimeText.Text) * 60;
            Waitingtimeuwp = float.Parse(WaitTimeText.Text);
            Cleaningtimeuwp = float.Parse(CleaningNumberText.Text) * 60;
            double lowestTvalue = float.Parse(LowHoldingTimeText.Text) * 60;
            double HoldT1value = float.Parse(Hold1Text.Text) * 60;
            double HoldT2value = float.Parse(Hold2Text.Text) * 60;
            double Temp1value = float.Parse(Temp1Text.Text);
            double lowestTempvalue = float.Parse(LowestTempText.Text);
            double RampSpeed1value = float.Parse(RampSpeed1Text.Text) / 60.0;
            double Temp2value = float.Parse(Temp2Text.Text);
            double RampSpeed2value = float.Parse(RampSpeed2Text.Text) / 60.0;
            Analysistimeuwp = lowestTvalue + HoldT1value + HoldT2value + (Temp1value - lowestTempvalue) / RampSpeed1value + (Temp2value - Temp1value) / RampSpeed2value; //s
            ProfileString += (float.Parse(SamplingTimeText.Text) * 60).ToString() + ",";
            ProfileString += float.Parse(SetPressureText.Text).ToString() + ",";
            ProfileString += float.Parse(WaitTimeText.Text).ToString() + ",";
            ProfileString += float.Parse(LowestTempText.Text).ToString() + ",";
            ProfileString += (float.Parse(LowHoldingTimeText.Text) * 60).ToString() + ",";
            ProfileString += float.Parse(Temp1Text.Text).ToString() + ",";
            ProfileString += (float.Parse(Hold1Text.Text) * 60).ToString() + ",";
            ProfileString += (float.Parse(RampSpeed1Text.Text) / 60).ToString() + ",";
            ProfileString += float.Parse(Temp2Text.Text).ToString() + ",";
            ProfileString += (float.Parse(Hold2Text.Text) * 60).ToString() + ",";
            ProfileString += (float.Parse(RampSpeed2Text.Text) / 60).ToString() + ",";
            ProfileString += heartcuttingNumber.ToString() + ",";
            ProfileString += heartcuttingStartList[1].ToString() + ",";
            ProfileString += heartcuttingStartList[2].ToString() + ",";
            ProfileString += heartcuttingStartList[3].ToString() + ",";
            ProfileString += heartcuttingStartList[4].ToString() + ",";
            ProfileString += heartcuttingEndList[1].ToString() + ",";
            ProfileString += heartcuttingEndList[2].ToString() + ",";
            ProfileString += heartcuttingEndList[3].ToString() + ",";
            ProfileString += heartcuttingEndList[4].ToString() + ",";
            ProfileString += RoomTempvalue.ToString() + ",";
            ProfileString += CleaningNumberText.Text + "," + '\n';
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
            timer.Stop();
            Debug.WriteLine("finish");
            CalcButtonImage.Source = new BitmapImage(new Uri("ms-appx:///Assets/start-button.png"));
            CalcTestText.Text = loader.GetString("Start1");
            ClickCalculationStatus = 0;
            if (heartcuttingNumber > 0)
            {
                PrimaryGrid.Visibility = Visibility.Visible;
                SecondaryGrid.Visibility = Visibility.Visible;
                BottomChartGrid.Visibility = Visibility.Visible;
                TopChartGrid.Height = 450;
            }
            else
            {
                PrimaryGrid.Visibility = Visibility.Visible;
                SecondaryGrid.Visibility = Visibility.Collapsed;
                BottomChartGrid.Visibility = Visibility.Collapsed;
                TopChartGrid.Height = 650;
            }
        }
        private Boolean HeartcuttingInputCheck()
        {
            Boolean CheckResult = true;
            if (!string.IsNullOrWhiteSpace(BeginTimeText1.Text) &&
                !string.IsNullOrWhiteSpace(EndTimeText1.Text))
            {
                double BeginTime1 = double.Parse(BeginTimeText1.Text);
                double EndTime1 = double.Parse(EndTimeText1.Text);
                if (BeginTime1 < EndTime1)
                {
                    heartcuttingStartList[1] = BeginTime1;
                    heartcuttingEndList[1] = EndTime1;
                    heartcuttingNumber++;
                }
                else if (BeginTime1 == 0 && EndTime1 == 0) ;
                else
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(BeginTimeText2.Text) &&
               !string.IsNullOrWhiteSpace(EndTimeText2.Text))
            {
                double BeginTime2 = double.Parse(BeginTimeText2.Text);
                double EndTime2 = double.Parse(EndTimeText2.Text);
                if (BeginTime2 < EndTime2)
                {
                    heartcuttingStartList[2] = BeginTime2;
                    heartcuttingEndList[2] = EndTime2;
                    heartcuttingNumber++;
                }
                else if (BeginTime2 == 0 && EndTime2 == 0) ;
                else
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(BeginTimeText3.Text) &&
               !string.IsNullOrWhiteSpace(EndTimeText3.Text))
            {
                double BeginTime3 = double.Parse(BeginTimeText3.Text);
                double EndTime3 = double.Parse(EndTimeText3.Text);
                if (BeginTime3 < EndTime3)
                {
                    heartcuttingStartList[3] = BeginTime3;
                    heartcuttingEndList[3] = EndTime3;
                    heartcuttingNumber++;
                }
                else if (BeginTime3 == 0 && EndTime3 == 0) ;
                else
                {
                    return false;
                }
            }
            if (!string.IsNullOrWhiteSpace(BeginTimeText4.Text) &&
               !string.IsNullOrWhiteSpace(EndTimeText4.Text))
            {
                double BeginTime4 = double.Parse(BeginTimeText4.Text);
                double EndTime4 = double.Parse(EndTimeText4.Text);
                if (BeginTime4 < EndTime4)
                {
                    heartcuttingStartList[4] = BeginTime4;
                    heartcuttingEndList[4] = EndTime4;
                    heartcuttingNumber++;
                }
                else if (BeginTime4 == 0 && EndTime4 == 0) ;
                else
                {
                    return false;
                }
            }
            return CheckResult;
        }

        //******************************Data analysis process*******************************//
        private int constant_m = 35; // for SNIP baseline formula
        private int constant_m_end = Convert.ToInt32(1 / Math.Sqrt(2)); // for SNIP baseline formula
        private int CONSECUTIVE_SCAN_STEPS = 3;   //for peak detection
        private double THRESHOLD = 0.005f;        //for peak detection: slope
        private double THRESHOLD_peak = 3.0;       //for peak detection: slope

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
        private DateTime time0;
        private DateTime time1;

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