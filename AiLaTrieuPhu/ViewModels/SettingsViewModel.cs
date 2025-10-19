using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiLaTrieuPhu.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly MainViewModel _mainNavigator;
        private readonly SettingsService _settingsService;

        // Model chứa các giá trị setting hiện tại
        [ObservableProperty]
        private AppSettings _currentSettings;

        // Các tùy chọn Resolution có sẵn (dùng cho ComboBox)
        public ObservableCollection<string> AvailableResolutions { get; } = new ObservableCollection<string>
        {
            "1280x720",
            "1600x900",
            "1920x1080",
            "2560x1440"
        };

        // Các tùy chọn DisplayMode (Enum)
        public Array DisplayModes => Enum.GetValues(typeof(DisplayMode));

        public SettingsViewModel(MainViewModel mainNavigator, SettingsService settingsService, AppSettings initialSettings)
        {
            _mainNavigator = mainNavigator;
            _settingsService = settingsService;
            _currentSettings = initialSettings;

            // Tải cài đặt hiện có hoặc tạo mới (sẽ được implement trong SettingsService)
            _currentSettings = settingsService.LoadSettings() ?? new AppSettings();
        }

        [RelayCommand]
        private void BackToMenu()
        {
            // 1. Lưu các thay đổi cài đặt trước khi thoát
            _settingsService.SaveSettings(CurrentSettings);

            // 2. Quay lại màn hình chính (MenuViewModel)
            _mainNavigator.NavigateToMenu();
        }

        // Command để kích hoạt việc thay đổi chế độ hiển thị (Nếu cần)
        [RelayCommand]
        private void ApplyDisplayChanges()
        {
            // Logic áp dụng thay đổi độ phân giải/chế độ Fullscreen/Windowed
            // Logic này thường được đặt trong MainView/MainViewModel để thao tác với Window
            _mainNavigator.ApplyDisplaySettings(CurrentSettings.SelectedResolution, CurrentSettings.CurrentDisplayMode);
        }
    }
}
