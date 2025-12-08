using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using System.Threading.Tasks;
using FlashCardApp.Controls;
using FlashCardApp.ViewModels;

namespace FlashCardApp.Views;

public partial class StudyView : UserControl
{
    private readonly Border? _knownOverlay;
    private readonly Border? _unknownOverlay;
    private readonly FlipCard? _flipCard;
    private Point _startPoint;
    private bool _isDragging;
    private bool _hasMoved; // Track if pointer has moved significantly
    private TranslateTransform? _translateTransform;
    private RotateTransform? _rotateTransform;
    private const double SwipeThreshold = 100;
    private const double DragStartThreshold = 10; // Minimum distance to start dragging

    private StudyViewModel? ViewModel => DataContext as StudyViewModel;

    public StudyView()
    {
        AvaloniaXamlLoader.Load(this);
        _knownOverlay = this.FindControl<Border>("KnownOverlay");
        _unknownOverlay = this.FindControl<Border>("UnknownOverlay");
        _flipCard = this.FindControl<FlipCard>("StudyFlipCard");
    }

    private void CardContainer_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border cardContainer) return;
        
        _startPoint = e.GetPosition(this);
        _isDragging = true;
        _hasMoved = false;
        // Don't capture immediately - let tap events pass through

        // Initialize transforms if needed
        if (cardContainer.RenderTransform is not TransformGroup transformGroup)
        {
            _translateTransform = new TranslateTransform();
            _rotateTransform = new RotateTransform();
            transformGroup = new TransformGroup();
            transformGroup.Children.Add(_translateTransform);
            transformGroup.Children.Add(_rotateTransform);
            cardContainer.RenderTransform = transformGroup;
            cardContainer.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);
        }
        else
        {
            _translateTransform = transformGroup.Children[0] as TranslateTransform;
            _rotateTransform = transformGroup.Children[1] as RotateTransform;
        }
    }

    private void CardContainer_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isDragging || sender is not Border cardContainer) return;
        
        var currentPoint = e.GetPosition(this);
        var deltaX = currentPoint.X - _startPoint.X;
        var deltaY = currentPoint.Y - _startPoint.Y;
        var distance = Math.Sqrt(deltaX * deltaX + deltaY * deltaY);

        // Only start actual dragging after moving beyond threshold
        if (!_hasMoved && distance > DragStartThreshold)
        {
            _hasMoved = true;
            e.Pointer.Capture(cardContainer);
        }

        // Only apply transforms if we're actually dragging
        if (_hasMoved)
        {
            if (_translateTransform != null)
            {
                _translateTransform.X = deltaX;
                _translateTransform.Y = deltaY * 0.3; // Limit vertical movement
            }

            if (_rotateTransform != null)
            {
                _rotateTransform.Angle = deltaX * 0.05; // Slight rotation for realism
            }

            // Update visual feedback (opacity/color overlay)
            UpdateSwipeFeedback(deltaX);
        }
    }

    private async void CardContainer_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isDragging || sender is not Border cardContainer) return;
        
        var wasDragging = _hasMoved;
        _isDragging = false;
        _hasMoved = false;
        e.Pointer.Capture(null);

        var currentPoint = e.GetPosition(this);
        var deltaX = currentPoint.X - _startPoint.X;

        if (wasDragging)
        {
            if (deltaX > SwipeThreshold)
            {
                // Swipe Right - Known
                await AnimateSwipeOff(cardContainer, 500);
                _flipCard?.Reset(); // Reset flip state before next card
                ViewModel?.SwipeRightCommand.Execute(null);
            }
            else if (deltaX < -SwipeThreshold)
            {
                // Swipe Left - Unknown
                await AnimateSwipeOff(cardContainer, -500);
                _flipCard?.Reset(); // Reset flip state before next card
                ViewModel?.SwipeLeftCommand.Execute(null);
            }
            else
            {
                // Spring back to center
                await AnimateSpringBack(cardContainer);
            }

            // Reset feedback
            ResetSwipeFeedback();
        }
        else
        {
            // It was a tap, not a drag - trigger flip with 3D animation
            if (_flipCard != null)
            {
                await _flipCard.FlipAsync();
            }
        }
    }

    private void UpdateSwipeFeedback(double deltaX)
    {
        if (_knownOverlay != null)
        {
            _knownOverlay.Opacity = Math.Max(0, Math.Min(1, deltaX / SwipeThreshold * 0.5));
        }

        if (_unknownOverlay != null)
        {
            _unknownOverlay.Opacity = Math.Max(0, Math.Min(1, -deltaX / SwipeThreshold * 0.5));
        }
    }

    private void ResetSwipeFeedback()
    {
        if (_knownOverlay != null) _knownOverlay.Opacity = 0;
        if (_unknownOverlay != null) _unknownOverlay.Opacity = 0;
    }

    private async Task AnimateSwipeOff(Border cardContainer, double targetX)
    {
        if (_translateTransform == null) return;

        var animation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(200),
            Easing = new CubicEaseOut(),
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = 
                    { 
                        new Setter(TranslateTransform.XProperty, _translateTransform.X),
                        new Setter(Control.OpacityProperty, 1.0)
                    },
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = 
                    { 
                        new Setter(TranslateTransform.XProperty, targetX),
                        new Setter(Control.OpacityProperty, 0.0)
                    },
                }
            }
        };

        await animation.RunAsync(cardContainer);
        
        // Reset position for next card
        _translateTransform.X = 0;
        _translateTransform.Y = 0;
        if (_rotateTransform != null) _rotateTransform.Angle = 0;
        cardContainer.Opacity = 1;
    }

    private async Task AnimateSpringBack(Border cardContainer)
    {
        if (_translateTransform == null || _rotateTransform == null) return;

        var startX = _translateTransform.X;
        var startY = _translateTransform.Y;
        var startAngle = _rotateTransform.Angle;

        // Animate back to center using a simple loop
        var steps = 10;
        var delay = TimeSpan.FromMilliseconds(15);

        for (int i = 1; i <= steps; i++)
        {
            var t = (double)i / steps;
            var eased = 1 - Math.Pow(1 - t, 3); // Cubic ease out

            _translateTransform.X = startX * (1 - eased);
            _translateTransform.Y = startY * (1 - eased);
            _rotateTransform.Angle = startAngle * (1 - eased);

            await Task.Delay(delay);
        }

        _translateTransform.X = 0;
        _translateTransform.Y = 0;
        _rotateTransform.Angle = 0;
    }
}
