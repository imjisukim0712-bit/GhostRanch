using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;

namespace GhostWidget;

/// <summary>A lightweight independent companion for each captured ghost the player summons.</summary>
public sealed class SummonedGhostWindow : Window
{
    private readonly Random random = new();
    // One shared frame clock avoids timer overhead. The artwork itself is prerendered below,
    // so companions retain their original 30 fps motion without reconstructing vectors every frame.
    private static readonly List<SummonedGhostWindow> activeCompanions = [];
    private static readonly DispatcherTimer movementScheduler = new(DispatcherPriority.Background)
    {
        Interval = TimeSpan.FromMilliseconds(33)
    };
    private readonly SummonedGhostCanvas canvas;
    private readonly Func<Rect>? hostBounds;
    private Point destination;
    private NativePoint dragCursorStart;
    private Point windowStart;
    private Point lastDragCursor;
    private DateTime lastDragSampleAt;
    private DateTime dragStartedAt;
    private Vector flingVelocity;
    private Vector recentDragVelocity;
    private Point velocitySampleCursor;
    private DateTime velocitySampleAt;
    private DateTime lastDragMotionAt;
    private readonly List<DragSample> dragSamples = [];
    private bool dragging;
    private bool sleeping;
    private double ghostScale;
    private DateTime lastFrameAt;
    private readonly Point? spawnPoint;
    private DecorWindow? targetDecorItem;
    private DecorWindow? lastVisitedDecor;
    private DateTime lastVisitedDecorAt = DateTime.MinValue;
    private DateTime pausedUntil = DateTime.MinValue;

    internal int SpeciesIndex { get; }

    static SummonedGhostWindow()
    {
        movementScheduler.Tick += (_, _) =>
        {
            DateTime now = DateTime.UtcNow;
            foreach (SummonedGhostWindow companion in activeCompanions.ToArray()) companion.MoveScheduledFrame(now);
        };
    }

    internal SummonedGhostWindow(int speciesIndex, string name, string color, string form, double scale,
        Point? spawnPoint = null, Func<Rect>? hostBounds = null)
    {
        SpeciesIndex = speciesIndex;
        this.spawnPoint = spawnPoint;
        this.hostBounds = hostBounds;
        Width = 135;
        Height = 154;
        WindowStyle = WindowStyle.None;
        AllowsTransparency = true;
        Background = Brushes.Transparent;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        Topmost = true;
        Title = $"유령 농장 — {name}";
        canvas = new SummonedGhostCanvas(name, (Color)ColorConverter.ConvertFromString(color)!, form);
        canvas.HorizontalAlignment = HorizontalAlignment.Left;
        canvas.VerticalAlignment = VerticalAlignment.Top;
        UseLayoutRounding = true;
        Content = canvas;
        SetGhostScale(scale);
        Loaded += (_, _) =>
        {
            if (sleeping)
            {
                Rect area = SystemParameters.WorkArea;
                Left = Math.Clamp(Left, area.Left, area.Right - Width);
                Top = Math.Clamp(Top, area.Top, area.Bottom - Height);
                canvas.SetSleeping(true);
            }
            else
            {
                if (this.spawnPoint is Point start)
                {
                    Rect area = SystemParameters.WorkArea;
                    Left = Math.Clamp(start.X, area.Left, area.Right - Width);
                    Top = Math.Clamp(start.Y, area.Top, area.Bottom - Height);
                }
                else
                {
                    ChooseDestination();
                    Left = destination.X;
                    Top = destination.Y;
                }
                ChooseDestination();
                if (TouchesHost(new Point(Left, Top)))
                {
                    ChooseDestination();
                    Left = destination.X;
                    Top = destination.Y;
                }
            }
            lastFrameAt = DateTime.UtcNow;
            activeCompanions.Add(this);
            if (!movementScheduler.IsEnabled) movementScheduler.Start();
        };
        MouseLeftButtonDown += Friend_MouseLeftButtonDown;
        MouseMove += Friend_MouseMove;
        MouseLeftButtonUp += Friend_MouseLeftButtonUp;
    }

    internal void SetGhostScale(double scale)
    {
        ghostScale = scale;
        Width = 135 * scale;
        Height = 154 * scale;
        // Do not scale an oversized child with RenderTransform. At 중/하 WPF can clip that
        // transformed child at the window boundary. The canvas itself is laid out at its final size.
        canvas.SetDisplayScale(scale);
    }

