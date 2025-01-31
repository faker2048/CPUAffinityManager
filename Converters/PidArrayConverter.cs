using System;
using System.Globalization;
using System.Windows.Data;

namespace _;

public class PidArrayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int[] pids)
        {
            return string.Join(", ", pids);
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 