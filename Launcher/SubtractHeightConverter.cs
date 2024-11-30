using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Application = System.Windows.Application;

namespace Launcher
{
    public class SubtractHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double screenBottom = (double)value;
            double windowHeight = Application.Current.MainWindow.ActualHeight;
            return screenBottom - windowHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 