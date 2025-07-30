using System;
using System.Globalization;
using System.Windows.Data;

namespace WhiteScan.Converters
{
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter is string param)
                {
                    var parts = param.Split('|');
                    if (parts.Length == 2)
                    {
                        return boolValue ? parts[1] : parts[0];
                    }
                }
                return boolValue ? "Enable" : "Disable";
            }
            return "Disable";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 