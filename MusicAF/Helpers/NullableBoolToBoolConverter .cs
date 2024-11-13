using Microsoft.UI.Xaml.Data;
using System;

namespace MusicAF.Helpers
{
    public class NullableBoolToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool?)
            {
                return (bool?)value ?? false;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                return (bool)value;
            }
            return false;
        }
    }
}