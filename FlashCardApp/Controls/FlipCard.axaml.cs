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
    
    private bool _isAnimating = false;
    
    //this bug can ignore, after the project build, it will work.
    public FlipCard()
    {
        InitializeComponent();
    }

    // Card got pressed will trigger the animation of flipping
    private async void CardBorder_OnPointerPressed_(object? sender, PointerPressedEventArgs e)
    {
        if (_isAnimating) return;
        await FlipAnimation();
    }
    
    /*
     *  1. Transform
     *  2. Opacity
     *  3. Shadow
     *  4. Scale
     */

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

    private Animation OpacityAnimation(double fromOpacity, double toOpacity, int durationMs, Easing easing)
    {
        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = easing,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Visual.OpacityProperty, fromOpacity) },
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Visual.OpacityProperty, toOpacity) },
                }
            }
        };
    }
    
    private Animation ShadowAnimation(string formShadow, string toShadow, int durationMs, Easing easing)
    {
        return new Animation
        {
            Duration = TimeSpan.FromMilliseconds(durationMs),
            Easing = easing,
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0.0),
                    Setters = { new Setter(Border.BoxShadowProperty, BoxShadow.Parse(formShadow)), }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(Border.BoxShadowProperty, BoxShadow.Parse(toShadow)), }
                }
            }
        };
    }
    
    private Animation ScaleXAnimation(double fromX, double toX, int durationMs, Easing easing)
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
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, fromX), }
                },
                new KeyFrame
                {
                    Cue = new Cue(1.0),
                    Setters = { new Setter(ScaleTransform.ScaleXProperty, toX), }
                }
            }
        };
    }
    
    //Animation
    private async Task FlipAnimation()
    {
        //Animating start
        _isAnimating = true;
        
        var cardBorder = this.FindControl<Border>("CardBorder");
        if (cardBorder == null) return;

        var transform = cardBorder.RenderTransform as Rotate3DTransform;
        if(transform == null) return;
        
        var frontPanel = this.FindControl<StackPanel>("FrontPanel");
        if (frontPanel == null) return;
        
        var backPanel = this.FindControl<StackPanel>("BackPanel");
        if (backPanel == null) return;
        
        //which panel fade out
        var fadeOutPanel = this.IsFlipped ? backPanel : frontPanel;
        var fadeInPanel = this.IsFlipped ? frontPanel : backPanel;
        
        //scale fadeOutPanel for proper view
        var fadeOutTransform = fadeOutPanel.RenderTransform as ScaleTransform;
        if(fadeOutTransform == null) return;
        
        fadeOutTransform.ScaleX = 1;

        var toSideRotate = RotateAnimation(0.0, 90.0, 500, new CubicEaseIn());
        await toSideRotate.RunAsync(cardBorder);
        
        this.IsFlipped = !this.IsFlipped;
        
        //scale fadeInPanel for proper view
        var fadeInTransform = fadeInPanel.RenderTransform as ScaleTransform;
        if(fadeInTransform == null) return;

        fadeInTransform.ScaleX = -1;
        
        var fromSideRotate = RotateAnimation(90.0, 180.0, 500, new CubicEaseOut());
        await fromSideRotate.RunAsync(cardBorder);

        _isAnimating = false;
    }

    public void Reset()
    {
        this.IsFlipped = false;
    }
}