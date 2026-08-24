using System.Collections.Generic;
using System.Threading.Tasks;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Services
{
    public interface IAchievementService
    {
        /// <summary>Danh sách tất cả thành tựu (metadata tĩnh).</summary>
        IReadOnlyList<Achievement> AllAchievements { get; }

        /// <summary>Lấy bản ghi (record) mở khóa của tất cả thành tựu.</summary>
        IReadOnlyList<AchievementRecord> AllRecords { get; }

        /// <summary>Tải dữ liệu thành tựu từ file lưu trữ.</summary>
        Task LoadAsync();

        /// <summary>
        /// Kiểm tra tất cả điều kiện thành tựu dựa trên context.
        /// Trả về danh sách thành tựu vừa được mở khóa (để hiện toast).
        /// </summary>
        Task<List<Achievement>> EvaluateAsync(AchievementContext context);

        /// <summary>Lấy record của một thành tựu cụ thể theo Id.</summary>
        AchievementRecord? GetRecord(string achievementId);
    }
}
