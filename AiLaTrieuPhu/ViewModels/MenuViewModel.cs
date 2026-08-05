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
        private readonly AudioService _audioService;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(ContinueGameCommand))] // Liên kết với Command
        private bool _isContinueGameAvailable;

        [ObservableProperty]
        private bool _isStartingGame;

        // BỔ SUNG: Thuộc tính này sẽ lấy giá trị từ MainViewModel để hiển thị
        public string StatusMessage => _mainNavigator.StatusMessage;

        public bool IsMillionaireCelebration => !string.IsNullOrEmpty(StatusMessage) && StatusMessage.StartsWith("Bạn là Triệu Phú!");


        public MenuViewModel(MainViewModel mainNavigator, SaveGameService saveService, AudioService audioService)
        {
            _mainNavigator = mainNavigator;
            _saveService = saveService;
            _audioService = audioService;

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
                OnPropertyChanged(nameof(IsMillionaireCelebration));
                
                if (IsMillionaireCelebration)
                {
                    _audioService.PlaySFX("SFX/Ban_phao_hoa.mp3");
                }
            }
        }

        [RelayCommand]
        private async Task StartGame()
        {
            if (IsStartingGame) return;
            IsStartingGame = true;
            await Task.Delay(2000); // Wait for blink animation
            await _mainNavigator.StartNewGameAsync();
            IsStartingGame = false;
        }

        [RelayCommand]
        private void OpenSettings() => _mainNavigator.NavigateToSettings();

        [RelayCommand]
        private void ExitApplication() => Application.Current.Shutdown();
    }
}
