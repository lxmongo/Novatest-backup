using System;
using Windows.UI.Xaml.Data;

namespace nanovaTest.Utils
{
    public class StringFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            if (value == null)
                return null;
            int second = 0;
            int minute = 0;
            second = System.Convert.ToInt32(value);
            if (second > 60)
            {
                minute = second / 60;
                second = second % 60;
            }
            string minuteStr = "", secondStr = "";
            if(minute == 0)
            {
                minuteStr = "00";
            }
            if(second == 0)
            {
                secondStr = "00";
            }

            if(minute < 10)
            {
                minuteStr = string.Format("0{0}", minute);
            }
            else
            {
                minuteStr = string.Format("{0}", minute);
            }
            if(second < 10)
            {
                secondStr = string.Format("0{0}", second);
            }
            else
            {
                secondStr = string.Format("{0}", second);
            }
            return string.Format("{0}:{1}", minuteStr, secondStr);
            //if (parameter == null)
            //    return string.Format("{0}{1}",value,loader.GetString("Second"));

            //return string.Format((string)parameter, value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
