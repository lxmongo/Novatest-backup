using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Data.Json;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace nanovaTest.Utils
{
    public class CustomUtils
    {
        /// <summary>
        /// 根据传入16进制的色值转换为Color对象
        /// </summary>
        /// <param name="hex">16进制的色值</param>
        /// <returns>Color对象</returns>
        public static Color GetColorFromHex(string hex)
        {
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;

            int start = 0;

            //handle ARGB strings (8 characters long) 
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }

            //convert RGB characters to bytes 
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// 自定义设置TitleBar
        /// </summary>
        /// <param name="GridTitleBar">TitleBar元素</param>
        public static void SetCustomTitleBar(UIElement GridTitleBar)
        {
            var coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(GridTitleBar);

            var view = ApplicationView.GetForCurrentView();
            view.TitleBar.ButtonBackgroundColor = CustomUtils.GetColorFromHex("#007DC4");
            view.TitleBar.ButtonForegroundColor = Colors.White;

            view.TitleBar.ButtonHoverBackgroundColor = CustomUtils.GetColorFromHex("#007DC4");
            view.TitleBar.ButtonHoverForegroundColor = Colors.White;

            view.TitleBar.ButtonPressedBackgroundColor = CustomUtils.GetColorFromHex("#4682B4");
            view.TitleBar.ButtonPressedForegroundColor = Colors.White;

            view.TitleBar.ButtonInactiveBackgroundColor = CustomUtils.GetColorFromHex("#007DC4");
            view.TitleBar.ButtonInactiveForegroundColor = Colors.WhiteSmoke;

            Window.Current.Activated += (sender, args) =>
            {
                if (args.WindowActivationState != CoreWindowActivationState.Deactivated)
                {
                    GridTitleBar.Opacity = 1;
                    //TxtSearchBox.Opacity = 1;
                }
                else
                {
                    GridTitleBar.Opacity = 0.5;
                    //TxtSearchBox.Opacity = 0.5;
                }
            };
        }

        /// <summary>
        /// 检查当天是否校准过
        /// </summary>
        /// <returns>true=已校准 false=未校准</returns>
        public static bool CheckCalibrate()
        {
            return false;
        }
        /// <summary>
        /// 检查是否存在文件名
        /// </summary>
        /// <param name="fileName">需要检查的文件名称</param>
        /// <returns>是否存在文件</returns>
        public static async Task<bool> isFilePresent(string fileName)
        {
            var item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(fileName);
            return item != null;
        }

        public static async Task<ObservableCollection<string>> GetOperator()
        {
            StorageFolder storageFolder = ApplicationData.Current.LocalFolder;
            StorageFile file = await storageFolder.CreateFileAsync("UserInfo.json", CreationCollisionOption.OpenIfExists);
            ObservableCollection<string> list = null;
            if (null != file)
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        if (null != content && !"".Equals(content))
                        {
                            JsonArray array = JsonArray.Parse(content);
                            list = new ObservableCollection<string>();
                            foreach (var jsonValue in array)
                            {
                                var value = JsonObject.Parse(jsonValue.Stringify());
                                string LastName = value.GetNamedString("LastName");
                                string FamilyName = value.GetNamedString("FamilyName");
                                list.Add(string.Format("{0}{1}", FamilyName, LastName));
                            }
                        }
                    }
                }
            }
             
            return list;
        }
    }
}
