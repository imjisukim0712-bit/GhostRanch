using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;

namespace GhostWidget;

/// <summary>Lets the player choose exactly which captured friends roam the desktop.</summary>
internal sealed class SummonSelectionWindow : Window
{
    private readonly MainWindow companion;
    private readonly Dictionary<int, ToggleButton> selections = [];
    private readonly TextBlock countLabel = new();

    internal SummonSelectionWindow(MainWindow companion, IEnumerable<int> candidateIndexes, IReadOnlySet<int> alreadySummoned)
    {
        this.companion = companion;
        Width = 430;
        Height = 540;
        MinWidth = 430;
        MinHeight = 420;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ShowInTaskbar = false;
        Topmost = true;
        Title = "유령 농장 — 친구 소환";

        Border shell = new()
        {
            Background = Brush("#17141F"),
            BorderBrush = Brush("#443B55"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(18)
        };
        Grid layout = new();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        shell.Child = layout;

        Grid header = new() { Margin = new Thickness(2, 0, 2, 13) };
        header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        header.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        TextBlock title = new()
        {
            Text = "친구 유령 소환",
            FontSize = 21,
            FontWeight = FontWeights.Bold,
            Foreground = Brushes.White
        };
        TextBlock description = new()
        {
            Text = "함께 데스크톱을 돌아다닐 친구만 골라줘.",
            Margin = new Thickness(0, 5, 0, 0),
            FontSize = 12,
            Foreground = Brush("#B8B0C6")
        };
        header.Children.Add(title);
        Grid.SetRow(description, 1);
        header.Children.Add(description);
        header.MouseLeftButtonDown += (_, e) => { if (e.ButtonState == MouseButtonState.Pressed) DragMove(); };
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        StackPanel list = new();
        foreach (int index in candidateIndexes)
        {
            GhostSpecies ghost = companion.GetSpecies(index);
            ToggleButton pick = CreateGhostChoice(index, ghost, alreadySummoned.Contains(index));
            pick.Checked += (_, _) => UpdateCount();
            pick.Unchecked += (_, _) => UpdateCount();
            selections.Add(index, pick);
            list.Children.Add(pick);
        }
        ScrollViewer scroller = new()
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(0, 0, 0, 12)
        };
        Grid.SetRow(scroller, 1);
        layout.Children.Add(scroller);

        Grid footer = new();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        countLabel.Foreground = Brush("#B8B0C6");
        countLabel.FontSize = 12;
        countLabel.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(countLabel);

        Button cancel = MakeButton("취소", "#302A3A");
        cancel.Click += (_, _) => Close();
        Grid.SetColumn(cancel, 1);
        footer.Children.Add(cancel);
        Button confirm = MakeButton("선택 소환", "#6557DB");
        confirm.Margin = new Thickness(8, 0, 0, 0);
        confirm.Click += (_, _) =>
        {
            companion.ApplySummonSelection(selections.Where(pair => pair.Value.IsChecked == true).Select(pair => pair.Key));
            Close();
        };
        Grid.SetColumn(confirm, 2);
        footer.Children.Add(confirm);
        Grid.SetRow(footer, 2);
        layout.Children.Add(footer);

        Content = shell;
        UpdateCount();
    }

    private ToggleButton CreateGhostChoice(int index, GhostSpecies ghost, bool isSelected)
    {
        GhostArtwork art = new()
        {
            Width = 47,
            Height = 55,
            ColorCode = ghost.Color,
            Form = ghost.Form,
            Margin = new Thickness(0, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        StackPanel detail = new() { VerticalAlignment = VerticalAlignment.Center };
        detail.Children.Add(new TextBlock
        {
            Text = $"{index + 1:00}  {ghost.Name}",
            FontSize = 14,
            FontWeight = FontWeights.SemiBold,
            Foreground = Brushes.White
        });
        detail.Children.Add(new TextBlock
        {
            Text = $"{ghost.Type} · {ghost.Personality}",
            Margin = new Thickness(0, 3, 0, 0),
            FontSize = 11,
            Foreground = Brush("#B8B0C6")
        });
        Grid content = new();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(art);
        Grid.SetColumn(detail, 1);
        content.Children.Add(detail);
        TextBlock checkMark = new()
        {
            Width = 25,
            Height = 25,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 17,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(8, 0, 0, 0)
        };
        Grid.SetColumn(checkMark, 2);
        content.Children.Add(checkMark);

        ToggleButton pick = new()
        {
            IsChecked = isSelected,
            Content = content,
            Foreground = Brushes.White,
            Background = Brush("#25202D"),
            BorderBrush = Brush("#443B55"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(12, 8, 12, 8),
            Margin = new Thickness(0, 0, 0, 8),
            Cursor = Cursors.Hand
        };
        void RefreshVisual()
        {
            bool selected = pick.IsChecked == true;
            pick.Background = Brush(selected ? "#332D68" : "#25202D");
            pick.BorderBrush = Brush(selected ? "#9387FF" : "#443B55");
            checkMark.Text = selected ? "✓" : "+";
            checkMark.Foreground = selected ? Brushes.White : Brush("#B8B0C6");
            checkMark.Background = Brush(selected ? "#7668ED" : "#393240");
        }
        pick.Checked += (_, _) => RefreshVisual();
        pick.Unchecked += (_, _) => RefreshVisual();
        RefreshVisual();
        return pick;
    }

    private void UpdateCount()
    {
        int count = selections.Values.Count(choice => choice.IsChecked == true);
        countLabel.Text = count == 0 ? "선택한 친구 없음" : $"{count}마리 선택";
    }

    private static Button MakeButton(string text, string color) => new()
    {
        Content = text,
        Foreground = Brushes.White,
        Background = Brush(color),
        BorderThickness = new Thickness(0),
        FontWeight = FontWeights.SemiBold,
        Padding = new Thickness(15, 9, 15, 9),
        Cursor = Cursors.Hand
    };

    private static SolidColorBrush Brush(string code) => new((Color)ColorConverter.ConvertFromString(code)!);
}
