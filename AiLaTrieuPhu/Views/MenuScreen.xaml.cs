using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AiLaTrieuPhu.Views
{
    public partial class MenuScreen : UserControl
    {
        // configuration (tweak these to change feel)
        private readonly Random _rand = new();
        private const double MinEntranceDuration = 2.0;    // seconds (faster entrance)
        private const double MaxEntranceDuration = 4.0;    // seconds (slower entrance)
        private const double MaxEntranceStagger = 1.0;     // seconds
        private const double ThrowVelocityThreshold = 50;  // px/s to trigger throw (dễ ném hơn)
        private const double ThrowMultiplier = 0.8;        // distance multiplier

        // Entrance distance factors (distance = diagonal * factor)
        private const double EntranceDistanceFactorMin = 0.6;
        private const double EntranceDistanceFactorMax = 1.3;
        private const double EntrancePerIndexSpread = 0.04; // small extra factor per index

        // rotation / throw tuning
        private const double RotationSensitivity = 0.38; // lower = less sensitive

        // runtime state
        private readonly Dictionary<Image, Storyboard> _oscillations = new();
        private readonly Dictionary<Image, List<(Point pos, long timeMs)>> _moveHistory = new();
        private ContentPresenter? _draggingContainer;
        private bool _isDragging = false;
        private Point _lastDragPoint;

        public MenuScreen()
        {
            InitializeComponent();
            Loaded += MenuScreen_Loaded;
        }

        private void MenuScreen_Loaded(object? sender, RoutedEventArgs e)
        {
            NostalgiaItems.ApplyTemplate();
            NostalgiaItems.Dispatcher.InvokeAsync(() => InitializeNostalgiaItems(), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void InitializeNostalgiaItems()
        {
            if (!(this.Resources["FloatingOscillation"] is Storyboard baseOsc)) return;

            double canvasW = FloatingCanvas?.ActualWidth ?? 0;
            double canvasH = FloatingCanvas?.ActualHeight ?? 0;
            if (canvasW == 0 || canvasH == 0)
            {
                if (Window.GetWindow(this) is Window win)
                {
                    canvasW = Math.Max(canvasW, win.ActualWidth);
                    canvasH = Math.Max(canvasH, win.ActualHeight);
                }
            }

            var diagonal = Math.Sqrt(canvasW * canvasW + canvasH * canvasH);
            var count = NostalgiaItems?.Items.Count ?? 0;

            for (int i = 0; i < count; i++)
            {
                var container = (ContentPresenter?)NostalgiaItems?.ItemContainerGenerator.ContainerFromIndex(i);
                if (container == null) continue;

                var image = FindVisualChild<Image>(container);
                if (image == null) continue;

                // attach handlers
                image.MouseLeftButtonDown += Image_MouseLeftButtonDown;
                image.MouseLeftButtonUp += Image_MouseLeftButtonUp;
                image.MouseMove += Image_MouseMove;
                image.MouseRightButtonDown += Image_MouseRightButtonDown;
                image.MouseRightButtonUp += Image_MouseRightButtonUp;
                image.MouseWheel += Image_MouseWheel;

                // ensure transforms
                var tg = EnsureWritableTransformGroup(image);
                if (tg.Children.Count < 4) continue;
                var floatingTranslate = tg.Children[0] as TranslateTransform;

                // ---- New: compute a randomized destination for each container so final positions differ ----
                double itemW = container.ActualWidth > 0 ? container.ActualWidth : (image.Width > 0 ? image.Width : 140);
                double itemH = container.ActualHeight > 0 ? container.ActualHeight : (image.Height > 0 ? image.Height : 140);

                // Keep a margin so images don't end up clipped
                const double margin = 20.0;
                double maxLeftPossible = Math.Max(0, canvasW - itemW - margin);
                double maxTopPossible = Math.Max(0, canvasH - itemH - margin);

                // Use a random position distributed across canvas; add small index-based offset to avoid overlaps
                double baseLeft = margin + (_rand.NextDouble() * maxLeftPossible) + (i * 6.0 % Math.Max(1, maxLeftPossible));
                double baseTop = margin + (_rand.NextDouble() * maxTopPossible) + (i * 11.0 % Math.Max(1, maxTopPossible));

                // Clamp to bounds just in case
                baseLeft = Math.Max(margin, Math.Min(baseLeft, canvasW - itemW - margin));
                baseTop = Math.Max(margin, Math.Min(baseTop, canvasH - itemH - margin));

                // assign the randomized destination to container
                Canvas.SetLeft(container, baseLeft);
                Canvas.SetTop(container, baseTop);
                // ---- End new destination logic ----

                // compute per-item entrance distance (varies by random + index)
                var factor = EntranceDistanceFactorMin + _rand.NextDouble() * (EntranceDistanceFactorMax - EntranceDistanceFactorMin)
                             + (i * EntrancePerIndexSpread);
                var distance = diagonal * factor;

                // pick a random angle so each image comes from a different direction
                var angle = _rand.NextDouble() * Math.PI * 2.0;
                var startX = Math.Cos(angle) * distance;
                var startY = Math.Sin(angle) * distance;

                // set initial floating translate so the image visually is outside
                if (floatingTranslate != null)
                {
                    floatingTranslate.X = startX;
                    floatingTranslate.Y = startY;
                }

                // create inward animation on the floating translate (continuous drift into position)
                var dur = TimeSpan.FromSeconds(MinEntranceDuration + _rand.NextDouble() * (MaxEntranceDuration - MinEntranceDuration));
                var delay = TimeSpan.FromSeconds(_rand.NextDouble() * MaxEntranceStagger);

                var animX = new DoubleAnimation(startX, 0, dur)
                {
                    BeginTime = delay,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                };
                var animY = new DoubleAnimation(startY, 0, TimeSpan.FromSeconds(dur.TotalSeconds * (0.9 + _rand.NextDouble() * 0.3)))
                {
                    BeginTime = delay,
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
                    FillBehavior = FillBehavior.Stop
                };

                Storyboard.SetTarget(animX, image);
                Storyboard.SetTargetProperty(animX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));
                Storyboard.SetTarget(animY, image);
                Storyboard.SetTargetProperty(animY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));

                var bringIn = new Storyboard();
                bringIn.Children.Add(animX);
                bringIn.Children.Add(animY);

                bringIn.Completed += (_, _) =>
                {
                    if (floatingTranslate != null)
                    {
                        floatingTranslate.X = 0;
                        floatingTranslate.Y = 0;
                    }

                    // start small oscillation (clone per image)
                    var osc = baseOsc.Clone();
                    osc.BeginTime = TimeSpan.FromSeconds(_rand.NextDouble() * 0.6);
                    osc.Begin(image, true);
                    _oscillations[image] = osc;

                    // initialize move history
                    _moveHistory[image] = new List<(Point, long)>();
                };

                // start bring-in animation
                bringIn.Begin(image, true);
            }
        }

        // —— Drag / throw / rotate logic —— //
        private void Image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image image) return;
            image.CaptureMouse();
            _isDragging = true;
            _lastDragPoint = e.GetPosition(FloatingCanvas);

            _draggingContainer = FindAncestor<ContentPresenter>(image);

            if (_oscillations.TryGetValue(image, out var sb))
            {
                try { sb.Stop(image); } catch { }
            }

            _moveHistory[image] = new List<(Point, long)> { (_lastDragPoint, Stopwatch.GetTimestampMs()) };

            e.Handled = true;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not Image image) return;

            // rotation with right button: handled separately in rotation handler
            if (e.RightButton == MouseButtonState.Pressed)
            {
                HandleRotationIfNeeded(image, e);
                return;
            }

            if (!_isDragging || _draggingContainer == null || FloatingCanvas == null) return;

            var pos = e.GetPosition(FloatingCanvas);

            // immediate move for responsiveness
            double left = Canvas.GetLeft(_draggingContainer);
            double top = Canvas.GetTop(_draggingContainer);

            double dx = pos.X - _lastDragPoint.X;
            double dy = pos.Y - _lastDragPoint.Y;

            double newLeft = left + dx;
            double newTop = top + dy;

            var maxLeft = Math.Max(0, FloatingCanvas.ActualWidth - (_draggingContainer.ActualWidth > 0 ? _draggingContainer.ActualWidth : 140));
            var maxTop = Math.Max(0, FloatingCanvas.ActualHeight - (_draggingContainer.ActualHeight > 0 ? _draggingContainer.ActualHeight : 140));
            newLeft = Math.Max(0, Math.Min(newLeft, maxLeft));
            newTop = Math.Max(0, Math.Min(newTop, maxTop));

            Canvas.SetLeft(_draggingContainer, newLeft);
            Canvas.SetTop(_draggingContainer, newTop);

            var hist = _moveHistory.GetValueOrDefault(image);
            hist?.Add((pos, Stopwatch.GetTimestampMs()));
            if (hist != null && hist.Count > 6) hist.RemoveAt(0);

            _lastDragPoint = pos;
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image image) return;
            if (!_isDragging) return;

            var hist = _moveHistory.GetValueOrDefault(image);
            (Point pos0, long t0) = (default, 0);
            (Point pos1, long t1) = (default, 0);
            if (hist != null && hist.Count >= 2)
            {
                pos0 = hist.First().pos; t0 = hist.First().timeMs;
                pos1 = hist.Last().pos; t1 = hist.Last().timeMs;
            }

            double velocity = 0;
            Vector v = new();
            if (t1 > t0)
            {
                double dt = (t1 - t0) / 1000.0;
                v = pos1 - pos0;
                velocity = v.Length / dt;
            }

            if (velocity >= ThrowVelocityThreshold && _draggingContainer != null)
            {
                PerformThrow(image, _draggingContainer, v, velocity);
            }
            else
            {
                ResumeOscillation(image);
            }

            image.ReleaseMouseCapture();
            _isDragging = false;
            _draggingContainer = null;
            e.Handled = true;
        }

        private void PerformThrow(Image image, ContentPresenter container, Vector direction, double speed)
        {
            if (direction.Length == 0)
            {
                ResumeOscillation(image);
                return;
            }
            direction.Normalize();

            // Lực ma sát và khoảng cách ném
            double distance = speed * ThrowMultiplier;
            double targetX = Canvas.GetLeft(container) + direction.X * distance;
            double targetY = Canvas.GetTop(container) + direction.Y * distance;

            // Tính thời gian bay (tối đa 2.5s)
            double durSeconds = Math.Min(2.5, distance / (speed * 0.5 + 1)); 
            if (durSeconds < 0.2) durSeconds = 0.2;
            var dur = TimeSpan.FromSeconds(durSeconds);

            // Dùng CubicEase để có ma sát mượt mà
            var animX = new DoubleAnimation(Canvas.GetLeft(container), targetX, dur) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var animY = new DoubleAnimation(Canvas.GetTop(container), targetY, dur) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            animX.FillBehavior = FillBehavior.Stop;
            animY.FillBehavior = FillBehavior.Stop;

            animX.Completed += (_, _) =>
            {
                Canvas.SetLeft(container, targetX);
                Canvas.SetTop(container, targetY);

                var boundsW = FloatingCanvas.ActualWidth;
                var boundsH = FloatingCanvas.ActualHeight;
                var itemW = container.ActualWidth > 0 ? container.ActualWidth : 140;
                var itemH = container.ActualHeight > 0 ? container.ActualHeight : 140;

                bool isOffScreen = targetX + itemW < -50 || targetX > boundsW + 50 || targetY + itemH < -50 || targetY > boundsH + 50;

                if (isOffScreen)
                {
                    _ = ReenterAfterDelayAsync(container, image, _rand);
                }
                else
                {
                    ResumeOscillation(image);
                }
            };

            var tgForSpin = EnsureWritableTransformGroup(image);
            if (tgForSpin.Children.Count >= 4 && tgForSpin.Children[3] is RotateTransform rt)
            {
                double spinAmount = (direction.X * direction.Y * 180 + (_rand.NextDouble() - 0.5) * 90) * (speed / 500.0);
                var spin = new DoubleAnimation(rt.Angle, rt.Angle + spinAmount, dur) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
                spin.FillBehavior = FillBehavior.Stop;
                spin.Completed += (_, _) => { rt.Angle += spinAmount; };
                rt.BeginAnimation(RotateTransform.AngleProperty, spin);
            }

            container.BeginAnimation(Canvas.LeftProperty, animX);
            container.BeginAnimation(Canvas.TopProperty, animY);
        }

        private async Task ReenterAfterDelayAsync(ContentPresenter container, Image image, Random rand)
        {
            try
            {
                await Task.Delay(800 + rand.Next(600)).ConfigureAwait(false);
                if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished) return;
                await Dispatcher.InvokeAsync(() => ResetAndReenter(container, image));
            }
            catch { }
        }

        private void ResetAndReenter(ContentPresenter container, Image image)
        {
            var tg = EnsureWritableTransformGroup(image);
            if (tg.Children.Count < 4) return;
            var floatingTranslate = tg.Children[0] as TranslateTransform;

            double canvasW = FloatingCanvas.ActualWidth;
            double canvasH = FloatingCanvas.ActualHeight;
            double baseLeft = _rand.NextDouble() * Math.Max(1, canvasW - 160);
            double baseTop = _rand.NextDouble() * Math.Max(1, canvasH - 160);

            Canvas.SetLeft(container, baseLeft);
            Canvas.SetTop(container, baseTop);

            var diagonal = Math.Sqrt(canvasW * canvasW + canvasH * canvasH);
            var factor = EntranceDistanceFactorMin + _rand.NextDouble() * (EntranceDistanceFactorMax - EntranceDistanceFactorMin);
            var distance = diagonal * factor;
            var angle = _rand.NextDouble() * Math.PI * 2.0;
            var startX = Math.Cos(angle) * distance;
            var startY = Math.Sin(angle) * distance;

            floatingTranslate.X = startX;
            floatingTranslate.Y = startY;

            var dur = TimeSpan.FromSeconds(MinEntranceDuration + _rand.NextDouble() * (MaxEntranceDuration - MinEntranceDuration));
            var animX = new DoubleAnimation(startX, 0, dur) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };
            var animY = new DoubleAnimation(startY, 0, TimeSpan.FromSeconds(dur.TotalSeconds * (0.9 + _rand.NextDouble() * 0.3))) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } };

            Storyboard.SetTarget(animX, image);
            Storyboard.SetTargetProperty(animX, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.X)"));
            Storyboard.SetTarget(animY, image);
            Storyboard.SetTargetProperty(animY, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[0].(TranslateTransform.Y)"));

            var bringIn = new Storyboard();
            bringIn.Children.Add(animX);
            bringIn.Children.Add(animY);

            bringIn.Completed += (_, _) =>
            {
                floatingTranslate.X = 0;
                floatingTranslate.Y = 0;
                ResumeOscillation(image);
            };

            bringIn.Begin(image, true);
        }

        private void ResumeOscillation(Image image)
        {
            if (!(this.Resources["FloatingOscillation"] is Storyboard baseOsc)) return;
            try
            {
                var osc = baseOsc.Clone();
                osc.BeginTime = TimeSpan.FromSeconds(_rand.NextDouble() * 0.6);
                osc.Begin(image, true);
                _oscillations[image] = osc;
                _moveHistory[image] = new List<(Point, long)>();
            }
            catch { }
        }

        // rotate with right mouse drag
        private bool _isRotating = false;
        private double _rotateStartAngle = 0;
        private Point _rotateStartPoint;

        private void Image_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image img) return;
            img.CaptureMouse();
            _isRotating = true;
            _rotateStartPoint = e.GetPosition(img);

            var tg = EnsureWritableTransformGroup(img);
            if (tg.Children.Count >= 4 && tg.Children[3] is RotateTransform rt)
            {
                _rotateStartAngle = rt.Angle;
            }
            e.Handled = true;
        }

        private void Image_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image img) return;
            img.ReleaseMouseCapture();
            _isRotating = false;
            e.Handled = true;
        }

        private void HandleRotationIfNeeded(Image img, MouseEventArgs e)
        {
            if (!_isRotating) return;
            var cur = e.GetPosition(img);
            double dx = cur.X - _rotateStartPoint.X;
            double angleDelta = dx * RotationSensitivity;

            var tg = EnsureWritableTransformGroup(img);
            if (tg.Children.Count >= 4 && tg.Children[3] is RotateTransform rt)
            {
                rt.Angle = _rotateStartAngle + angleDelta;
            }
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not Image img) return;
            var tg = EnsureWritableTransformGroup(img);
            if (tg.Children.Count >= 4 && tg.Children[3] is RotateTransform rt)
            {
                double delta = e.Delta > 0 ? 25 : -25;
                
                var anim = new DoubleAnimation(rt.Angle, rt.Angle + delta, TimeSpan.FromSeconds(0.25))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                anim.FillBehavior = FillBehavior.Stop;
                anim.Completed += (_, _) => { rt.Angle += delta; };
                
                rt.BeginAnimation(RotateTransform.AngleProperty, anim);
            }
            e.Handled = true;
        }

        // small helper to get timestamp ms
        private static class Stopwatch
        {
            private static readonly System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            public static long GetTimestampMs() => sw.ElapsedMilliseconds;
        }

        // Ensure TransformGroup and its children are writable (not frozen). If frozen, clone and assign.
        private TransformGroup EnsureWritableTransformGroup(Image image)
        {
            if (image.RenderTransform is TransformGroup tg)
            {
                if (tg.IsFrozen)
                {
                    var clone = tg.Clone();
                    image.RenderTransform = clone;
                    tg = clone;
                }

                for (int i = 0; i < tg.Children.Count; i++)
                {
                    var child = tg.Children[i];
                    if (child is Freezable f && f.IsFrozen)
                    {
                        tg.Children[i] = (Transform)f.Clone();
                    }
                }

                return tg;
            }
            else
            {
                var newTg = new TransformGroup();
                if (image.RenderTransform is Transform t) newTg.Children.Add(t);
                image.RenderTransform = newTg;
                return newTg;
            }
        }

        // visual tree helpers
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) return default;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T t) return t;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return default;
        }

        private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return default;
        }
    }
}