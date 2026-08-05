using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace AiLaTrieuPhu.Views
{
    public partial class FireworkEffectControl : UserControl
    {
        // ─── Dependency Property: IsActive ───────────────────────────────────────
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(
                nameof(IsActive),
                typeof(bool),
                typeof(FireworkEffectControl),
                new PropertyMetadata(false, OnIsActiveChanged));

        public bool IsActive
        {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        // ─── Constants ───────────────────────────────────────────────────────────
        /// <summary>Số ms giữa mỗi khung hình nổ (30 FPS ≈ 33ms)</summary>
        private const int ExplosionFrameMs = 33;

        /// <summary>Số ms giữa mỗi lần sinh pháo mới</summary>
        private const int SpawnIntervalMs = 600;

        /// <summary>Thời gian (ms) pháo bay từ đất lên điểm nổ</summary>
        private const int RisingDurationMs = 700;

        /// <summary>Kích thước hiển thị sprite (px) lúc bay lên</summary>
        private const double RisingSize = 24;

        /// <summary>Kích thước hiển thị sprite (px) khi nổ</summary>
        private const double ExplosionSize = 180;

        // ─── Fields ──────────────────────────────────────────────────────────────
        private static readonly BitmapImage[] Sprites = new BitmapImage[36];
        private static bool _spritesLoaded;

        private readonly DispatcherTimer _spawner = new();
        private readonly Random _rng = new();

        // ─── Constructor ─────────────────────────────────────────────────────────
        public FireworkEffectControl()
        {
            InitializeComponent();
            EnsureSpritesLoaded();

            _spawner.Interval = TimeSpan.FromMilliseconds(SpawnIntervalMs);
            _spawner.Tick += OnSpawnTick;

            Unloaded += (_, _) => _spawner.Stop();
        }

        // ─── Sprite Preloader ─────────────────────────────────────────────────────
        private static void EnsureSpritesLoaded()
        {
            if (_spritesLoaded) return;
            for (int i = 0; i < 36; i++)
            {
                var uri = new Uri(
                    $"pack://application:,,,/AiLaTrieuPhu;component/Assets/Image/phao_hoa/fireworks_{i:D2}.png",
                    UriKind.Absolute);
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze(); // thread-safe
                Sprites[i] = bmp;
            }
            _spritesLoaded = true;
        }

        // ─── IsActive callback ────────────────────────────────────────────────────
        private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var ctrl = (FireworkEffectControl)d;
            if ((bool)e.NewValue)
                ctrl._spawner.Start();
            else
            {
                ctrl._spawner.Stop();
                ctrl.FireworkCanvas.Children.Clear();
            }
        }

        // ─── Spawn Loop ───────────────────────────────────────────────────────────
        private void OnSpawnTick(object? sender, EventArgs e)
        {
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            LaunchFirework();
        }

        // ─── Core: Launch one firework ────────────────────────────────────────────
        private void LaunchFirework()
        {
            // ── Pick random horizontal position (avoid edges) ──
            double targetX = _rng.NextDouble() * (ActualWidth - ExplosionSize) + ExplosionSize / 2;

            // ── Pick random explosion height (top 10%–55% of screen) ──
            double targetY = ActualHeight * (_rng.NextDouble() * 0.45 + 0.05);

            // ── Create Image element starting at bottom-center ──
            var img = new Image
            {
                Width = RisingSize,
                Height = RisingSize,
                Source = Sprites[0],
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = new ScaleTransform(1, 1)
            };

            double startX = targetX - RisingSize / 2;
            double startY = ActualHeight;

            Canvas.SetLeft(img, startX);
            Canvas.SetTop(img, startY);
            FireworkCanvas.Children.Add(img);

            // ── Animate: fly from bottom (startY) up to targetY ──
            var riseAnim = new DoubleAnimation
            {
                From = startY,
                To = targetY,
                Duration = TimeSpan.FromMilliseconds(RisingDurationMs),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            riseAnim.Completed += (_, _) => StartExplosion(img, targetX, targetY);
            img.BeginAnimation(Canvas.TopProperty, riseAnim);
        }

        // ─── Core: Start explosion sprite sequence ────────────────────────────────
        private void StartExplosion(Image img, double centerX, double centerY)
        {
            // Resize to explosion size, center on explosion point
            img.Width = ExplosionSize;
            img.Height = ExplosionSize;
            Canvas.SetLeft(img, centerX - ExplosionSize / 2);
            Canvas.SetTop(img, centerY - ExplosionSize / 2);

            // Stop the flying animation
            img.BeginAnimation(Canvas.TopProperty, null);

            int frameIndex = 1; // start from 01 (00 was the rising missile)
            var frameTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(ExplosionFrameMs)
            };

            frameTimer.Tick += (_, _) =>
            {
                if (frameIndex >= 36)
                {
                    // Done — fade out and remove
                    frameTimer.Stop();
                    FadeOutAndRemove(img);
                    return;
                }

                img.Source = Sprites[frameIndex];
                frameIndex++;
            };

            frameTimer.Start();
        }

        // ─── Cleanup: fade out then remove from canvas ────────────────────────────
        private void FadeOutAndRemove(Image img)
        {
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(300)
            };
            fadeOut.Completed += (_, _) => FireworkCanvas.Children.Remove(img);
            img.BeginAnimation(OpacityProperty, fadeOut);
        }
    }
}
