using System.Windows.Controls;
using AiLaTrieuPhu.ViewModels;

namespace AiLaTrieuPhu.Views
{
    public partial class AchievementScreen : UserControl
    {
        public AchievementScreen()
        {
            InitializeComponent();
        }

        private void BackButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is AchievementViewModel vm)
                vm.NavigateBack();
        }
    }
}
