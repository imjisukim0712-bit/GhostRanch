using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace GhostWidget;

public partial class CaptureGameWindow : Window
{
    private readonly MainWindow companion;
    private readonly FarmWindow? panel;
    private readonly int speciesIndex;
    private readonly DispatcherTimer gameTimer = new() { Interval = TimeSpan.FromMilliseconds(16) };
    private double elapsed;
    private bool finished;
    private int hitPoints;
    private int maxHitPoints;
    private int playerHitPoints = 3;

    internal CaptureGameWindow(MainWindow companion, FarmWindow? panel, int speciesIndex)
    {
        InitializeComponent();
        this.companion = companion;
        this.panel = panel;
        this.speciesIndex = speciesIndex;
        GhostSpecies ghost = companion.GetSpecies(speciesIndex);
        WildGhostArtwork.ColorCode = ghost.Color;
        WildGhostArtwork.Form = ghost.Form;
        WildInfoText.Text = $"야생 {ghost.Name}  ·  {ghost.Type} 속성  ·  {ghost.Rarity}";
        maxHitPoints = hitPoints = companion.GetCaptureHitPoints(speciesIndex);
        gameTimer.Tick += GameFrame;
        UpdateHealth();
        UpdatePlayerHealth();
        RefreshOrbs();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        gameTimer.Start();
        if (!Environment.GetCommandLineArgs().Contains("--capture-preview")) return;
        await Task.Delay(380);
        RenderTargetBitmap bitmap = new((int)CaptureRoot.ActualWidth, (int)CaptureRoot.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(CaptureRoot);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "capture-preview.png"));
        encoder.Save(stream);
        Close();
        companion.Close();
    }

    private void GameFrame(object? sender, EventArgs e)
    {
        if (finished) return;
        elapsed += .016;
        EncounterMove.X = Math.Sin(elapsed * 1.55) * 34;
        EncounterMove.Y = Math.Sin(elapsed * 2.15) * 8;
        // Keep the fast rhythm, but curve the aim substantially toward the moving wild ghost.
        double rawAimX = Math.Sin(elapsed * 8.4) * 126 + Math.Sin(elapsed * 13.2) * 17;
        double rawAimY = Math.Sin(elapsed * 7.2) * 76 + Math.Sin(elapsed * 11.6) * 12;
        AimMove.X = rawAimX * .62 + EncounterMove.X * .38;
        AimMove.Y = rawAimY * .62 + EncounterMove.Y * .38;
        bool aligned = IsAimOnGhost();
        AimRing.Stroke = aligned ? new SolidColorBrush(Color.FromRgb(191, 245, 103)) : new SolidColorBrush(Color.FromRgb(243, 59, 59));
    }

    private void Throw_Click(object sender, RoutedEventArgs e)
    {
        if (finished) { Close(); return; }
        bool hit = IsAimOnGhost();
        GradeText.Text = hit ? "HIT!" : "MISS";
        GradeBadge.Background = hit ? new SolidColorBrush(Color.FromRgb(191, 245, 103)) : new SolidColorBrush(Color.FromRgb(255, 130, 125));
        GradeBadge.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(100)));
        GhostSpecies wild = companion.GetSpecies(speciesIndex);
        if (!hit)
        {
            playerHitPoints--;
            UpdatePlayerHealth();
            ResultText.Text = "MISS! 플레이어 체력이 1 감소했어요.";
            if (playerHitPoints <= 0)
            {
                finished = true;
                gameTimer.Stop();
                GuideText.Text = "기력이 다했어요. 유령은 다음에 다시 만나봐요.";
                ThrowButton.Content = "회복하고 돌아가기";
                ThrowButton.Background = new SolidColorBrush(Color.FromRgb(70, 65, 90));
                panel?.ShowResult("포획 실패: 플레이어 체력이 0이 되었어요.");
            }
            return;
        }

        hitPoints--;
        UpdateHealth();
        ResultText.Text = $"명중! {wild.Name}의 체력이 1 감소했어요.";
        if (hitPoints <= 0)
        {
            companion.ResolveCapture(speciesIndex);
            finished = true;
            gameTimer.Stop();
            ResultText.Text = $"성공! {wild.Name}이(가) 도감에 등록됐어요.";
            GuideText.Text = "새로운 유령과 친구가 되었어요!";
            ThrowButton.Content = "동료로 맞이하기";
            ThrowButton.Background = new SolidColorBrush(Color.FromRgb(114, 169, 86));
            panel?.ShowResult($"포획 성공! {wild.Name} · {wild.Type} 유령을 발견했어요.");
        }
    }

    private bool IsAimOnGhost()
    {
        double x = AimMove.X - EncounterMove.X;
        double y = AimMove.Y - EncounterMove.Y;
        // Deliberately wider than the visual reticle: aim anywhere over most of the ghost body.
        return (x * x) / (38 * 38) + (y * y) / (46 * 46) <= 1;
    }

    private void UpdateHealth()
    {
        string filled = new('●', hitPoints);
        string empty = new('○', Math.Max(0, maxHitPoints - hitPoints));
        WildHpText.Text = $"HP  {filled}{empty}";
    }

    private void UpdatePlayerHealth()
    {
        string filled = new('♥', Math.Max(0, playerHitPoints));
        string empty = new('♡', Math.Max(0, 3 - playerHitPoints));
        PlayerHpText.Text = $"YOU  {filled}{empty}";
    }

    private void RefreshOrbs() => OrbText.Text = $"◉ {companion.GetFarmSnapshot().Orbs}";
    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    protected override void OnClosed(EventArgs e) { gameTimer.Stop(); panel?.RefreshState(); base.OnClosed(e); }
}
