using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.IO;

namespace GhostWidget;

internal enum QuestKind { Timing, HideAndSeek, CandySort, StarClean, Delivery, AppleSum }

/// <summary>Five small one-screen tasks requested by roaming ghosts.</summary>
internal sealed class QuestWindow : Window
{
    private const int ApplePointsPerClear = 10;
    private const int AppleTargetScore = 120;
    private readonly MainWindow companion;
    private readonly Random random = new();
    private readonly QuestKind kind;
    private readonly int reward;
    private readonly Grid gameArea = new();
    private readonly TextBlock status = new() { Foreground = new SolidColorBrush(Color.FromRgb(77, 69, 80)), FontSize = 12, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(55) };
    private readonly DispatcherTimer revealTimer = new();
    private double progress;
    private int success;
    private int sequenceIndex;
    private string[] sequence = [];
    private Border? timingMarker;
    private Border? timingTarget;
    private TextBlock? timingScore;
    private Action? afterReveal;
    private int revealGeneration;
    private Canvas? appleBoard;
    private readonly List<AppleCell> appleCells = [];
    private Point? appleDragStartPoint;
    private Point? appleDragCurrentPoint;
    private bool appleDragging;
    private int appleScore;
    private TextBlock? appleScoreText;
    private Border? appleSelectionRectangle;

