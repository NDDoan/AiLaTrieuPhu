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

        // Thế mạnh của chuyên gia
        public List<string> Strengths { get; set; } = new List<string>();

        // Câu thoại cá tính của chuyên gia
        public string PersonaStyle { get; set; }

        // Đáp án mà chuyên gia đưa ra (A, B, C, hoặc D)
        [ObservableProperty]
        private string _expertAnswer = "Đang chờ tư vấn...";

        // Logic tính xác suất đúng
        public double CalculateSuccessRate(int level, string category)
        {
            // 1. Tỉ lệ mặc định dựa trên level
            double baseRate = level <= 5 ? 67 : (level <= 10 ? 60 : 50);

            // 2. Thưởng điểm chuyên môn
            double bonus = 0;
            if (Strengths.Contains(category))
            {
                bonus = level <= 5 ? 23 : 20;
            }

            return baseRate + bonus;
        }
    }
}
