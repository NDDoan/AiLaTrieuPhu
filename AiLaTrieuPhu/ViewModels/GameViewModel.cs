using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AiLaTrieuPhu.ViewModels
{
    public partial class GameViewModel : ObservableObject
    {
        private readonly MainViewModel _mainNavigator;
        private readonly SaveGameService _saveService;

        // Bảng tiền thưởng tĩnh (Dùng cho logic tính toán)
        private static readonly Dictionary<int, long> StaticPrizeLadder = new Dictionary<int, long>
        {
            { 1, 1000 }, { 2, 2000 }, { 3, 3000 }, { 4, 5000 }, { 5, 10000 }, // Mốc an toàn 1
            { 6, 20000 }, { 7, 30000 }, { 8, 50000 }, { 9, 75000 }, { 10, 150000 }, // Mốc an toàn 2
            { 11, 250000 }, { 12, 500000 }, { 13, 750000 }, { 14, 1000000 }, { 15, 2000000 } // Đỉnh
        };

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsConsultancyVisible))]
        private GameStatus _currentGameStatus;

        [ObservableProperty]
        private Question _currentQuestion = new Question(); // Khởi tạo để tránh lỗi null

        [ObservableProperty]
        private bool _isAnswerPhase = true;

        [ObservableProperty]
        private string _gameStatusMessage = "Sẵn sàng! Chờ đợi câu hỏi đầu tiên.";

        [ObservableProperty]
        private ObservableCollection<PrizeLevel> _prizeLadderDisplay = new ObservableCollection<PrizeLevel>();

        // Thuộc tính kiểm soát hiển thị nút Tổ Tư Vấn (Xuất hiện từ câu 6, index 5)
        public bool IsConsultancyVisible => CurrentGameStatus?.CurrentQuestionIndex >= 5;

        public bool Is5050Enabled => !CurrentGameStatus.Is5050Used && IsAnswerPhase;

        public GameViewModel(MainViewModel mainNavigator, GameStatus status, SaveGameService saveService)
        {
            _mainNavigator = mainNavigator;
            _saveService = saveService;

            CurrentGameStatus = status;

            InitializePrizeLadderDisplay();

            if (CurrentGameStatus.GameQuestions.Any())
            {
                // Đảm bảo chỉ số nằm trong phạm vi
                int index = Math.Clamp(CurrentGameStatus.CurrentQuestionIndex, 0, CurrentGameStatus.GameQuestions.Count - 1);
                CurrentQuestion = CurrentGameStatus.GameQuestions[index];

                UpdatePrizeLadderStatus();
                GameStatusMessage = $"Câu hỏi số {index + 1}. Giá trị: {GetPrizeForQuestionNumber(index + 1):N0} VNĐ";
            }
        }

        // =========================================================================
        // KHỞI TẠO VÀ CẬP NHẬT PRIZE LADDER (Theo ý tưởng Giao diện)
        // =========================================================================

        private void InitializePrizeLadderDisplay()
        {
            // Sắp xếp ngược lại (15 về 1) để giống giao diện
            for (int i = 15; i >= 1; i--)
            {
                PrizeLadderDisplay.Add(new PrizeLevel
                {
                    QuestionNumber = i,
                    PrizeAmount = StaticPrizeLadder[i],
                    IsSafePoint = (i == 5 || i == 10 || i == 15),
                    // Trạng thái sẽ được cập nhật ngay sau khi khởi tạo
                });
            }
        }

        private void UpdatePrizeLadderStatus()
        {
            // Cập nhật trạng thái từng cấp độ trong PrizeLadderDisplay
            foreach (var level in PrizeLadderDisplay)
            {
                // Đã trả lời đúng (Hiển thị ⋄)
                level.IsAnswered = (level.QuestionNumber <= CurrentGameStatus.CurrentQuestionIndex);

                // Câu hỏi hiện tại (Highlight)
                level.IsCurrent = (level.QuestionNumber == CurrentGameStatus.CurrentQuestionIndex + 1);
            }

            // Buộc cập nhật trạng thái hiển thị Tổ Tư Vấn
            OnPropertyChanged(nameof(IsConsultancyVisible));
        }

        // =========================================================================
        // LOGIC CHƠI GAME
        // =========================================================================

        [RelayCommand(CanExecute = nameof(CanAnswer))]
        private async Task Answer(int selectedAnswerIndex)
        {
            IsAnswerPhase = false; // Vô hiệu hóa nút bấm ngay lập tức

            if (selectedAnswerIndex == CurrentQuestion.CorrectAnswerIndex)
            {
                GameStatusMessage = $"Chúc mừng! Đáp án {IntToChar(selectedAnswerIndex)} là chính xác!";

                UpdateGameStatusOnCorrectAnswer();

                await MoveToNextQuestionAsync();
            }
            else
            {
                // Thua cuộc
                GameStatusMessage = $"Rất tiếc! Đáp án {IntToChar(selectedAnswerIndex)} là sai. Đáp án đúng là {IntToChar(CurrentQuestion.CorrectAnswerIndex)}.";
                GameOver(false);
            }
        }

        private void UpdateGameStatusOnCorrectAnswer()
        {
            int answeredQuestionNumber = CurrentGameStatus.CurrentQuestionIndex + 1;

            // Cập nhật tiền thưởng hiện tại (đã đạt được)
            CurrentGameStatus.CurrentPrizeMoney = StaticPrizeLadder[answeredQuestionNumber];

            // Cập nhật mốc an toàn
            if (answeredQuestionNumber == 5) CurrentGameStatus.SafePointReached = 5;
            if (answeredQuestionNumber == 10) CurrentGameStatus.SafePointReached = 10;

            // Tăng Index để chuyển sang câu tiếp theo
            CurrentGameStatus.CurrentQuestionIndex++;
        }

        private async Task MoveToNextQuestionAsync()
        {
            await Task.Delay(4000); // Đợi 4 giây

            if (CurrentGameStatus.CurrentQuestionIndex < CurrentGameStatus.GameQuestions.Count)
            {
                // Chuyển sang câu hỏi tiếp theo
                CurrentQuestion = CurrentGameStatus.GameQuestions[CurrentGameStatus.CurrentQuestionIndex];

                // Cập nhật trạng thái Prize Ladder
                UpdatePrizeLadderStatus();

                int nextQuestionNumber = CurrentGameStatus.CurrentQuestionIndex + 1;
                GameStatusMessage = $"Câu hỏi số {nextQuestionNumber}. Giá trị: {GetPrizeForQuestionNumber(nextQuestionNumber):N0} VNĐ";

                await _saveService.SaveGameAsync(CurrentGameStatus);
                IsAnswerPhase = true; // Bật lại nút bấm
            }
            else
            {
                // Hoàn thành hết 15 câu
                GameOver(true);
            }
        }

        private async void GameOver(bool isWinner, bool isVoluntaryQuit = false)
        {
            IsAnswerPhase = false;
            await Task.Delay(5000);

            long finalPrize = 0;
            if (isWinner)
            {
                finalPrize = StaticPrizeLadder[15];
            }
            else if (isVoluntaryQuit)
            {
                // Nếu tự bỏ, ra về với tiền thưởng đã đạt được (CurrentPrizeMoney)
                // Lưu ý: CurrentPrizeMoney đã được cập nhật sau mỗi câu trả lời đúng
                finalPrize = CurrentGameStatus.CurrentPrizeMoney;
            }
            else
            {
                // Nếu thua (trả lời sai), ra về với tiền ở mốc an toàn cuối cùng
                finalPrize = CalculateFinalPrize();
            }

            _mainNavigator.StatusMessage = $"Trò chơi kết thúc! Số tiền bạn mang về là: {finalPrize:N0} VNĐ.";

            // Xóa file save và điều hướng
            // ...
            _mainNavigator.NavigateToMenu();
        }

        // =========================================================================
        // CÁC HÀM HỖ TRỢ VÀ COMMANDS KHÁC
        // =========================================================================

        [RelayCommand]
        private async Task SaveAndExit()
        {
            await _saveService.SaveGameAsync(CurrentGameStatus);
            _mainNavigator.NavigateToMenu();
        }

        private bool CanAnswer() => IsAnswerPhase && CurrentQuestion != null;

        private long CalculateFinalPrize()
        {
            if (CurrentGameStatus.SafePointReached == 10) return StaticPrizeLadder[10];
            if (CurrentGameStatus.SafePointReached == 5) return StaticPrizeLadder[5];
            return 0;
        }

        private char IntToChar(int index) => (char)('A' + index);

        private long GetPrizeForQuestionNumber(int questionNumber)
        {
            return StaticPrizeLadder.GetValueOrDefault(questionNumber, 0);
        }

        [RelayCommand]
        private void QuitGame()
        {
            // Xác nhận trước khi TỪ BỎ (nên có MessageBox)
            var result = MessageBox.Show(
                "Bạn có chắc muốn dừng cuộc chơi và ra về với số tiền hiện tại?",
                "Xác nhận TỪ BỎ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                // Tính toán số tiền cuối cùng (tiền ở mốc an toàn cuối cùng đã đạt)
                GameOver(false, true); // False: không phải thắng 15 câu, True: người chơi tự nguyện dừng
            }
        }

        [RelayCommand(CanExecute = nameof(Is5050Enabled))]
        private void Use5050()
        {
            // 1. Kiểm tra trạng thái và cập nhật cờ đã dùng
            if (CurrentQuestion == null || CurrentGameStatus.Is5050Used) return;

            CurrentGameStatus.Is5050Used = true;

            // Is5050Enabled trở thành FALSE, vô hiệu hóa nút.
            OnPropertyChanged(nameof(CurrentGameStatus));

            // Bỏ dòng báo lỗi: Is5050Enabled = false; 

            // 2. Tìm 2 đáp án sai để loại bỏ (Logic giữ nguyên)
            var incorrectIndices = new List<int>();
            for (int i = 0; i < CurrentQuestion.Options.Count; i++)
            {
                if (i != CurrentQuestion.CorrectAnswerIndex)
                {
                    incorrectIndices.Add(i);
                }
            }

            // 3. Chọn ngẫu nhiên 2 index sai để ẩn
            var random = new Random();
            var indicesToHide = incorrectIndices.OrderBy(x => random.Next()).Take(2).ToList();

            // 4. Cập nhật thuộc tính IsOptionHidden trong Question
            foreach (var index in indicesToHide)
            {
                if (index >= 0 && index < CurrentQuestion.IsOptionHidden.Count)
                {
                    // Thiết lập trạng thái ẩn
                    CurrentQuestion.IsOptionHidden[index] = true;

                    // Thay nội dung đáp án thành rỗng để UI ẩn đi hoặc hiển thị " "
                    CurrentQuestion.Options[index] = " ";
                }
            }

            // 5. Thông báo thay đổi
            OnPropertyChanged(nameof(CurrentQuestion));

            // Bắn lại lệnh CanExecute để vô hiệu hóa nút 50:50 (Do OnPropertyChanged(CurrentGameStatus) đã làm điều này, dòng này là dư nhưng không gây hại)
            Use5050Command.NotifyCanExecuteChanged();
        }
    }
}