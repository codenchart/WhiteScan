using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WhiteScan.Converters
{
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string param)
            {
                var parts = param.Split('|');
                if (parts.Length == 2)
                {
                    var colorString = boolValue ? parts[1] : parts[0];
                    return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorString));
                }
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 