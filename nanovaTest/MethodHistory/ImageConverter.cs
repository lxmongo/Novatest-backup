using Syncfusion.UI.Xaml.Grid.Cells;
using System;
using Windows.UI.Xaml.Data;

namespace nanovaTest.MethodHistory
{
    public class ImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var datacontext = value as DataContextHelper;
            if (datacontext.Value.ToString() == "TVOC" || datacontext.Value.ToString() == "BTEX"
                || datacontext.Value.ToString() == "TCE&PCE" || datacontext.Value.ToString() == "Malodorous Gas"
                || datacontext.Value.ToString() == "Vehicle" || datacontext.Value.ToString() == "Air Quality"
                || datacontext.Value.ToString() == "Pollution Source" || datacontext.Value.ToString() == "Water Quality"
                || datacontext.Value.ToString() == "MTBE" || datacontext.Value.ToString() == "Advance Test")
            {
                return "ms-appx:///Assets///" + "Folder.png";
            }
            return "ms-appx:///Assets///" + "File.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
