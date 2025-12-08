using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Styling;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;

namespace FlashCardApp.Controls;

public partial class FlipCard : UserControl
{
    public static readonly StyledProperty<string> FrontTextProperty =
        AvaloniaProperty.Register<FlipCard, string>(nameof(FrontText), "Front");
    public static readonly StyledProperty<string> BackTextProperty =
        AvaloniaProperty.Register<FlipCard, string>(nameof(BackText), "Back");
    public static readonly StyledProperty<bool> IsFlippedProperty =
        AvaloniaProperty.Register<FlipCard, bool>(nameof(IsFlipped), false);
    public static readonly StyledProperty<bool> IsInteractiveProperty =
        AvaloniaProperty.Register<FlipCard, bool>(nameof(IsInteractive), true);
    
    private bool _isAnimating;
    
    public bool IsFlipped { 
        get => GetValue(IsFlippedProperty);
        set => SetValue(IsFlippedProperty, value);
    }

    public string FrontText
    {
        get => GetValue(FrontTextProperty); 
        set => SetValue(FrontTextProperty, value);
    }

    public string BackText
    {
        get => GetValue(BackTextProperty); 
        set => SetValue(BackTextProperty, value);
    }
    
    public bool IsInteractive
    {
        get => GetValue(IsInteractiveProperty);
        set => SetValue(IsInteractiveProperty, value);
    }
    
    public FlipCard()
    {
        InitializeComponent();
    }

    private async void CardContainer_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsInteractive && !_isAnimating)
        {
            await FlipAsync();
        }
    }
    
    /// <summary>
    /// Public method to flip the card programmatically
    /// </summary>
    public async Task FlipAsync()
    {
        if (_isAnimating) return;
        _isAnimating = true;

        try
        {
            var frontBorder = this.FindControl<Border>("FrontBorder");
            var backBorder = this.FindControl<Border>("BackBorder");
            
            if (frontBorder == null || backBorder == null) return;

            var currentBorder = IsFlipped ? backBorder : frontBorder;
            var nextBorder = IsFlipped ? frontBorder : backBorder;
            
            var currentTransform = currentBorder.RenderTransform as ScaleTransform;
            var nextTransform = nextBorder.RenderTransform as ScaleTransform;
            
            if (currentTransform == null || nextTransform == null) return;

            // Shrink current side
            var shrinkAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new CubicEaseIn(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.0) },
                    }
                }
            };

            await shrinkAnimation.RunAsync(currentBorder);

            // Toggle flipped state
            IsFlipped = !IsFlipped;

            // Expand new side
            var expandAnimation = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(200),
                Easing = new CubicEaseOut(),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0.0),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 0.0) },
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1.0),
                        Setters = { new Setter(ScaleTransform.ScaleXProperty, 1.0) },
                    }
                }
            };

            await expandAnimation.RunAsync(nextBorder);
        }
        finally
        {
            _isAnimating = false;
        }
    }

    /// <summary>
    /// Reset card to front side without animation
    /// </summary>
    public void Reset()
    {
        IsFlipped = false;
    }
}