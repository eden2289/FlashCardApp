using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Styling;
using System.Threading.Tasks;
using Avalonia.Animation.Easings;
using Avalonia.Markup.Xaml;

namespace FlashCardApp.Controls;

public partial class FlipCard : UserControl
{
    public static readonly StyledProperty<string> FrontTextProperty =
        AvaloniaProperty.Register<FlipCard, string>(nameof(FrontText), string.Empty);
    public static readonly StyledProperty<string> BackTextProperty =
        AvaloniaProperty.Register<FlipCard, string>(nameof(BackText), string.Empty);
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
        AvaloniaXamlLoader.Load(this);
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
        await FlipAnimation();
    }

    private Animation RotateAnimation(double fromAngle, double toAngle, int durationMs, Easing easing)
    {
        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = easing,
            FillMode = FillMode.Forward,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Rotate3DTransform.AngleYProperty, fromAngle) },
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Rotate3DTransform.AngleYProperty, toAngle) },
                }
            }
        };
    }
    
    private async Task FlipAnimation()
    {
        _isAnimating = true;
        
        var cardBorder = this.FindControl<Border>("CardBorder");
        if (cardBorder == null) { _isAnimating = false; return; }

        var transform = cardBorder.RenderTransform as Rotate3DTransform;
        if (transform == null) { _isAnimating = false; return; }
        
        var frontPanel = this.FindControl<StackPanel>("FrontPanel");
        if (frontPanel == null) { _isAnimating = false; return; }
        
        var backPanel = this.FindControl<StackPanel>("BackPanel");
        if (backPanel == null) { _isAnimating = false; return; }
        
        // which panel fade out
        var fadeOutPanel = IsFlipped ? backPanel : frontPanel;
        var fadeInPanel = IsFlipped ? frontPanel : backPanel;
        
        // scale fadeOutPanel for proper view
        var fadeOutTransform = fadeOutPanel.RenderTransform as ScaleTransform;
        if (fadeOutTransform != null)
            fadeOutTransform.ScaleX = 1;

        var toSideRotate = RotateAnimation(0.0, 90.0, 300, new CubicEaseIn());
        await toSideRotate.RunAsync(cardBorder);
        
        IsFlipped = !IsFlipped;
        
        // scale fadeInPanel for proper view
        var fadeInTransform = fadeInPanel.RenderTransform as ScaleTransform;
        if (fadeInTransform != null)
            fadeInTransform.ScaleX = -1;
        
        var fromSideRotate = RotateAnimation(90.0, 180.0, 300, new CubicEaseOut());
        await fromSideRotate.RunAsync(cardBorder);

        _isAnimating = false;
    }

    /// <summary>
    /// Reset card to front side without animation
    /// </summary>
    public void Reset()
    {
        IsFlipped = false;
        
        var cardBorder = this.FindControl<Border>("CardBorder");
        if (cardBorder?.RenderTransform is Rotate3DTransform transform)
        {
            transform.AngleY = 0;
        }
        
        var frontPanel = this.FindControl<StackPanel>("FrontPanel");
        if (frontPanel?.RenderTransform is ScaleTransform frontTransform)
        {
            frontTransform.ScaleX = 1;
        }
        
        var backPanel = this.FindControl<StackPanel>("BackPanel");
        if (backPanel?.RenderTransform is ScaleTransform backTransform)
        {
            backTransform.ScaleX = 1;
        }
    }
}
