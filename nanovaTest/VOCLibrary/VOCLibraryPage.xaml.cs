using System;
using System.Collections.Generic;
using nanovaTest.Utils;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Windows.ApplicationModel;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using WinRTXamlToolkit.Controls.Extensions;
using Windows.ApplicationModel.Resources;
using System.Threading.Tasks;

namespace nanovaTest.VOCLibrary
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class VOCLibraryPage : Page
    {
        private ObservableCollection<TestInfo> VOCLibraryList;

        private Dictionary<string, string> _jsonFiles; //name filename
        private Dictionary<string, string> methodtrans; //name filename
        private string[,] newinfo;
        private List<double> RetentionTimeList;
        private double DelayTime = 100;

        private string vocsFile = "VOCS.json";

        private Dictionary<string, JsonObject> vocDictionary;

        public VOCLibraryPage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            VOCLibraryList = new ObservableCollection<TestInfo>();
            //配置JSON文件
            _jsonFiles = new Dictionary<string, string>();
            _jsonFiles.Add("BTEX", "BTEX.json");
            _jsonFiles.Add("Malodorous", "Malodorous.json");
            _jsonFiles.Add("MTBE", "MTBE.json");
            _jsonFiles.Add("TCEPCE", "TCEPCE.json");
            _jsonFiles.Add("VOCS", "VOCS.json");
            _jsonFiles.Add("MG", "Vehicle.json");
            _jsonFiles.Add("EA", "AirQuality.json");
            _jsonFiles.Add("PS", "PollutionSource.json");
            _jsonFiles.Add("WA", "WaterQuality.json");
            _jsonFiles.Add("TVOC", "TVOC.json");

            //dictionary for method name
            methodtrans = new Dictionary<string, string>();
            methodtrans.Add("BTEX", "BTEX");
            methodtrans.Add("Malodorous", "Malodorous Gas");
            methodtrans.Add("MTBE", "MTBE");
            methodtrans.Add("TCEPCE", "TCE&PCE");
            methodtrans.Add("VOCS", "VOCS");
            methodtrans.Add("MG", "Vehicle");
            methodtrans.Add("EA", "Air Quality");
            methodtrans.Add("PS", "Pollution Source");
            methodtrans.Add("WA", "Water Quality");
            methodtrans.Add("TVOC", "TVOC");

            InitVocDictionary();
            Button_TVOC_Click(TVOCButton, new RoutedEventArgs());
        }

        //初始化VOC静态数据
        private async void InitVocDictionary()
        {
            vocDictionary = new Dictionary<string, JsonObject>();
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync(vocsFile);
            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    JsonArray array = JsonArray.Parse(content);
                    foreach (var jsonValue in array)
                    {
                        var value = JsonObject.Parse(jsonValue.Stringify());
                        vocDictionary.Add(value.GetNamedString("VOCName").ToUpper(), value);
                    }
                }
            }
        }

        //update VOC CF
        private async void UpdateCF(string Name)
        {
            try
            {
                string methodFileName = methodtrans[Name];
                //Create a folder: fileFloder dir calibrate -->methodFileName -->dateTimeFileName
                StorageFolder applicationFolder = ApplicationData.Current.LocalFolder;
                StorageFolder retentionFolder = await applicationFolder.CreateFolderAsync("calibrate_test",
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
                String[] result = text.Split(new[] { '|' });
                if (result.Length == VOCLibraryList.Count)
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
                    newinfo[result.Length,0] = "datetime";
                    newinfo[result.Length,1] = maxvalue.ToString();
                }
                if (newinfo.Length > 0)
                {
                    for (var index = 0; index < VOCLibraryList.Count; index++)
                    {
                        VOCLibraryList[index].CalibrateDate = newinfo[VOCLibraryList.Count, 1];
                        //info.ConcentrationFactor = json.GetNamedNumber("CalibrationFactor").ToString();
                        VOCLibraryList[index].ConcentrationFactor = newinfo[index, 1];
                        //if (!json.GetNamedArray("VOCRetentionTime")[0].ToString().Equals("null"))
                        //{
                        //    VOCLibraryList[index].Time = json.GetNamedArray("VOCRetentionTime")[index].GetNumber().ToString();
                        //}
                        //else
                        //{
                        //    VOCLibraryList[index].Time = "--";
                        //}
                    }
                }
            }
            catch (FileNotFoundException)
            {
                Debug.WriteLine("Update file not found");
            }
            //update CF
        }


        //update retention
        private async void UpdateRentention(string name)
        {
            try
            {
                RetentionTimeList = new List<double>();
                string methodFileName = methodtrans[name];
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
                    RetentionTimeList.Add(double.Parse(result[i]));
                    Debug.WriteLine(result[i]);
                }
                for (var index = 0; index < RetentionTimeList.Count; index++)
                {
                    VOCLibraryList[index].Time = RetentionTimeList[index].ToString("0.00");
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



        //通过VOC NAME 初始化列表
        private async void InitListByName(string name)
        {
            if (!_jsonFiles.ContainsKey(name))
            {
                new NotifyPopup(name + " json file not configured!").Show();
                return;
            }
            VOCLibraryList.Clear();
            ResourceLoader loader = new ResourceLoader();
            var fileName = _jsonFiles[name];
            var folder = await Package.Current.InstalledLocation.GetFolderAsync("Assets");
            var file = await folder.GetFileAsync(fileName);
            using (var stream = await file.OpenStreamForReadAsync())
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    JsonObject json = JsonObject.Parse(content);
                    var id = 0;
                    for (var index = 0; index < json.GetNamedArray("VOCList").Count; index++)
                    {
                        var vocName = json.GetNamedArray("VOCList")[index];
                        var voc = vocDictionary[vocName.GetString().ToUpper()];
                        //静态数据
                        TestInfo info = new TestInfo();
                        var displayName = loader.GetString(vocName.GetString());
                        if(null != displayName && !"".Equals(displayName))
                        {
                            info.VOC = vocName.GetString();
                        }
                        else
                        {
                            info.VOC =vocName.GetString();
                        }
                        
                        if (voc != null)
                        {
                            info.CAS = voc.GetNamedString("CASNum");
                            IJsonValue idValue = voc.GetNamedValue("MolecularWeight");
                            if (voc.ContainsKey("MolecularWeight") && idValue.ValueType != JsonValueType.Null)
                            {
                                info.Mw = voc.GetNamedNumber("MolecularWeight").ToString();
                            }
                            else
                            {
                                info.Mw = "--";
                            }
                        }
                        //动态数据
                        //info.CalibrateDate = json.GetNamedString("CalibrationDateTime").Substring(0,11);
                        //info.ConcentrationFactor = json.GetNamedNumber("CalibrationFactor").ToString();
                        //info.ConcentrationFactor = "N/A";
                        info.ID = (++id).ToString();
                        if(!json.GetNamedArray("VOCRetentionTime")[0].ToString().Equals("null"))
                        {
                            info.Time = json.GetNamedArray("VOCRetentionTime")[index].GetNumber().ToString();
                        }
                        else
                        {
                            info.Time = "--";
                        }
                        VOCLibraryList.Add(info);
                    }
                }
            }
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
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

        //TVOC
        private async void Button_TVOC_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("TVOC");
            InitListByName("TVOC");
            UpdateRentention("TVOC");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //BTEX
        private async void Button_BTEX_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("BTEX");
            InitListByName("BTEX");
            UpdateRentention("BTEX");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //MTBE
        private async void Button_MTBE_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("MTBE");
            InitListByName("MTBE");
            UpdateRentention("MTBE");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //TCE/PCE
        private async void Button_TCE_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("TCEPCE");
            InitListByName("TCEPCE");
            UpdateRentention("TCEPCE");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //Malodorous Gas
        private async void Button_MG_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("Malodorous");
            InitListByName("Malodorous");
            UpdateRentention("Malodorous");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //Vehicle Indoor
        private async void Button_VI_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("MG");
            InitListByName("MG");
            UpdateRentention("MG");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //Environmental Air
        private async void Button_EA_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("EA");
            InitListByName("EA");
            UpdateRentention("EA");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //Pollution Source
        private async void Button_PS_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("PS");
            InitListByName("PS");
            UpdateRentention("PS");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        //Water Sample-Online
        private async void Button_Water_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            VOCViewList.Visibility = Visibility.Collapsed;
            UpdateCF("WA");
            InitListByName("WA");
            UpdateRentention("WA");
            await Task.Delay(TimeSpan.FromMilliseconds(DelayTime));
            VOCViewList.Visibility = Visibility.Visible;
        }

        private void SetButtonColor(Button button)
        {
            if (button == null)
            {
                return;
            }
            foreach (DependencyObject o in button.GetSiblings())
            {
                if (o.GetType() == typeof(Button))
                {
                    Button sb = o as Button;
                    sb.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 0, 0));
                }
            }

            button.Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 124, 196));
        }
    }
}