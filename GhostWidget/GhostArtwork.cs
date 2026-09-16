using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace GhostWidget;

/// <summary>Shared vector renderer for every in-game ghost. Base bodies keep the supplied 64x80 ratio.</summary>
public sealed class GhostArtwork : FrameworkElement
{
    public static readonly DependencyProperty ColorCodeProperty = DependencyProperty.Register(
        nameof(ColorCode), typeof(string), typeof(GhostArtwork), new FrameworkPropertyMetadata("#F30100", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty FormProperty = DependencyProperty.Register(
        nameof(Form), typeof(string), typeof(GhostArtwork), new FrameworkPropertyMetadata("Base", FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty IsSleepingProperty = DependencyProperty.Register(
        nameof(IsSleeping), typeof(bool), typeof(GhostArtwork), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));
    public static readonly DependencyProperty FlashOpacityProperty = DependencyProperty.Register(
        nameof(FlashOpacity), typeof(double), typeof(GhostArtwork), new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    private double gazeX;
    private double gazeY;
    private int blinkFrames;
    private readonly DispatcherTimer bounceTimer = new() { Interval = TimeSpan.FromMilliseconds(70) };
    private double bouncePhase = Random.Shared.NextDouble() * Math.PI * 2;

    public GhostArtwork()
    {
        bounceTimer.Tick += (_, _) =>
        {
            if (IsSleeping) return;
            bouncePhase += .27;
            InvalidateVisual();
        };
        Loaded += (_, _) => bounceTimer.Start();
        Unloaded += (_, _) => bounceTimer.Stop();
    }

    public string ColorCode { get => (string)GetValue(ColorCodeProperty); set => SetValue(ColorCodeProperty, value); }
    public string Form { get => (string)GetValue(FormProperty); set => SetValue(FormProperty, value); }
    public bool IsSleeping { get => (bool)GetValue(IsSleepingProperty); set => SetValue(IsSleepingProperty, value); }
    public double FlashOpacity { get => (double)GetValue(FlashOpacityProperty); private set => SetValue(FlashOpacityProperty, value); }
    private Color flashColor = Colors.White;

    /// <summary>Brief hit-flash overlay. Works uniformly across every form (including the New1-30 art,
    /// whose brushes are hardcoded and can't be recolored directly) since it paints on top, not through the silhouette.</summary>
    public void PlayHitReaction(bool superEffective)
    {
        flashColor = superEffective ? Color.FromRgb(0xFF, 0xD9, 0x4A) : Colors.White;
        DoubleAnimationUsingKeyFrames animation = new() { Duration = TimeSpan.FromMilliseconds(superEffective ? 340 : 240) };
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(superEffective ? .75 : .55, KeyTime.FromPercent(0)));
        animation.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromPercent(1)));
        BeginAnimation(FlashOpacityProperty, animation);
    }

    public void SetGaze(double x, double y)
    {
        if (Math.Abs(gazeX - x) < .01 && Math.Abs(gazeY - y) < .01) return;
        gazeX = x;
        gazeY = y;
        InvalidateVisual();
    }
    public void Blink()
    {
        blinkFrames = 5;
        InvalidateVisual();
        DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(110) };
        timer.Tick += (_, _) => { blinkFrames = 0; timer.Stop(); InvalidateVisual(); };
        timer.Start();
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        Color color;
        try { color = (Color)ColorConverter.ConvertFromString(ColorCode)!; }
        catch { color = Color.FromRgb(243, 1, 0); }
        double wave = IsSleeping ? 0 : Math.Sin(bouncePhase);
        double airborne = Math.Max(0, wave);
        double squash = Math.Max(0, -wave);
        dc.PushTransform(new TranslateTransform(0, -airborne * 3.2));
        dc.PushTransform(new ScaleTransform(1 + squash * .085, 1 - squash * .055 + airborne * .04,
            RenderSize.Width / 2, RenderSize.Height * .86));
        GhostArt.Draw(dc, RenderSize, color, Form, gazeX, gazeY, blinkFrames > 0 || IsSleeping, FlashOpacity, flashColor);
        dc.Pop();
        dc.Pop();
    }
}

internal static class GhostArt
{
    private static readonly Geometry BaseBody = Geometry.Parse("M 49,10 C 31.3,10 17,24.3 17,42 L 17,79 C 17,86 21,90 28,90 C 34,90 38,86 39,79 C 42,87 45,90 50,90 C 55,90 58,86 59,79 C 61,86 65,90 71,90 C 77,90 81,86 81,79 L 81,42 C 81,24.3 66.7,10 49,10 Z");

    internal static void Draw(DrawingContext dc, Size size, Color color, string form, double gazeX = 0, double gazeY = 0, bool blink = false, double flashOpacity = 0, Color? flashColor = null)
    {
        double scale = Math.Min(size.Width / 98d, size.Height / 112d);
        double x = (size.Width - 98 * scale) / 2;
        double y = (size.Height - 112 * scale) / 2;
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(scale, scale));

