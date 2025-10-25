using System;
using System.Globalization;
using System.Windows.Data;

namespace AiLaTrieuPhu.Utilities
{
    // Trả về vị trí cố định (dựa trên index) để tránh phụ thuộc vào kích thước Canvas
    // Nếu parameter == "1" thì trả giá trị Top (Y), ngược lại trả giá trị Left (X)
    public class IndexToRandomPosConverter : IValueConverter
    {
        // Mảng vị trí X mặc định
        private static readonly double[] XPositions = new double[] { 40, 140, 260, 360, 480, 580, 680 };
        // Mảng vị trí Y mặc định
        private static readonly double[] YPositions = new double[] { 30, 120, 200, 60, 220, 160, 40 };

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return 0d;

            if (!int.TryParse(value.ToString(), out int index))
                index = 0;

            index = Math.Abs(index) % Math.Max(XPositions.Length, 1);

            bool isTop = (parameter != null && parameter.ToString() == "1");

            if (isTop)
            {
                return YPositions[index % YPositions.Length];
            }

            return XPositions[index % XPositions.Length];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
