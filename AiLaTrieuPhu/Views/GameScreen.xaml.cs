using System.Windows;
using System.Windows.Controls;
using AiLaTrieuPhu.ViewModels;

namespace AiLaTrieuPhu.Views
{
    /// <summary>
    /// Interaction logic for GameScreen.xaml.
    /// Chịu trách nhiệm bắt sự kiện cửa sổ (Alt-Tab) để phát hiện gian lận.
    /// </summary>
    public partial class GameScreen : UserControl
    {
        private Window? _parentWindow;

        public GameScreen()
        {
            InitializeComponent();
            Loaded += GameScreen_Loaded;
            Unloaded += GameScreen_Unloaded;
        }

        private void GameScreen_Loaded(object sender, RoutedEventArgs e)
        {
            _parentWindow = Window.GetWindow(this);
            if (_parentWindow != null)
                _parentWindow.Deactivated += ParentWindow_Deactivated;
        }

        private void GameScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_parentWindow != null)
                _parentWindow.Deactivated -= ParentWindow_Deactivated;
        }

        private void ParentWindow_Deactivated(object? sender, System.EventArgs e)
        {
            if (DataContext is GameViewModel vm)
                vm.OnWindowDeactivated();
        }
    }
}
