namespace AiLaTrieuPhu.Models
{
    /// <summary>
    /// Snapshot trạng thái game được truyền vào AchievementService để kiểm tra điều kiện thành tựu.
    /// Tránh việc AchievementService phụ thuộc trực tiếp vào GameViewModel.
    /// </summary>
    public class AchievementContext
    {
        // ── Thông tin câu hỏi ──────────────────────────────────────────────

        /// <summary>Số câu hỏi hiện tại (1-15) khi sự kiện xảy ra.</summary>
        public int QuestionNumber { get; set; }

        /// <summary>Người chơi đã trả lời đúng không?</summary>
        public bool IsAnswerCorrect { get; set; }

        /// <summary>Người chơi đã chiến thắng (trả lời đúng câu 15)?</summary>
        public bool IsWin { get; set; }

        /// <summary>Người chơi đã tự nguyện từ bỏ?</summary>
        public bool IsVoluntaryQuit { get; set; }

        // ── Thông tin quyền trợ giúp ───────────────────────────────────────

        public bool Is5050Used { get; set; }
        public bool IsAudienceUsed { get; set; }
        public bool IsCallUsed { get; set; }
        public bool IsConsultancyUsed { get; set; }

        /// <summary>Câu hỏi mà quyền Hỏi Khán giả được dùng (0 = chưa dùng).</summary>
        public int AudienceUsedAtQuestion { get; set; }

        /// <summary>Câu hỏi mà quyền Gọi Điện được dùng (0 = chưa dùng).</summary>
        public int CallUsedAtQuestion { get; set; }

        /// <summary>Câu hỏi mà quyền Tổ Tư Vấn được dùng (0 = chưa dùng).</summary>
        public int ConsultancyUsedAtQuestion { get; set; }

        /// <summary>Câu hỏi mà quyền 50/50 được dùng (0 = chưa dùng).</summary>
        public int FiftyFiftyUsedAtQuestion { get; set; }

        // ── Chống gian lận ─────────────────────────────────────────────────

        /// <summary>
        /// True nếu game phát hiện dấu hiệu gian lận trong ván chơi này
        /// (Alt-Tab, không fullscreen, ...). Khóa các thành tựu liên quan đến thời gian và streak.
        /// </summary>
        public bool IsCheated { get; set; }

        // ── Thời gian ──────────────────────────────────────────────────────

        /// <summary>Tổng thời gian trả lời (chỉ tính khi câu hỏi đang hiện). Đơn vị: giây.</summary>
        public double TotalAnswerTimeSeconds { get; set; }

        // ── Dữ liệu Profile tích lũy (sau khi đã cập nhật) ────────────────

        /// <summary>Tổng số lần thắng (kể cả ván này, nếu IsWin = true).</summary>
        public int TotalWins { get; set; }

        /// <summary>Chuỗi thắng liên tiếp hiện tại (kể cả ván này).</summary>
        public int CurrentWinStreak { get; set; }
    }
}
