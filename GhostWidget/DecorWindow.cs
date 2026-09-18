using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace GhostWidget;

/// <summary>A placed decoration on the desktop: stationary furniture, not a roaming agent.
/// Draggable to reposition; right-click removes it (ownership is kept, only the placement goes).
/// PlacedItems is the single source of truth for "what's out and where" — used for saving,
/// for spawn-point avoidance, and by the ghosts' roam AI to find something to visit.</summary>
internal sealed class DecorWindow : Window
{
    internal const double Size = 100;
    internal static readonly List<DecorWindow> PlacedItems = [];

    private readonly Action onChanged;
    private NativePoint dragCursorStart;
    private Point windowStart;
    private bool dragging;

    internal int DecorIndex { get; }

    internal DecorWindow(int decorIndex, Point spawnPoint, Action onChanged)
    {
        DecorIndex = decorIndex;
        this.onChanged = onChanged;
        DecorSpecies species = DecorCatalog.Items[decorIndex];
        Width = Size; Height = Size;
        WindowStyle = WindowStyle.None; AllowsTransparency = true; Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize; ShowInTaskbar = false; Topmost = true;
        Title = $"유령 농장 — {species.Name}";
        Left = spawnPoint.X; Top = spawnPoint.Y;
        Content = new DecorCanvas(species.Art) { Width = Size, Height = Size };
        ContextMenu = BuildContextMenu();
        Loaded += (_, _) => PlacedItems.Add(this);
        MouseLeftButtonDown += Decor_MouseLeftButtonDown;
        MouseMove += Decor_MouseMove;
        MouseLeftButtonUp += Decor_MouseLeftButtonUp;
    }

    internal Rect Bounds => new(Left, Top, Width, Height);

    /// <summary>Where a ghost should stand to "use" this item — just beside it, not on top of the art.</summary>
    internal Point GetApproachPoint(double approacherWidth, double approacherHeight) =>
        new(Left - approacherWidth * .3, Top + Height * .55 - approacherHeight * .5);

    private ContextMenu BuildContextMenu()
    {
        // App.xaml has no shared ResourceDictionary, so this mirrors MainWindow.xaml's inline menu colors by hand.
        ContextMenu menu = new()
        {
            Background = new SolidColorBrush(Color.FromArgb(0xF2, 0x1A, 0x17, 0x23)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x30, 0x3B)),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(5)
        };
        MenuItem remove = new()
        {
            Header = "치우기",
            Foreground = new SolidColorBrush(Color.FromRgb(0xF8, 0xF7, 0xFF)),
            Padding = new Thickness(15, 10, 15, 10)
        };
        remove.Click += (_, _) => Close();
        menu.Items.Add(remove);
        return menu;
    }

    private void Decor_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!GetCursorPos(out dragCursorStart)) return;
        windowStart = new Point(Left, Top);
        dragging = true;
        CaptureMouse();
        e.Handled = true;
    }

    private void Decor_MouseMove(object sender, MouseEventArgs e)
    {
        if (!dragging || !GetCursorPos(out NativePoint current)) return;
        Left = windowStart.X + current.X - dragCursorStart.X;
        Top = windowStart.Y + current.Y - dragCursorStart.Y;
    }

    private void Decor_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!dragging) return;
        dragging = false;
        ReleaseMouseCapture();
        onChanged();
    }

    protected override void OnClosed(EventArgs e)
    {
        PlacedItems.Remove(this);
        onChanged();
        base.OnClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }
}

/// <summary>Renders a decoration's art once into a cached bitmap (it never changes — no facing,
/// no walk cycle) so redraws are a single DrawImage instead of re-issuing vector geometry.</summary>
internal sealed class DecorCanvas : FrameworkElement
{
    private readonly ImageSource art;

    internal DecorCanvas(string artKey)
    {
        const double density = 3;
        DrawingVisual visual = new();
        using (DrawingContext dc = visual.RenderOpen()) DecorArt.Draw(dc, new Size(100, 100), artKey);
        RenderTargetBitmap bitmap = new((int)(100 * density), (int)(100 * density), 96 * density, 96 * density, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        art = bitmap;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    protected override void OnRender(DrawingContext dc) => dc.DrawImage(art, new Rect(0, 0, ActualWidth, ActualHeight));
}
