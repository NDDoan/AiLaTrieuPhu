using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace AiLaTrieuPhu.Utilities
{
    public class BoolToHighlightColorConverter : IValueConverter
    {
        // Dùng StaticResource trong code-behind (trực tiếp) hoặc System Colors
        // Để đơn giản, ta sẽ dùng màu cố định (hoặc tên Resource Key, nhưng phức tạp hơn)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Màu Highlight (AccentGoldBrush)
                return new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xD7, 0x00)); // #FFFFD700 - Gold
            }
            // Màu mặc định (Trắng)
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
