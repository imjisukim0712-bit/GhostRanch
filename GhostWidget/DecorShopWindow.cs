using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GhostWidget;

/// <summary>Buy, place, and remove the 15 desktop decorations. Purely a Luna sink + placement
/// manager — nothing here touches affection/energy/experience.</summary>
internal sealed class DecorShopWindow : Window
{
    private readonly MainWindow companion;
    private readonly StackPanel list = new();
    private readonly TextBlock lunaText = new();
    private readonly TextBlock resultText = new();
    private readonly Dictionary<DecorCategory, Button> tabs = [];
    private DecorCategory activeCategory = DecorCategory.Toy;

    internal DecorShopWindow(MainWindow companion)
    {
        this.companion = companion;
        Width = 460;
        Height = 620;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Title = "유령 농장 — 유령 상점";

        Border shell = new()
        {
            Background = Brush("#FFFDFC"),
            BorderBrush = Brush("#17000000"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(25),
            Effect = new System.Windows.Media.Effects.DropShadowEffect { Color = Colors.Black, BlurRadius = 28, ShadowDepth = 8, Opacity = .3 }
        };
        Grid layout = new();
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        layout.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        shell.Child = layout;

        Border header = new() { Background = Brush("#17131C"), CornerRadius = new CornerRadius(24, 24, 0, 0), Padding = new Thickness(24, 18, 18, 18) };
        header.MouseLeftButtonDown += (_, e) => { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); };
        Grid headerGrid = new();
        StackPanel heading = new();
        heading.Children.Add(new TextBlock { Text = "유령 상점", Foreground = Brushes.White, FontSize = 20, FontWeight = FontWeights.Bold });
        lunaText.Foreground = Brush("#F0A51D");
        lunaText.FontSize = 12;
        lunaText.FontWeight = FontWeights.Bold;
        lunaText.Margin = new Thickness(0, 4, 0, 0);
        heading.Children.Add(lunaText);
        headerGrid.Children.Add(heading);
        Button close = new()
        {
            Content = "×", HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top,
            Width = 30, Height = 30, FontSize = 20, Foreground = Brushes.White, Background = Brushes.Transparent,
            BorderThickness = new Thickness(0), Cursor = Cursors.Hand
        };
        close.Click += (_, _) => Close();
        headerGrid.Children.Add(close);
        header.Child = headerGrid;
        Grid.SetRow(header, 0);
        layout.Children.Add(header);

        Grid tabsRow = new() { Margin = new Thickness(18, 14, 18, 6) };
        tabsRow.ColumnDefinitions.Add(new ColumnDefinition());
        tabsRow.ColumnDefinitions.Add(new ColumnDefinition());
        tabsRow.ColumnDefinitions.Add(new ColumnDefinition());
        AddTab(tabsRow, DecorCategory.Toy, "장난감", 0);
        AddTab(tabsRow, DecorCategory.Hideout, "은신처", 1);
        AddTab(tabsRow, DecorCategory.Rest, "휴식 시설", 2);
        Grid.SetRow(tabsRow, 1);
        layout.Children.Add(tabsRow);

        Border resultBox = new()
        {
            Background = Brush("#F4F1F5"), CornerRadius = new CornerRadius(12), Padding = new Thickness(12, 7, 12, 7),
            Margin = new Thickness(18, 2, 18, 8), Child = resultText
        };
        resultText.Text = "가지고 놀 거리를 골라 놓아보세요.";
        resultText.FontSize = 11;
        resultText.TextWrapping = TextWrapping.Wrap;
        resultText.Foreground = Brush("#4B4550");
        Grid.SetRow(resultBox, 2);
        layout.Children.Add(resultBox);

        ScrollViewer scroller = new()
        {
            Content = list,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Margin = new Thickness(18, 0, 18, 18)
        };
        Grid.SetRow(scroller, 3);
        layout.Children.Add(scroller);

        Content = shell;
        RefreshTabs();
        RefreshList();
    }

    private void AddTab(Grid tabsRow, DecorCategory category, string label, int column)
    {
        Button tab = new()
        {
            Content = label, Height = 34, Margin = new Thickness(3, 0, 3, 0),
            BorderThickness = new Thickness(0), FontSize = 12, FontWeight = FontWeights.Bold, Cursor = Cursors.Hand
        };
        Grid.SetColumn(tab, column);
        tab.Click += (_, _) => { activeCategory = category; RefreshTabs(); RefreshList(); };
        tabs[category] = tab;
        tabsRow.Children.Add(tab);
    }

    private void RefreshTabs()
    {
        foreach ((DecorCategory category, Button tab) in tabs)
        {
            bool active = category == activeCategory;
            tab.Background = Brush(active ? "#6557DB" : "#F1EEF3");
            tab.Foreground = active ? Brushes.White : Brush("#5A5460");
        }
    }

    private void RefreshList()
    {
        lunaText.Text = $"보유 루나 ✦ {companion.Luna}";
        list.Children.Clear();
        for (int index = 0; index < DecorCatalog.Items.Length; index++)
        {
            DecorSpecies species = DecorCatalog.Items[index];
            if (species.Category != activeCategory) continue;
            list.Children.Add(CreateItemRow(index, species));
        }
    }

    private Border CreateItemRow(int index, DecorSpecies species)
    {
        Border thumbBorder = new()
        {
            Width = 58, Height = 58, CornerRadius = new CornerRadius(14), Background = Brush("#F1EEF3"),
            Child = new DecorCanvas(species.Art) { Width = 50, Height = 50, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center }
        };

        StackPanel detail = new() { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 8, 0) };
        detail.Children.Add(new TextBlock { Text = species.Name, FontSize = 14, FontWeight = FontWeights.Bold, Foreground = Brush("#231F29") });
        detail.Children.Add(new TextBlock
        {
            Text = species.Description, FontSize = 10.5, Foreground = Brush("#8A8390"), TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 0, 0), MaxWidth = 210
        });

        bool owned = companion.OwnsDecor(index);
        bool placed = companion.IsDecorPlaced(index);
        Button action = new()
        {
            Width = 82, Height = 40, BorderThickness = new Thickness(0), FontWeight = FontWeights.Bold, FontSize = 11,
            Foreground = Brushes.White, Cursor = Cursors.Hand
        };
        if (!owned)
        {
            bool affordable = companion.Luna >= species.Price;
            action.Content = $"구매\n{species.Price}✦";
            action.Background = Brush(affordable ? "#6557DB" : "#C9C4CF");
            action.IsEnabled = affordable;
            action.Click += (_, _) => { resultText.Text = companion.PurchaseDecor(index); RefreshList(); };
        }
        else if (!placed)
        {
            action.Content = "배치하기";
            action.Background = Brush("#4FA35A");
            action.Click += (_, _) => { companion.PlaceDecor(index); resultText.Text = $"{species.Name}을(를) 데스크톱에 놓았어요."; RefreshList(); };
        }
        else
        {
            action.Content = "치우기";
            action.Background = Brush("#D65C5C");
            action.Click += (_, _) => { companion.RemoveDecorPlacement(index); resultText.Text = $"{species.Name}을(를) 치웠어요. 다시 배치할 수 있어요."; RefreshList(); };
        }

        Grid content = new();
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.Children.Add(thumbBorder);
        Grid.SetColumn(detail, 1);
        content.Children.Add(detail);
        Grid.SetColumn(action, 2);
        content.Children.Add(action);

        return new Border
        {
            Background = Brush("#F8F7F9"),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 0, 0, 8),
            Child = content
        };
    }

    private static SolidColorBrush Brush(string code) => new((Color)ColorConverter.ConvertFromString(code)!);
}
