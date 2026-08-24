using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    public enum DisplayMode
    {
        Windowed,
        Fullscreen,
        Borderless
    }

    // Lớp chứa toàn bộ các cài đặt ứng dụng
    public partial class AppSettings : ObservableObject
    {
        // 1. Cài đặt Âm thanh
        [ObservableProperty]
        private int _musicVolume = 50; // Nhạc nền

        [ObservableProperty]
        private int _sfxVolume = 70; // Hiệu ứng âm thanh

        [ObservableProperty]
        private int _voiceVolume = 80; // Lồng tiếng/Giả lập AI

        // 2. Cài đặt Hiển thị
        [ObservableProperty]
        private DisplayMode _currentDisplayMode = DisplayMode.Windowed;

        // Lưu trữ Index hoặc Giá trị của độ phân giải được chọn
        // Ta dùng string để dễ Binding với ComboBox của các lựa chọn (VD: 1920x1080)
        [ObservableProperty]
        private string _selectedResolution = "1280x720";

        // 3. Các tính năng khác
        [ObservableProperty]
        private bool _isTimerVisible = true;
    }
}
