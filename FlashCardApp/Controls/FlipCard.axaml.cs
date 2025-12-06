using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Animation;
using Avalonia.Styling;
using System.Threading.Tasks;

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
        //Wait for Implementation
    }
}