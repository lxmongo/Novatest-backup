using nanovaTest.Calibrate;
using nanovaTest.Utils;
using System;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace nanovaTest.SelectMethod
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class SelectMethodPage : Page
    {
        public SelectMethodPage()
        {
            this.InitializeComponent();
            CustomUtils.SetCustomTitleBar(GridTitleBar);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
            initPage();
        }

        private async void initPage()
        {
            try
            {
                LoadingIndicator.IsActive = true;
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            finally
            {
                MethodGrid.Visibility = Visibility.Visible;
                LoadingGrid.Visibility = Visibility.Collapsed;
                LoadingIndicator.IsActive = false;
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            
        }

        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(MainPage), null);
        }
        //Cleaning
        private void Button_Cleaning_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "Cleaning");
        }
        //TVOC
        private void Button_TVOC_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "TVOC");
        }
        //BTEX
        private void Button_BTEX_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "BTEX");
        }
        //TCE/PCE
        private void Button_TCE_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "TCE/PCE");
        }
        //Malodorous Gas
        private void Button_MG_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "Malodorous");
        }
        //Vehicle Indoor
        private void Button_VI_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "VehicleIndoor");
        }
        //Environmental Air
        private void Button_EA_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "EnvironmentalAir");
        }
        //Pollution Source
        private void Button_PS_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "PollutionSource");
        }
        //Water Sample-Online
        private void Button_Water_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunTestPage), "WaterSample-Online");
        }

        private void delete_Button_TVOC_Click(object sender, RoutedEventArgs e)
        {

        }

        private void delete_Button_TCE_Click(object sender, RoutedEventArgs e)
        {

        }

        private void delete_Button_MG_Click(object sender, RoutedEventArgs e)
        {

        }

        private void delete_Button_VI_Click(object sender, RoutedEventArgs e)
        {

        }

        private void delete_Button_PS_Click(object sender, RoutedEventArgs e)
        {

        }

        private void delete_Button_Water_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
