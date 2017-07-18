using nanovaTest.Calibrate;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace nanovaTest
{
    public sealed partial class AlertPopup : UserControl
    {
        private Popup m_Popup;
        private TimeSpan m_ShowTime;
        private string m_methodName;
        public event EventHandler UserControlButtonClicked;
        public AlertPopup()
        {
            this.InitializeComponent();
            m_Popup = new Popup();
            this.Width = Window.Current.Bounds.Width;
            this.Height = Window.Current.Bounds.Height;
            m_Popup.Child = this;
            this.Loaded += NotifyPopup_Loaded; ;
            this.Unloaded += NotifyPopup_Unloaded; 
        }

        public AlertPopup(string methodName, TimeSpan showTime) : this()
        {
            m_ShowTime = showTime;
            m_methodName = methodName;
        }
       

        public void Show()
        {
            this.sbOut.BeginTime = this.m_ShowTime;
            this.sbOut.Begin();
            this.sbOut.Completed += SbOut_Completed;
            this.m_Popup.IsOpen = true;
        }

        public void Hide()
        {
            this.m_Popup.IsOpen = false;
        }

        private void SbOut_Completed(object sender, object e)
        {
            this.m_Popup.IsOpen = false;
        }

        private void NotifyPopup_Loaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged += Current_SizeChanged; ;
        }

        private void Current_SizeChanged(object sender, Windows.UI.Core.WindowSizeChangedEventArgs e)
        {
            this.Width = e.Size.Width;
            this.Height = e.Size.Height;
        }

        private void NotifyPopup_Unloaded(object sender, RoutedEventArgs e)
        {
            Window.Current.SizeChanged -= Current_SizeChanged;
        }

        private void GoToButton_Click(object sender, RoutedEventArgs e)
        {
            var frame = new Frame();
            frame.Navigate(typeof(RunCalibratePage), m_methodName);
            Window.Current.Content = frame;
            this.m_Popup.IsOpen = false;
        }

        private void RunTestButton_Click(object sender, RoutedEventArgs e)
        {
            this.m_Popup.IsOpen = false;
            UserControlButtonClicked?.Invoke(this, EventArgs.Empty);
        }
    }
}
