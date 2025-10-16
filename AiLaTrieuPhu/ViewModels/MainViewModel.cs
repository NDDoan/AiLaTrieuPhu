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

        [ObservableProperty]
        private ObservableObject _currentViewModel;

        // THÊM: Thuộc tính StatusMessage
        [ObservableProperty]
        private string _statusMessage = "Sẵn sàng chinh phục Triệu Phú!";

        public MainViewModel()
        {
            NavigateToMenu();
        }

        public void NavigateToMenu()
        {
            bool canContinue = _saveService.HasSaveFile();
            CurrentViewModel = new MenuViewModel(this, canContinue);

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
            CurrentViewModel = new GameViewModel(this, status, _saveService);
        }
    }
}
