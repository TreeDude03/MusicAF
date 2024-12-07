
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MusicAF.Helpers
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool booleanValue)
            {
                // Check for inversion
                bool isInverted = parameter?.ToString().Equals("Invert", StringComparison.OrdinalIgnoreCase) == true;
                return (booleanValue ^ isInverted) ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}

