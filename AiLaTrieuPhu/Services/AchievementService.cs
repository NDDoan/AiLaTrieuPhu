using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    /// <summary>
    /// Quản lý toàn bộ logic thành tựu: Định nghĩa, lưu/tải, và đánh giá điều kiện.
    /// </summary>
    public class AchievementService : IAchievementService
    {
        private static readonly string SaveFilePath =
            Path.Combine(AppContext.BaseDirectory, "Assets", "Saves", "achievements.json");

        // ── Danh sách thành tựu định nghĩa cứng ──────────────────────────────
        private readonly List<Achievement> _achievements;
        private readonly Dictionary<string, AchievementRecord> _records = new();

        public IReadOnlyList<Achievement> AllAchievements => _achievements;
        public IReadOnlyList<AchievementRecord> AllRecords => _records.Values.ToList();

        public AchievementService()
        {
            _achievements = BuildAchievementDefinitions();
        }

        // ── Tải / Lưu ────────────────────────────────────────────────────────

        public async Task LoadAsync()
        {
            // Khởi tạo tất cả record với giá trị mặc định
            foreach (var ach in _achievements)
            {
                _records[ach.Id] = new AchievementRecord
                {
                    AchievementId = ach.Id,
                    MaxProgress = GetMaxProgress(ach.Id)
                };
            }

            if (!File.Exists(SaveFilePath)) return;

            try
            {
                byte[] data = await File.ReadAllBytesAsync(SaveFilePath);
                string json = System.Text.Encoding.UTF8.GetString(data);
                var loaded = JsonSerializer.Deserialize<List<AchievementRecord>>(json);
                if (loaded == null) return;

                foreach (var record in loaded)
                {
                    if (_records.ContainsKey(record.AchievementId))
                    {
                        _records[record.AchievementId] = record;
                        // Đảm bảo MaxProgress không bị mất khi load
                        _records[record.AchievementId].MaxProgress = GetMaxProgress(record.AchievementId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AchievementService] Lỗi tải thành tựu: {ex.Message}");
            }
        }

        private async Task SaveAsync()
        {
            string dir = Path.GetDirectoryName(SaveFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_records.Values.ToList(), options);
            await File.WriteAllTextAsync(SaveFilePath, json);
        }

        // ── Đánh giá điều kiện ───────────────────────────────────────────────

        public async Task<List<Achievement>> EvaluateAsync(AchievementContext ctx)
        {
            var newlyUnlocked = new List<Achievement>();
            bool progressChanged = false;

            void Check(string id, bool condition)
            {
                if (!condition) return;
                if (!_records.TryGetValue(id, out var record)) return;
                if (record.IsUnlocked) return;
                record.IsUnlocked = true;
                record.Progress = record.MaxProgress; // Đảm bảo full thanh
                record.UnlockedDate = DateTime.Now;
                newlyUnlocked.Add(_achievements.First(a => a.Id == id));
            }

            void UpdateProgress(string id, int currentProgress)
            {
                if (!_records.TryGetValue(id, out var record)) return;
                if (record.IsUnlocked) return;
                
                int newProgress = Math.Min(currentProgress, record.MaxProgress);
                if (record.Progress != newProgress)
                {
                    record.Progress = newProgress;
                    progressChanged = true;
                }
            }

            // ── Nhóm: Trả lời đúng từng câu (câu 1 → 15) ───────────────────
            if (ctx.IsAnswerCorrect)
            {
                Check($"ACH_Q{ctx.QuestionNumber}_CORRECT", true);
            }

            // ── Nhóm: Thất bại ───────────────────────────────────────────────
            if (!ctx.IsAnswerCorrect && !ctx.IsVoluntaryQuit)
            {
                if (ctx.QuestionNumber == 1)
                    Check("ACH_LOSE_Q1", true);       // Bạn không bằng con gà

                if (ctx.QuestionNumber <= 5)
                    Check("ACH_LOSE_ZERO", true);      // Tay trắng

                if (ctx.QuestionNumber <= 10)
                    Check("ACH_LOSE_MIDGAME", true);   // Mất thời gian

                if (ctx.QuestionNumber == 15)
                    Check("ACH_LOSE_FINAL", true);     // Không thành triệu phú
            }

            // ── Nhóm: Từ bỏ ─────────────────────────────────────────────────
            if (ctx.IsVoluntaryQuit)
            {
                if (ctx.QuestionNumber >= 2)
                    Check("ACH_QUIT_SAFE", true);      // Tiền vẫn an toàn

                if (ctx.QuestionNumber == 1)
                    Check("ACH_QUIT_Q1", true);        // Bro, đến đây làm gì? (ẩn)
            }

            // ── Nhóm: Thắng / Cày cuốc ──────────────────────────────────────
            if (ctx.IsWin)
            {
                Check("ACH_WIN_FIRST", true);          // Trở thành triệu phú

                UpdateProgress("ACH_WIN_10", ctx.TotalWins);
                if (ctx.TotalWins >= 10)
                    Check("ACH_WIN_10", true);         // Triệu phú 10 lần

                UpdateProgress("ACH_WIN_100", ctx.TotalWins);
                if (ctx.TotalWins >= 100)
                    Check("ACH_WIN_100", true);        // Triệu phú 100 lần

                UpdateProgress("ACH_WIN_1000", ctx.TotalWins);
                if (ctx.TotalWins >= 1000)
                    Check("ACH_WIN_1000", true);       // Đừng gọi tôi là Triệu phú, mà là Tỷ phú

                // Thắng bằng thực lực (không dùng bất kỳ quyền trợ giúp nào)
                if (!ctx.Is5050Used && !ctx.IsAudienceUsed && !ctx.IsCallUsed && !ctx.IsConsultancyUsed)
                    Check("ACH_WIN_NO_HELP", true);    // Triệu phú bằng thực lực

                // Triệu phú bằng ăn may (ẩn): Dùng 3 quyền trong 5 câu đầu, quyền còn lại ở câu 6
                bool luckyCondition = IsLuckyWinCondition(ctx);
                Check("ACH_WIN_LUCKY", luckyCondition); // ẩn

                // Thành tựu tốc độ và streak (chỉ khi không gian lận)
                if (!ctx.IsCheated)
                {
                    // Xem Dream quá 180 phút: Thắng trong < 300 giây (5 phút)
                    Check("ACH_SPEEDRUN_5MIN", ctx.TotalAnswerTimeSeconds < 300.0);

                    // Phá vỡ giới hạn: Thắng với thời gian thấp hơn kỷ lục cũ (ẩn)
                    var speedRecord = GetRecord("ACH_SPEEDRUN_5MIN");
                    var prevBest = GetRecord("ACH_SPEEDRUN_BEST");
                    bool hasPrevRecord = prevBest?.IsUnlocked == true;
                    // Lưu ý: để so sánh với best time ta dùng PlayerProfile, ở đây check theo context
                    // Thành tựu này chỉ cần thắng và nhanh hơn mốc 5 phút (nếu đã có ACH_SPEEDRUN_5MIN)
                    if (speedRecord?.IsUnlocked == true && ctx.TotalAnswerTimeSeconds > 0)
                        Check("ACH_SPEEDRUN_BEST", true); // ẩn - điều kiện chi tiết xử lý trong GameViewModel

                    // Triệu phú trong tầm tay: Thắng 5 lần liên tiếp
                    UpdateProgress("ACH_STREAK_5", ctx.CurrentWinStreak);
                    if (ctx.CurrentWinStreak >= 5)
                        Check("ACH_STREAK_5", true);

                    // Triệu phú thật sự trong tầm tay: Thắng 10 lần liên tiếp (ẩn)
                    UpdateProgress("ACH_STREAK_10", ctx.CurrentWinStreak);
                    if (ctx.CurrentWinStreak >= 10)
                        Check("ACH_STREAK_10", true);
                }
            }

            // Streak bị reset khi thua hoặc từ bỏ (cập nhật UI về 0)
            if (!ctx.IsWin)
            {
                UpdateProgress("ACH_STREAK_5", 0);
                UpdateProgress("ACH_STREAK_10", 0);
            }

            // ── Thành tựu ẩn đặc biệt: Bro là Nguyễn Lê Anh ────────────────
            if (ctx.IsVoluntaryQuit && ctx.QuestionNumber == 15)
            {
                bool isNLA = ctx.AudienceUsedAtQuestion == 6
                          && ctx.ConsultancyUsedAtQuestion == 10
                          && ctx.CallUsedAtQuestion == 10
                          && ctx.FiftyFiftyUsedAtQuestion == 12;
                Check("ACH_HIDDEN_NLA", isNLA);
            }

            if (newlyUnlocked.Count > 0 || progressChanged)
                await SaveAsync();

            return newlyUnlocked;
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public AchievementRecord? GetRecord(string achievementId)
            => _records.TryGetValue(achievementId, out var r) ? r : null;

        /// <summary>
        /// Triệu phú bằng ăn may:
        /// - Dùng quyền Khán giả (Câu ≤5) + Tổ tư vấn (Câu ≤5) + Gọi điện (Câu ≤5) trong 5 câu đầu
        /// - Dùng 50/50 ở câu 6
        /// </summary>
        private static bool IsLuckyWinCondition(AchievementContext ctx)
        {
            if (!ctx.IsWin) return false;
            bool audienceInFirst5 = ctx.AudienceUsedAtQuestion >= 1 && ctx.AudienceUsedAtQuestion <= 5;
            bool callInFirst5 = ctx.CallUsedAtQuestion >= 1 && ctx.CallUsedAtQuestion <= 5;
            bool consultInFirst5 = ctx.ConsultancyUsedAtQuestion >= 1 && ctx.ConsultancyUsedAtQuestion <= 5;
            bool fiftyFiftyAtQ6 = ctx.FiftyFiftyUsedAtQuestion == 6;
            return audienceInFirst5 && callInFirst5 && consultInFirst5 && fiftyFiftyAtQ6;
        }

        private static int GetMaxProgress(string id) => id switch
        {
            "ACH_WIN_10" => 10,
            "ACH_WIN_100" => 100,
            "ACH_WIN_1000" => 1000,
            "ACH_STREAK_5" => 5,
            "ACH_STREAK_10" => 10,
            _ => 1
        };

        // ── Định nghĩa tất cả thành tựu ──────────────────────────────────────

        private static List<Achievement> BuildAchievementDefinitions()
        {
            var list = new List<Achievement>();

            // Nhóm: Trả lời đúng từng câu
            for (int q = 1; q <= 15; q++)
            {
                list.Add(new Achievement
                {
                    Id = $"ACH_Q{q}_CORRECT",
                    Name = $"Trả lời đúng câu số {q}",
                    Description = $"Trả lời chính xác câu hỏi số {q} trong một ván chơi.",
                    IsHidden = false,
                    IconPath = $"Assets/Achievements/ach_q{q}.png"
                });
            }

            // Nhóm: Thất bại
            list.Add(new Achievement
            {
                Id = "ACH_LOSE_Q1",
                Name = "Bạn không bằng con gà",
                Description = "Trả lời sai ngay câu hỏi đầu tiên.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_lose_q1.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_LOSE_ZERO",
                Name = "Tay trắng",
                Description = "Trả lời sai một câu trong khoảng câu 1 đến câu 5, ra về tay không.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_lose_zero.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_LOSE_MIDGAME",
                Name = "Mất thời gian",
                Description = "Trả lời sai một câu trong khoảng câu 6 đến câu 10.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_lose_midgame.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_LOSE_FINAL",
                Name = "Không thành triệu phú",
                Description = "Trả lời sai câu hỏi số 15 - câu hỏi cuối cùng.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_lose_final.png"
            });

            // Nhóm: Từ bỏ
            list.Add(new Achievement
            {
                Id = "ACH_QUIT_SAFE",
                Name = "Tiền vẫn an toàn",
                Description = "Tự nguyện dừng cuộc chơi từ câu số 2 trở đi.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_quit_safe.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_QUIT_Q1",
                Name = "Bro, đến đây làm gì?",
                Description = "Tự nguyện dừng cuộc chơi ngay câu số 1.",
                IsHidden = true,
                IconPath = "Assets/Achievements/ach_quit_q1.png"
            });

            // Nhóm: Thắng / Cày cuốc
            list.Add(new Achievement
            {
                Id = "ACH_WIN_FIRST",
                Name = "Trở thành triệu phú",
                Description = "Lần đầu tiên trả lời đúng tất cả 15 câu và trở thành Triệu Phú!",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_win_first.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_WIN_10",
                Name = "Triệu phú 10 lần",
                Description = "Thắng cuộc chơi lần thứ 10.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_win_10.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_WIN_100",
                Name = "Triệu phú 100 lần",
                Description = "Thắng cuộc chơi lần thứ 100.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_win_100.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_WIN_1000",
                Name = "Đừng gọi tôi là Triệu phú, mà là Tỷ phú",
                Description = "Thắng cuộc chơi lần thứ 1000.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_win_1000.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_WIN_NO_HELP",
                Name = "Triệu phú bằng thực lực",
                Description = "Chiến thắng mà không sử dụng bất kỳ quyền trợ giúp nào.",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_win_no_help.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_WIN_LUCKY",
                Name = "Triệu phú bằng ăn may",
                Description = "Dùng hết 3 quyền trợ giúp trong 5 câu đầu, rồi dùng 50/50 ở câu 6 và vẫn chiến thắng.",
                IsHidden = true,
                IconPath = "Assets/Achievements/ach_win_lucky.png"
            });

            // Nhóm: Thành tựu ẩn đặc biệt
            list.Add(new Achievement
            {
                Id = "ACH_HIDDEN_NLA",
                Name = "Bro là Nguyễn Lê Anh",
                Description = "Dùng Hỏi khán giả ở câu 6, Tổ tư vấn + Gọi điện ở câu 10, 50/50 ở câu 12, rồi từ bỏ ở câu 15.",
                IsHidden = true,
                IconPath = "Assets/Achievements/ach_nla.png"
            });

            // Nhóm: Tốc độ & chống gian lận
            list.Add(new Achievement
            {
                Id = "ACH_SPEEDRUN_5MIN",
                Name = "Xem Dream quá 180 phút",
                Description = "Chiến thắng với tổng thời gian trả lời dưới 5 phút (Không thể thực hiện nếu gian lận).",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_speedrun_5min.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_SPEEDRUN_BEST",
                Name = "Phá vỡ giới hạn",
                Description = "Chiến thắng với thời gian thấp hơn kỷ lục của chính mình (Không thể thực hiện nếu gian lận).",
                IsHidden = true,
                IconPath = "Assets/Achievements/ach_speedrun_best.png"
            });

            // Nhóm: Streak thắng liên tiếp
            list.Add(new Achievement
            {
                Id = "ACH_STREAK_5",
                Name = "Triệu phú trong tầm tay",
                Description = "Chiến thắng 5 lần liên tiếp (Không thể thực hiện nếu gian lận).",
                IsHidden = false,
                IconPath = "Assets/Achievements/ach_streak_5.png"
            });
            list.Add(new Achievement
            {
                Id = "ACH_STREAK_10",
                Name = "Triệu phú thật sự trong tầm tay",
                Description = "Chiến thắng 10 lần liên tiếp (Không thể thực hiện nếu gian lận).",
                IsHidden = true,
                IconPath = "Assets/Achievements/ach_streak_10.png"
            });

            return list;
        }
    }
}
