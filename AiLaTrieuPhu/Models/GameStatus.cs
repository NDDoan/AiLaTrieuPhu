using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiLaTrieuPhu.Models
{
    public class GameStatus
    {
        public int CurrentQuestionIndex { get; set; } = 0; // Index 0-14 (tương ứng câu 1-15)
        public long CurrentPrizeMoney { get; set; } = 0;
        public int SafePointReached { get; set; } = 0; // 0, 5, hoặc 10

        // Trạng thái quyền trợ giúp
        public bool Is5050Used { get; set; } = false;
        public bool IsCallingUsed { get; set; } = false;
        public bool IsKhanGiaUsed { get; set; } = false;
        public bool IsToTuVanUsed { get; set; } = false;
        public bool IsCurrentQuestionIndexUsed { get; set; } = false;

        // Toàn bộ 15 câu hỏi của phiên chơi này (rất quan trọng cho việc Save/Load)
        public List<Question> GameQuestions { get; set; } = new List<Question>();
    }
}
