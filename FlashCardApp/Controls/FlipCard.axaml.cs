using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
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
    
    //Property
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
    
    //this bug can ignore, after the project build, it will work.
    public FlipCard()
    {
        InitializeComponent();
    }

    // Card got pressed will trigger the animation of flipping
    private async void CardBorder_OnPointerPressed_(object? sender, PointerPressedEventArgs e)
    {
        await FlipAnimation();
    }
    
    //Animation
    private async Task FlipAnimation()
    {
        var cardBorder = this.FindControl<Border>("CardBorder");
        if (cardBorder == null) return;
        
        var transform = cardBorder.RenderTransform as ScaleTransform;
        if (transform == null) return;

        var shrinkAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseInOut(),
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

        await shrinkAnimation.RunAsync(cardBorder);

        this.IsFlipped = !this.IsFlipped;

        var expandAnimation = new Animation
        {
            Duration = TimeSpan.FromMilliseconds(300),
            Easing = new CubicEaseInOut(),
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
        
        await expandAnimation.RunAsync(cardBorder);
    }

    public void Reset()
    {
        this.IsFlipped = false;
    }
}