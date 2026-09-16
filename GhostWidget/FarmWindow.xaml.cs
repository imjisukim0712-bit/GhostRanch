using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GhostWidget;

public partial class FarmWindow : Window
{
    private readonly MainWindow companion;

    public FarmWindow(MainWindow companion)
    {
        InitializeComponent();
        this.companion = companion;
        RefreshState();
    }

    internal void RefreshState()
    {
        FarmSnapshot s = companion.GetFarmSnapshot();
        LunaText.Text = $"✦ {s.Luna}";
        OrbText.Text = $"◉ {s.Orbs}";
        WinText.Text = $"⚔ {s.Wins}";
        GhostNameText.Text = s.Name;
        LevelText.Text = $"{s.Type} 유령  ·  LV. {s.Level}  ·  EXP {s.Experience}/{s.Level * 25}";
        PersonalityText.Text = $"성격 · {s.Personality}";
        AffectionBar.Value = s.Affection;
        EnergyBar.Value = s.Energy;
        PanelGhostArtwork.ColorCode = s.Color;
        PanelGhostArtwork.Form = s.Form;
        SetAction(CaptureButton, s.CaptureReady, "◉", s.CaptureReady ? "포획하기" : "포획 대기", "#F33B3B");
        SetAction(RaiseButton, s.RaiseReady, "●", s.RaiseReady ? "달과자 10루나" : $"{s.RaiseSeconds / 60:00}:{s.RaiseSeconds % 60:00}", "#4356E0");
        SetAction(BattleButton, s.BattleReady, "⚔", s.BattleReady ? "결투하기" : "결투 대기", "#72A956");

        IReadOnlyList<GhostRosterItem> roster = companion.GetRoster();
        RosterCountText.Text = $"유령 도감  ·  {roster.Count}종";
        RosterItems.ItemsSource = roster;
    }

    private void Capture_Click(object sender, RoutedEventArgs e) => companion.OpenCaptureGame(this);
    private void Raise_Click(object sender, RoutedEventArgs e) => Act(companion.FeedGhost());
    private void Battle_Click(object sender, RoutedEventArgs e) => companion.OpenBattle(this);
    private void Gacha_Click(object sender, RoutedEventArgs e) => Act(companion.DrawGhostGacha());
    private void Rest_Click(object sender, RoutedEventArgs e) => Act(companion.FarmRest());
    private void GhostChoice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: not null } button && int.TryParse(button.Tag.ToString(), out int index)) Select(index);
    }

    private void Act(string result)
    {
        ResultText.Text = result;
        RefreshState();
    }

    private static void SetAction(Button button, bool enabled, string icon, string label, string activeColor)
    {
        button.IsEnabled = enabled;
        button.Background = (Brush)new BrushConverter().ConvertFromString(enabled ? activeColor : "#74717A")!;
        button.Content = new TextBlock { Text = $"{icon}\n{label}", TextAlignment = TextAlignment.Center, FontSize = enabled && label.Contains("10루나") ? 11 : 13 };
    }

    internal void ShowResult(string result) => Act(result);

    private void Select(int index)
    {
        companion.SelectGhost(index);
        ResultText.Text = "대표 유령을 바꿨어요.";
        RefreshState();
    }

    private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!Environment.GetCommandLineArgs().Contains("--farm-preview")) return;
        await Task.Delay(350);
        RenderTargetBitmap bitmap = new((int)PanelRoot.ActualWidth, (int)PanelRoot.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(PanelRoot);
        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using FileStream stream = File.Create(Path.Combine(AppContext.BaseDirectory, "farm-preview.png"));
        encoder.Save(stream);
        Close();
        companion.Close();
    }
}
