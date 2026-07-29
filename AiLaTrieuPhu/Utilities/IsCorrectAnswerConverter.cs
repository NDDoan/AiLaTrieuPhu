using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace AiLaTrieuPhu.Utilities
{
    public class IsCorrectAnswerConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values == null || values.Length < 3 ||
                values.Any(v => v == null || v == DependencyProperty.UnsetValue))
            {
                return false;
            }

            try
            {
                int currentIndex = System.Convert.ToInt32(values[0]);
                int correctIndex = System.Convert.ToInt32(values[1]);
                bool isRevealed = (bool)values[2];

                return isRevealed && currentIndex == correctIndex;
            }
            catch
            {
                return false;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
