using CommunityToolkit.Mvvm.ComponentModel;

namespace AiLaTrieuPhu.Models
{
    public partial class DummyAudience : ObservableObject
    {
        public int Id { get; set; }
        
        [ObservableProperty]
        private bool _isVoting;
    }
}
