using nanovaTest.Setting;
using System.Diagnostics;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace nanovaTest
{
    public sealed partial class SamplesPane : UserControl
    {
        public SamplesPane()
        {
            this.InitializeComponent();
        }

        private void NavigateToAbout(object sender, RoutedEventArgs e)
        {
            var frame = new Frame();
            frame.Navigate(typeof(AboutPage), null);
            Window.Current.Content = frame;
        }

        private void NavigateToSetting(object sender, RoutedEventArgs e)
        {
            //NotifyPopup notifyPopup = new NotifyPopup("come soon.");
            //notifyPopup.Show();
            var frame = new Frame();
            frame.Navigate(typeof(UserPage), null);
            Window.Current.Content = frame;
        }


    }
}
