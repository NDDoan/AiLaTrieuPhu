using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    public partial class Consultant : ObservableObject
    {
        public string Name { get; set; }

        // Tỷ lệ phần trăm người này đưa ra đáp án ĐÚNG (nên thấp hơn CallExpert)
        public int CorrectnessRate { get; set; }

        // Đáp án mà chuyên gia đưa ra (A, B, C, hoặc D)
        [ObservableProperty]
        private string _consultantAnswer = "Chưa có ý kiến...";

        // Cờ để ẩn/hiện ý kiến (nếu cần)
        [ObservableProperty]
        private bool _hasAnswered = false;
    }
}
