using nanovaTest.Utils;
using nanovaTest.CustomMethod;
using System.Collections.Generic;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using nanovaTest.SelectMethod;
using nanovaTest.MethodHistory;
using nanovaTest.Calibrate;
using nanovaTest.VOCLibrary;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using Windows.Foundation;

namespace nanovaTest
{
    public sealed partial class MainPage : Page
    {
        private List<MainMenu> menus;
        public MainPage()
        {
            this.InitializeComponent();
            menus = MainMenuManger.GetMainMenus();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            MoreMenuButton.Click += ShowSliptView;
            //ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(900, 1150));

            ApplicationView.PreferredLaunchViewSize = new Size(980, 680);
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
        }

        private void ShowSliptView(object sender, RoutedEventArgs e)
        {
            MySamplesPane.SamplesSplitView.IsPaneOpen = !MySamplesPane.SamplesSplitView.IsPaneOpen;
        }


        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var mainMenu = (MainMenu) e.ClickedItem;
            Frame rootFrame = Window.Current.Content as Frame;
            switch(mainMenu.ID)
            {
                case 1:
                    //Select Method
                    rootFrame.Navigate(typeof(SelectMethodPage), "RunTest");
                    break;
                case 2:
                    //Custom Method
                    rootFrame.Navigate(typeof(CustomMethodPage), true);
                    break;
                case 3:
                    //Method History
                    rootFrame.Navigate(typeof(MethodHistoryPage), true);
                    break;
                case 4:
                    //Calibrate
                    rootFrame.Navigate(typeof(CalibratePage), true);
                    break;
                case 5:
                    //VOC Library
                    rootFrame.Navigate(typeof(VOCLibraryPage), true);
                    break;
                default:
                    break;
            }
        }
    }
}
