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
    private bool playSessionActive;
    private bool closed;

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
        if (playSessionActive) return;
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
        if (!playSessionActive && FindNearbyDecor() is { } item)
        {
            flingVelocity = new Vector();
            PlayWithDecor(item);
            return;
        }
        BeginFling();
    }

    private Rect InteractionBounds()
    {
        const double margin = 20;
        return new Rect(Left - margin, Top - margin, Width + margin * 2, Height + margin * 2);
    }

    // Dropping a companion on or near a placed decoration plays with it immediately, instead of
    // waiting on the ambient roam-AI chance in ChooseDestination.
    private DecorWindow? FindNearbyDecor() =>
        DecorWindow.PlacedItems.FirstOrDefault(item => InteractionBounds().IntersectsWith(item.Bounds));

    /// <summary>Called by MainWindow.TryPlayWithNearbyDecor when a dragged item lands near this
    /// companion instead of the main ghost.</summary>
    internal bool TryPlayWithNearbyDecor(DecorWindow item)
    {
        if (sleeping || playSessionActive || item.IsPlaying || !InteractionBounds().IntersectsWith(item.Bounds)) return false;
        PlayWithDecor(item);
        return true;
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

    // Companions get the same per-item choreography as the main ghost (see MainWindow.PlayWithDecor
    // and its sequence methods for the equivalents this mirrors), adapted to what a companion window
    // actually has: no speech bubble (skipped, not replaced), SummonedGhostCanvas.Pulse() instead of
    // Bounce(), canvas.SetSleeping(...) instead of GhostArtwork.IsSleeping. Purely cosmetic either way.
    private async void PlayWithDecor(DecorWindow item)
    {
        if (playSessionActive || item.IsPlaying) return;
        lastVisitedDecor = item;
        lastVisitedDecorAt = DateTime.UtcNow;
        targetDecorItem = null;
        playSessionActive = true;
        item.IsPlaying = true;
        try
        {
            switch (DecorCatalog.Items[item.DecorIndex].Art)
            {
                case "SoccerBall": await DribbleSequence(item); break;
                case "YarnBall": await RollSequence(item); break;
                case "Frisbee": await ThrowFetchSequence(item); break;
                case "ToyCar": await AutoLoopSequence(item); break;
                case "BubbleMachine": await WiggleSequence(item); break;
                case "Curtain": await PeekabooSequence(item, .10, 900); break;
                case "TreeStump": await PeekabooSequence(item, .20, 700); break;
                case "Grave": await PeekabooSequence(item, .55, 1100); break;
                case "AtticTrunk": await PeekabooSequence(item, .10, 900, midPeek: true); break;
                case "Cave": await PeekabooSequence(item, .05, 1300); break;
                case "Hammock": await SettleSequence(item, sleep: true, 1300); break;
                case "Campfire": await SettleSequence(item, sleep: false, 1200); break;
                case "Gym": await ExerciseSequence(item); break;
                case "CafeTable": await SettleSequence(item, sleep: false, 1300); break;
                case "HotSpring": await SettleSequence(item, sleep: true, 1500); break;
                default: await SettleSequence(item, sleep: false, 900); break;
            }
        }
        catch (InvalidOperationException)
        {
            // "소환 유령 해산" (or the placed item being removed) can close a window mid-sequence;
            // the next property touch on it throws. Nothing left to do but stop cleanly.
        }
        finally
        {
            item.IsPlaying = false;
            playSessionActive = false;
        }
        if (!closed) ChooseDestination();
    }

    private async Task DribbleSequence(DecorWindow item)
    {
        Rect area = MainWindow.VirtualDesktopBounds;
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 300);
        for (int rep = 0; rep < 4; rep++)
        {
            canvas.Pulse();
            await Task.Delay(110);
            double angle = random.NextDouble() * Math.PI * 2;
            double distance = 70 + random.NextDouble() * 55;
            Point roll = new(
                Math.Clamp(item.Left + Math.Cos(angle) * distance, area.Left + 8, area.Right - item.Width - 8),
                Math.Clamp(item.Top + Math.Sin(angle) * distance, area.Top + 8, area.Bottom - item.Height - 8));
            canvas.SetDirection(roll.X >= item.Left ? 1 : -1);
            await item.AnimateToAsync(roll, 320);
            await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 260);
        }
        canvas.Pulse();
        await Task.Delay(120);
        await item.AnimateToAsync(item.HomePosition, 460);
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 260);
    }

    private async Task RollSequence(DecorWindow item)
    {
        Rect area = MainWindow.VirtualDesktopBounds;
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 300);
        for (int rep = 0; rep < 3; rep++)
        {
            canvas.Pulse();
            double angle = random.NextDouble() * Math.PI * 2;
            double distance = 90 + random.NextDouble() * 60;
            Point roll = new(
                Math.Clamp(item.Left + Math.Cos(angle) * distance, area.Left + 8, area.Right - item.Width - 8),
                Math.Clamp(item.Top + Math.Sin(angle) * distance, area.Top + 8, area.Bottom - item.Height - 8));
            canvas.SetDirection(roll.X >= item.Left ? 1 : -1);
            Task rollTask = item.AnimateToAsync(roll, 560);
            await Task.Delay(140);
            await AnimateSelfToAsync(item.ApproachPointFrom(roll, Width, Height), 480);
            await rollTask;
        }
        await item.AnimateToAsync(item.HomePosition, 480);
    }

    private async Task ThrowFetchSequence(DecorWindow item)
    {
        Rect area = MainWindow.VirtualDesktopBounds;
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 300);
        canvas.Pulse();
        await Task.Delay(100);
        double angle = random.NextDouble() * Math.PI * 2;
        double distance = 170 + random.NextDouble() * 90;
        Point landing = new(
            Math.Clamp(item.Left + Math.Cos(angle) * distance, area.Left + 8, area.Right - item.Width - 8),
            Math.Clamp(item.Top + Math.Sin(angle) * distance, area.Top + 8, area.Bottom - item.Height - 8));
        canvas.SetDirection(landing.X >= item.Left ? 1 : -1);
        await item.AnimateToAsync(landing, 480);
        await Task.Delay(260);
        await AnimateSelfToAsync(item.ApproachPointFrom(landing, Width, Height), 520);
        canvas.Pulse();
        await Task.Delay(120);
        await item.AnimateToAsync(item.HomePosition, 460);
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 260);
    }

    private async Task AutoLoopSequence(DecorWindow item)
    {
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 300);
        canvas.Pulse();
        await Task.Delay(160);
        Rect area = MainWindow.VirtualDesktopBounds;
        Point home = item.HomePosition;
        Point[] loop = [new(home.X + 60, home.Y), new(home.X + 60, home.Y + 55), new(home.X - 25, home.Y + 55), new(home.X - 25, home.Y), home];
        foreach (Point raw in loop)
        {
            Point clamped = new(
                Math.Clamp(raw.X, area.Left + 8, area.Right - item.Width - 8),
                Math.Clamp(raw.Y, area.Top + 8, area.Bottom - item.Height - 8));
            canvas.SetDirection(clamped.X >= item.Left ? 1 : -1);
            await item.AnimateToAsync(clamped, 260);
        }
        canvas.Pulse();
    }

    private async Task WiggleSequence(DecorWindow item)
    {
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 280);
        Point home = item.HomePosition;
        for (int rep = 0; rep < 4; rep++)
        {
            canvas.Pulse();
            await item.AnimateToAsync(new Point(home.X + (rep % 2 == 0 ? 4 : -4), home.Y), 90);
            await Task.Delay(90);
        }
        await item.AnimateToAsync(home, 140);
    }

    private async Task PeekabooSequence(DecorWindow item, double fadeTo, int holdMs, bool midPeek = false)
    {
        canvas.SetDirection(item.Left >= Left ? 1 : -1);
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 280);
        Fade(fadeTo, 240);
        if (midPeek)
        {
            await Task.Delay(holdMs / 2);
            Fade(.65, 160);
            await Task.Delay(240);
            Fade(fadeTo, 160);
            await Task.Delay(holdMs / 2);
        }
        else
        {
            await Task.Delay(holdMs);
        }
        Fade(1, 220);
        await Task.Delay(200);
    }

    private async Task SettleSequence(DecorWindow item, bool sleep, int holdMs)
    {
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 280);
        if (sleep) canvas.SetSleeping(true);
        await Task.Delay(holdMs);
        if (sleep) canvas.SetSleeping(sleeping);
    }

    private async Task ExerciseSequence(DecorWindow item)
    {
        await AnimateSelfToAsync(item.GetApproachPoint(Width, Height), 260);
        for (int rep = 0; rep < 5; rep++)
        {
            canvas.Pulse();
            await Task.Delay(230);
        }
    }

    private async Task AnimateSelfToAsync(Point target, int milliseconds)
    {
        Point start = new(Left, Top);
        int steps = Math.Max(1, milliseconds / 16);
        for (int step = 1; step <= steps; step++)
        {
            double t = 1 - Math.Pow(1 - (double)step / steps, 3);
            Left = start.X + (target.X - start.X) * t;
            Top = start.Y + (target.Y - start.Y) * t;
            canvas.SetDirection(target.X >= start.X ? 1 : -1);
            canvas.AdvanceFrame(16);
            await Task.Delay(16);
        }
    }

    private void Fade(double to, int milliseconds) =>
        BeginAnimation(OpacityProperty, new System.Windows.Media.Animation.DoubleAnimation(to, TimeSpan.FromMilliseconds(milliseconds))
        { EasingFunction = new System.Windows.Media.Animation.QuadraticEase { EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut } });

    private void MoveScheduledFrame(DateTime now)
    {
        if (!IsLoaded) return;
        double elapsedMilliseconds = Math.Clamp((now - lastFrameAt).TotalMilliseconds, 12, 66);
        lastFrameAt = now;
        MoveFrame(elapsedMilliseconds);
    }

    private void MoveFrame(double elapsedMilliseconds)
    {
        if (playSessionActive)
        {
            // A decor play sequence is directly tweening Left/Top itself; just keep the idle bob alive.
            canvas.AdvanceFrame(elapsedMilliseconds);
            return;
        }
        if (sleeping) return;
        if (dragging)
        {
            SampleDragMotion();
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
            if (targetDecorItem is { } item) PlayWithDecor(item);
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
        closed = true;
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

    /// <summary>Companions have no separate Bounce animation like the main ghost — this jumps the
    /// idle bob/squash phase forward for a visible little pop, standing in for a "kick"/"pop" cue.</summary>
    internal void Pulse()
    {
        frame += 6;
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
