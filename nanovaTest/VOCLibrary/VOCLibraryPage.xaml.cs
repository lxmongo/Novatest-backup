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

namespace nanovaTest.VOCLibrary
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class VOCLibraryPage : Page
    {
        private ObservableCollection<TestInfo> VOCLibraryList;

        private Dictionary<string, string> _jsonFiles; //name filename

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
                            info.VOC = string.Format("{0}({1})", displayName, vocName.GetString());
                        }
                        else
                        {
                            info.VOC = string.Format("{0}({1})", vocName.GetString(), vocName.GetString());
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
                        info.CalibrateDate = json.GetNamedString("CalibrationDateTime").Substring(0,11);
                        //info.ConcentrationFactor = json.GetNamedNumber("CalibrationFactor").ToString();
                        info.ConcentrationFactor = "N/A";
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
        private void Button_TVOC_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("TVOC");
        }

        //BTEX
        private void Button_BTEX_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("BTEX");
        }

        //MTBE
        private void Button_MTBE_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("MTBE");
        }

        //TCE/PCE
        private void Button_TCE_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("TCEPCE");
        }

        //Malodorous Gas
        private void Button_MG_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("Malodorous");
        }

        //Vehicle Indoor
        private void Button_VI_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("MG");
        }

        //Environmental Air
        private void Button_EA_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("EA");
        }

        //Pollution Source
        private void Button_PS_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("PS");
        }

        //Water Sample-Online
        private void Button_Water_Click(object sender, RoutedEventArgs e)
        {
            //设置选中颜色
            SetButtonColor(sender as Button);
            //加载数据
            InitListByName("WA");
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