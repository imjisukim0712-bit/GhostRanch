using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GhostWidget;

internal enum DesktopEventKind { Quest, Capture, Battle }

/// <summary>Small in-app toast positioned just above the Windows taskbar.</summary>
internal sealed class DesktopNotificationWindow : Window
{
    private readonly MainWindow companion;
    private readonly DesktopEventKind kind;

    internal DesktopNotificationWindow(MainWindow companion, DesktopEventKind kind)
    {
        this.companion = companion;
        this.kind = kind;
        Width = 355; Height = 142;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Topmost = true;
        (string icon, string title, string message, string action) = kind switch
        {
            DesktopEventKind.Quest => ("✦", "유령의 작은 부탁", "친구 유령이 심부름을 부탁했어요. 완료하면 루나를 받아요!", "퀘스트 보기"),
            DesktopEventKind.Capture => ("◉", "야생 유령 출현", "안개 숲에서 포획 신호가 왔어요. 지금 봉인구를 던져볼까요?", "포획하러 가기"),
            _ => ("⚔", "결투 초대 도착", "떠돌이 유령이 결투를 신청했어요. 기술을 골라 맞서세요!", "결투 수락")
        };
        Border card = new() { Background = new SolidColorBrush(Color.FromRgb(24, 21, 32)), CornerRadius = new CornerRadius(19), Padding = new Thickness(18) };
        card.Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 22, ShadowDepth = 5, Opacity = .35 };
        Grid grid = new(); grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(45) }); grid.ColumnDefinitions.Add(new ColumnDefinition());
        TextBlock iconText = new() { Text = icon, Foreground = new SolidColorBrush(Color.FromRgb(255, 217, 83)), FontSize = 27, VerticalAlignment = VerticalAlignment.Top, HorizontalAlignment = HorizontalAlignment.Left };
        Grid.SetColumn(iconText, 0); grid.Children.Add(iconText);
        StackPanel body = new(); Grid.SetColumn(body, 1); grid.Children.Add(body);
        body.Children.Add(new TextBlock { Text = title, Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeights.Bold });
        body.Children.Add(new TextBlock { Text = message, Foreground = new SolidColorBrush(Color.FromRgb(194, 185, 204)), FontSize = 10.5, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 9) });
        StackPanel actions = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        Button later = new() { Content = "나중에", Foreground = new SolidColorBrush(Color.FromRgb(190, 180, 198)), Background = Brushes.Transparent, BorderThickness = new Thickness(0), FontSize = 10, Padding = new Thickness(8, 3, 8, 3) };
        Button accept = new() { Content = action, Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(82, 71, 211)), BorderThickness = new Thickness(0), FontWeight = FontWeights.Bold, FontSize = 10.5, Padding = new Thickness(12, 5, 12, 5) };
        later.Click += (_, _) => Close();
        accept.Click += (_, _) => { Close(); StartEvent(); };
        actions.Children.Add(later); actions.Children.Add(accept); body.Children.Add(actions);
        card.Child = grid; Content = card;
        Loaded += (_, _) =>
        {
            Rect area = SystemParameters.WorkArea;
            Left = area.Right - Width - 18; Top = area.Bottom - Height - 18;
        };
    }

    private void StartEvent()
    {
        switch (kind)
        {
            case DesktopEventKind.Quest: companion.StartQuestFromNotification(); break;
            case DesktopEventKind.Capture: companion.StartCaptureFromNotification(); break;
            case DesktopEventKind.Battle: companion.StartBattleFromNotification(); break;
        }
    }
}
