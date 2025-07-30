using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace WhiteScan.Converters
{
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? 
                        (Visibility)Enum.Parse(typeof(Visibility), parts[0]) : 
                        (Visibility)Enum.Parse(typeof(Visibility), parts[1]);
                }
            }
            
            if (value is bool boolVal)
            {
                return boolVal ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 