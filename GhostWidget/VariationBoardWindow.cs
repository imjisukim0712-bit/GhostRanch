using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GhostWidget;

/// <summary>Review board showing the exact species renderer used by the widget and capture game.</summary>
public sealed class VariationBoardWindow : Window
{
    private readonly MainWindow companion;
    private readonly VariationBoardCanvas board;

    internal VariationBoardWindow(MainWindow companion)
    {
        this.companion = companion;
        board = new VariationBoardCanvas(companion.GetRoster());
        Width = 1370;
        Height = 1340;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Content = board;
        Loaded += RenderPreview;
    }

    private async void RenderPreview(object sender, RoutedEventArgs e)
    {
        if (!Environment.GetCommandLineArgs().Contains("--variation-preview")) return;
        await Task.Delay(150);
        // Windows constrains a borderless window to the visible desktop.  The review export
        // must nevertheless include all 50 cards, including rows below a 1080p display.
        Size previewSize = new(1370, 1335);
        board.Measure(previewSize);
        board.Arrange(new Rect(new Point(), previewSize));
        board.UpdateLayout();
        RenderTargetBitmap bitmap = new((int)previewSize.Width, (int)previewSize.Height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(board);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "variation-board-preview.png"));
        encoder.Save(stream);
        Close();
        companion.Close();
    }
}

internal sealed class VariationBoardCanvas : FrameworkElement
{
    private readonly IReadOnlyList<GhostRosterItem> ghosts;

    internal VariationBoardCanvas(IReadOnlyList<GhostRosterItem> ghosts) => this.ghosts = ghosts;

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(20, 18, 28)), null, new Rect(0, 0, ActualWidth, ActualHeight), 26, 26);
        DrawText(dc, "유령 도감 · 실루엣 시안", new Point(34, 27), 25, Brushes.White, FontWeights.Bold);
        DrawText(dc, "기본 실루엣 색상형은 5종만 · 이후 45종은 서로 다른 테마 실루엣", new Point(34, 62), 12,
            new SolidColorBrush(Color.FromRgb(173, 166, 183)), FontWeights.Normal);

        for (int index = 0; index < ghosts.Count; index++)
        {
            int column = index % 5;
            int row = index / 5;
            DrawCard(dc, new Rect(28 + column * 267, 103 + row * 122, 246, 104), ghosts[index]);
        }
    }

    private void DrawCard(DrawingContext dc, Rect card, GhostRosterItem ghost)
    {
        Brush cardBrush = new SolidColorBrush(Color.FromRgb(12, 11, 16));
        Brush accent = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ghost.Color)!);
        dc.DrawRoundedRectangle(cardBrush, new Pen(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)), 1), card, 17, 17);
        dc.PushTransform(new TranslateTransform(card.X + 10, card.Y + 4));
        GhostArt.Draw(dc, new Size(90, 96), (Color)ColorConverter.ConvertFromString(ghost.Color)!, ghost.Form);
        dc.Pop();
        DrawText(dc, $"{(ghost.Index + 1):00}  {ghost.Name}", new Point(card.X + 103, card.Y + 22), 15, Brushes.White, FontWeights.Bold);
        DrawText(dc, ghost.Type, new Point(card.X + 103, card.Y + 48), 11, new SolidColorBrush(Color.FromRgb(190, 183, 198)), FontWeights.Normal);
        DrawText(dc, ghost.Index < 5 ? "기본 실루엣 · 팔레트형" : "테마 실루엣", new Point(card.X + 103, card.Y + 75), 10, accent, FontWeights.SemiBold);
    }

    private void DrawText(DrawingContext dc, string text, Point point, double size, Brush brush, FontWeight weight)
    {
        FormattedText formatted = new(text, System.Globalization.CultureInfo.GetCultureInfo("ko-KR"), FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal), size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, point);
    }
}
