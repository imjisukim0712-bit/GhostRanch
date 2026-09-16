using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GhostWidget;

/// <summary>Approval-only board: exact live renderer on the left, proposed art on the right.</summary>
internal sealed class ActualComparisonBoardWindow : Window
{
    private readonly MainWindow companion;
    private readonly ActualComparisonCanvas board;

    internal ActualComparisonBoardWindow(MainWindow companion)
    {
        this.companion = companion;
        board = new ActualComparisonCanvas(companion.GetRoster().Skip(5).Take(15).ToArray());
        Width = 1560;
        Height = 920;
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
        await Task.Delay(150);
        RenderTargetBitmap bitmap = new((int)board.ActualWidth, (int)board.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(board);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "actual-vs-candidate-preview.png"));
        encoder.Save(stream);
        Close();
        companion.Close();
    }
}

internal sealed class ActualComparisonCanvas : FrameworkElement
{
    private readonly IReadOnlyList<GhostRosterItem> current;
    private readonly BitmapImage candidates;

    internal ActualComparisonCanvas(IReadOnlyList<GhostRosterItem> current)
    {
        this.current = current;
        candidates = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "PreviewAssets", "candidate-pairs.png"), UriKind.Absolute));
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(20, 18, 28)), null, new Rect(0, 0, ActualWidth, ActualHeight), 26, 26);
        DrawText(dc, "현재 게임 유령  ↔  후보 시안", new Point(34, 27), 25, Brushes.White, FontWeights.Bold);
        DrawText(dc, "왼쪽은 게임이 실제로 그리는 유령 · 오른쪽은 신규 시안", new Point(34, 62), 12,
            new SolidColorBrush(Color.FromRgb(173, 166, 183)), FontWeights.Normal);

        for (int index = 0; index < current.Count; index++)
        {
            int column = index % 5;
            int row = index / 5;
            DrawPair(dc, new Rect(24 + column * 306, 105 + row * 258, 286, 234), current[index], index);
        }
    }

    private void DrawPair(DrawingContext dc, Rect card, GhostRosterItem ghost, int candidateIndex)
    {
        Brush cardBrush = new SolidColorBrush(Color.FromRgb(12, 11, 16));
        dc.DrawRoundedRectangle(cardBrush, new Pen(new SolidColorBrush(Color.FromArgb(38, 255, 255, 255)), 1), card, 17, 17);
        DrawText(dc, $"{ghost.Index + 1:00}  {ghost.Name}", new Point(card.X + 15, card.Y + 13), 14, Brushes.White, FontWeights.Bold);
        DrawText(dc, "현재", new Point(card.X + 57, card.Y + 42), 10, new SolidColorBrush(Color.FromRgb(130, 181, 255)), FontWeights.SemiBold);
        DrawText(dc, "후보", new Point(card.X + 199, card.Y + 42), 10, new SolidColorBrush(Color.FromRgb(214, 159, 255)), FontWeights.SemiBold);
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(100, 75, 133)), 1.5), new Point(card.X + 143, card.Y + 47), new Point(card.X + 143, card.Bottom - 16));

        dc.PushTransform(new TranslateTransform(card.X + 25, card.Y + 62));
        GhostArt.Draw(dc, new Size(112, 139), (Color)ColorConverter.ConvertFromString(ghost.Color)!, ghost.Form);
        dc.Pop();

        int sourceColumn = candidateIndex % 5;
        int sourceRow = candidateIndex / 5;
        int groupWidth = candidates.PixelWidth / 5;
        int groupHeight = candidates.PixelHeight / 3;
        int cropX = sourceColumn * groupWidth + groupWidth / 2;
        int cropY = sourceRow * groupHeight;
        int cropWidth = Math.Min(groupWidth / 2, candidates.PixelWidth - cropX);
        int cropHeight = Math.Min(groupHeight, candidates.PixelHeight - cropY);
        CroppedBitmap candidate = new(candidates, new Int32Rect(cropX, cropY, cropWidth, cropHeight));
        dc.DrawImage(candidate, new Rect(card.X + 150, card.Y + 51, 124, 166));
    }

    private void DrawText(DrawingContext dc, string text, Point point, double size, Brush brush, FontWeight weight)
    {
        FormattedText formatted = new(text, System.Globalization.CultureInfo.GetCultureInfo("ko-KR"), FlowDirection.LeftToRight,
            new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, weight, FontStretches.Normal), size, brush,
            VisualTreeHelper.GetDpi(this).PixelsPerDip);
        dc.DrawText(formatted, point);
    }
}