        Brush body = new SolidColorBrush(color);
        if (form.StartsWith("New", StringComparison.Ordinal) && int.TryParse(form[3..], out int newIndex))
        {
            // These are independent vector compositions, not crops from a contact sheet.
            // Their source-inspired parts stay crisp at every desktop-widget scale.
            ApprovedGhostArt.Draw(dc, newIndex);
            DrawFlashOverlay(dc, flashOpacity, flashColor);
            dc.Pop();
            dc.Pop();
            return;
        }
        switch (form)
        {
            case "Round": DrawRound(dc, body); break;
            case "Pumpkin": DrawPumpkin(dc, body); break;
            case "Pirate": DrawPirate(dc, body); break;
            case "Candle": DrawCandle(dc, body); break;
            case "Moss": DrawMoss(dc, body); break;
            case "Moon": DrawMoon(dc, body); break;
            case "Star": DrawStarBody(dc, body); break;
            case "Cloud": DrawCloud(dc, body); break;
            case "Shadow": DrawShadow(dc, body); break;
            case "Rose": DrawRose(dc, body); break;
            case "Lava": DrawLava(dc, body); break;
            case "Bat": DrawBat(dc, body); break;
            case "Jelly": DrawJelly(dc, body); break;
            case "Wave": DrawWave(dc, body); break;
            case "Sand": DrawSand(dc, body); break;
            case "Witch": DrawWitch(dc, body); break;
            case "Legend": DrawLegend(dc, body); break;
            case "BossHearth": DrawBossHearth(dc, body); break;
            case "BossMoss": DrawBossMoss(dc, body); break;
            case "BossMist": DrawBossMist(dc, body); break;
            case "BossDawn": DrawBossDawn(dc, body); break;
            case "BossGate": DrawBossGate(dc, body); break;
            case "BossVolcano": DrawLava(dc, body); break;
            case "BossStorm": DrawWave(dc, body); break;
            case "BossTree": DrawBossTree(dc, body); break;
            case "BossCelestial": DrawBossCelestial(dc, body); break;
            case "BossVoid": DrawBossVoid(dc, body); break;
            default: dc.DrawGeometry(body, null, BaseBody); break;
        }

