using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging.Messages;

namespace AiLaTrieuPhu.Models
{
    public class GameStatusChangedMessage : ValueChangedMessage<bool>
    {
        public GameStatusChangedMessage(bool hasSaveFile) : base(hasSaveFile)
        {
        }
    }
}
