using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AiLaTrieuPhu.Utilities
{
    public class AnswerColorConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // Kiểm tra xem các giá trị truyền vào có đầy đủ và hợp lệ không
            // DependencyProperty.UnsetValue thường xuất hiện khi Bindings đang bị ngắt hoặc khởi tạo
            if (values == null || values.Length < 4 ||
                values.Any(v => v == null || v == DependencyProperty.UnsetValue))
            {
                // Trả về màu mặc định nếu dữ liệu chưa sẵn sàng
                return new SolidColorBrush(Color.FromRgb(10, 10, 32));
            }

            try
            {
                // Ép kiểu an toàn hơn
                int currentIndex = System.Convert.ToInt32(values[0]);
                int selectedIndex = System.Convert.ToInt32(values[1]);
                bool isRevealed = (bool)values[2];
                int correctIndex = System.Convert.ToInt32(values[3]);

                // 1. Ưu tiên màu Xanh Lá nếu đã công bố và đây là đáp án đúng
                if (isRevealed && currentIndex == correctIndex)
                    return new SolidColorBrush(Color.FromRgb(46, 204, 113));

                // 2. Màu Cam nếu đây là đáp án đang được chọn
                if (selectedIndex != -1 && currentIndex == selectedIndex)
                    return new SolidColorBrush(Color.FromRgb(243, 156, 18));

                // 3. Màu mặc định
                return new SolidColorBrush(Color.FromRgb(10, 10, 32));
            }
            catch
            {
                // Phòng trường hợp có giá trị rác khác khi đang hủy UI
                return new SolidColorBrush(Color.FromRgb(10, 10, 32));
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