        // Texture/ornaments belong behind the face; the eyes must stay the foremost readable feature.
        DrawTheme(dc, form, color, body);
        DrawEyes(dc, gazeX, gazeY, blink, form);
        DrawFlashOverlay(dc, flashOpacity, flashColor);
        dc.Pop();
        dc.Pop();
    }

    private static void DrawFlashOverlay(DrawingContext dc, double flashOpacity, Color? flashColor)
    {
        if (flashOpacity <= 0) return;
        Brush flashBrush = new SolidColorBrush(flashColor ?? Colors.White) { Opacity = Math.Clamp(flashOpacity, 0, 1) };
        dc.DrawRoundedRectangle(flashBrush, null, new Rect(4, 2, 90, 100), 28, 28);
    }

    private static void DrawNewGhost(DrawingContext dc, Brush body, int kind)
    {
        Pen bodyPen = new(body, 8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        switch (kind)
        {
            case 1: dc.DrawGeometry(body, null, BaseBody); break; // sheet
            case 2: dc.DrawEllipse(body, null, new Point(49, 48), 27, 27); dc.DrawEllipse(body, null, new Point(34, 76), 9, 12); dc.DrawEllipse(body, null, new Point(64, 76), 9, 12); break;
            case 3: dc.DrawRoundedRectangle(body, null, new Rect(24, 19, 50, 66), 9, 9); break;
            case 4: dc.DrawGeometry(body, null, BaseBody); dc.DrawGeometry(body, null, Triangle(new Point(29, 25), new Point(34, 5), new Point(40, 25))); dc.DrawGeometry(body, null, Triangle(new Point(58, 25), new Point(64, 5), new Point(70, 25))); break;
            case 5: dc.DrawGeometry(body, null, Triangle(new Point(18, 82), new Point(49, 8), new Point(80, 82))); dc.DrawRoundedRectangle(body, null, new Rect(25, 51, 48, 34), 12, 12); break;
            case 6: dc.DrawGeometry(body, null, BaseBody); dc.DrawGeometry(body, null, Triangle(new Point(23, 29), new Point(30, 8), new Point(39, 29))); dc.DrawGeometry(body, null, Triangle(new Point(59, 29), new Point(68, 8), new Point(75, 29))); dc.DrawLine(bodyPen, new Point(76, 72), new Point(88, 82)); break;
            case 7: dc.DrawGeometry(body, null, BaseBody); dc.DrawEllipse(body, null, new Point(19, 45), 10, 17); dc.DrawEllipse(body, null, new Point(79, 45), 10, 17); break;
            case 8: dc.DrawEllipse(body, null, new Point(49, 45), 28, 25); for (int x = 25; x <= 73; x += 12) dc.DrawEllipse(body, null, new Point(x, 76), 5, 17); break;
            case 9: dc.DrawEllipse(body, null, new Point(49, 61), 39, 16); dc.DrawEllipse(body, null, new Point(49, 47), 20, 14); break;
            case 10: dc.DrawRoundedRectangle(body, null, new Rect(19, 28, 60, 50), 8, 8); dc.DrawLine(bodyPen, new Point(34, 28), new Point(27, 14)); dc.DrawLine(bodyPen, new Point(64, 28), new Point(71, 14)); break;
            case 11: dc.DrawGeometry(body, null, Triangle(new Point(13, 26), new Point(49, 39), new Point(13, 82))); dc.DrawGeometry(body, null, Triangle(new Point(85, 26), new Point(49, 39), new Point(85, 82))); break;
            case 12: dc.DrawEllipse(body, null, new Point(47, 58), 26, 22); dc.DrawEllipse(null, bodyPen, new Point(72, 58), 11, 13); dc.DrawGeometry(body, null, Triangle(new Point(22, 57), new Point(8, 68), new Point(23, 73))); break;
            case 13: dc.DrawEllipse(body, null, new Point(49, 46), 24, 29); dc.DrawLine(bodyPen, new Point(49, 75), new Point(49, 93)); break;
            case 14: dc.DrawRoundedRectangle(body, null, new Rect(25, 35, 48, 47), 15, 15); dc.DrawRectangle(body, null, new Rect(40, 19, 18, 19)); break;
            case 15: dc.DrawRectangle(body, null, new Rect(19, 48, 60, 34)); dc.DrawEllipse(body, null, new Point(49, 46), 30, 10); break;
            case 16: dc.DrawRoundedRectangle(body, null, new Rect(34, 49, 30, 38), 9, 9); dc.DrawEllipse(body, null, new Point(49, 40), 34, 17); break;
            case 17: dc.DrawRoundedRectangle(body, null, new Rect(31, 51, 36, 35), 12, 12); dc.DrawLine(bodyPen, new Point(38, 53), new Point(24, 20)); dc.DrawLine(bodyPen, new Point(60, 53), new Point(76, 22)); break;
            case 18: dc.DrawEllipse(body, null, new Point(49, 62), 28, 25); dc.DrawEllipse(body, null, new Point(49, 32), 20, 20); break;
            case 19: dc.DrawGeometry(body, null, Triangle(new Point(15, 56), new Point(49, 16), new Point(83, 56))); dc.DrawLine(bodyPen, new Point(49, 55), new Point(49, 88)); break;
            case 20: dc.DrawRoundedRectangle(body, null, new Rect(25, 31, 48, 51), 7, 7); dc.DrawEllipse(null, bodyPen, new Point(49, 28), 18, 13); break;
            case 21: dc.DrawGeometry(body, null, Triangle(new Point(13, 67), new Point(49, 20), new Point(84, 67))); dc.DrawGeometry(body, null, Triangle(new Point(35, 82), new Point(49, 20), new Point(63, 82))); break;
            case 22: dc.DrawEllipse(body, null, new Point(49, 57), 20, 21); for (int x = 18; x <= 80; x += 31) { dc.DrawLine(bodyPen, new Point(36, 57), new Point(x, 37)); dc.DrawLine(bodyPen, new Point(62, 64), new Point(x, 82)); } break;
            case 23: dc.DrawRoundedRectangle(body, null, new Rect(18, 58, 61, 25), 12, 12); dc.DrawEllipse(body, null, new Point(61, 47), 21, 21); break;
            case 24: dc.DrawGeometry(body, null, BaseBody); dc.DrawGeometry(body, null, Triangle(new Point(23, 54), new Point(4, 34), new Point(27, 73))); dc.DrawGeometry(body, null, Triangle(new Point(75, 54), new Point(94, 34), new Point(71, 73))); break;
            case 25: dc.DrawGeometry(body, null, BaseBody); for (int x = 27; x <= 67; x += 20) dc.DrawGeometry(body, null, Triangle(new Point(x, 22), new Point(x + 8, 3), new Point(x + 16, 22))); break;
            case 26: dc.DrawRoundedRectangle(body, null, new Rect(23, 25, 52, 59), 12, 12); dc.DrawRectangle(body, null, new Rect(18, 25, 62, 13)); break;
            case 27: dc.DrawGeometry(body, null, BaseBody); dc.DrawGeometry(body, null, Triangle(new Point(20, 27), new Point(8, 7), new Point(36, 27))); dc.DrawGeometry(body, null, Triangle(new Point(62, 27), new Point(90, 7), new Point(78, 27))); break;
            case 28: dc.DrawEllipse(body, null, new Point(49, 58), 18, 25); dc.DrawEllipse(body, null, new Point(25, 48), 20, 30); dc.DrawEllipse(body, null, new Point(73, 48), 20, 30); break;
            case 29: dc.DrawGeometry(body, null, Triangle(new Point(49, 7), new Point(20, 82), new Point(49, 94))); dc.DrawGeometry(body, null, Triangle(new Point(49, 7), new Point(78, 82), new Point(49, 94))); break;
            case 30: dc.DrawGeometry(body, null, StarPolygon(new Point(38, 53), 27, 12, 5)); dc.DrawGeometry(body, null, Triangle(new Point(54, 53), new Point(90, 35), new Point(63, 69))); break;
            default: dc.DrawGeometry(body, null, BaseBody); break;
        }
    }

    private static void DrawRound(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 46), 32, 32);
        dc.DrawEllipse(body, null, new Point(29, 74), 14, 15);
        dc.DrawEllipse(body, null, new Point(49, 76), 14, 15);
        dc.DrawEllipse(body, null, new Point(69, 74), 14, 15);
    }

    private static void DrawPumpkin(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(34, 52), 20, 35);
        dc.DrawEllipse(body, null, new Point(49, 48), 25, 39);
        dc.DrawEllipse(body, null, new Point(64, 52), 20, 35);
        dc.DrawRoundedRectangle(body, null, new Rect(29, 52, 40, 38), 17, 17);
    }

    private static void DrawPirate(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 48), 28, 25);
        dc.DrawRoundedRectangle(body, null, new Rect(22, 51, 54, 28), 13, 13);
        dc.DrawEllipse(body, null, new Point(23, 79), 8, 16);
        dc.DrawEllipse(body, null, new Point(40, 84), 8, 12);
        dc.DrawEllipse(body, null, new Point(58, 84), 8, 12);
        dc.DrawEllipse(body, null, new Point(75, 79), 8, 16);
    }

    private static void DrawCandle(DrawingContext dc, Brush body)
    {
        dc.DrawRoundedRectangle(body, null, new Rect(31, 24, 36, 58), 12, 12);
        dc.DrawEllipse(body, null, new Point(38, 81), 8, 10);
        dc.DrawEllipse(body, null, new Point(49, 85), 8, 8);
        dc.DrawEllipse(body, null, new Point(60, 81), 8, 10);
    }

    private static void DrawMoss(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(28, 49), 17, 23);
        dc.DrawEllipse(body, null, new Point(49, 35), 25, 33);
        dc.DrawEllipse(body, null, new Point(70, 52), 16, 21);
        dc.DrawEllipse(body, null, new Point(27, 75), 14, 16);
        dc.DrawEllipse(body, null, new Point(48, 80), 17, 16);
        dc.DrawEllipse(body, null, new Point(70, 75), 14, 16);
    }

    private static void DrawMoon(DrawingContext dc, Brush body)
    {
        Geometry crescent = Geometry.Combine(new EllipseGeometry(new Point(47, 52), 34, 39),
            new EllipseGeometry(new Point(63, 42), 29, 35), GeometryCombineMode.Exclude, null);
        dc.DrawGeometry(body, null, crescent);
        // A compact face core keeps both eyes inside the moon rather than on its cut-out.
        dc.DrawEllipse(body, null, new Point(49, 54), 21, 24);
        dc.DrawEllipse(body, null, new Point(34, 82), 12, 11);
    }

    private static void DrawStarBody(DrawingContext dc, Brush body)
    {
        dc.DrawGeometry(body, null, StarPolygon(new Point(49, 52), 39, 18, 5));
    }

    private static void DrawCloud(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(33, 37), 18, 18);
        dc.DrawEllipse(body, null, new Point(51, 28), 21, 22);
        dc.DrawEllipse(body, null, new Point(69, 40), 16, 16);
        dc.DrawRoundedRectangle(body, null, new Rect(18, 40, 64, 43), 18, 18);
        dc.DrawEllipse(body, null, new Point(31, 77), 13, 10);
        dc.DrawEllipse(body, null, new Point(50, 80), 13, 10);
        dc.DrawEllipse(body, null, new Point(68, 77), 13, 10);
    }

    private static void DrawShadow(DrawingContext dc, Brush body)
    {
        StreamGeometry wispy = new();
        using (StreamGeometryContext c = wispy.Open())
        {
            c.BeginFigure(new Point(49, 7), true, true);
            c.BezierTo(new Point(25, 10), new Point(13, 29), new Point(22, 51), true, false);
            c.BezierTo(new Point(29, 67), new Point(8, 87), new Point(17, 96), true, false);
            c.BezierTo(new Point(27, 101), new Point(31, 82), new Point(39, 75), true, false);
            c.BezierTo(new Point(42, 98), new Point(59, 100), new Point(58, 75), true, false);
            c.BezierTo(new Point(68, 91), new Point(79, 79), new Point(75, 55), true, false);
            c.BezierTo(new Point(89, 28), new Point(70, 8), new Point(49, 7), true, false);
        }
        dc.DrawGeometry(body, null, wispy);
    }

    private static void DrawRose(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 50), 23, 25);
        dc.DrawEllipse(body, null, new Point(31, 46), 18, 17);
        dc.DrawEllipse(body, null, new Point(67, 46), 18, 17);
        dc.DrawEllipse(body, null, new Point(38, 70), 17, 17);
        dc.DrawEllipse(body, null, new Point(60, 70), 17, 17);
        dc.DrawRoundedRectangle(body, null, new Rect(39, 63, 20, 27), 8, 8);
    }

    private static void DrawLava(DrawingContext dc, Brush body)
    {
        StreamGeometry flame = new();
        using (StreamGeometryContext c = flame.Open())
        {
            c.BeginFigure(new Point(49, 4), true, true);
            c.BezierTo(new Point(63, 24), new Point(77, 33), new Point(72, 57), true, false);
            c.BezierTo(new Point(70, 78), new Point(60, 91), new Point(49, 93), true, false);
            c.BezierTo(new Point(33, 93), new Point(20, 81), new Point(24, 61), true, false);
            c.BezierTo(new Point(27, 44), new Point(39, 41), new Point(35, 25), true, false);
            c.BezierTo(new Point(42, 29), new Point(47, 19), new Point(49, 4), true, false);
        }
        dc.DrawGeometry(body, null, flame);
    }

    private static void DrawBat(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 53), 19, 24);
        dc.DrawGeometry(body, null, Triangle(new Point(35, 38), new Point(40, 18), new Point(47, 36)));
        dc.DrawGeometry(body, null, Triangle(new Point(51, 36), new Point(58, 18), new Point(63, 38)));
        StreamGeometry leftWing = new();
        using (StreamGeometryContext c = leftWing.Open())
        {
            c.BeginFigure(new Point(35, 47), true, true);
            c.BezierTo(new Point(20, 37), new Point(6, 46), new Point(9, 69), true, false);
            c.LineTo(new Point(19, 62), true, false); c.LineTo(new Point(24, 73), true, false); c.LineTo(new Point(35, 62), true, false);
        }
        dc.DrawGeometry(body, null, leftWing);
        dc.PushTransform(new ScaleTransform(-1, 1, 49, 0));
        dc.DrawGeometry(body, null, leftWing);
        dc.Pop();
        dc.DrawEllipse(body, null, new Point(42, 76), 7, 12);
        dc.DrawEllipse(body, null, new Point(56, 76), 7, 12);
    }

    private static void DrawJelly(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 47), 29, 27);
        dc.DrawRoundedRectangle(body, null, new Rect(20, 47, 58, 28), 18, 18);
        dc.DrawEllipse(body, null, new Point(28, 78), 6, 16);
        dc.DrawEllipse(body, null, new Point(40, 82), 6, 19);
        dc.DrawEllipse(body, null, new Point(52, 82), 6, 19);
        dc.DrawEllipse(body, null, new Point(66, 78), 6, 16);
    }

    private static void DrawWave(DrawingContext dc, Brush body)
    {
        dc.DrawGeometry(body, null, BaseBody);
        dc.DrawGeometry(body, null, Triangle(new Point(17, 47), new Point(2, 35), new Point(17, 66)));
        dc.DrawGeometry(body, null, Triangle(new Point(81, 48), new Point(95, 36), new Point(81, 67)));
        dc.DrawEllipse(body, null, new Point(49, 89), 22, 8);
    }

    private static void DrawSand(DrawingContext dc, Brush body)
    {
        StreamGeometry dune = new();
        using (StreamGeometryContext c = dune.Open())
        {
            c.BeginFigure(new Point(49, 7), true, true);
            c.LineTo(new Point(83, 80), true, false);
            c.BezierTo(new Point(76, 89), new Point(66, 91), new Point(49, 87), true, false);
            c.BezierTo(new Point(32, 92), new Point(22, 89), new Point(15, 80), true, false);
            c.LineTo(new Point(49, 7), true, false);
        }
        dc.DrawGeometry(body, null, dune);
    }

    private static void DrawWitch(DrawingContext dc, Brush body)
    {
        // Face and cauldron/robe overlap: the hat now connects to a single silhouette.
        dc.DrawEllipse(body, null, new Point(49, 53), 23, 25);
        dc.DrawRoundedRectangle(body, null, new Rect(22, 64, 54, 24), 11, 11);
        dc.DrawEllipse(body, null, new Point(33, 86), 8, 7);
        dc.DrawEllipse(body, null, new Point(65, 86), 8, 7);
    }

    private static void DrawLegend(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 54), 34, 34);
        dc.DrawEllipse(body, null, new Point(24, 71), 13, 14);
        dc.DrawEllipse(body, null, new Point(17, 82), 8, 9);
        dc.DrawEllipse(body, null, new Point(74, 71), 13, 14);
        dc.DrawEllipse(body, null, new Point(81, 82), 8, 9);
    }

    // Boss silhouettes: each pairs with a DrawTheme accent (crown/horns/wings/halo) for a
    // richer read than the roster's 2-6 shape forms. BossVolcano/BossStorm reuse DrawLava/DrawWave
    // as their base body (see the Draw switch) since those forms are otherwise unused by any species.
    private static void DrawBossHearth(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 92), 27, 11);
        dc.DrawEllipse(body, null, new Point(49, 62), 30, 32);
        dc.DrawEllipse(body, null, new Point(49, 27), 14, 13);
    }

    private static void DrawBossMoss(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 96), 28, 10);
        dc.DrawEllipse(body, null, new Point(49, 64), 26, 32);
        dc.DrawEllipse(body, null, new Point(49, 29), 16, 15);
    }

    private static void DrawBossMist(DrawingContext dc, Brush body)
    {
        dc.DrawEllipse(body, null, new Point(49, 78), 30, 24);
        dc.DrawEllipse(body, null, new Point(49, 40), 22, 20);
    }

    private static void DrawBossDawn(DrawingContext dc, Brush body)
    {
        Geometry crescent = Geometry.Combine(new EllipseGeometry(new Point(46, 50), 36, 42),
            new EllipseGeometry(new Point(64, 40), 30, 37), GeometryCombineMode.Exclude, null);
        dc.DrawGeometry(body, null, crescent);
        dc.DrawEllipse(body, null, new Point(49, 56), 24, 30);
        dc.DrawEllipse(body, null, new Point(49, 94), 20, 12);
    }

    private static void DrawBossGate(DrawingContext dc, Brush body)
    {
        dc.DrawGeometry(body, null, Triangle(new Point(23, 88), new Point(49, 6), new Point(75, 88)));
        dc.DrawRoundedRectangle(body, null, new Rect(30, 80, 38, 14), 7, 7);
    }

    private static void DrawBossTree(DrawingContext dc, Brush body)
    {
        Brush canopy = new SolidColorBrush(Color.FromRgb(63, 143, 79));
        dc.DrawEllipse(canopy, null, new Point(32, 38), 15, 14);
        dc.DrawEllipse(canopy, null, new Point(66, 38), 15, 14);
        dc.DrawEllipse(canopy, null, new Point(49, 26), 21, 19);
        dc.DrawRoundedRectangle(body, null, new Rect(34, 52, 30, 42), 6, 6);
    }

    private static void DrawBossCelestial(DrawingContext dc, Brush body)
    {
        dc.DrawGeometry(body, null, StarPolygon(new Point(49, 54), 44, 20, 8));
        dc.DrawEllipse(body, null, new Point(49, 58), 22, 26);
    }

    private static void DrawBossVoid(DrawingContext dc, Brush body)
    {
        dc.DrawGeometry(body, null, Triangle(new Point(18, 92), new Point(49, 2), new Point(80, 92)));
        dc.DrawRoundedRectangle(body, null, new Rect(26, 82, 46, 16), 8, 8);
    }

    private static void DrawEyes(DrawingContext dc, double gazeX, double gazeY, bool blink, string form)
    {
        Brush white = Brushes.White;
        bool boldEyes = form is "BossGate" or "BossVoid";
        Brush pupil = new SolidColorBrush(boldEyes ? Color.FromRgb(0x8B, 0x42, 0xE8) : Color.FromRgb(9, 18, 253));
        double leftX = 31.5, rightX = 60.5, eyeY = 42.5, width = 7.5, height = blink ? 3 : 12.5;
        switch (form)
        {
            case "Candle": leftX = 42; rightX = 56; eyeY = 49; width = 5.5; height = blink ? 2.5 : 10; break;
            case "Moon": leftX = 42; rightX = 56; eyeY = 52; width = 5.5; height = blink ? 2.5 : 10; break;
            case "Star": leftX = 41; rightX = 57; eyeY = 52; width = 5.5; height = blink ? 2.5 : 10; break;
            case "Sand": leftX = 42; rightX = 56; eyeY = 53; width = 5.5; height = blink ? 2.5 : 10; break;
            case "Bat": leftX = 42; rightX = 56; eyeY = 53; width = 5.5; height = blink ? 2.5 : 9.5; break;
            case "Witch": leftX = 40; rightX = 58; eyeY = 54; width = 5.8; height = blink ? 2.5 : 9; break;
            case "Legend": leftX = 39; rightX = 59; eyeY = 54; width = 6.2; height = blink ? 2.5 : 10.5; break;
            case "BossHearth": leftX = 40; rightX = 58; eyeY = 27; width = 6; height = blink ? 2.5 : 10; break;
            case "BossMoss": leftX = 39; rightX = 59; eyeY = 30; width = 6.5; height = blink ? 2.5 : 10.5; break;
            case "BossMist": leftX = 37; rightX = 61; eyeY = 40; width = 7; height = blink ? 2.5 : 11; break;
            case "BossDawn": leftX = 37; rightX = 61; eyeY = 50; width = 6.5; height = blink ? 2.5 : 10.5; break;
            case "BossGate": leftX = 38; rightX = 60; eyeY = 56; width = 6; height = blink ? 2.5 : 9; break;
            case "BossVolcano": leftX = 40; rightX = 58; eyeY = 45; width = 6.5; height = blink ? 2.5 : 10; break;
            case "BossStorm": leftX = 36; rightX = 62; eyeY = 44; width = 7; height = blink ? 2.5 : 11; break;
            case "BossTree": leftX = 42; rightX = 56; eyeY = 68; width = 5.5; height = blink ? 2.5 : 9; break;
            case "BossCelestial": leftX = 38; rightX = 60; eyeY = 52; width = 6.5; height = blink ? 2.5 : 10; break;
            case "BossVoid": leftX = 37; rightX = 61; eyeY = 60; width = 6.5; height = blink ? 2.5 : 10; break;
        }
        dc.DrawEllipse(white, null, new Point(leftX, eyeY), width, height);
        dc.DrawEllipse(white, null, new Point(rightX, eyeY), width, height);
        if (!blink)
        {
            // Keep each pupil inside its own white ellipse. The directional cap still reads clearly at widget size.
            double pupilY = eyeY + height * .38 + Math.Clamp(gazeY * .12, -height * .10, height * .10);
            double leftPupilX = leftX + Math.Clamp(gazeX * .25, -width * .32, width * .32);
            double rightPupilX = rightX + Math.Clamp(gazeX * .22, -width * .28, width * .28);
            dc.DrawEllipse(pupil, null, new Point(leftPupilX, pupilY), width * .6, height * .52);
            dc.DrawEllipse(pupil, null, new Point(rightPupilX, pupilY), width * .65, height * .48);
        }
        if (form == "Pirate")
            dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(25, 23, 32)), null, new Point(rightX, eyeY), width + .7, height + .7);
    }

    private static void DrawTheme(DrawingContext dc, string form, Color bodyColor, Brush body)
    {
        switch (form)
        {
            case "Pumpkin":
                Pen groove = new(new SolidColorBrush(Color.FromRgb(190, 85, 0)), 2);
                dc.DrawGeometry(null, groove, Curve(new Point(38, 18), new Point(29, 42), new Point(29, 60), new Point(38, 80)));
                dc.DrawGeometry(null, groove, Curve(new Point(60, 18), new Point(69, 42), new Point(69, 60), new Point(60, 80)));
                dc.PushTransform(new RotateTransform(-25, 49, 13));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(72, 145, 48)), null, new Point(49, 11), 7, 3);
                dc.Pop();
                break;
            case "Pirate":
                Brush dark = new SolidColorBrush(Color.FromRgb(24, 22, 31));
                dc.DrawRoundedRectangle(dark, null, new Rect(14, 28, 70, 8), 3, 3);
                StreamGeometry tricorne = new();
                using (StreamGeometryContext c = tricorne.Open())
                {
                    c.BeginFigure(new Point(16, 29), true, true);
                    c.BezierTo(new Point(19, 9), new Point(33, 11), new Point(40, 24), true, false);
                    c.BezierTo(new Point(48, 5), new Point(61, 10), new Point(61, 24), true, false);
                    c.BezierTo(new Point(70, 9), new Point(80, 12), new Point(82, 29), true, false);
                }
                dc.DrawGeometry(dark, null, tricorne);
                dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(236, 190, 61)), 1.5), new Point(39, 25), new Point(61, 25));
                dc.DrawLine(new Pen(dark, 1.5), new Point(42, 34), new Point(69, 53));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(255, 210, 62)), null, new Point(82, 59), 2.5, 2.5);
                break;
            case "Candle":
                // Rounded, asymmetric flame curves avoid the party-hat silhouette.
                StreamGeometry outerFlame = new();
                using (StreamGeometryContext c = outerFlame.Open())
                {
                    c.BeginFigure(new Point(49, -5), true, true);
                    c.BezierTo(new Point(42, 5), new Point(39, 14), new Point(42, 20), true, false);
                    c.BezierTo(new Point(44, 25), new Point(47, 25), new Point(49, 21), true, false);
                    c.BezierTo(new Point(53, 25), new Point(59, 21), new Point(57, 14), true, false);
                    c.BezierTo(new Point(56, 7), new Point(51, 3), new Point(49, -5), true, false);
                }
                StreamGeometry innerFlame = new();
                using (StreamGeometryContext c = innerFlame.Open())
                {
                    c.BeginFigure(new Point(49, 5), true, true);
                    c.BezierTo(new Point(45, 11), new Point(45, 17), new Point(49, 20), true, false);
                    c.BezierTo(new Point(53, 17), new Point(53, 11), new Point(49, 5), true, false);
                }
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 128, 36)), null, outerFlame);
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(255, 239, 145)), null, innerFlame);
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)), null, new Point(35, 63), 3, 8);
                break;
            case "Moss":
                dc.PushTransform(new RotateTransform(-28, 48, 13));
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(183, 236, 99)), null, new Point(48, 10), 8, 3.2);
                dc.Pop();
                Brush moss = new SolidColorBrush(Color.FromArgb(135, 35, 112, 47));
                dc.DrawEllipse(moss, null, new Point(25, 70), 4, 3);
                dc.DrawEllipse(moss, null, new Point(66, 73), 3, 2.5);
                break;
            case "Moon":
                Brush moon = new SolidColorBrush(Color.FromRgb(255, 239, 163));
                DrawStar(dc, new Point(77, 19), 4, moon);
                break;
            case "Legend":
                Brush halo = new SolidColorBrush(Color.FromArgb(110, 255, 239, 163));
                dc.DrawEllipse(null, new Pen(halo, 2), new Point(49, 54), 41, 15);
                DrawStar(dc, new Point(14, 33), 3, halo); DrawStar(dc, new Point(84, 25), 3, halo);
                break;
            case "Star":
                DrawStar(dc, new Point(49, 12), 8, new SolidColorBrush(Color.FromRgb(255, 240, 139)));
                break;
            case "Rose":
                Brush petal = new SolidColorBrush(Color.FromRgb(255, 200, 223));
                dc.DrawEllipse(petal, null, new Point(40, 13), 7, 4);
                dc.DrawEllipse(petal, null, new Point(49, 10), 7, 4);
                dc.DrawEllipse(petal, null, new Point(58, 13), 7, 4);
                break;
            case "Lava":
                Brush flame = new SolidColorBrush(Color.FromRgb(255, 215, 66));
                dc.DrawGeometry(flame, null, Triangle(new Point(32, 17), new Point(37, 0), new Point(42, 17)));
                dc.DrawGeometry(flame, null, Triangle(new Point(55, 17), new Point(62, 2), new Point(66, 18)));
                break;
            case "Witch":
                Brush hat = new SolidColorBrush(Color.FromRgb(27, 23, 47));
                dc.DrawRoundedRectangle(hat, null, new Rect(15, 29, 68, 7), 3, 3);
                dc.DrawGeometry(hat, null, Triangle(new Point(25, 30), new Point(53, -4), new Point(70, 30)));
                dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(255, 209, 55)), null, new Rect(46, 28, 13, 2));
                break;
            case "Neon":
                Brush neon = new SolidColorBrush(Color.FromRgb(226, 255, 74));
                dc.DrawGeometry(neon, null, Triangle(new Point(49, 15), new Point(43, -4), new Point(56, 7)));
                break;
            case "Ice":
                Brush ice = new SolidColorBrush(Color.FromRgb(221, 255, 255));
                dc.DrawGeometry(ice, null, Triangle(new Point(26, 19), new Point(33, -4), new Point(39, 19)));
                dc.DrawGeometry(ice, null, Triangle(new Point(49, 15), new Point(56, -8), new Point(62, 18)));
                dc.DrawGeometry(ice, null, Triangle(new Point(65, 19), new Point(72, 0), new Point(78, 22)));
                break;
            case "BossHearth":
                Brush hearthFlame = new SolidColorBrush(Color.FromRgb(255, 178, 89));
                dc.DrawGeometry(hearthFlame, null, Triangle(new Point(30, 20), new Point(12, -14), new Point(48, 10)));
                dc.DrawGeometry(hearthFlame, null, Triangle(new Point(68, 20), new Point(86, -14), new Point(50, 10)));
                dc.DrawGeometry(hearthFlame, null, Triangle(new Point(40, 12), new Point(49, -18), new Point(58, 12)));
                dc.DrawGeometry(hearthFlame, null, Triangle(new Point(16, 64), new Point(-2, 44), new Point(24, 84)));
                dc.DrawGeometry(hearthFlame, null, Triangle(new Point(82, 64), new Point(100, 44), new Point(74, 84)));
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(190, 255, 150, 70)), 3), new Point(49, 62), 48, 44);
                break;
            case "BossMoss":
                Pen antlerMain = new(new SolidColorBrush(Color.FromRgb(72, 104, 48)), 5.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                Pen antlerTine = new(new SolidColorBrush(Color.FromRgb(72, 104, 48)), 4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                dc.DrawLine(antlerMain, new Point(38, 20), new Point(20, -16));
                dc.DrawLine(antlerMain, new Point(60, 20), new Point(78, -16));
                dc.DrawLine(antlerTine, new Point(30, 2), new Point(14, -2));
                dc.DrawLine(antlerTine, new Point(68, 2), new Point(84, -2));
                Brush leaf = new SolidColorBrush(Color.FromRgb(168, 224, 100));
                dc.DrawEllipse(leaf, null, new Point(20, -16), 6, 5);
                dc.DrawEllipse(leaf, null, new Point(78, -16), 6, 5);
                dc.DrawEllipse(leaf, null, new Point(14, -2), 4.5, 3.5);
                dc.DrawEllipse(leaf, null, new Point(84, -2), 4.5, 3.5);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(72, 132, 54)), null, new Point(18, 60), 12, 11);
                dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(72, 132, 54)), null, new Point(80, 60), 12, 11);
                break;
            case "BossMist":
                Brush mistFin = new SolidColorBrush(Color.FromRgb(16, 138, 161));
                dc.DrawGeometry(mistFin, null, Triangle(new Point(34, 26), new Point(42, -14), new Point(49, 14)));
                dc.DrawGeometry(mistFin, null, Triangle(new Point(64, 26), new Point(56, -14), new Point(49, 14)));
                dc.DrawGeometry(mistFin, null, Triangle(new Point(20, 44), new Point(-4, 30), new Point(26, 60)));
                dc.DrawGeometry(mistFin, null, Triangle(new Point(78, 44), new Point(102, 30), new Point(72, 60)));
                Pen tentacle = new(new SolidColorBrush(Color.FromRgb(30, 190, 220)), 5.5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                dc.DrawGeometry(null, tentacle, Curve(new Point(30, 92), new Point(20, 104), new Point(26, 112), new Point(16, 124)));
                dc.DrawGeometry(null, tentacle, Curve(new Point(49, 96), new Point(49, 108), new Point(53, 116), new Point(47, 128)));
                dc.DrawGeometry(null, tentacle, Curve(new Point(68, 92), new Point(78, 104), new Point(72, 112), new Point(82, 124)));
                break;
            case "BossDawn":
                Brush dawnAccent = new SolidColorBrush(Color.FromRgb(255, 216, 90));
                dc.DrawEllipse(null, new Pen(dawnAccent, 4), new Point(49, 24), 34, 12);
                DrawStar(dc, new Point(10, 44), 4.5, dawnAccent);
                DrawStar(dc, new Point(90, 38), 4.5, dawnAccent);
                DrawStar(dc, new Point(49, -8), 5, new SolidColorBrush(Color.FromRgb(255, 246, 222)));
                break;
            case "BossGate":
                Brush gateAccent = new SolidColorBrush(Color.FromRgb(58, 42, 82));
                dc.DrawGeometry(gateAccent, null, Triangle(new Point(22, 48), new Point(-2, 34), new Point(30, 62)));
                dc.DrawGeometry(gateAccent, null, Triangle(new Point(76, 48), new Point(100, 34), new Point(68, 62)));
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(170, 155, 84, 245)), 2.5), new Point(49, 62), 44, 20);
                break;
            case "BossVolcano":
                Brush obsidian = new SolidColorBrush(Color.FromRgb(50, 12, 0));
                dc.DrawGeometry(obsidian, null, Triangle(new Point(20, 46), new Point(-4, 30), new Point(28, 58)));
                dc.DrawGeometry(obsidian, null, Triangle(new Point(78, 46), new Point(102, 30), new Point(70, 58)));
                Brush crownGlow = new SolidColorBrush(Color.FromRgb(255, 224, 90));
                dc.DrawGeometry(crownGlow, null, Triangle(new Point(30, 16), new Point(38, -16), new Point(46, 16)));
                dc.DrawGeometry(crownGlow, null, Triangle(new Point(52, 16), new Point(60, -20), new Point(68, 16)));
                Pen crack = new(crownGlow, 2) { StartLineCap = PenLineCap.Round };
                dc.DrawLine(crack, new Point(33, 34), new Point(40, 54));
                dc.DrawLine(crack, new Point(64, 32), new Point(57, 52));
                break;
            case "BossStorm":
                Brush waveAccent = new SolidColorBrush(Color.FromRgb(13, 94, 128));
                dc.DrawGeometry(waveAccent, null, Triangle(new Point(24, 24), new Point(32, -16), new Point(44, 18)));
                dc.DrawGeometry(waveAccent, null, Triangle(new Point(54, 18), new Point(66, -16), new Point(74, 24)));
                dc.DrawGeometry(new SolidColorBrush(Color.FromRgb(234, 246, 255)), null, Triangle(new Point(38, 18), new Point(49, -10), new Point(60, 18)));
                break;
            case "BossTree":
                Pen bark = new(new SolidColorBrush(Color.FromRgb(46, 32, 19)), 2.5);
                dc.DrawLine(bark, new Point(41, 56), new Point(38, 92));
                dc.DrawLine(bark, new Point(57, 58), new Point(60, 92));
                Pen root = new(new SolidColorBrush(Color.FromRgb(85, 62, 38)), 5) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
                dc.DrawLine(root, new Point(37, 94), new Point(18, 104));
                dc.DrawLine(root, new Point(61, 94), new Point(80, 104));
                break;
            case "BossCelestial":
                Brush celestialHalo = new SolidColorBrush(Color.FromArgb(160, 255, 233, 176));
                dc.DrawEllipse(null, new Pen(celestialHalo, 2), new Point(49, 54), 50, 20);
                DrawStar(dc, new Point(2, 38), 3.5, new SolidColorBrush(Color.FromRgb(255, 224, 100)));
                DrawStar(dc, new Point(96, 44), 3.5, new SolidColorBrush(Color.FromRgb(255, 224, 100)));
                break;
            case "BossVoid":
                Brush voidAccent = new SolidColorBrush(Color.FromRgb(193, 100, 255));
                dc.DrawGeometry(voidAccent, null, Triangle(new Point(28, 20), new Point(38, -18), new Point(46, 20)));
                dc.DrawGeometry(voidAccent, null, Triangle(new Point(42, 20), new Point(49, -24), new Point(56, 20)));
                dc.DrawGeometry(voidAccent, null, Triangle(new Point(52, 20), new Point(60, -18), new Point(70, 20)));
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(120, 193, 100, 255)), 2.5), new Point(49, 64), 50, 24);
                dc.DrawEllipse(null, new Pen(new SolidColorBrush(Color.FromArgb(180, 193, 100, 255)), 2.5), new Point(49, 64), 36, 17);
                break;
        }
    }

    private static StreamGeometry Triangle(Point a, Point b, Point c)
    {
        StreamGeometry g = new(); using StreamGeometryContext cxt = g.Open();
        cxt.BeginFigure(a, true, true); cxt.LineTo(b, true, false); cxt.LineTo(c, true, false); return g;
    }
    private static StreamGeometry StarPolygon(Point center, double outerRadius, double innerRadius, int points)
    {
        StreamGeometry g = new(); using StreamGeometryContext c = g.Open();
        for (int i = 0; i < points * 2; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / points;
            double radius = i % 2 == 0 ? outerRadius : innerRadius;
            Point p = new(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
            if (i == 0) c.BeginFigure(p, true, true); else c.LineTo(p, true, false);
        }
        return g;
    }
    private static StreamGeometry Curve(Point start, Point c1, Point c2, Point end)
    {
        StreamGeometry g = new(); using StreamGeometryContext cxt = g.Open();
        cxt.BeginFigure(start, false, false); cxt.BezierTo(c1, c2, end, true, false); return g;
    }
    private static void DrawStar(DrawingContext dc, Point center, double radius, Brush brush)
    {
        StreamGeometry g = new(); using StreamGeometryContext c = g.Open();
        for (int i = 0; i < 8; i++)
        {
            double a = -Math.PI / 2 + i * Math.PI / 4; double r = i % 2 == 0 ? radius : radius * .42;
            Point p = new(center.X + Math.Cos(a) * r, center.Y + Math.Sin(a) * r);
            if (i == 0) c.BeginFigure(p, true, true); else c.LineTo(p, true, false);
        }
        dc.DrawGeometry(brush, null, g);
    }
}
