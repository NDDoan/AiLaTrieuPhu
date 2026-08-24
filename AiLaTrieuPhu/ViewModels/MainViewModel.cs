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
        private readonly AchievementService _achievementService = new AchievementService();
        private readonly PlayerProfileService _playerProfileService = new PlayerProfileService();

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

            // Tải dữ liệu thành tựu và profile người chơi bất đồng bộ
            _ = _achievementService.LoadAsync();
            _ = _playerProfileService.LoadAsync();

            NavigateToMenu();
        }

        public void NavigateToMenu()
        {
            bool canContinue = _saveService.HasSaveFile();
            CurrentViewModel = new MenuViewModel(this, _saveService, _audioService);

            // Đặt lại thông báo mặc định nếu không có thông báo từ game over
            if (string.IsNullOrEmpty(StatusMessage) || StatusMessage.StartsWith("Câu hỏi số"))
            {
                StatusMessage = "Sẵn sàng chinh phục Triệu Phú!";
            }
        }

        public async Task StartNewGameAsync()
        {   
            _audioService.PlayVA("VA/chaomungquividenvoichuongtrinhailatrieuphu.mp3");
            _audioService.PlaySFX("SFX/Bat_dau_game.mp3");
            
            // File dài 28s, nhưng chỉ cần chờ 10s nhạc dạo lớn ban đầu
            await Task.Delay(10000);

            var questions = await _questionService.GetRandomGameQuestionsAsync();
            var newStatus = new GameStatus { GameQuestions = questions };
            NavigateToGame(newStatus);
        }

        public async Task LoadSavedGameAsync()
        {
            var status = await _saveService.LoadGameAsync();
            if (status != null)
            {
                NavigateToGame(status, true);
            }
        }

        private void NavigateToGame(GameStatus status, bool isLoadedGame = false)
        {
            CurrentViewModel = new GameViewModel(this, status, _saveService, _audioService, _achievementService, _playerProfileService, isLoadedGame);
        }

        public void NavigateToAchievements()
        {
            CurrentViewModel = new AchievementViewModel(this, _achievementService);
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