    internal QuestWindow(MainWindow companion, QuestKind? forcedKind = null)
    {
        this.companion = companion;
        kind = forcedKind ?? (QuestKind)random.Next(6);
        reward = random.Next(18, 24);
        Width = kind == QuestKind.AppleSum ? 660 : 440;
        Height = kind == QuestKind.AppleSum ? 800 : 500;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Topmost = true;
        (string title, string description) = kind switch
        {
            QuestKind.Timing => ("달빛 박자", "좁은 PERFECT 구역에서 4연속 콤보를 만드세요."),
            QuestKind.HideAndSeek => ("안개 속 눈동자", "잠깐 보인 눈동자의 위치를 기억해 찾으세요."),
            QuestKind.CandySort => ("사탕 암호", "사탕 색 순서를 보고 같은 순서로 입력하세요."),
            QuestKind.StarClean => ("별자리 봉인", "사라진 숫자 순서대로 별을 눌러 봉인하세요."),
            QuestKind.Delivery => ("안개 배달", "잠깐 보인 6칸 경로를 기억해 배달하세요."),
            _ => ("사과 합치기", "사각형을 드래그해 합계 10을 만들 때마다 10점을 얻고, 120점을 모으세요.")
        };
        Border root = new() { Background = new SolidColorBrush(Color.FromRgb(255, 253, 252)), CornerRadius = new CornerRadius(24), BorderBrush = new SolidColorBrush(Color.FromArgb(24, 0, 0, 0)), BorderThickness = new Thickness(1) };
        root.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 26, ShadowDepth = 8, Opacity = .3 };
        Grid layout = new(); layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(73) }); layout.RowDefinitions.Add(new RowDefinition()); layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(62) });
        Border header = new() { Background = new SolidColorBrush(Color.FromRgb(23, 19, 28)), CornerRadius = new CornerRadius(23, 23, 0, 0) };
        header.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        Grid headerGrid = new() { Margin = new Thickness(24, 0, 24, 0) };
        StackPanel heading = new() { VerticalAlignment = VerticalAlignment.Center };
        heading.Children.Add(new TextBlock { Text = $"유령 심부름 · {title}", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 19 });
        heading.Children.Add(new TextBlock { Text = description, Foreground = new SolidColorBrush(Color.FromRgb(178, 169, 187)), FontSize = 10.5, Margin = new Thickness(0, 3, 0, 0) });
        headerGrid.Children.Add(heading); header.Child = headerGrid; layout.Children.Add(header);
        Border play = new() { Margin = new Thickness(22, 18, 22, 7), Background = new SolidColorBrush(Color.FromRgb(19, 17, 29)), CornerRadius = new CornerRadius(18), Padding = new Thickness(15) };
        gameArea.HorizontalAlignment = HorizontalAlignment.Stretch; gameArea.VerticalAlignment = VerticalAlignment.Stretch; play.Child = gameArea; Grid.SetRow(play, 1); layout.Children.Add(play);
        Grid footerContent = new();
        status.Margin = new Thickness(0, 0, kind == QuestKind.AppleSum ? 136 : 62, 0);
        footerContent.Children.Add(status);
        if (kind == QuestKind.AppleSum)
        {
            Button retry = new() { Content = "새 판", Width = 68, Height = 31, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 0, 58, 0), VerticalAlignment = VerticalAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(240, 96, 87)), Foreground = Brushes.White, BorderThickness = new Thickness(0), FontWeight = FontWeights.Bold, Cursor = Cursors.Hand };
            retry.Click += (_, _) => SetupAppleSum();
            footerContent.Children.Add(retry);
        }
        Button giveUp = new() { Content = "포기", Width = 52, Height = 31, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(219, 213, 224)), Foreground = new SolidColorBrush(Color.FromRgb(79, 70, 87)), BorderThickness = new Thickness(0), FontWeight = FontWeights.Bold, Cursor = Cursors.Hand };
        giveUp.Click += (_, _) => Close();
        footerContent.Children.Add(giveUp);
        Border footer = new() { Margin = new Thickness(22, 0, 22, 16), Background = new SolidColorBrush(Color.FromRgb(241, 238, 243)), CornerRadius = new CornerRadius(13), Padding = new Thickness(13, 7, 13, 7), Child = footerContent };
        Grid.SetRow(footer, 2); layout.Children.Add(footer); root.Child = layout; Content = root;
        timer.Tick += Timer_Tick;
        revealTimer.Tick += (_, _) => { revealTimer.Stop(); Action? action = afterReveal; afterReveal = null; action?.Invoke(); };
        SetupGame();
        Loaded += RenderPreview;
    }

    private void SetupGame()
    {
        switch (kind)
        {
            case QuestKind.Timing: SetupTiming(); break;
            case QuestKind.HideAndSeek: SetupHideAndSeek(); break;
            case QuestKind.CandySort: SetupCandy(); break;
            case QuestKind.StarClean: SetupStars(); break;
            case QuestKind.Delivery: SetupDelivery(); break;
            case QuestKind.AppleSum: SetupAppleSum(); break;
        }
    }

    private void SetupTiming()
    {
        gameArea.Children.Clear();
        StackPanel stack = new() { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = "MOON RHYTHM", Foreground = new SolidColorBrush(Color.FromRgb(203, 185, 255)), FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
        stack.Children.Add(new TextBlock { Text = "빛이 초록 달 조각 위를 지날 때 잡으세요", Foreground = new SolidColorBrush(Color.FromRgb(231, 226, 239)), FontSize = 14, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 7, 0, 14) });
        Canvas track = new() { Width = 350, Height = 38, HorizontalAlignment = HorizontalAlignment.Center };
        Border rail = new() { Width = 350, Height = 16, Background = new SolidColorBrush(Color.FromRgb(60, 56, 73)), CornerRadius = new CornerRadius(8) };
        Canvas.SetTop(rail, 11); track.Children.Add(rail);
        timingTarget = new Border { Width = 70, Height = 24, Background = new SolidColorBrush(Color.FromRgb(83, 190, 114)), CornerRadius = new CornerRadius(8), Child = new TextBlock { Text = "PERFECT", FontSize = 8, Foreground = Brushes.White, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center } };
        Canvas.SetLeft(timingTarget, 140); Canvas.SetTop(timingTarget, 7); track.Children.Add(timingTarget);
        timingMarker = new Border { Width = 8, Height = 34, Background = new SolidColorBrush(Color.FromRgb(255, 218, 75)), CornerRadius = new CornerRadius(4) };
        Canvas.SetTop(timingMarker, 2); track.Children.Add(timingMarker);
        stack.Children.Add(track);
        timingScore = new TextBlock { Text = "COMBO  0 / 4", Foreground = new SolidColorBrush(Color.FromRgb(255, 218, 75)), FontSize = 12, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 13, 0, 6) };
        stack.Children.Add(timingScore);
        Button hit = MakeButton("달빛 잡기", new SolidColorBrush(Color.FromRgb(92, 78, 218))); hit.Margin = new Thickness(77, 5, 77, 0); hit.Click += (_, _) =>
        {
            double targetWidth = 70 - success * 12;
            double targetStart = (100 - targetWidth / 3.42) / 2;
            double targetEnd = targetStart + targetWidth / 3.42;
            if (progress >= targetStart && progress <= targetEnd)
            {
                success++;
                timingScore!.Text = $"COMBO  {success} / 4  ·  SPEED ×{1 + success * .45:0.00}";
                status.Text = success == 4 ? "FINAL PERFECT!" : "PERFECT! 다음 박자는 더 빨라집니다.";
                UpdateTimingDifficulty();
            }
            else
            {
                success = 0;
                timingScore!.Text = "COMBO  0 / 4  ·  SPEED ×1.00";
                status.Text = "MISS! 콤보가 끊겼어요. 천천히 다시 시작합니다.";
                UpdateTimingDifficulty();
            }
            if (success >= 4) Complete("달빛 박자");
        };
        stack.Children.Add(hit); gameArea.Children.Add(stack); timer.Start(); status.Text = "바늘이 초록색 중앙을 지날 때 눌러주세요.";
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        progress = (progress + 1.6 + success * 1.7) % 101;
        if (timingMarker is not null) Canvas.SetLeft(timingMarker, progress * 3.42);
    }

    private void UpdateTimingDifficulty()
    {
        if (timingTarget is null) return;
        double width = 70 - success * 12;
        timingTarget.Width = width;
        Canvas.SetLeft(timingTarget, (350 - width) / 2);
    }

    private void SetupHideAndSeek()
    {
        gameArea.Children.Clear();
        revealGeneration++;
        int generation = revealGeneration;
        int sequenceLength = Math.Min(5, 3 + success);
        int[] eyeSequence = Enumerable.Range(0, 9).OrderBy(_ => random.Next()).Take(sequenceLength).ToArray();
        sequenceIndex = 0;
        UniformGrid grid = new() { Columns = 3, Margin = new Thickness(28, 12, 28, 12) };
        List<Button> tiles = [];
        for (int index = 0; index < 9; index++)
        {
            int tileIndex = index;
            Button tile = MakeButton("?", new SolidColorBrush(Color.FromRgb(57, 53, 69)));
            tile.IsEnabled = false;
            tile.Click += (_, _) =>
            {
                if (tileIndex != eyeSequence[sequenceIndex])
                {
                    status.Text = "순서가 달라요. 조금 쉬운 길이로 다시 보여줄게요.";
                    success = Math.Max(0, success - 1);
                    SetupHideAndSeek();
                    return;
                }
                sequenceIndex++;
                if (sequenceIndex == eyeSequence.Length)
                {
                    success++;
                    if (success >= 3) Complete("안개 속 눈동자");
                    else { status.Text = $"성공! 다음엔 {Math.Min(5, 3 + success)}개를 기억하세요."; SetupHideAndSeek(); }
                }
                else status.Text = $"맞았어요! 다음은 {sequenceIndex + 1}번째 눈동자예요.";
            };
            tiles.Add(tile); grid.Children.Add(tile);
        }
        gameArea.Children.Add(grid);
        status.Text = $"눈동자 위치 {sequenceLength}개를 순서대로 보여줄게요. 잘 기억하세요!";
        // SetupGame runs from the constructor, before this popup is visible.  Queue the
        // reveal until layout is live; otherwise IsVisible would cancel the first sequence.
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => RevealEyeSequence(tiles, eyeSequence, generation)));
    }

    private async void RevealEyeSequence(IReadOnlyList<Button> tiles, IReadOnlyList<int> eyeSequence, int generation)
    {
        for (int position = 0; position < eyeSequence.Count; position++)
        {
            if (generation != revealGeneration || !IsVisible) return;
            Button tile = tiles[eyeSequence[position]];
            tile.Content = (position + 1).ToString();
            tile.Background = new SolidColorBrush(Color.FromRgb(112, 88, 225));
            await Task.Delay(650);
            if (generation != revealGeneration || !IsVisible) return;
            tile.Content = "?";
            tile.Background = new SolidColorBrush(Color.FromRgb(57, 53, 69));
            await Task.Delay(180);
        }
        if (generation != revealGeneration || !IsVisible) return;
        foreach (Button tile in tiles) tile.IsEnabled = true;
        status.Text = $"{string.Join(" → ", Enumerable.Range(1, eyeSequence.Count))} 순서대로 눈동자를 눌러주세요.";
    }

    private void SetupCandy()
    {
        gameArea.Children.Clear();
        string[] colors = ["빨강", "파랑", "노랑"];
        string[] code = Enumerable.Range(0, 3 + success).Select(_ => colors[random.Next(colors.Length)]).ToArray();
        int codeIndex = 0;
        StackPanel stack = new() { VerticalAlignment = VerticalAlignment.Center };
        TextBlock codeText = new() { Text = string.Join("  ", code.Select(color => color[..1])), Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 22, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 18) };
        stack.Children.Add(codeText);
        UniformGrid choices = new() { Columns = 3 };
        foreach (string color in colors.OrderBy(_ => random.Next()))
        {
            Brush brush = color switch { "빨강" => new SolidColorBrush(Color.FromRgb(243, 59, 59)), "파랑" => new SolidColorBrush(Color.FromRgb(67, 86, 224)), _ => new SolidColorBrush(Color.FromRgb(240, 165, 29)) };
            Button button = MakeButton(color, brush); button.IsEnabled = false; button.Click += (_, _) =>
            {
                if (color != code[codeIndex]) { success = Math.Max(0, success - 1); status.Text = "암호가 틀렸어요! 콤보가 하나 줄고 새 암호가 나옵니다."; SetupCandy(); return; }
                codeIndex++; status.Text = $"암호 입력 {codeIndex}/{code.Length}";
                if (codeIndex == code.Length) { success++; if (success >= 3) Complete("사탕 암호"); else SetupCandy(); }
            };
            choices.Children.Add(button);
        }
        int candyRevealMs = Math.Max(500, 1200 - success * 220);
        stack.Children.Add(choices); gameArea.Children.Add(stack); status.Text = $"색 순서를 외우세요. {candyRevealMs / 1000.0:0.0}초 뒤 사라집니다.";
        BriefReveal(() => { codeText.Text = string.Join("  ", code.Select(_ => "●")); foreach (Button button in choices.Children.OfType<Button>()) button.IsEnabled = true; }, candyRevealMs);
    }

    private void SetupStars()
    {
        gameArea.Children.Clear();
        Canvas canvas = new() { Width = 350, Height = 225 };
        int next = 1;
        List<Button> stars = [];
        // Fixed, widely spaced slots guarantee every numbered card stays readable.
        Point[] slots = [new(25, 22), new(153, 18), new(282, 28), new(52, 142), new(176, 151), new(288, 133)];
        foreach ((int number, int slotIndex) in Enumerable.Range(1, 6).OrderBy(_ => random.Next()).Select((number, index) => (number, index)))
        {
            int starNumber = number;
            Button star = new() { Content = starNumber.ToString(), Foreground = new SolidColorBrush(Color.FromRgb(32, 25, 59)), FontSize = 22, FontWeight = FontWeights.Bold, Background = new SolidColorBrush(Color.FromRgb(232, 223, 255)), BorderBrush = new SolidColorBrush(Color.FromRgb(135, 112, 224)), BorderThickness = new Thickness(2), Cursor = Cursors.Hand, Width = 42, Height = 42, IsHitTestVisible = false };
            Canvas.SetLeft(star, slots[slotIndex].X); Canvas.SetTop(star, slots[slotIndex].Y);
            star.Click += (_, _) => { if (starNumber != next) { status.Text = "별자리 순서가 틀렸어요! 별자리가 다시 흩어집니다."; SetupStars(); return; } canvas.Children.Remove(star); next++; status.Text = $"별자리 봉인 {next - 1}/6"; if (next == 7) Complete("별자리 봉인"); };
            stars.Add(star); canvas.Children.Add(star);
        }
        gameArea.Children.Add(canvas); status.Text = "숫자 위치를 5초간 보여줍니다. 순서대로 눌러 봉인하세요.";
        BriefReveal(() => { foreach (Button star in stars) { star.Content = "✦"; star.Foreground = new SolidColorBrush(Color.FromRgb(255, 218, 75)); star.Background = Brushes.Transparent; star.BorderThickness = new Thickness(0); star.IsHitTestVisible = true; } }, 5000);
    }

    private void SetupDelivery()
    {
        gameArea.Children.Clear();
        sequenceIndex = 0;
        sequence = Enumerable.Range(0, 6).Select(_ => new[] { "↑", "→", "↓", "←" }[random.Next(4)]).ToArray();
        StackPanel stack = new() { VerticalAlignment = VerticalAlignment.Center };
        TextBlock route = new() { Text = string.Join("  ", sequence), Foreground = new SolidColorBrush(Color.FromRgb(255, 218, 75)), FontSize = 25, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 18) };
        stack.Children.Add(route);
        UniformGrid arrows = new() { Columns = 4 };
        foreach (string arrow in new[] { "↑", "→", "↓", "←" })
        {
            Button button = MakeButton(arrow, new SolidColorBrush(Color.FromRgb(85, 75, 150))); button.FontSize = 20; button.IsEnabled = false; button.Click += (_, _) =>
            {
                if (arrow == sequence[sequenceIndex]) { sequenceIndex++; status.Text = $"배달 경로 {sequenceIndex}/6"; if (sequenceIndex == 6) Complete("안개 배달"); }
                else { status.Text = "안개에 길을 잃었어요! 새 경로를 다시 외워야 해요."; SetupDelivery(); }
            };
            arrows.Children.Add(button);
        }
        stack.Children.Add(arrows); gameArea.Children.Add(stack); status.Text = "6칸 경로를 3초간 보여줍니다.";
        BriefReveal(() => { route.Text = string.Join("  ", sequence.Select(_ => "·")); foreach (Button button in arrows.Children.OfType<Button>()) button.IsEnabled = true; }, 3000);
    }

    private void SetupAppleSum()
    {
        gameArea.Children.Clear();
        appleCells.Clear();
        appleDragStartPoint = null;
        appleDragCurrentPoint = null;
        appleDragging = false;
        appleScore = 0;
        appleSelectionRectangle = null;

        StackPanel stack = new() { VerticalAlignment = VerticalAlignment.Center };
        stack.Children.Add(new TextBlock { Text = "APPLE 10", Foreground = new SolidColorBrush(Color.FromRgb(255, 153, 143)), FontSize = 11, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center });
        appleScoreText = new TextBlock { Text = $"점수  0 / {AppleTargetScore}  ·  남은 사과 144", Foreground = Brushes.White, FontSize = 17, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 5, 0, 8) };
        stack.Children.Add(appleScoreText);

        appleBoard = new Canvas { Width = 540, Height = 540, HorizontalAlignment = HorizontalAlignment.Center, Background = new SolidColorBrush(Color.FromRgb(34, 29, 40)) };
        appleBoard.MouseLeftButtonDown += AppleBoard_MouseLeftButtonDown;
        appleBoard.MouseMove += AppleBoard_MouseMove;
        appleBoard.MouseLeftButtonUp += AppleBoard_MouseLeftButtonUp;
        Brush[] appleColors =
        [
            new SolidColorBrush(Color.FromRgb(242, 83, 78)), new SolidColorBrush(Color.FromRgb(232, 54, 93)),
            new SolidColorBrush(Color.FromRgb(246, 119, 70)), new SolidColorBrush(Color.FromRgb(210, 69, 76))
        ];

        int[,] values = BuildAppleBoard();
        for (int row = 0; row < 12; row++)
        {
            for (int column = 0; column < 12; column++)
            {
                int value = values[row, column];
                Border apple = MakeApple(value, appleColors[(row * 5 + column) % appleColors.Length]);
                Canvas.SetLeft(apple, 12 + column * 44);
                Canvas.SetTop(apple, 6 + row * 44);
                AppleCell cell = new(row, column, value, apple);
                appleCells.Add(cell);
                appleBoard.Children.Add(apple);
            }
        }
        appleSelectionRectangle = new Border
        {
            Visibility = Visibility.Collapsed,
            IsHitTestVisible = false,
            Background = new SolidColorBrush(Color.FromArgb(57, 255, 220, 82)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(255, 226, 92)),
            BorderThickness = new Thickness(3),
            CornerRadius = new CornerRadius(11),
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = Color.FromRgb(255, 214, 55), BlurRadius = 10, ShadowDepth = 0, Opacity = .52
            }
        };
        Canvas.SetZIndex(appleSelectionRectangle, 10);
        appleBoard.Children.Add(appleSelectionRectangle);
        stack.Children.Add(appleBoard);
        gameArea.Children.Add(stack);
        status.Text = "사과 위를 드래그하면 선택 사각형이 나타납니다. 합계가 정확히 10이면 사라져요.";

        if (Environment.GetCommandLineArgs().Contains("--apple-drag-preview"))
            Dispatcher.BeginInvoke(ShowAppleDragPreview);
    }

    private int[,] BuildAppleBoard()
    {
        // Like the reference game, values are independently random 1–9; no neighbouring
        // complement pairs or answer patterns are planted.  We only reject a rare board
        // that has too few total rectangles to make the 120-point goal reasonable.
        for (int attempt = 0; attempt < 80; attempt++)
        {
            int[,] values = new int[12, 12];
            for (int row = 0; row < 12; row++)
                for (int column = 0; column < 12; column++)
                    values[row, column] = random.Next(1, 10);
            if (CountAppleRoutes(values) >= 34) return values;
        }
        // A normal random board virtually always passes the threshold. This final fallback
        // preserves the same independent random distribution if one unusually sparse board does not.
        int[,] fallback = new int[12, 12];
        for (int row = 0; row < 12; row++)
            for (int column = 0; column < 12; column++)
                fallback[row, column] = random.Next(1, 10);
        return fallback;
    }

    private static int CountAppleRoutes(int[,] values)
    {
        int routes = 0;
        for (int top = 0; top < 12; top++)
            for (int left = 0; left < 12; left++)
                for (int bottom = top; bottom < 12; bottom++)
                    for (int right = left; right < 12; right++)
                    {
                        int count = (bottom - top + 1) * (right - left + 1);
                        if (count is < 2 or > 6) continue;
                        int sum = 0;
                        for (int row = top; row <= bottom; row++)
                            for (int column = left; column <= right; column++)
                                sum += values[row, column];
                        if (sum == 10) routes++;
                    }
        return routes;
    }

    private static Border MakeApple(int value, Brush color)
    {
        Border apple = new()
        {
            Width = 36,
            Height = 36,
            Background = color,
            CornerRadius = new CornerRadius(18),
            BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255)),
            BorderThickness = new Thickness(1.5),
            Cursor = Cursors.Hand,
            Child = new TextBlock { Text = value.ToString(), Foreground = Brushes.White, FontSize = 16, FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };
        apple.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 5, ShadowDepth = 2, Opacity = .24 };
        return apple;
    }

    private void AppleBoard_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (appleBoard is null) return;
        // The rectangle is drawn from the exact pointer position, not from a particular apple.
        // This lets a player begin in any empty gap and include only the apples inside it.
        Point point = ClampToAppleBoard(e.GetPosition(appleBoard));
        appleDragStartPoint = point;
        appleDragCurrentPoint = point;
        appleDragging = true;
        appleBoard.CaptureMouse();
        UpdateAppleSelection();
        e.Handled = true;
    }

    private void AppleBoard_MouseMove(object sender, MouseEventArgs e)
    {
        if (!appleDragging || appleBoard is null) return;
        Point point = ClampToAppleBoard(e.GetPosition(appleBoard));
        if (appleDragCurrentPoint == point) return;
        appleDragCurrentPoint = point;
        UpdateAppleSelection();
    }

    private void AppleBoard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!appleDragging || appleBoard is null) return;
        appleDragging = false;
        appleBoard.ReleaseMouseCapture();
        ResolveAppleSelection();
        e.Handled = true;
    }

    private Point ClampToAppleBoard(Point point)
    {
        if (appleBoard is null) return point;
        return new Point(Math.Clamp(point.X, 0, appleBoard.Width), Math.Clamp(point.Y, 0, appleBoard.Height));
    }

    private List<AppleCell> SelectedAppleCells()
    {
        if (appleDragStartPoint is not Point start || appleDragCurrentPoint is not Point end) return [];
        double left = Math.Min(start.X, end.X);
        double right = Math.Max(start.X, end.X);
        double top = Math.Min(start.Y, end.Y);
        double bottom = Math.Max(start.Y, end.Y);
        // Count apples whose centre lies in the pointer-drawn rectangle. This makes empty-space
        // starts predictable while matching the visible range the player has enclosed.
        return appleCells.Where(cell => !cell.Removed
            && 30 + cell.Column * 44 >= left && 30 + cell.Column * 44 <= right
            && 24 + cell.Row * 44 >= top && 24 + cell.Row * 44 <= bottom).ToList();
    }

    private void UpdateAppleSelection()
    {
        HashSet<AppleCell> selected = SelectedAppleCells().ToHashSet();
        foreach (AppleCell cell in appleCells.Where(cell => !cell.Removed))
        {
            cell.Tile.BorderBrush = selected.Contains(cell) ? new SolidColorBrush(Color.FromRgb(255, 239, 130)) : new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
            cell.Tile.BorderThickness = selected.Contains(cell) ? new Thickness(3) : new Thickness(1.5);
        }
        int sum = selected.Sum(cell => cell.Value);
        UpdateAppleSelectionRectangle();
        if (selected.Count > 0) status.Text = $"드래그 중 · 선택한 사과 {selected.Count}개 · 합계 {sum}";
    }

    private void UpdateAppleSelectionRectangle()
    {
        if (appleSelectionRectangle is null || appleDragStartPoint is not Point start || appleDragCurrentPoint is not Point end) return;
        double left = Math.Min(start.X, end.X);
        double top = Math.Min(start.Y, end.Y);
        appleSelectionRectangle.Width = Math.Max(3, Math.Abs(end.X - start.X));
        appleSelectionRectangle.Height = Math.Max(3, Math.Abs(end.Y - start.Y));
        Canvas.SetLeft(appleSelectionRectangle, left);
        Canvas.SetTop(appleSelectionRectangle, top);
        appleSelectionRectangle.Visibility = Visibility.Visible;
    }

    private void HideAppleSelectionRectangle()
    {
        if (appleSelectionRectangle is not null) appleSelectionRectangle.Visibility = Visibility.Collapsed;
    }

    private void ResolveAppleSelection()
    {
        List<AppleCell> selected = SelectedAppleCells();
        int sum = selected.Sum(cell => cell.Value);
        if (selected.Count >= 2 && sum == 10)
        {
            foreach (AppleCell cell in selected)
            {
                cell.Removed = true;
                cell.Tile.Visibility = Visibility.Hidden;
            }
            appleScore += ApplePointsPerClear;
            int remaining = appleCells.Count(cell => !cell.Removed);
            appleScoreText!.Text = $"점수  {appleScore} / {AppleTargetScore}  ·  남은 사과 {remaining}";
            status.Text = $"합계 10 성공! +{ApplePointsPerClear}점 · {Math.Max(0, AppleTargetScore - appleScore)}점 남았어요.";
            if (appleScore >= AppleTargetScore) Complete("사과 합치기");
        }
        else
        {
            status.Text = selected.Count < 2
                ? "사과를 2개 이상 드래그해야 해요."
                : $"사과 {selected.Count}개 합계 {sum} · 정확히 10이 되도록 다시 드래그해보세요.";
        }
        appleDragStartPoint = null;
        appleDragCurrentPoint = null;
        HideAppleSelectionRectangle();
        foreach (AppleCell cell in appleCells.Where(cell => !cell.Removed))
        {
            cell.Tile.BorderBrush = new SolidColorBrush(Color.FromArgb(90, 255, 255, 255));
            cell.Tile.BorderThickness = new Thickness(1.5);
        }
    }

    private void ShowAppleDragPreview()
    {
        // The preview mode shows exactly the same mid-drag state as a player sees on the board.
        for (int top = 0; top < 12; top++)
        for (int left = 0; left < 12; left++)
        for (int bottom = top; bottom < 12; bottom++)
        for (int right = left; right < 12; right++)
        {
            List<AppleCell> cells = appleCells.Where(cell => cell.Row >= top && cell.Row <= bottom && cell.Column >= left && cell.Column <= right).ToList();
            if (cells.Count is < 2 or > 6 || cells.Sum(cell => cell.Value) != 10) continue;
            appleDragStartPoint = new Point(8 + left * 44, 2 + top * 44);
            appleDragCurrentPoint = new Point(52 + right * 44, 46 + bottom * 44);
            appleDragging = true;
            UpdateAppleSelection();
            return;
        }
    }

    private Button MakeButton(string text, Brush background)
    {
        FrameworkElementFactory border = new(typeof(Border));
        border.SetBinding(Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(14));
        FrameworkElementFactory presenter = new(typeof(ContentPresenter));
        presenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);
        ControlTemplate template = new(typeof(Button)) { VisualTree = border };
        return new Button { Content = text, Background = background, Foreground = Brushes.White, BorderThickness = new Thickness(0), FontWeight = FontWeights.Bold, Height = 50, Margin = new Thickness(4), Cursor = Cursors.Hand, Template = template };
    }

    private void BriefReveal(Action action, int milliseconds = 900)
    {
        revealTimer.Stop(); afterReveal = action; revealTimer.Interval = TimeSpan.FromMilliseconds(milliseconds); revealTimer.Start();
    }

    private async void Complete(string name)
    {
        timer.Stop();
        status.Text = companion.CompleteQuest(name, reward);
        gameArea.IsEnabled = false;
        await Task.Delay(900);
        Close();
    }

    private async void RenderPreview(object sender, RoutedEventArgs e)
    {
        if (!Environment.GetCommandLineArgs().Any(argument => argument is "--quest-preview" or "--apple-preview" or "--apple-drag-preview" or "--hide-preview")) return;
        await Task.Delay(250);
        RenderTargetBitmap bitmap = new((int)ActualWidth, (int)ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(this);
        PngBitmapEncoder encoder = new(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "quest-preview.png")); encoder.Save(stream);
        Close(); companion.Close();
    }
    protected override void OnClosed(EventArgs e) { timer.Stop(); revealTimer.Stop(); base.OnClosed(e); }

    private sealed class AppleCell(int row, int column, int value, Border tile)
    {
        internal int Row { get; } = row;
        internal int Column { get; } = column;
        internal int Value { get; } = value;
        internal Border Tile { get; } = tile;
        internal bool Removed { get; set; }
    }
}
