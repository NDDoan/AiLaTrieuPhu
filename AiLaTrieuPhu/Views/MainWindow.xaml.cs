using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using AiLaTrieuPhu.ViewModels;

namespace AiLaTrieuPhu.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var audioService = new AudioService();
            var mainViewModel = new MainViewModel(audioService);
            this.DataContext = mainViewModel;

            // GÁN ACTION ĐỂ MAINVIEWMODEL CÓ THỂ ĐIỀU KHIỂN WINDOW
            mainViewModel.ApplyDisplaySettingsAction = ApplyDisplaySettings;
            // Sau khi Action được gán, ta mới gọi hàm Load
            mainViewModel.LoadInitialSettings();
        }

        // Hàm thực hiện thay đổi chế độ hiển thị trên Window vật lý
        private void ApplyDisplaySettings(string resolution, DisplayMode mode)
        {
            // Logic cho Fullscreen/Windowed
            switch (mode)
            {
                case DisplayMode.Fullscreen:
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Maximized;
                    // (Thường cần thiết lập Resolution nếu không dùng Borderless)
                    break;
                case DisplayMode.Borderless:
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Normal;
                    this.ResizeMode = ResizeMode.NoResize;
                    this.WindowState = WindowState.Maximized; // Vẫn Maximize nhưng không có Border
                    break;
                case DisplayMode.Windowed:
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                    this.ResizeMode = ResizeMode.CanResize;
                    // Áp dụng độ phân giải (Tùy chọn)
                    if (resolution.Contains('x'))
                    {
                        var parts = resolution.Split('x');
                        if (int.TryParse(parts[0], out int w) && int.TryParse(parts[1], out int h))
                        {
                            this.Width = w;
                            this.Height = h;
                        }
                    }
                    break;
            }
        }
    }
}
