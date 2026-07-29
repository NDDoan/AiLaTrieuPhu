using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    // Dữ liệu cho từng cột (A, B, C, D)
    public partial class AudienceOption : ObservableObject
    {
        public string OptionLetter { get; set; } // A, B, C, D
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DisplayHeight))]
        private double _percentage; // Tỷ lệ phần trăm (0-100)
        
        // Nhân tỷ lệ để thanh có chiều cao hiển thị rõ hơn (tối đa 250px thay vì 100px)
        public double DisplayHeight => Percentage * 2.5;
    }

    public partial class AudienceResult : ObservableObject
    {
        // Danh sách kết quả cho 4 đáp án
        public ObservableCollection<AudienceOption> Results { get; set; } = new ObservableCollection<AudienceOption>();
    }
}
