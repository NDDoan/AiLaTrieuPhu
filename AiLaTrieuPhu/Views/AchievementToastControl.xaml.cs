using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using AiLaTrieuPhu.Models;

namespace AiLaTrieuPhu.Views
{
    public partial class AchievementToastControl : UserControl
    {
        public AchievementToastControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.TryFindResource("SlideIn") is Storyboard sb)
                sb.Begin(this);
        }

        public void SetData(AchievementToastItem item)
        {
            ToastName.Text = item.Name;

            try
            {
                if (!string.IsNullOrEmpty(item.IconPath))
                {
                    string fullPath = System.IO.Path.Combine(AppContext.BaseDirectory, item.IconPath);
                    if (System.IO.File.Exists(fullPath))
                    {
                        ToastIcon.Source = new BitmapImage(new Uri(fullPath, UriKind.Absolute));
                    }
                }
            }
            catch { /* Icon không tìm thấy – bỏ qua */ }
        }

        public void PlaySlideOut(Action? onCompleted = null)
        {
            if (this.TryFindResource("SlideOut") is Storyboard sb)
            {
                sb = sb.Clone();
                sb.Completed += (_, _) => onCompleted?.Invoke();
                sb.Begin(this);
            }
            else
            {
                onCompleted?.Invoke();
            }
        }
    }
}
