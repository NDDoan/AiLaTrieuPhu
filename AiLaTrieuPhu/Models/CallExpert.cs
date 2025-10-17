using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    public partial class CallExpert : ObservableObject
    {
        // Tên hiển thị của người được gọi
        public string Name { get; set; }

        // Chuyên môn (ví dụ: Lịch sử, Văn học, Bạn bè)
        public string Specialty { get; set; }

        // Tỷ lệ phần trăm người này sẽ đưa ra đáp án ĐÚNG (0 đến 100)
        public int CorrectnessRate { get; set; }

        // Đáp án mà chuyên gia đưa ra (A, B, C, hoặc D)
        [ObservableProperty]
        private string _expertAnswer = "Đang chờ tư vấn...";
    }
}
