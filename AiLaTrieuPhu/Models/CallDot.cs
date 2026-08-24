using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    public partial class CallDot : ObservableObject
    {
        public double Angle { get; set; }

        [ObservableProperty]
        private bool _isActive = true;
    }
}
