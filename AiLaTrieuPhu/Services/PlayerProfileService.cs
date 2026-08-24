using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    /// <summary>
    /// Lưu và tải dữ liệu cày cuốc tích lũy của người chơi (PlayerProfile).
    /// </summary>
    public class PlayerProfileService
    {
        private static readonly string SaveFilePath =
            Path.Combine(AppContext.BaseDirectory, "Assets", "Saves", "player_profile.json");

        private PlayerProfile _profile = new PlayerProfile();

        public PlayerProfile Profile => _profile;

        public async Task LoadAsync()
        {
            if (!File.Exists(SaveFilePath))
            {
                _profile = new PlayerProfile();
                return;
            }
            try
            {
                string json = await File.ReadAllTextAsync(SaveFilePath);
                _profile = JsonSerializer.Deserialize<PlayerProfile>(json) ?? new PlayerProfile();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[PlayerProfileService] Lỗi tải profile: {ex.Message}");
                _profile = new PlayerProfile();
            }
        }

        public async Task SaveAsync()
        {
            string dir = Path.GetDirectoryName(SaveFilePath)!;
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_profile, options);
            await File.WriteAllTextAsync(SaveFilePath, json);
        }

        /// <summary>
        /// Cập nhật profile sau khi ván chơi kết thúc và lưu lại.
        /// </summary>
        /// <param name="isWin">Ván chơi có phải chiến thắng hợp lệ không.</param>
        /// <param name="isCheated">Ván chơi có dấu hiệu gian lận không.</param>
        /// <param name="totalAnswerTimeSeconds">Tổng thời gian trả lời (giây). Chỉ dùng khi thắng hợp lệ.</param>
        public async Task<PlayerProfile> UpdateAfterGameAsync(bool isWin, bool isCheated, double totalAnswerTimeSeconds = 0)
        {
            if (isWin)
            {
                _profile.TotalWins++;

                if (!isCheated)
                {
                    _profile.CurrentWinStreak++;
                    if (_profile.CurrentWinStreak > _profile.MaxWinStreak)
                        _profile.MaxWinStreak = _profile.CurrentWinStreak;

                    // Cập nhật best time
                    if (totalAnswerTimeSeconds > 0)
                    {
                        if (!_profile.BestWinTimeSeconds.HasValue || totalAnswerTimeSeconds < _profile.BestWinTimeSeconds.Value)
                            _profile.BestWinTimeSeconds = totalAnswerTimeSeconds;
                    }
                }
                // Nếu gian lận: TotalWins vẫn tăng nhưng streak không tính
            }
            else
            {
                // Thua hoặc từ bỏ: reset streak
                if (!isCheated)
                    _profile.CurrentWinStreak = 0;
            }

            await SaveAsync();
            return _profile;
        }
    }
}
