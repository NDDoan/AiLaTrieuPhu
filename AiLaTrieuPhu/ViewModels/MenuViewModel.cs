using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiLaTrieuPhu.ViewModels
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly MainViewModel _mainNavigator;

        public bool CanContinueGame { get; } // Dùng cho IsEnabled nút 'Tiếp tục'

        // BỔ SUNG: Thuộc tính này sẽ lấy giá trị từ MainViewModel để hiển thị
        public string StatusMessage => _mainNavigator.StatusMessage;

        public MenuViewModel(MainViewModel mainNavigator, bool canContinue)
        {
            _mainNavigator = mainNavigator;
            CanContinueGame = canContinue;

            // THÊM: Đăng ký lắng nghe sự kiện PropertyChanged từ MainViewModel
            _mainNavigator.PropertyChanged += MainNavigator_PropertyChanged;
        }

        // Hủy đăng ký (nên dùng IDisposable cho ứng dụng lớn, nhưng tạm thời dùng cách này)
        private void MainNavigator_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Nếu thuộc tính StatusMessage của MainViewModel thay đổi
            if (e.PropertyName == nameof(MainViewModel.StatusMessage))
            {
                // Thông báo cho MenuScreen rằng StatusMessage của chính nó đã thay đổi
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        [RelayCommand]
        private async Task StartGame() => await _mainNavigator.StartNewGameAsync();

        [RelayCommand(CanExecute = nameof(CanContinueGame))]
        private async Task ContinueGame() => await _mainNavigator.LoadSavedGameAsync();

        [RelayCommand]
        private void OpenSettings()
        {
            // Logic tạm thời: Navigate tới một màn hình Cài đặt (ViewModel mới)
            // Ví dụ: _mainNavigator.NavigateToSettings(); 

            // Hiện tại, ta chỉ cần phương thức này tồn tại để Command được sinh ra.
        }

        [RelayCommand]
        private void ExitApplication() => Application.Current.Shutdown();
    }
}
