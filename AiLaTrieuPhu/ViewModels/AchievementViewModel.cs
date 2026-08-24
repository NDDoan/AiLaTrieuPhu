using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using AiLaTrieuPhu.Models;
using AiLaTrieuPhu.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.ViewModels
{
    /// <summary>
    /// Kết hợp Achievement (metadata) và AchievementRecord (trạng thái người chơi)
    /// thành một ViewModel tiện dụng để hiển thị trong giao diện.
    /// </summary>
    public class AchievementDisplayItem : ObservableObject
    {
        private readonly Achievement _achievement;
        private readonly AchievementRecord _record;

        public string Id => _achievement.Id;
        public bool IsUnlocked => _record.IsUnlocked;
        public bool IsHidden => _achievement.IsHidden;

        /// <summary>Tên hiển thị: "???" nếu ẩn và chưa mở khóa.</summary>
        public string DisplayName => _achievement.GetDisplayName(_record.IsUnlocked);

        /// <summary>Mô tả hiển thị: ẩn nếu chưa mở khóa.</summary>
        public string DisplayDescription => _achievement.GetDisplayDescription(_record.IsUnlocked);

        /// <summary>Ngày giờ hoàn thành ở định dạng "dd/MM/yyyy HH:mm".</summary>
        public string UnlockedDateDisplay => _record.UnlockedDateDisplay;

        /// <summary>Đường dẫn icon.</summary>
        public string IconPath => _achievement.IconPath;

        /// <summary>Tiến độ hiện tại (ví dụ: 7).</summary>
        public int Progress => _record.Progress;

        /// <summary>Tiến độ tối đa (ví dụ: 15).</summary>
        public int MaxProgress => _record.MaxProgress;

        /// <summary>Có hiển thị thanh tiến trình không? (Chỉ với thành tựu cày cuốc).</summary>
        public bool HasProgressBar => _record.MaxProgress > 1 && !_record.IsUnlocked;

        public AchievementDisplayItem(Achievement achievement, AchievementRecord record)
        {
            _achievement = achievement;
            _record = record;
        }
    }

    /// <summary>
    /// ViewModel cho màn hình xem thành tựu.
    /// </summary>
    public partial class AchievementViewModel : ObservableObject
    {
        private readonly IAchievementService _achievementService;
        private readonly MainViewModel _mainNavigator;

        [ObservableProperty]
        private ObservableCollection<AchievementDisplayItem> _achievementItems = new();

        [ObservableProperty]
        private int _unlockedCount;

        [ObservableProperty]
        private int _totalCount;

        public AchievementViewModel(MainViewModel mainNavigator, IAchievementService achievementService)
        {
            _mainNavigator = mainNavigator;
            _achievementService = achievementService;
            LoadAchievements();
        }

        private void LoadAchievements()
        {
            AchievementItems.Clear();

            var allAch = _achievementService.AllAchievements;
            var allRec = _achievementService.AllRecords;
            var recordLookup = allRec.ToDictionary(r => r.AchievementId);

            foreach (var ach in allAch)
            {
                if (!recordLookup.TryGetValue(ach.Id, out var record))
                {
                    record = new AchievementRecord { AchievementId = ach.Id, MaxProgress = 1 };
                }
                AchievementItems.Add(new AchievementDisplayItem(ach, record));
            }

            UnlockedCount = AchievementItems.Count(i => i.IsUnlocked);
            TotalCount = AchievementItems.Count;
        }

        public void NavigateBack()
        {
            _mainNavigator.NavigateToMenu();
        }
    }
}
