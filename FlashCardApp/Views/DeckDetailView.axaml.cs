using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using FlashCardApp.ViewModels;

namespace FlashCardApp.Views;

public partial class DeckDetailView : UserControl
{
    private Point _swipeStartPoint;
    private bool _isSwiping;
    private const double SwipeThreshold = 50;

    private DeckDetailViewModel? ViewModel => DataContext as DeckDetailViewModel;

    public DeckDetailView()
    {
        AvaloniaXamlLoader.Load(this);
        
        var previewCard = this.FindControl<Border>("PreviewCardBorder");
        if (previewCard != null)
        {
            previewCard.PointerPressed += PreviewCard_PointerPressed;
            previewCard.PointerMoved += PreviewCard_PointerMoved;
            previewCard.PointerReleased += PreviewCard_PointerReleased;
        }
    }

    private void PreviewCard_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _swipeStartPoint = e.GetPosition(this);
        _isSwiping = true;
    }

    private void PreviewCard_PointerMoved(object? sender, PointerEventArgs e)
    {
        // Optional: could add visual feedback here
    }

    private void PreviewCard_PointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSwiping) return;
        _isSwiping = false;

        var endPoint = e.GetPosition(this);
        var deltaX = endPoint.X - _swipeStartPoint.X;
        var deltaY = endPoint.Y - _swipeStartPoint.Y;

        // Only horizontal swipe if horizontal movement is dominant
        if (Math.Abs(deltaX) > Math.Abs(deltaY) && Math.Abs(deltaX) > SwipeThreshold)
        {
            if (deltaX > 0)
            {
                // Swipe right -> previous card
                ViewModel?.PreviousCardCommand.Execute(null);
            }
            else
            {
                // Swipe left -> next card
                ViewModel?.NextCardCommand.Execute(null);
            }
        }
        else if (Math.Abs(deltaX) < 10 && Math.Abs(deltaY) < 10)
        {
            // It was a tap, flip the card
            ViewModel?.FlipPreviewCommand.Execute(null);
        }
    }
}
