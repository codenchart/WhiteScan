using System.Globalization;
using System.Windows.Data;

namespace WhiteScan.Infrastructure;

public class IntListConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is List<int> intList)
        {
            return string.Join(", ", intList);
        }
        return string.Empty;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string str && !string.IsNullOrWhiteSpace(str))
        {
            var result = new List<int>();
            var parts = str.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            
            foreach (var part in parts)
            {
                if (int.TryParse(part.Trim(), out var number))
                {
                    result.Add(number);
                }
            }
            
            return result;
        }
        
        return new List<int>();
    }
} 