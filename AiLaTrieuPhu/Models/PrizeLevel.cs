using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    // Kế thừa ObservableObject để UI tự động cập nhật khi trạng thái thay đổi
    public partial class PrizeLevel : ObservableObject
    {
        public int QuestionNumber { get; set; } // Số câu (1-15)
        public long PrizeAmount { get; set; }   // Giá trị tiền thưởng
        public bool IsSafePoint { get; set; }   // Có phải mốc an toàn (5, 10, 15) không

        [ObservableProperty]
        private bool _isAnswered; // Đã trả lời đúng câu này chưa (hiển thị ⋄)

        [ObservableProperty]
        private bool _isCurrent; // Có phải câu hỏi hiện tại đang chơi không (hiển thị ô màu xanh)
    }
}
