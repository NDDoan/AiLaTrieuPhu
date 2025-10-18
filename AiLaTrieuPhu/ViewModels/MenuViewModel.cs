using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AiLaTrieuPhu.ViewModels
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly MainViewModel _mainNavigator;
        private readonly SaveGameService _saveService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ContinueGameCommand))] // Liên kết với Command
        private bool _isContinueGameAvailable;

        // BỔ SUNG: Thuộc tính này sẽ lấy giá trị từ MainViewModel để hiển thị
        public string StatusMessage => _mainNavigator.StatusMessage;

        public MenuViewModel(MainViewModel mainNavigator, SaveGameService saveService)
        {
            _mainNavigator = mainNavigator;
            _saveService = saveService;

            // Đăng ký lắng nghe từ MainViewModel
            _mainNavigator.PropertyChanged += MainNavigator_PropertyChanged;

            // ĐĂNG KÝ LẮNG NGHE MESSENGER TỪ GameViewModel
            WeakReferenceMessenger.Default.Register<GameStatusChangedMessage>(this, (r, m) =>
            {
                // Khi nhận được message, cập nhật thuộc tính
                IsContinueGameAvailable = m.Value; // m.Value là bool hasSaveFile
            });

            // Kích hoạt kiểm tra trạng thái ban đầu (fire-and-forget is fine here)
            _ = CheckSaveGameStatus();
        }

        // Logic kiểm tra trạng thái save
        private async Task CheckSaveGameStatus()
        {
            var savedGame = await _saveService.LoadGameAsync();
            IsContinueGameAvailable = savedGame != null;
        }

        [RelayCommand(CanExecute = nameof(IsContinueGameAvailable))]
        private async Task ContinueGame() => await _mainNavigator.LoadSavedGameAsync();

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

        [RelayCommand]
        private void OpenSettings()
        {
            // Logic tạm thời: Navigate tới một màn hình Cài đặt (ViewModel mới)
            // Ví dụ: _mainNavigator.NavigateToSettings(); 
        }

        [RelayCommand]
        private void ExitApplication() => Application.Current.Shutdown();
    }
}
