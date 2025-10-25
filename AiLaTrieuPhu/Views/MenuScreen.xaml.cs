using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace AiLaTrieuPhu.Views
{
    public partial class MenuScreen : UserControl
    {
        // configuration
        private readonly Random _rand = new();
        private const double MinEntranceDuration = 2.4; // seconds
        private const double MaxEntranceDuration = 4.2; // seconds
        private const double MaxEntranceStagger = 1.0;  // seconds
        private const double ThrowVelocityThreshold = 900.0; // pixels/second to trigger throw
        private const double ThrowMultiplier = 0.8; // how far to send thrown item relative to velocity

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
            // Ensure ItemsControl generated containers
            NostalgiaItems.ApplyTemplate();
            var generator = NostalgiaItems.ItemContainerGenerator;

            // Wait until containers are ready
            NostalgiaItems.Dispatcher.InvokeAsync(() =>
            {
                InitializeNostalgiaItems();
            }, System.Windows.Threading.DispatcherPriority.Loaded);
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

                // ensure transforms (and make sure they're writable / not frozen)
                var tg = EnsureWritableTransformGroup(image);
                if (tg.Children.Count < 4) continue;
                var floatingTranslate = tg.Children[0] as TranslateTransform;
                var rotate = tg.Children[3] as RotateTransform;

                // read base container position
                double baseLeft = Canvas.GetLeft(container);
                double baseTop = Canvas.GetTop(container);

                // compute off-screen start (choose a distant offset so it's outside current view)
                double startX = (baseLeft < canvasW * 0.5) ? -(canvasW * (0.4 + _rand.NextDouble())) : (canvasW * (0.4 + _rand.NextDouble()));
                double startY = (baseTop < canvasH * 0.5) ? -(canvasH * (0.25 + _rand.NextDouble())) : (canvasH * (0.25 + _rand.NextDouble()));

                // set initial floating translate so the image visually is outside
                if (floatingTranslate != null)
                {
                    floatingTranslate.X = startX;
                    floatingTranslate.Y = startY;
                }

                // create inward animation on the floating translate (continuous drift into position)
                var dur = TimeSpan.FromSeconds(MinEntranceDuration + _rand.NextDouble() * (MaxEntranceDuration - MinEntranceDuration));
                var delay = TimeSpan.FromSeconds(_rand.NextDouble() * MaxEntranceStagger);

                var animX = new DoubleAnimation(startX, 0, dur) { BeginTime = delay, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop };
                var animY = new DoubleAnimation(startY, 0, TimeSpan.FromSeconds(dur.TotalSeconds * (0.9 + _rand.NextDouble() * 0.3))) { BeginTime = delay, EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop };

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

            // stop oscillation for this image
            if (_oscillations.TryGetValue(image, out var sb))
            {
                try { sb.Stop(image); } catch { }
            }

            // clear move history and add initial point
            _moveHistory[image] = new List<(Point, long)> { (_lastDragPoint, Stopwatch.GetTimestampMs()) };

            e.Handled = true;
        }

        private void Image_MouseMove(object sender, MouseEventArgs e)
        {
            if (sender is not Image image) return;

            // rotation with right button: handled separately
            if (e.RightButton == MouseButtonState.Pressed) return;

            if (!_isDragging || _draggingContainer == null || FloatingCanvas == null) return;

            var pos = e.GetPosition(FloatingCanvas);

            // immediate move for responsiveness (no heavy animations)
            double left = Canvas.GetLeft(_draggingContainer);
            double top = Canvas.GetTop(_draggingContainer);

            double dx = pos.X - _lastDragPoint.X;
            double dy = pos.Y - _lastDragPoint.Y;

            double newLeft = left + dx;
            double newTop = top + dy;

            // clamp
            var maxLeft = Math.Max(0, FloatingCanvas.ActualWidth - (_draggingContainer.ActualWidth > 0 ? _draggingContainer.ActualWidth : 140));
            var maxTop = Math.Max(0, FloatingCanvas.ActualHeight - (_draggingContainer.ActualHeight > 0 ? _draggingContainer.ActualHeight : 140));
            newLeft = Math.Max(0, Math.Min(newLeft, maxLeft));
            newTop = Math.Max(0, Math.Min(newTop, maxTop));

            Canvas.SetLeft(_draggingContainer, newLeft);
            Canvas.SetTop(_draggingContainer, newTop);

            // append history for velocity calculation
            var hist = _moveHistory.GetValueOrDefault(image);
            hist?.Add((pos, Stopwatch.GetTimestampMs()));
            // keep last ~6 records
            if (hist != null && hist.Count > 6) hist.RemoveAt(0);

            _lastDragPoint = pos;
        }

        private void Image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Image image) return;
            if (!_isDragging) return;

            // compute velocity from move history
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

            // if throw threshold passed, animate container to fly out
            if (velocity >= ThrowVelocityThreshold && _draggingContainer != null)
            {
                PerformThrow(image, _draggingContainer, v, velocity);
            }
            else
            {
                // resume oscillation
                ResumeOscillation(image);
            }

            image.ReleaseMouseCapture();
            _isDragging = false;
            _draggingContainer = null;
            e.Handled = true;
        }

        private void PerformThrow(Image image, ContentPresenter container, Vector direction, double speed)
        {
            // normalize dir
            if (direction.Length == 0)
            {
                ResumeOscillation(image);
                return;
            }
            direction.Normalize();

            // compute target off-screen point
            double distance = speed * ThrowMultiplier; // px
            double targetX = Canvas.GetLeft(container) + direction.X * distance;
            double targetY = Canvas.GetTop(container) + direction.Y * distance;

            // ensure target moves off screen: extend until outside bounds
            var boundsW = FloatingCanvas.ActualWidth;
            var boundsH = FloatingCanvas.ActualHeight;
            // simple extension to guarantee off-screen
            if (targetX >= 0 && targetX <= boundsW) targetX += direction.X * (boundsW * 1.2);
            if (targetY >= 0 && targetY <= boundsH) targetY += direction.Y * (boundsH * 1.2);

            // duration proportional to speed
            var dur = TimeSpan.FromSeconds(Math.Min(1.8, 0.6 + (speed / 2000.0)));

            // animate Canvas.Left/Top
            var animX = new DoubleAnimation(Canvas.GetLeft(container), targetX, dur) { EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut } };
            var animY = new DoubleAnimation(Canvas.GetTop(container), targetY, dur) { EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut } };

            animX.FillBehavior = FillBehavior.Stop;
            animY.FillBehavior = FillBehavior.Stop;

            animX.Completed += (_, _) =>
            {
                // put it fully off-screen
                Canvas.SetLeft(container, targetX);
                Canvas.SetTop(container, targetY);

                // after throwing out, reinitialize this item to re-enter after short delay
                System.Threading.Tasks.Task.Delay(800 + _rand.Next(600)).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        ResetAndReenter(container, image);
                    });
                });
            };

            // ensure rotate transform is writable before animating
            var tgForSpin = EnsureWritableTransformGroup(image);
            if (tgForSpin.Children.Count >= 4 && tgForSpin.Children[3] is RotateTransform rt)
            {
                var spin = new DoubleAnimation(rt.Angle, rt.Angle + (direction.X * 360 + direction.Y * 180) * (0.6 + speed / 1500.0), dur) { EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut } };
                spin.FillBehavior = FillBehavior.Stop;
                rt.BeginAnimation(RotateTransform.AngleProperty, spin);
            }

            // start animations
            container.BeginAnimation(Canvas.LeftProperty, animX);
            container.BeginAnimation(Canvas.TopProperty, animY);
        }

        // reset container back to a fresh off-screen starting point and re-run inward animation
        private void ResetAndReenter(ContentPresenter container, Image image)
        {
            var tg = EnsureWritableTransformGroup(image);
            if (tg.Children.Count < 4) return;
            var floatingTranslate = tg.Children[0] as TranslateTransform;

            double canvasW = FloatingCanvas.ActualWidth;
            double canvasH = FloatingCanvas.ActualHeight;
            double baseLeft = _rand.NextDouble() * Math.Max(1, canvasW - 160);
            double baseTop = _rand.NextDouble() * Math.Max(1, canvasH - 160);

            // place container off-screen at new base position
            Canvas.SetLeft(container, baseLeft);
            Canvas.SetTop(container, baseTop);

            // compute start offsets
            double startX = (_rand.Next(0, 2) == 0) ? -(canvasW * (0.4 + _rand.NextDouble())) : (canvasW * (0.4 + _rand.NextDouble()));
            double startY = (_rand.Next(0, 2) == 0) ? -(canvasH * (0.25 + _rand.NextDouble())) : (canvasH * (0.25 + _rand.NextDouble()));

            floatingTranslate.X = startX;
            floatingTranslate.Y = startY;

            // inward animation
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

                // resume oscillation
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

            // ensure writable transform so subsequent modifications won't fail
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

        // handle rotation inside MouseMove (if right-button pressed)
        private void HandleRotationIfNeeded(Image img, MouseEventArgs e)
        {
            if (!_isRotating) return;
            var cur = e.GetPosition(img);
            double dx = cur.X - _rotateStartPoint.X;
            // horizontal movement -> angle change
            double angleDelta = dx * 0.4; // tweak sensitivity

            var tg = EnsureWritableTransformGroup(img);
            if (tg.Children.Count >= 4 && tg.Children[3] is RotateTransform rt)
            {
                rt.Angle = _rotateStartAngle + angleDelta;
            }
        }

        // we need to call rotation handler from MouseMove (augment existing)
        private void Image_MouseMove_RotationAdapter(object? sender, MouseEventArgs e)
        {
            if (sender is Image img) HandleRotationIfNeeded(img, e);
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

                // clone any frozen children
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