    internal void EnterSleepMode(Point sleepSpot)
    {
        sleeping = true;
        dragging = false;
        flingVelocity = new Vector();
        destination = sleepSpot;
        Left = sleepSpot.X;
        Top = sleepSpot.Y;
        canvas.SetSleeping(true);
    }

    internal void WakeUp()
    {
        if (!sleeping) return;
        sleeping = false;
        canvas.SetSleeping(false);
        ChooseDestination();
    }

    private void Friend_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sleeping)
        {
            WakeUp();
            e.Handled = true;
            return;
        }
        StartDrag();
        e.Handled = true;
    }

    private void StartDrag()
    {
        if (!GetCursorPos(out dragCursorStart)) return;
        windowStart = new Point(Left, Top);
        lastDragCursor = new Point(dragCursorStart.X, dragCursorStart.Y);
        lastDragSampleAt = DateTime.UtcNow;
        dragStartedAt = lastDragSampleAt;
        velocitySampleCursor = lastDragCursor;
        velocitySampleAt = lastDragSampleAt;
        lastDragMotionAt = lastDragSampleAt;
        recentDragVelocity = new Vector();
        flingVelocity = new Vector();
        dragSamples.Clear();
        RecordDragSample(lastDragCursor);
        dragging = true;
        CaptureMouse();
    }

    private void Friend_MouseMove(object sender, MouseEventArgs e)
    {
        if (!dragging) return;
        if (!GetCursorPos(out NativePoint current)) return;
        Left = windowStart.X + current.X - dragCursorStart.X;
        Top = windowStart.Y + current.Y - dragCursorStart.Y;
        SampleDragVelocity(new Point(current.X, current.Y));
        RecordDragSample(new Point(current.X, current.Y));
    }

    private void Friend_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!dragging) return;
        dragging = false;
        ReleaseMouseCapture();
        BeginFling();
    }

    private void ChooseDestination()
    {
        targetDecorItem = null;
        DecorWindow[] decorCandidates = DecorWindow.PlacedItems
            .Where(item => !TouchesHost(new Point(item.Left, item.Top))
                && (item != lastVisitedDecor || DateTime.UtcNow - lastVisitedDecorAt > TimeSpan.FromSeconds(45)))
            .ToArray();
        if (decorCandidates.Length > 0 && random.NextDouble() < 0.15)
        {
            double DistanceSquaredTo(DecorWindow item) => (item.Left - Left) * (item.Left - Left) + (item.Top - Top) * (item.Top - Top);
            targetDecorItem = decorCandidates.OrderBy(DistanceSquaredTo).First();
            destination = targetDecorItem.GetApproachPoint(Width, Height);
            return;
        }
        Rect area = MainWindow.VirtualDesktopBounds;
        double minX = area.Left + 8;
        double minY = area.Top + 8;
        double maxX = Math.Max(minX + 1, area.Right - Width - 8);
        double maxY = Math.Max(minY + 1, area.Bottom - Height - 8);
        Point fallback = new(minX + random.NextDouble() * (maxX - minX), minY + random.NextDouble() * (maxY - minY));
        for (int attempt = 0; attempt < 18; attempt++)
        {
            Point candidate = new(minX + random.NextDouble() * (maxX - minX), minY + random.NextDouble() * (maxY - minY));
            if (!TouchesHost(candidate))
            {
                destination = candidate;
                return;
            }
        }
        destination = fallback;
    }

    private bool TouchesHost(Point position)
    {
        if (hostBounds is null) return false;
        Rect host = hostBounds();
        return new Rect(position.X, position.Y, Width, Height).IntersectsWith(host);
    }

    private void MoveScheduledFrame(DateTime now)
    {
        if (!IsLoaded) return;
        double elapsedMilliseconds = Math.Clamp((now - lastFrameAt).TotalMilliseconds, 12, 66);
        lastFrameAt = now;
        MoveFrame(elapsedMilliseconds);
    }

    private void MoveFrame(double elapsedMilliseconds)
    {
        if (sleeping) return;
        if (dragging)
        {
            SampleDragMotion();
            return;
        }
        if (DateTime.UtcNow < pausedUntil)
        {
            // Settled in at a decor item: hold position, but keep the idle bob/squash alive.
            canvas.AdvanceFrame(elapsedMilliseconds);
            return;
        }
        if (flingVelocity.Length > .05)
        {
            MoveFling(elapsedMilliseconds);
            canvas.AdvanceFrame(elapsedMilliseconds);
            return;
        }
        // A friend must not drift over the main pet's bubble.  Apart from looking clipped,
        // overlapping transparent top-level windows is far costlier for the compositor.
        if (TouchesHost(new Point(Left, Top))) ChooseDestination();
        double dx = destination.X - Left;
        double dy = destination.Y - Top;
        canvas.SetDirection(dx >= 0 ? 1 : -1);
        double distance = Math.Sqrt(dx * dx + dy * dy);
        if (distance < 4)
        {
            if (targetDecorItem is { } item)
            {
                lastVisitedDecor = item;
                lastVisitedDecorAt = DateTime.UtcNow;
                targetDecorItem = null;
                pausedUntil = DateTime.UtcNow.AddSeconds(1.2 + random.NextDouble() * .6);
            }
            else ChooseDestination();
        }
        else
        {
            double distanceScale = elapsedMilliseconds / 16d;
            Left += dx / distance * .82 * distanceScale;
            Top += dy / distance * .82 * distanceScale;
        }
        canvas.AdvanceFrame(elapsedMilliseconds);
    }

    private void SampleDragVelocity(Point cursor)
    {
        double milliseconds = Math.Max(1, (DateTime.UtcNow - lastDragSampleAt).TotalMilliseconds);
        flingVelocity = new Vector((cursor.X - lastDragCursor.X) / milliseconds, (cursor.Y - lastDragCursor.Y) / milliseconds);
        lastDragCursor = cursor;
        lastDragSampleAt = DateTime.UtcNow;
    }

    private void BeginFling()
    {
        if (GetCursorPos(out NativePoint cursor) && new Point(cursor.X, cursor.Y) != lastDragCursor)
        {
            SampleDragVelocity(new Point(cursor.X, cursor.Y));
            RecordDragSample(new Point(cursor.X, cursor.Y));
        }
        SampleDragMotion();
        DateTime now = DateTime.UtcNow;
        bool isQuickFlick = (now - lastDragMotionAt).TotalMilliseconds <= 130 && recentDragVelocity.Length >= .65;
        if (!isQuickFlick) { ChooseDestination(); return; }
        double launchSpeed = Math.Clamp(.45 + (recentDragVelocity.Length - .65) * 4.5, .45, 4.0);
        recentDragVelocity.Normalize();
        flingVelocity = recentDragVelocity * launchSpeed;
    }

    private void SampleDragMotion()
    {
        if (!GetCursorPos(out NativePoint cursor)) return;
        Point current = new(cursor.X, cursor.Y);
        DateTime now = DateTime.UtcNow;
        double elapsed = Math.Max(1, (now - velocitySampleAt).TotalMilliseconds);
        if (elapsed < 12) return;
        Vector rawVelocity = (current - velocitySampleCursor) / elapsed;
        if (rawVelocity.Length > .01)
        {
            recentDragVelocity = recentDragVelocity * .28 + rawVelocity * .72;
            lastDragMotionAt = now;
        }
        else recentDragVelocity *= .65;
        velocitySampleCursor = current;
        velocitySampleAt = now;
    }

    private void RecordDragSample(Point cursor)
    {
        DateTime now = DateTime.UtcNow;
        dragSamples.Add(new DragSample(cursor, now));
        dragSamples.RemoveAll(item => (now - item.At).TotalMilliseconds > 220);
    }

    private void MoveFling(double elapsedMilliseconds)
    {
        Rect area = MainWindow.VirtualDesktopBounds;
        double minX = area.Left + 8;
        double minY = area.Top + 8;
        double maxX = Math.Max(minX, area.Right - Width - 8);
        double maxY = Math.Max(minY, area.Bottom - Height - 8);
        double nextX = Left + flingVelocity.X * elapsedMilliseconds;
        double nextY = Top + flingVelocity.Y * elapsedMilliseconds;
        if (nextX < minX || nextX > maxX)
        {
            nextX = Math.Clamp(nextX, minX, maxX);
            flingVelocity.X *= -.42;
        }
        if (nextY < minY || nextY > maxY)
        {
            nextY = Math.Clamp(nextY, minY, maxY);
            flingVelocity.Y *= -.42;
        }
        canvas.SetDirection(flingVelocity.X >= 0 ? 1 : -1);
        Left = nextX;
        Top = nextY;
        flingVelocity *= Math.Pow(.90, elapsedMilliseconds / 16d);
        if (flingVelocity.Length < .05)
        {
            flingVelocity = new Vector();
            ChooseDestination();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        activeCompanions.Remove(this);
        if (activeCompanions.Count == 0) movementScheduler.Stop();
        base.OnClosed(e);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint { public int X; public int Y; }

    private readonly record struct DragSample(Point Position, DateTime At);
}

internal sealed class SummonedGhostCanvas : FrameworkElement
{
    private const double BaseWidth = 135;
    private const double BaseHeight = 154;
    private readonly string name;
    private readonly Color color;
    private readonly string form;
    private double frame;
    private double gazeX = 14;
    private int facing = 1;
    private double displayScale = 1;
    private readonly ImageSource leftFacingArt;
    private readonly ImageSource rightFacingArt;
    private readonly ImageSource sleepingArt;
    private bool sleeping;
    private FormattedText? label;
    private double labelPixelsPerDip;

    internal SummonedGhostCanvas(string name, Color color, string form)
    {
        this.name = name;
        this.color = color;
        this.form = form;
        // GhostArt creates many paths, brushes, and effects. Rasterizing the two directions once
        // removes that CPU work from every animation frame while remaining crisp at the largest size.
        leftFacingArt = RenderArtwork(-14);
        rightFacingArt = RenderArtwork(14);
        sleepingArt = RenderArtwork(0, blink: true);
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
    }

    private ImageSource RenderArtwork(double gaze, bool blink = false)
    {
        const double artWidth = 105;
        const double artHeight = 108;
        const double density = 3;
        DrawingVisual visual = new();
        using (DrawingContext drawing = visual.RenderOpen())
        {
            GhostArt.Draw(drawing, new Size(artWidth, artHeight), color, form, gaze, 0, blink);
        }
        RenderTargetBitmap bitmap = new((int)(artWidth * density), (int)(artHeight * density),
            96 * density, 96 * density, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    internal void AdvanceFrame(double elapsedMilliseconds)
    {
        frame += elapsedMilliseconds / 16d;
        InvalidateVisual();
    }

    internal void SetDirection(int direction)
    {
        if (facing == direction) return;
        facing = direction;
        gazeX = direction * 14;
    }

    internal void SetDisplayScale(double scale)
    {
        displayScale = scale;
        Width = BaseWidth * scale;
        Height = BaseHeight * scale;
        InvalidateVisual();
    }

    internal void SetSleeping(bool value)
    {
        if (sleeping == value) return;
        sleeping = value;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double bob = Math.Sin(frame / 9.0) * 3;
        double wave = Math.Sin(frame * .27);
        double airborne = Math.Max(0, wave);
        double squash = Math.Max(0, -wave);
        dc.PushTransform(new ScaleTransform(displayScale, displayScale));
        dc.PushTransform(new TranslateTransform(15, 12 + bob - airborne * 3));
        // A small forward lean complements the strongly offset pupils without mirroring themed hats/accessories.
        dc.PushTransform(new RotateTransform(facing * 3.8, 52, 80));
        dc.PushTransform(new ScaleTransform(1 + squash * .085, 1 - squash * .055 + airborne * .04, 52, 92));
        dc.DrawImage(sleeping ? sleepingArt : facing >= 0 ? rightFacingArt : leftFacingArt, new Rect(0, 0, 105, 108));
        dc.Pop();
        dc.Pop();
        dc.Pop();

        double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (label is null || Math.Abs(labelPixelsPerDip - pixelsPerDip) > .001)
        {
            labelPixelsPerDip = pixelsPerDip;
            label = new FormattedText(name, System.Globalization.CultureInfo.GetCultureInfo("ko-KR"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 10, Brushes.White, pixelsPerDip);
        }
        dc.DrawText(label, new Point((BaseWidth - label.Width) / 2, 121));
        if (sleeping)
        {
            FormattedText zzz = new("Zz", System.Globalization.CultureInfo.GetCultureInfo("ko-KR"), FlowDirection.LeftToRight,
                new Typeface("Segoe UI"), 17, new SolidColorBrush(Color.FromRgb(226, 210, 255)), pixelsPerDip);
            dc.DrawText(zzz, new Point(91, 10));
        }
        dc.Pop();
    }
}
