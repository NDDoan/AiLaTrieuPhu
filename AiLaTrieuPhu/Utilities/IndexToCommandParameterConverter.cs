using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AiLaTrieuPhu.Utilities
{
    // Converter này sẽ tính toán Index dựa trên container của item
    public class IndexToCommandParameterConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0] là ItemContainer (ContentPresenter)
            // values[1] là ItemsControl (cha)

            if (values.Length < 2 || values[0] == DependencyProperty.UnsetValue || values[1] == DependencyProperty.UnsetValue)
                return 0; // Trả về 0 nếu có lỗi

            var container = values[0] as ContentPresenter;
            var itemsControl = values[1] as ItemsControl;

            if (container != null && itemsControl != null)
            {
                // Sử dụng ItemContainerGenerator để lấy Index
                return itemsControl.ItemContainerGenerator.IndexFromContainer(container);
            }

            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
