using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly SaveGameService _saveService = new SaveGameService();
        private readonly IQuestionDataService _questionService = new QuestionDataService();
        private readonly SettingsService _settingsService;
        private readonly AudioService _audioService;

        // Cần lưu trữ AppSettings đã tải
        private AppSettings _currentAppSettings;

        // Định nghĩa Action để báo cho Window thực hiện thay đổi
        // (Action này sẽ được gán trong MainWindow.xaml.cs)
        public Action<string, DisplayMode>? ApplyDisplaySettingsAction { get; set; }

        [ObservableProperty]
        private ObservableObject _currentViewModel;

        // THÊM: Thuộc tính StatusMessage
        [ObservableProperty]
        private string _statusMessage = "Sẵn sàng chinh phục Triệu Phú!";

        public MainViewModel(AudioService audioService)
        {
            _audioService = audioService;
            _saveService = new SaveGameService();
            _settingsService = new SettingsService();

            _currentAppSettings = _settingsService.LoadSettings() ?? new AppSettings();

            NavigateToMenu();
        }

        public void NavigateToMenu()
        {
            bool canContinue = _saveService.HasSaveFile();
            CurrentViewModel = new MenuViewModel(this, _saveService);

            // Đặt lại thông báo mặc định nếu không có thông báo từ game over
            if (string.IsNullOrEmpty(StatusMessage) || StatusMessage.StartsWith("Câu hỏi số"))
            {
                StatusMessage = "Sẵn sàng chinh phục Triệu Phú!";
            }
        }

        public async Task StartNewGameAsync()
        {
            var questions = await _questionService.GetRandomGameQuestionsAsync();
            var newStatus = new GameStatus { GameQuestions = questions };
            NavigateToGame(newStatus);
        }

        public async Task LoadSavedGameAsync()
        {
            var status = await _saveService.LoadGameAsync();
            if (status != null)
            {
                NavigateToGame(status);
            }
        }

        private void NavigateToGame(GameStatus status)
        {
            CurrentViewModel = new GameViewModel(this, status, _saveService, _audioService);
        }

        // Thêm hàm công khai để MainWindow.xaml.cs gọi sau khi đã gán Action
        public void LoadInitialSettings()
        {
            if (_currentAppSettings != null)
            {
                // Áp dụng cài đặt hiển thị đã tải
                ApplyDisplaySettings(_currentAppSettings.SelectedResolution, _currentAppSettings.CurrentDisplayMode);

                // Áp dụng âm lượng, v.v. lên Audio Service
                _audioService.SetMusicVolume(_currentAppSettings.MusicVolume);
                _audioService.SetSFXVolume(_currentAppSettings.SfxVolume);
                // Nếu có VoiceVolume: audioService.SetVoiceVolume(_currentAppSettings.VoiceVolume);
            }
        }

        // Thêm hàm Navigation cho SettingsViewModel
        public void NavigateToSettings()
        {
            CurrentViewModel = new SettingsViewModel(this, _settingsService, _currentAppSettings, _audioService);
        }

        // Thêm hàm Apply Display Settings (để SettingsViewModel gọi)
        public void ApplyDisplaySettings(string resolution, DisplayMode mode)
        {
            // Kiểm tra và gọi Action đã được gán
            ApplyDisplaySettingsAction?.Invoke(resolution, mode);
        }
    }
}
