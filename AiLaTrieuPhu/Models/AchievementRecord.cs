using System;
using System.Text.Json.Serialization;

namespace AiLaTrieuPhu.Models
{
    /// <summary>
    /// Lưu trạng thái mở khóa thành tựu của người chơi. Được serialize ra file JSON.
    /// </summary>
    public class AchievementRecord
    {
        [JsonPropertyName("achievementId")]
        public string AchievementId { get; set; } = string.Empty;

        [JsonPropertyName("isUnlocked")]
        public bool IsUnlocked { get; set; } = false;

        /// <summary>Ngày giờ mở khóa (null nếu chưa mở).</summary>
        [JsonPropertyName("unlockedDate")]
        public DateTime? UnlockedDate { get; set; } = null;

        /// <summary>Tiến độ hiện tại (cho các thành tựu có thanh tiến trình). VD: 7/15.</summary>
        [JsonPropertyName("progress")]
        public int Progress { get; set; } = 0;

        /// <summary>Giá trị tối đa cần đạt (set bởi AchievementService khi khởi tạo).</summary>
        [JsonPropertyName("maxProgress")]
        public int MaxProgress { get; set; } = 1;

        /// <summary>Tiện ích: hiển thị ngày giờ theo định dạng "dd/MM/yyyy HH:mm".</summary>
        [JsonIgnore]
        public string UnlockedDateDisplay
            => UnlockedDate.HasValue
                ? UnlockedDate.Value.ToString("dd/MM/yyyy HH:mm")
                : string.Empty;
    }
}
