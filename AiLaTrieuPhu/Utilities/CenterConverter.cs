using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace AiLaTrieuPhu.Utilities
{
    public class CenterConverter : IValueConverter
    {
        // value: Kích thước thực tế của Canvas (ActualWidth hoặc ActualHeight)
        // parameter: Kích thước cố định của phần tử (Width hoặc Height của Logo)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double actualSize && parameter is string sizeString && double.TryParse(sizeString, out double elementSize))
            {
                // Công thức căn giữa: (Kích thước chứa - Kích thước phần tử) / 2
                return (actualSize - elementSize) / 2;
            }
            // Nếu không xác định được kích thước, đặt ở vị trí 0
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
