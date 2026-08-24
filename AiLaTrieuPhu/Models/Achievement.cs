using System;

namespace AiLaTrieuPhu.Models
{
    /// <summary>
    /// Dữ liệu tĩnh (Metadata) của một thành tựu. Không thay đổi theo người chơi.
    /// </summary>
    public class Achievement
    {
        /// <summary>Mã định danh duy nhất. VD: "ACH_Q1_CORRECT"</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>Tên thành tựu hiển thị cho người chơi.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Mô tả điều kiện hoàn thành.</summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Nếu true, Name và Description sẽ hiển thị là "???" cho đến khi được mở khóa.
        /// </summary>
        public bool IsHidden { get; set; } = false;

        /// <summary>Đường dẫn tương đối tới file icon. VD: "Assets/Achievements/icon_q1.png"</summary>
        public string IconPath { get; set; } = string.Empty;

        /// <summary>Tên hiển thị: ẩn nếu IsHidden và chưa mở khóa.</summary>
        public string GetDisplayName(bool isUnlocked)
            => IsHidden && !isUnlocked ? "???" : Name;

        /// <summary>Mô tả hiển thị: ẩn nếu IsHidden và chưa mở khóa.</summary>
        public string GetDisplayDescription(bool isUnlocked)
            => IsHidden && !isUnlocked ? "Hoàn thành điều kiện bí ẩn để mở khóa." : Description;
    }
}
