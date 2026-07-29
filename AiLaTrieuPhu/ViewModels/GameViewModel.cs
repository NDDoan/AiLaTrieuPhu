using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using AiLaTrieuPhu.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace AiLaTrieuPhu.ViewModels
{
    public partial class GameViewModel : ObservableObject
    {
        private readonly MainViewModel _mainNavigator;
        private readonly SaveGameService _saveService;
        private readonly AudioService _audioService;
        // Random instance tập trung, tránh tạo nhiều instance trong thời gian ngắn
        // (nhiều instance cùng seed → kết quả bị lặp lại)
        private readonly Random _random = new Random();

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
        [NotifyCanExecuteChangedFor(nameof(AnswerCommand))]
        [NotifyCanExecuteChangedFor(nameof(Use5050Command))]
        [NotifyCanExecuteChangedFor(nameof(UseCallCommand))]
        [NotifyCanExecuteChangedFor(nameof(UseAudienceCommand))]
        [NotifyCanExecuteChangedFor(nameof(UseConsultancyCommand))]
        [NotifyPropertyChangedFor(nameof(Is5050Enabled))]
        [NotifyPropertyChangedFor(nameof(IsCallingEnabled))]
        [NotifyPropertyChangedFor(nameof(IsAudienceEnabled))]
        [NotifyPropertyChangedFor(nameof(IsToTuVanEnabled))]
        private bool _isAnswerPhase = true;

        [ObservableProperty]
        private string _gameStatusMessage = "Sẵn sàng! Chờ đợi câu hỏi đầu tiên.";

        [ObservableProperty]
        private ObservableCollection<PrizeLevel> _prizeLadderDisplay = new ObservableCollection<PrizeLevel>();

        // Thêm vào vùng khai báo biến của GameViewModel.cs
        [ObservableProperty]
        private int _selectedAnswerIndex = -1; // -1 nghĩa là chưa chọn gì

        [ObservableProperty]
        private bool _isResultRevealed = false;

        public bool Is5050Enabled => !CurrentGameStatus.Is5050Used && IsAnswerPhase;

        // Danh sách các chuyên gia
        public List<CallExpert> Experts { get; } = new List<CallExpert>
        {
            new CallExpert {
                Name = "Giáo sư Văn",
                Strengths = new List<string> { "Literature", "Culture", "Civics" },
                PersonaStyle = "Dựa trên điển tích và ý nghĩa văn học, mình tin là..."
            },
            new CallExpert {
                Name = "Giáo sư Toán",
                Strengths = new List<string> { "Math", "Physics", "Chemistry", "Biology" },
                PersonaStyle = "Theo tính toán logic và các định luật tự nhiên, đáp án phải là..."
            },
            new CallExpert {
                Name = "Wibu Chúa",
                Strengths = new List<string> { "AnimeManga", "Film", "Art", "Music" },
                PersonaStyle = "Cái này mình xem rồi, không lệch đi đâu được, là..."
            },
            new CallExpert {
                Name = "Fan MU",
                Strengths = new List<string> { "Sports", "Culture", "Music" },
                PersonaStyle = "Gáy lên nào! Glory Glory Man United! Chắc chắn là..."
            },
            new CallExpert {
                Name = "Hai Phớ",
                Strengths = new List<string> { "Civics", "Culture", "Geography", "History" },
                PersonaStyle = "Bằng các biện pháp nghiệp vụ, tôi xác nhận đáp án là..."
            },
            new CallExpert {
                Name = "Hắc cơ Lỏ",
                Strengths = new List<string> { "ComputerScience", "Technology" },
                PersonaStyle = "Vừa check database xong, con hàng này là..."
            },
            new CallExpert {
                Name = "??????",
                Strengths = new List<string> { "Geography", "ComputerScience", "Technology", "Civics" },
                PersonaStyle = "Đừng hỏi tôi ở đâu, chỉ biết đáp án là..."
            }
        };

        // Thuộc tính để bật/tắt nút Gọi điện
        public bool IsCallingEnabled => !CurrentGameStatus.IsCallingUsed && IsAnswerPhase;

        // Thuộc tính để hiển thị/ẩn cửa sổ pop-up chọn chuyên gia
        [ObservableProperty]
        private bool _isExpertSelectionVisible = false;

        // Thuộc tính để lưu chuyên gia người chơi đã chọn
        [ObservableProperty]
        private CallExpert _selectedExpert;

        // Kích hoạt cửa sổ chọn chuyên gia
        [RelayCommand(CanExecute = nameof(IsCallingEnabled))]
        private async Task UseCall()
        {
            if (CurrentGameStatus.IsCallingUsed) return;
            IsAnswerPhase = false;

            // Mở cửa sổ chọn chuyên gia (giả sử có 1 UserControl/Window tương ứng)
            IsExpertSelectionVisible = true;
        }

        // Thuộc tính để hiển thị/ẩn cửa sổ pop-up kết quả tư vấn
        [ObservableProperty]
        private bool _isExpertAnswerVisible = false;

        // Thuộc tính để lưu kết quả và hiển thị pop-up
        [ObservableProperty]
        private AudienceResult _currentAudienceResult;

        [ObservableProperty]
        private bool _isAudienceResultVisible = false;

        [ObservableProperty]
        private bool _isAudienceVotingCompleted = false;

        // Thuộc tính để bật/tắt nút Hỏi khán giả
        public bool IsAudienceEnabled => !CurrentGameStatus.IsKhanGiaUsed && IsAnswerPhase;

        // 3 thành viên tổ tư vấn
        public ObservableCollection<Consultant> Consultants { get; } = new ObservableCollection<Consultant>
        {
            new Consultant { Name = "Khán giả ngẫu nhiên 1", CorrectnessRate = 60 },
            new Consultant { Name = "Khán giả ngẫu nhiên 2", CorrectnessRate = 70 },
            new Consultant { Name = "Khán giả ngẫu nhiên 3", CorrectnessRate = 80 }
        };

        [ObservableProperty]
        private ObservableCollection<DummyAudience> _dummyAudiences = new ObservableCollection<DummyAudience>();

        // Thuộc tính để hiển thị/ẩn cửa sổ pop-up kết quả Tổ Tư Vấn
        [ObservableProperty]
        private bool _isConsultantResultVisible = false;

        // Thuộc tính kiểm soát hiển thị nút Tổ Tư Vấn (Xuất hiện từ câu 6)
        public bool IsConsultancyVisible => CurrentGameStatus.CurrentQuestionIndex >= 5;

        // Thuộc tính để bật/tắt nút Tổ Tư Vấn
        public bool IsToTuVanEnabled => !CurrentGameStatus.IsToTuVanUsed && IsAnswerPhase;

        [ObservableProperty]
        private bool _isContinueGameAvailable = false;

        public GameViewModel(MainViewModel mainNavigator, GameStatus status, SaveGameService saveService, AudioService audioService)
        {
            _mainNavigator = mainNavigator;
            _saveService = saveService;
            _audioService = audioService;

            CurrentGameStatus = status;

            InitializePrizeLadderDisplay();

            if (CurrentGameStatus.GameQuestions.Any())
            {
                if (CurrentGameStatus.CurrentQuestionIndex > 0)
                {
                    // Đảm bảo chỉ số nằm trong phạm vi
                    int index = Math.Clamp(CurrentGameStatus.CurrentQuestionIndex, 0, CurrentGameStatus.GameQuestions.Count - 1);
                    CurrentQuestion = CurrentGameStatus.GameQuestions[index];

                    UpdatePrizeLadderStatus();
                    GameStatusMessage = $"Câu hỏi số {index + 1}. Giá trị: {GetPrizeForQuestionNumber(index + 1):N0} VNĐ";
                }
                else
                {
                    UpdatePrizeLadderStatus();
                    GameStatusMessage = "Sẵn sàng! Chờ đợi câu hỏi đầu tiên.";
                }

                _ = StartGameSequenceAsync().ContinueWith(
                    t => System.Diagnostics.Debug.WriteLine($"[GameViewModel] StartGameSequenceAsync lỗi: {t.Exception}"),
                    System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);
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
            SelectedAnswerIndex = selectedAnswerIndex;
            IsResultRevealed = false;

            // XÁC ĐỊNH NHỊP ĐỘ DỰA TRÊN CÂU HỎI
            int currentQuestionNumber = CurrentGameStatus.CurrentQuestionIndex + 1;
            int suspenseDelay = 2500; // Mặc định 2,5 giây cho câu 1-5

            if (currentQuestionNumber >= 6 && currentQuestionNumber <= 14)
            {
                _audioService.StopBGM();
                // Nhịp độ chậm lại đáng kể từ câu 6 trở đi
                suspenseDelay = 8000; // Đợi 8 giây để tạo sự hồi hộp

                // PHÁT NHẠC CHỜ KỊCH TÍNH
                _audioService.PlaySFX("SFX/Answer/Tra_Loi_Cau_6_Den_15.mp3");
            }

            // Đợi một khoảng thời gian trước khi công bố kết quả
            await Task.Delay(suspenseDelay);

            // KIỂM TRA ĐÁP ÁN
            IsResultRevealed = true; // Kích hoạt trạng thái hiện màu Xanh
            bool isCorrect = selectedAnswerIndex == CurrentQuestion.CorrectAnswerIndex;
            // PHÁT ÂM THANH KẾT QUẢ ĐÚNG/SAI
            await PlayResultSound(isCorrect);

            if (isCorrect)
            {
                GameStatusMessage = $"Chúc mừng! Đáp án {IntToChar(selectedAnswerIndex)} là chính xác!";

                await _audioService.PlayVAAsync($"VA/Answer/dolacautraloidung.mp3");

                // Đợi nhấp nháy đáp án đúng rồi hiện tiền thưởng
                await Task.Delay(2500); 
                long prizeAmount = StaticPrizeLadder[CurrentGameStatus.CurrentQuestionIndex + 1];
                CurrentQuestion.QuestionText = $"BẠN ĐÃ GIÀNH ĐƯỢC:\n{prizeAmount:N0} VNĐ";
                
                // Đợi để người chơi xem tiền thưởng
                await Task.Delay(3500);

                UpdateGameStatusOnCorrectAnswer();

                await MoveToNextQuestionAsync();
            }
            else
            {
                // Thua cuộc
                GameStatusMessage = $"Rất tiếc! Đáp án {IntToChar(selectedAnswerIndex)} là sai. Đáp án đúng là {IntToChar(CurrentQuestion.CorrectAnswerIndex)}.";
                await GameOver(false);
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
            int currentQuestionNumber = CurrentGameStatus.CurrentQuestionIndex + 1;

            if (currentQuestionNumber == 6)
            {
                _audioService.StopBGM();
                await _audioService.PlayVAAsync("VA/bandavuotqua5cauhoidautiencuachuongtrinh.mp3");
                await _audioService.PlayVAAsync("VA/vabatdautucauso6dolatotuvantaicho.mp3");
                await Task.Delay(7000);
            }

            if (currentQuestionNumber == 11)
            {
                _audioService.StopBGM();
                await Task.Delay(7000);
            }

            if (currentQuestionNumber > 1)
            {
                await Task.Delay(4000); // Đợi nhịp nghỉ chung
            }

            if (CurrentGameStatus.CurrentQuestionIndex > 4 && CurrentGameStatus.CurrentQuestionIndex < 15)
            {
                _audioService.PlaySFX("SFX/Question/Bat_dau_cau_hoi.mp3");
            }

            if (currentQuestionNumber > 1)
            {
                await Task.Delay(3000);
            }

            SelectedAnswerIndex = -1;
            IsResultRevealed = false;

            if (CurrentGameStatus.CurrentQuestionIndex < CurrentGameStatus.GameQuestions.Count)
            {
                CurrentQuestion = CurrentGameStatus.GameQuestions[CurrentGameStatus.CurrentQuestionIndex];
                UpdatePrizeLadderStatus();

                int nextQuestionNumber = CurrentGameStatus.CurrentQuestionIndex + 1;
                GameStatusMessage = $"Câu hỏi số {nextQuestionNumber}. Giá trị: {GetPrizeForQuestionNumber(nextQuestionNumber):N0} VNĐ";

                string questionVA = currentQuestionNumber == 1 ? "cauhoidautien.mp3" : $"cauhoiso{currentQuestionNumber}.mp3";
                await _audioService.PlayVAAsync($"VA/Question/{questionVA}");

                HandleBGM();

                await _saveService.SaveGameAsync(CurrentGameStatus);
                IsAnswerPhase = true;
            }
            else
            {
                await GameOver(true);
            }
        }

        private async Task GameOver(bool isWinner, bool isVoluntaryQuit = false)
        {
            IsAnswerPhase = false;
            _audioService.StopBGM();
            await Task.Delay(5000);

            long finalPrize = 0;
            if (isWinner)
            {
                finalPrize = StaticPrizeLadder[15];
                CurrentGameStatus.CompletionStatus = GameCompletionStatus.QuitAndKeptMoney; // Hoặc một trạng thái Win riêng
                GameStatusMessage = $"Bạn là Triệu Phú! Chúc mừng với {finalPrize:N0} VNĐ!";
            }
            else if (isVoluntaryQuit)
            {
                // Nếu tự bỏ, ra về với tiền thưởng đã đạt được (CurrentPrizeMoney)
                // Lưu ý: CurrentPrizeMoney đã được cập nhật sau mỗi câu trả lời đúng
                finalPrize = CurrentGameStatus.CurrentPrizeMoney;
                CurrentGameStatus.CompletionStatus = GameCompletionStatus.QuitAndKeptMoney;
                GameStatusMessage = $"Chúc mừng! Bạn ra về với {finalPrize:N0} VNĐ!";
            }
            else
            {
                // Nếu thua (trả lời sai), ra về với tiền ở mốc an toàn cuối cùng
                finalPrize = CalculateFinalPrize();
                CurrentGameStatus.CompletionStatus = GameCompletionStatus.Lost;
                GameStatusMessage = $"Thật tiếc! Bạn phải dừng cuộc chơi. Tiền thưởng của bạn là {finalPrize:N0} VNĐ.";
            }

            // Lưu trạng thái cuối cùng vào file save (bao gồm CompletionStatus)
            await _saveService.SaveGameAsync(CurrentGameStatus);

            _mainNavigator.StatusMessage = GameStatusMessage;
            _mainNavigator.NavigateToMenu();

            if (CurrentGameStatus.CompletionStatus != GameCompletionStatus.InProgress)
            {
                // Sau khi game kết thúc (Quit/Lost) và file save bị xóa/cập nhật, gửi message
                WeakReferenceMessenger.Default.Send(new GameStatusChangedMessage(false)); // False: không còn save file để tiếp tục
            }
        }

        // =========================================================================
        // CÁC HÀM HỖ TRỢ VÀ COMMANDS KHÁC
        // =========================================================================

        [RelayCommand]
        private async Task SaveAndExit()
        {
            _audioService.StopBGM();
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
        public async Task QuitGame()
        {
            // Xác nhận trước khi TỪ BỎ (nên có MessageBox)
            var result = MessageBox.Show(
                "Bạn có chắc muốn dừng cuộc chơi và ra về với số tiền hiện tại?",
                "Xác nhận TỪ BỎ",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _audioService.StopBGM();
                // Tính toán số tiền cuối cùng (tiền ở mốc an toàn cuối cùng đã đạt)
                await GameOver(false, true); // False: không phải thắng 15 câu, True: người chơi tự nguyện dừng
            }
        }

        [RelayCommand(CanExecute = nameof(Is5050Enabled))]
        private async Task Use5050()
        {
            // 1. Kiểm tra trạng thái và cập nhật cờ đã dùng
            if (CurrentQuestion == null || CurrentGameStatus.Is5050Used) return;

            IsAnswerPhase = false;

            await _audioService.PlayVAAsync("VA/Help/dung5050.mp3");

            CurrentGameStatus.Is5050Used = true;

            // Is5050Enabled trở thành FALSE, vô hiệu hóa nút.
            OnPropertyChanged(nameof(CurrentGameStatus));

            // 2. Tìm 2 đáp án sai để loại bỏ
            var incorrectIndices = new List<int>();
            for (int i = 0; i < CurrentQuestion.Options.Count; i++)
            {
                if (i != CurrentQuestion.CorrectAnswerIndex)
                {
                    incorrectIndices.Add(i);
                }
            }

            // 3. Chọn ngẫu nhiên 2 index sai để ẩn
            var indicesToHide = incorrectIndices.OrderBy(x => _random.Next()).Take(2).ToList();

            // 4. Cập nhật thuộc tính IsOptionHidden trong Question
            foreach (var index in indicesToHide)
            {
                if (index >= 0 && index < CurrentQuestion.IsOptionHidden.Count)
                {
                    // Thiết lập trạng thái ẩn
                    CurrentQuestion.IsOptionHidden[index] = true;
                }
            }

            // 5. Thông báo thay đổi
            OnPropertyChanged(nameof(CurrentQuestion));

            IsAnswerPhase = true;
            // Bắn lại lệnh CanExecute để vô hiệu hóa nút 50:50 (Do OnPropertyChanged(CurrentGameStatus) đã làm điều này, dòng này là dư nhưng không gây hại)
            Use5050Command.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private async Task SelectExpertAndGetAnswer(CallExpert expert)
        {
            if (expert == null || CurrentGameStatus.IsCallingUsed) return;

            SelectedExpert = expert;
            IsExpertSelectionVisible = false; // Đóng danh bạ

            // 1. Đánh dấu quyền trợ giúp đã dùng
            CurrentGameStatus.IsCallingUsed = true;
            OnPropertyChanged(nameof(CurrentGameStatus));
            UseCallCommand.NotifyCanExecuteChanged(); // Cập nhật trạng thái nút bấm trợ giúp

            // 2. TÍNH TOÁN TỈ LỆ THÀNH CÔNG ĐỘNG
            int currentLevel = CurrentQuestion.Level;
            string currentCategory = CurrentQuestion.Category;

            // Gọi hàm tính toán xác suất mà chúng ta đã xây dựng trong Model CallExpert
            double successRate = expert.CalculateSuccessRate(currentLevel, currentCategory);

            int roll = _random.Next(1, 101);
            int correctIndex = CurrentQuestion.CorrectAnswerIndex;
            int expertIndex;

            if (roll <= successRate)
            {
                // Chuyên gia trả lời ĐÚNG
                expertIndex = correctIndex;
            }
            else
            {
                // Chuyên gia trả lời SAI
                var incorrectIndices = CurrentQuestion.Options
                    .Select((_, index) => index)
                    .Where(index => index != correctIndex)
                    .ToList();

                // Lọc bỏ các đáp án đã bị 50:50 ẩn đi
                var availableIncorrectIndices = incorrectIndices
                    .Where(i => !CurrentQuestion.IsOptionHidden[i])
                    .ToList();

                if (!availableIncorrectIndices.Any())
                {
                    expertIndex = correctIndex;
                }
                else
                {
                    expertIndex = availableIncorrectIndices[_random.Next(availableIncorrectIndices.Count)];
                }
            }

            // 3. TẠO CÂU TRẢ LỜI CÁ TÍNH
            string answerLetter = ((char)('A' + expertIndex)).ToString();

            // Nếu đúng chuyên môn, nói tự tin hơn, hoặc là không?
            bool isStrength = expert.Strengths.Contains(currentCategory);
            if (isStrength)
            {
                expert.ExpertAnswer = $"{expert.PersonaStyle} chắc chắn là đáp án {answerLetter}! Đây là sở trường của tôi mà.";
            }
            else
            {
                expert.ExpertAnswer = $"Câu này không phải chuyên môn của tôi lắm... nhưng theo phán đoán thì có lẽ là {answerLetter}.";
            }

            await _audioService.PlayVAAsync("VA/Help/dunggoidienthoaichonguoithan.mp3");
            // 4. HIỆU ỨNG CHỜ (Simulate "Calling...") có thể thêm hiệu ứng âm thanh "Tút tút" ở đây
            await Task.Delay(2500);
            IsExpertAnswerVisible = true; // Hiển thị khung chat lời thoại của chuyên gia
            IsAnswerPhase = true;
        }

        [RelayCommand]
        private void CloseExpertAnswer()
        {
            IsExpertAnswerVisible = false;
            GameStatusMessage = "Bạn đã có lời khuyên. Hãy đưa ra quyết định cuối cùng.";
        }

        [RelayCommand(CanExecute = nameof(IsAudienceEnabled))]
        private async Task UseAudience()
        {
            if (CurrentQuestion == null || CurrentGameStatus.IsKhanGiaUsed) return;

            IsAnswerPhase = false;
            // Phát âm thanh khán giả (dùng PlaySFX vì đây là file SFX, không phải VA)
            _audioService.PlaySFX("SFX/Help/hoiykienkhangiatrongtruongquay.mp3");

            CurrentGameStatus.IsKhanGiaUsed = true;
            OnPropertyChanged(nameof(CurrentGameStatus));
            UseAudienceCommand.NotifyCanExecuteChanged();

            // 1. Khởi tạo kết quả và tính toán
            var finalResult = CalculateAudienceVotes(CurrentQuestion.CorrectAnswerIndex, CurrentQuestion.IsOptionHidden);

            // 2. Khởi tạo kết quả ban đầu là 0% cho hiệu ứng
            CurrentAudienceResult = new AudienceResult();
            foreach (var option in finalResult.Results)
            {
                CurrentAudienceResult.Results.Add(new AudienceOption
                {
                    OptionLetter = option.OptionLetter,
                    Percentage = 0
                });
            }

            // 3. Hiển thị Pop-up
            IsAudienceResultVisible = true;
            IsAudienceVotingCompleted = false;
            GameStatusMessage = "Khán giả đang suy nghĩ và bình chọn...";

            // Prepare Dummy Audience
            if (DummyAudiences.Count == 0)
            {
                for (int i = 0; i < 48; i++) // 6x8 grid for example
                {
                    DummyAudiences.Add(new DummyAudience { Id = i, IsVoting = false });
                }
            }
            else
            {
                foreach (var da in DummyAudiences) da.IsVoting = false;
            }

            // Giả lập khán giả đang bỏ phiếu (8 giây, cải thiện so với 15s cũ)
            // Dùng Task.Run để tránh block UI thread
            const int totalVotingMs = 8000;
            const int voteDelay = 120; // 120ms/frame ≈ 67 frames
            int totalFrames = totalVotingMs / voteDelay;
            var unvotedIndices = Enumerable.Range(0, 48).ToList();

            for (int f = 0; f < totalFrames; f++)
            {
                // Spread dummy voting across frames
                if (unvotedIndices.Count > 0 && f % 2 == 0)
                {
                    int numToVote = _random.Next(1, 3);
                    for (int v = 0; v < numToVote && unvotedIndices.Count > 0; v++)
                    {
                        int rndIdx = _random.Next(unvotedIndices.Count);
                        int daIdx = unvotedIndices[rndIdx];
                        DummyAudiences[daIdx].IsVoting = true;
                        unvotedIndices.RemoveAt(rndIdx);
                    }
                }

                // Ép hết số còn lại trước frame cuối
                if (f == totalFrames - 8 && unvotedIndices.Count > 0)
                {
                    foreach (var idx in unvotedIndices)
                        DummyAudiences[idx].IsVoting = true;
                    unvotedIndices.Clear();
                }

                await Task.Delay(voteDelay);
            }

            // Đặt tất cả về 0 trước khi chạy kết quả thực sự
            for (int j = 0; j < 4; j++)
            {
                CurrentAudienceResult.Results[j].Percentage = 0;
            }
            
            // Hiện chữ kết quả phần trăm
            IsAudienceVotingCompleted = true;

            // 4. Hiệu ứng chạy thanh phần trăm (Animation)
            int steps = 25; // 25 bước
            int delayPerStep = 60; // 60ms mỗi bước
            
            for (int i = 1; i <= steps; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    // Tăng dần phần trăm
                    double target = finalResult.Results[j].Percentage;
                    double current = (target / steps) * i;
                    
                    CurrentAudienceResult.Results[j].Percentage = Math.Round(current);
                }
                await Task.Delay(delayPerStep);
            }

            // Đảm bảo số cuối cùng chính xác
            for (int j = 0; j < 4; j++)
            {
                CurrentAudienceResult.Results[j].Percentage = finalResult.Results[j].Percentage;
            }

            IsAnswerPhase = true;
            GameStatusMessage = "Khán giả đã bỏ phiếu xong. Mời bạn xem kết quả.";
        }


        // HÀM TÍNH TOÁN PHIẾU BẦU (Core Logic)
        private AudienceResult CalculateAudienceVotes(int correctIndex, System.Collections.ObjectModel.ObservableCollection<bool> hiddenOptions)
        {
            int totalVotes = 100; // Tổng số phiếu là 100
            int questionLevel = CurrentGameStatus.CurrentQuestionIndex;
            var result = new AudienceResult();

            // 1. Xây dựng tham số độ khó: Tỷ lệ ủng hộ đúng cơ bản (giảm khi câu hỏi khó hơn)
            int baseCorrectRate = 90 - (questionLevel * 3);
            baseCorrectRate = Math.Max(baseCorrectRate, 40);

            // 2. Xử lý 50:50
            var availableIndices = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                if (!hiddenOptions[i]) availableIndices.Add(i);
            }

            // Nếu 50:50 đã được dùng, tăng độ chính xác còn lại
            if (availableIndices.Count == 2)
            {
                baseCorrectRate = Math.Min(baseCorrectRate + 20, 95); // Tăng khả năng đúng lên 95%
            }

            // 3. Phân bổ phiếu cho đáp án đúng
            int correctVotes = 0;
            if (availableIndices.Contains(correctIndex))
            {
                // Sử dụng một số ngẫu nhiên để tạo tính bất ngờ
                int minVotes = Math.Max(baseCorrectRate - 5, 10); // Đảm bảo ít nhất 10%
                int maxVotes = Math.Min(baseCorrectRate + 5, 100);
                correctVotes = _random.Next(minVotes, maxVotes + 1);
                correctVotes = Math.Min(correctVotes, totalVotes - (availableIndices.Count - 1)); // Giới hạn tổng số phiếu
                totalVotes -= correctVotes;
            }

            // 4. Phân bổ phiếu còn lại cho các đáp án sai
            var remainingIndices = availableIndices.Where(i => i != correctIndex).ToList();
            var votes = new int[4];
            votes[correctIndex] = correctVotes;

            // Phân bổ phần còn lại (totalVotes) cho các đáp án sai
            if (remainingIndices.Any())
            {
                int remainingVotes = totalVotes;

                // Phân bổ ngẫu nhiên phần còn lại, tránh quá chênh lệch
                for (int i = 0; i < remainingIndices.Count - 1; i++)
                {
                    int maxDistribute = remainingVotes / (remainingIndices.Count - i);
                    int currentVotes = _random.Next(1, Math.Max(2, maxDistribute + 1));
                    votes[remainingIndices[i]] = currentVotes;
                    remainingVotes -= currentVotes;
                }
                // Gán phần còn lại cho đáp án sai cuối cùng
                votes[remainingIndices.Last()] = remainingVotes;
            }

            // 5. Xây dựng kết quả
            for (int i = 0; i < 4; i++)
            {
                result.Results.Add(new AudienceOption
                {
                    OptionLetter = ((char)('A' + i)).ToString(),
                    Percentage = votes[i] // Vì totalVotes là 100, votes[i] cũng là %
                });
            }

            return result;
        }

        // Command đóng Pop-up kết quả Khán giả
        [RelayCommand]
        private void CloseAudienceResult()
        {
            IsAudienceResultVisible = false;
            GameStatusMessage = "Bạn đã có ý kiến từ khán giả. Hãy đưa ra quyết định cuối cùng.";
        }

        [RelayCommand(CanExecute = nameof(IsToTuVanEnabled))]
        private async Task UseConsultancy()
        {
            if (CurrentQuestion == null || CurrentGameStatus.IsToTuVanUsed) return;

            IsAnswerPhase = false;
            await _audioService.PlayVAAsync("VA/Help/dungtotuvantaicho.mp3");

            // 1. Đánh dấu quyền trợ giúp đã dùng
            CurrentGameStatus.IsToTuVanUsed = true;
            OnPropertyChanged(nameof(CurrentGameStatus));
            UseConsultancyCommand.NotifyCanExecuteChanged();

            // 2. Tính toán ý kiến cho từng Tư vấn viên
            foreach (var consultant in Consultants)
            {
                CalculateConsultantAnswer(consultant);
            }

            // 3. Hiển thị Pop-up
            IsConsultantResultVisible = true;
            IsAnswerPhase = true;

            GameStatusMessage = "Tổ tư vấn đã đưa ra ý kiến của mình.";
        }


        // HÀM TÍNH TOÁN Ý KIẾN RIÊNG LẺ
        private void CalculateConsultantAnswer(Consultant consultant)
        {
            int correctIndex = CurrentQuestion.CorrectAnswerIndex;
            int consultantIndex;

            // Tỷ lệ chuyên gia trả lời ĐÚNG
            int chance = _random.Next(1, 101);

            if (chance <= consultant.CorrectnessRate)
            {
                // Chuyên gia trả lời ĐÚNG
                consultantIndex = correctIndex;
            }
            else
            {
                // Chuyên gia trả lời SAI (chọn ngẫu nhiên 1 đáp án SAI)
                var incorrectIndices = CurrentQuestion.Options
                    .Select((_, index) => index)
                    .Where(index => index != correctIndex)
                    .ToList();

                // Lọc bỏ các đáp án đã bị 50:50 ẩn đi (nếu có)
                var availableIncorrectIndices = incorrectIndices
                    .Where(i => !CurrentQuestion.IsOptionHidden[i])
                    .ToList();

                // Nếu không còn đáp án sai khả dụng, trả lời đúng
                if (!availableIncorrectIndices.Any())
                {
                    consultantIndex = correctIndex;
                }
                else
                {
                    consultantIndex = availableIncorrectIndices[_random.Next(availableIncorrectIndices.Count)];
                }
            }

            // Gán kết quả
            string answerLetter = ((char)('A' + consultantIndex)).ToString();
            consultant.ConsultantAnswer = $"Tôi chọn đáp án {answerLetter}.";
            consultant.HasAnswered = true;
        }


        // Command đóng Pop-up kết quả Tổ Tư Vấn
        [RelayCommand]
        private void CloseConsultantResult()
        {
            IsConsultantResultVisible = false;
            GameStatusMessage = "Bạn đã có ý kiến từ tổ tư vấn. Hãy đưa ra quyết định cuối cùng.";
        }

        public async Task StartGameSequenceAsync()
        {
            // Nếu là Game mới
            if (CurrentGameStatus.CurrentQuestionIndex == 0)
            {
                IsAnswerPhase = false; // Khóa UI không cho bấm bậy bạ
                await _audioService.PlayVAAsync("VA/phobienluatchoi.mp3");

                await MoveToNextQuestionAsync(); // Bắt đầu đọc câu hỏi 1
            }
            else
            {
                // Nếu load từ save game, chỉ cần đọc câu hỏi hiện tại
                IsAnswerPhase = false;
                await MoveToNextQuestionAsync();
            }
        }

        public async Task CheckSaveGameStatus()
        {
            var savedGame = await _saveService.LoadGameAsync();
            // Nếu LoadGameAsync trả về null (vì không có file hoặc đã kết thúc)
            IsContinueGameAvailable = savedGame != null;
        }

        private async Task PlayResultSound(bool isCorrect)
        {
            int level = CurrentGameStatus.CurrentQuestionIndex + 1;
            string subFolder = "SFX/Answer/";

            if (level >= 6)
            {
                _audioService.StopBGM();
            }

            if (isCorrect)
            {
                if (level <= 4) _audioService.PlaySFX($"{subFolder}Tra_Loi_Dung_1_den_4.mp3");
                else if (level == 5 || level == 10) _audioService.PlaySFX($"{subFolder}Tra_Loi_Dung_Cau_5_Va_10.mp3");
                else if (level == 15) _audioService.PlaySFX($"{subFolder}Tra_Loi_Dung_Cau_15.mp3");
                else _audioService.PlaySFX($"{subFolder}Tra_Loi_Dung_Cau_6_Den_14.mp3");
            }
            else
            {
                if (level <= 5) _audioService.PlaySFX($"{subFolder}Tra_Loi_Sai_1_5.mp3");
                else if (level == 15) _audioService.PlaySFX($"{subFolder}Tra_Loi_Sai_Cau_15.mp3");
                else _audioService.PlaySFX($"{subFolder}Tra_Loi_Sai_Cau_6_Den_14.mp3");
            }
        }

        private void HandleBGM()
        {
            int currentQuestionNumber = CurrentGameStatus.CurrentQuestionIndex + 1;

            if (currentQuestionNumber <= 5)
            {
                // Giai đoạn 1-5: Nhạc chạy liên tục
                _audioService.PlayBGM("BGM/Q1_Q5.mp3");
            }
            else if (currentQuestionNumber <= 15)
            {
                // Giai đoạn 6-15: Phát lại từ đầu mỗi câu hỏi
                _audioService.StopBGM();
                _audioService.PlayBGM("BGM/Q6_Q15.mp3");
            }
        }
    }
}