using System.Text.Json.Serialization;

namespace AiLaTrieuPhu.Models
{
    /// <summary>
    /// Dữ liệu cày cuốc tích lũy của người chơi, lưu qua các phiên chơi.
    /// </summary>
    public class PlayerProfile
    {
        /// <summary>Tổng số lần thắng cuộc chơi (trả lời đúng câu 15).</summary>
        [JsonPropertyName("totalWins")]
        public int TotalWins { get; set; } = 0;

        /// <summary>Chuỗi thắng liên tiếp hiện tại (không gian lận). Reset về 0 khi thua hoặc từ bỏ.</summary>
        [JsonPropertyName("currentWinStreak")]
        public int CurrentWinStreak { get; set; } = 0;

        /// <summary>Chuỗi thắng liên tiếp cao nhất từ trước đến nay (không gian lận).</summary>
        [JsonPropertyName("maxWinStreak")]
        public int MaxWinStreak { get; set; } = 0;

        /// <summary>Thời gian chiến thắng nhanh nhất (giây). Null nếu chưa từng thắng hợp lệ.</summary>
        [JsonPropertyName("bestWinTimeSeconds")]
        public double? BestWinTimeSeconds { get; set; } = null;
    }
}
