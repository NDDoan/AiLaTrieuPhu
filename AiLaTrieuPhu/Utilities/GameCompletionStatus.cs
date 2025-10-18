using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiLaTrieuPhu.Utilities
{
    public enum GameCompletionStatus
    {
        InProgress, // Đang chơi, có thể tiếp tục
        QuitAndKeptMoney, // Đã Từ Bỏ, tiền thưởng được bảo toàn
        Lost // Đã thua
    }
}
