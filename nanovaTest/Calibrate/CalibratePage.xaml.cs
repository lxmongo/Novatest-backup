using nanovaTest.Utils;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace nanovaTest.Calibrate
{
    
    public sealed partial class CalibratePage : Page
    {
        public CalibratePage()
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
                MethodGrid.Visibility = Visibility.Collapsed;
                await Task.Delay(TimeSpan.FromMilliseconds(500));
            }
            finally
            {
                LoadingIndicator.IsActive = false;
                LoadingGrid.Visibility = Visibility.Collapsed;
                MethodGrid.Visibility = Visibility.Visible;
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
            Frame.Navigate(typeof(RunCalibratePage), "Cleaning");
        }
        //TVOC
        private void Button_TVOC_Click(object sender, RoutedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;
            rootFrame.Navigate(typeof(RunCalibratePage), "TVOC");
        }
        //BTEX
        private void Button_BTEX_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "BTEX");
        }
        //MTBE
        private void Button_MTBE_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "MTBE");
        }
        //TCE/PCE
        private void Button_TCE_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "TCE/PCE");
        }
        //Malodorous Gas
        private void Button_MG_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "Malodorous");
        }
        //Vehicle Indoor
        private void Button_VI_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "VehicleIndoor");
        }
        //Environmental Air
        private void Button_EA_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "EnvironmentalAir");
        }
        //Pollution Source
        private void Button_PS_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "PollutionSource");
        }
        //Water Sample-Online
        private void Button_Water_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(RunCalibratePage), "WaterSample-Online");
        }

        private void delete_Button_Cleaning_Click(object sender, RoutedEventArgs e)
        {

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
