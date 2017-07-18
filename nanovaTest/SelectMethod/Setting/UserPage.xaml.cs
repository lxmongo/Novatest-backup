using System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Media;
using System.ComponentModel;
using System.Collections.ObjectModel;
using WinRTXamlToolkit.Controls.DataVisualization.Charting;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.Globalization;
using nanovaTest.Utils;
using System.Collections.Generic;
using Windows.Data.Json;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;
using Windows.UI.Text;
using Windows.UI;
using Newtonsoft.Json;

namespace nanovaTest.Setting
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class UserPage : Page
    {
        private string fileName = "UserInfo.json";
        private ResourceLoader loader;

        public UserPage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            loader = new ResourceLoader();
            InitializeGrid();
            this.LoadData();
            this.InitializeData();
        }

        private void InitializeGrid()
        {
            this.cellGrid.RowCount = 55;
            this.cellGrid.ColumnCount = 7;

            // Default Column Width and Default Row Height reduced for Mobile View
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                this.cellGrid.DefaultColumnWidth = 100;
                this.cellGrid.DefaultRowHeight = 30;
            }
            else
            {
                this.cellGrid.DefaultColumnWidth = 180;
                this.cellGrid.DefaultRowHeight = 40;
            }
        }

        private void LoadData()
        {
            var mod = cellGrid.Model;
            mod[0, 0].CellValue = "ID";
            mod[0, 0].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 0].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 0].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 0].Font.FontSize = 18;
            mod[0, 0].Font.FontWeight = FontWeights.Bold;

            mod[0, 1].CellValue = "Last Name";
            mod[0, 1].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 1].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 1].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 1].Font.FontSize = 18;
            mod[0, 1].Font.FontWeight = FontWeights.Bold;

            mod[0, 2].CellValue = "First Name";
            mod[0, 2].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 2].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 2].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 2].Font.FontSize = 18;
            mod[0, 2].Font.FontWeight = FontWeights.Bold;

            mod[0, 3].CellValue = "Employee number";
            mod[0, 3].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 3].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 3].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 3].Font.FontSize = 18;
            mod[0, 3].Font.FontWeight = FontWeights.Bold;

            mod[0, 4].CellValue = "title";
            mod[0, 4].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 4].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 4].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 4].Font.FontSize = 18;
            mod[0, 4].Font.FontWeight = FontWeights.Bold;

            mod[0, 5].CellValue = "Dep.";
            mod[0, 5].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 5].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 5].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 5].Font.FontSize = 18;
            mod[0, 5].Font.FontWeight = FontWeights.Bold;

            mod[0, 6].CellValue = "Remark";
            mod[0, 6].Background = new SolidColorBrush(Colors.Gray);
            mod[0, 6].HorizontalAlignment = HorizontalAlignment.Center;
            mod[0, 6].VerticalAlignment = VerticalAlignment.Center;
            mod[0, 6].Font.FontSize = 18;
            mod[0, 6].Font.FontWeight = FontWeights.Bold;

            for (int i = 1;i <= 55;i ++)
            {
                mod[i, 0].CellValue = i;
                mod[i, 0].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 0].VerticalAlignment = VerticalAlignment.Center;

                mod[i, 1].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 1].VerticalAlignment = VerticalAlignment.Center;

                mod[i, 2].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 2].VerticalAlignment = VerticalAlignment.Center;

                mod[i, 3].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 3].VerticalAlignment = VerticalAlignment.Center;

                mod[i, 4].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 4].VerticalAlignment = VerticalAlignment.Center;

                mod[i, 5].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 5].VerticalAlignment = VerticalAlignment.Center;

                mod[i, 6].HorizontalAlignment = HorizontalAlignment.Center;
                mod[i, 6].VerticalAlignment = VerticalAlignment.Center;
            }
        }

        private async void InitializeData()
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            if (null != file)
            {
                using (var stream = await file.OpenStreamForReadAsync())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var content = reader.ReadToEnd();
                        if(null != content && !"".Equals(content))
                        {
                            var mod = cellGrid.Model;
                            JsonArray array = JsonArray.Parse(content);
                            foreach (var jsonValue in array)
                            {
                                var value = JsonObject.Parse(jsonValue.Stringify());
                                string ID = value.GetNamedString("ID");
                                string LastName = value.GetNamedString("LastName");
                                string FamilyName = value.GetNamedString("FamilyName");
                                string EmployeeNumber = value.GetNamedString("EmployeeNumber");
                                string Title = value.GetNamedString("Title");
                                string Dep = value.GetNamedString("Dep");
                                string Remark = value.GetNamedString("Remark");

                                mod[int.Parse(ID), 1].CellValue = LastName;
                                mod[int.Parse(ID), 2].CellValue = FamilyName;
                                mod[int.Parse(ID), 3].CellValue = EmployeeNumber;
                                mod[int.Parse(ID), 4].CellValue = Title;
                                mod[int.Parse(ID), 5].CellValue = Dep;
                                mod[int.Parse(ID), 6].CellValue = Remark;
                            }
                        }
                    }
                }
            }
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), null);
        }

        private void savaUser_Click(object sender, RoutedEventArgs e)
        {
            var mod = cellGrid.Model;
            ObservableCollection<UserInfo> list = new ObservableCollection<UserInfo>();
            for(int i = 1; i < 55; i++)
            {
                UserInfo userInfo = new UserInfo();
                userInfo.ID = i.ToString();
                bool flag = false;
                var lastName = mod[i, 1].CellValue;
                var familyName = mod[i, 2].CellValue;
                var employeeNumber = mod[i, 3].CellValue;
                var title = mod[i, 4].CellValue;
                var dep = mod[i, 5].CellValue;
                var remark = mod[i, 6].CellValue;
                //lastName
                if (null != lastName && !"".Equals(lastName))
                {
                    userInfo.LastName = lastName.ToString();
                    flag = true;
                }
                else
                {
                    userInfo.LastName = "";
                }
                //familyName
                if(null != familyName && !"".Equals(familyName))
                {
                    userInfo.FamilyName = familyName.ToString();
                    flag = true;
                }
                else
                {
                    userInfo.FamilyName = "";
                }
                //employeeNumber
                if(null != employeeNumber && !"".Equals(employeeNumber))
                {
                    userInfo.EmployeeNumber = employeeNumber.ToString();
                    flag = true;
                }
                else
                {
                    userInfo.EmployeeNumber = "";
                }
                //title
                if(null != title && !"".Equals(title))
                {
                    userInfo.Title = title.ToString();
                    flag = true;
                }
                else
                {
                    userInfo.Title = "";
                }
                //dep
                if(null != dep && !"".Equals(dep))
                {
                    userInfo.Dep = dep.ToString();
                    flag = true;
                }
                else
                {
                    userInfo.Dep = "";
                }
                //remark
                if(null != remark && !"".Equals(remark))
                {
                    userInfo.Remark = remark.ToString();
                    flag = true;
                }
                else
                {
                    userInfo.Remark = "";
                }
                if(flag)
                {
                    list.Add(userInfo);
                }
            }
            if (list.Count > 0)
            {
                string json = JsonConvert.SerializeObject(list);
                WiteJsonFile(json);
            }
        }

        private async void WiteJsonFile(string json)
        {
            Windows.Storage.StorageFolder storageFolder = Windows.Storage.ApplicationData.Current.LocalFolder;
            Windows.Storage.StorageFile file = await storageFolder.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteTextAsync(file, json);
            NotifyPopup notifyPopup = new NotifyPopup(loader.GetString("UserSaveSuccess"));
            notifyPopup.Show();
        }
    }
}
