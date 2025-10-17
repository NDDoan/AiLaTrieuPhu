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
    public class IndexToVisibilityConverter : IMultiValueConverter
    {
        // Value[0]: ObservableCollection<bool> IsOptionHidden (từ CurrentQuestion)
        // Value[1]: ContentPresenter (để lấy AlternationIndex)
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // 1. Lấy danh sách trạng thái ẩn (IsOptionHidden)
            if (values.Length < 1 || values[0] == null || !(values[0] is IList<bool> isHiddenList))
            {
                // Mặc định là hiển thị nếu không có danh sách (chưa dùng 50/50)
                return Visibility.Visible;
            }

            // 2. Lấy Index của đáp án hiện tại
            int index = -1;
            if (values.Length > 1 && values[1] is ContentPresenter presenter)
            {
                // Lấy AlternationIndex từ ContentPresenter (Đây là index 0, 1, 2, 3)
                index = ItemsControl.GetAlternationIndex(presenter);
            }

            // 3. Kiểm tra trạng thái ẩn tại Index
            if (index >= 0 && index < isHiddenList.Count)
            {
                // Nếu IsOptionHidden[index] là TRUE, trả về Collapsed (Ẩn đi)
                if (isHiddenList[index])
                {
                    return Visibility.Collapsed;
                }
            }

            // Mặc định: Trả về Visible (Hiển thị)
            return Visibility.Visible;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
