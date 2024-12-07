using Microsoft.UI.Xaml.Data;
using System;

namespace MusicAF.Helpers
{
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("MMM dd, yyyy HH:mm"); // Example: Dec 07, 2024 14:30
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
