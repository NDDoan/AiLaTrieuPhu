using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AiLaTrieuPhu.Utilities
{
    public class BoolToDiamondConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && boolValue)
            {
                // Trả về ký tự Kim cương (hoặc chuỗi "⋄ ") nếu đã trả lời đúng
                return "⋄";
            }
            return string.Empty; // Trả về chuỗi rỗng nếu chưa trả lời
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
