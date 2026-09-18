using System.Windows;
using System.Windows.Media;

namespace GhostWidget;

/// <summary>Hand-drawn vector art for the 15 desktop decorations, one static composition per
/// DecorSpecies.Art key. Same fixed-box-then-scale convention as GhostArt.Draw: callers pass the
/// element's actual Size and this centers/scales a 100x100 design to fit it.</summary>
internal static class DecorArt
{
    internal static void Draw(DrawingContext dc, Size size, string art)
    {
        double scale = Math.Min(size.Width / 100d, size.Height / 100d);
        double x = (size.Width - 100 * scale) / 2;
        double y = (size.Height - 100 * scale) / 2;
        dc.PushTransform(new TranslateTransform(x, y));
        dc.PushTransform(new ScaleTransform(scale, scale));
        switch (art)
        {
            case "SoccerBall": SoccerBall(dc); break;
            case "YarnBall": YarnBall(dc); break;
            case "Frisbee": Frisbee(dc); break;
            case "ToyCar": ToyCar(dc); break;
            case "BubbleMachine": BubbleMachine(dc); break;
            case "Curtain": Curtain(dc); break;
            case "TreeStump": TreeStump(dc); break;
            case "Grave": Grave(dc); break;
            case "AtticTrunk": AtticTrunk(dc); break;
            case "Cave": Cave(dc); break;
            case "Hammock": Hammock(dc); break;
            case "Campfire": Campfire(dc); break;
            case "Gym": Gym(dc); break;
            case "CafeTable": CafeTable(dc); break;
            case "HotSpring": HotSpring(dc); break;
            default: SoccerBall(dc); break;
        }
        dc.Pop();
        dc.Pop();
    }

    private static Brush Paint(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)!);

    private static Geometry Polygon(params Point[] points)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext c = geometry.Open();
        c.BeginFigure(points[0], true, true);
        for (int i = 1; i < points.Length; i++) c.LineTo(points[i], true, false);
        return geometry;
    }

    private static void GroundShadow(DrawingContext dc, double cx, double groundY, double width) =>
        dc.DrawEllipse(Paint("#22000000"), null, new Point(cx, groundY), width, width * .16);

    private static void SoccerBall(DrawingContext dc)
    {
        GroundShadow(dc, 50, 86, 26);
        dc.DrawEllipse(Brushes.White, new Pen(Paint("#D8D8DC"), 1.5), new Point(50, 60), 26, 26);
        Brush ink = Paint("#22202A");
        dc.DrawGeometry(ink, null, Polygon(new Point(50, 44), new Point(58, 51), new Point(55, 61), new Point(45, 61), new Point(42, 51)));
        dc.DrawGeometry(ink, null, Polygon(new Point(28, 55), new Point(35, 51), new Point(38, 60), new Point(32, 68), new Point(25, 64)));
        dc.DrawGeometry(ink, null, Polygon(new Point(72, 55), new Point(65, 51), new Point(62, 60), new Point(68, 68), new Point(75, 64)));
        dc.DrawGeometry(ink, null, Polygon(new Point(40, 78), new Point(48, 70), new Point(52, 70), new Point(60, 78), new Point(50, 83)));
    }

    private static void YarnBall(DrawingContext dc)
    {
        GroundShadow(dc, 50, 86, 25);
        Brush red = Paint("#E24B6B");
        dc.DrawEllipse(red, null, new Point(50, 60), 25, 25);
        Pen strand = new(Paint("#B7264A"), 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.PushClip(new EllipseGeometry(new Point(50, 60), 25, 25));
        for (int i = -2; i <= 2; i++)
        {
            double offset = i * 11;
            dc.DrawGeometry(null, strand, PathGeometry(new Point(25 + offset, 40), new Point(50 + offset * .3, 60), new Point(25 + offset, 82)));
        }
        dc.Pop();
        dc.DrawGeometry(null, strand, PathGeometry(new Point(70, 68), new Point(84, 76), new Point(80, 90)));
    }

    private static void Frisbee(DrawingContext dc)
    {
        GroundShadow(dc, 50, 82, 32);
        Brush orange = Paint("#F6A23D");
        dc.DrawEllipse(orange, null, new Point(50, 66), 34, 13);
        dc.DrawEllipse(Paint("#FFC876"), null, new Point(50, 62), 26, 9.5);
    }

    private static void ToyCar(DrawingContext dc)
    {
        GroundShadow(dc, 50, 84, 28);
        Brush body = Paint("#4EA1F2");
        dc.DrawRoundedRectangle(body, null, new Rect(18, 58, 64, 20), 8, 8);
        dc.DrawRoundedRectangle(Paint("#7EC0FF"), null, new Rect(33, 42, 34, 20), 9, 9);
        dc.DrawRoundedRectangle(Paint("#DFF2FF"), null, new Rect(37, 46, 26, 12), 5, 5);
        dc.DrawEllipse(Paint("#26232B"), null, new Point(33, 80), 9, 9);
        dc.DrawEllipse(Paint("#26232B"), null, new Point(67, 80), 9, 9);
        dc.DrawEllipse(Paint("#6E6B75"), null, new Point(33, 80), 3.6, 3.6);
        dc.DrawEllipse(Paint("#6E6B75"), null, new Point(67, 80), 3.6, 3.6);
        Pen speedLine = new(Paint("#9FC9FF"), 2.4) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(speedLine, new Point(6, 56), new Point(15, 56));
        dc.DrawLine(speedLine, new Point(3, 64), new Point(14, 64));
        dc.DrawLine(speedLine, new Point(6, 72), new Point(15, 72));
    }

    private static void BubbleMachine(DrawingContext dc)
    {
        GroundShadow(dc, 50, 88, 20);
        Brush body = Paint("#F0779A");
        dc.DrawRoundedRectangle(body, null, new Rect(33, 56, 34, 30), 9, 9);
        dc.DrawRoundedRectangle(Paint("#FFB0C4"), null, new Rect(39, 44, 22, 16), 7, 7);
        Brush bubble = Paint("#5FD8F0");
        DrawBubble(dc, 34, 34, 9, bubble);
        DrawBubble(dc, 58, 22, 6, bubble);
        DrawBubble(dc, 68, 40, 11, bubble);
        DrawBubble(dc, 46, 16, 5, bubble);
    }

    private static void DrawBubble(DrawingContext dc, double cx, double cy, double r, Brush fill)
    {
        dc.DrawEllipse(new SolidColorBrush(((SolidColorBrush)fill).Color) { Opacity = .38 }, new Pen(fill, 1.4), new Point(cx, cy), r, r);
        dc.DrawEllipse(Brushes.White, null, new Point(cx - r * .35, cy - r * .35), r * .22, r * .22);
    }

    private static void Curtain(DrawingContext dc)
    {
        GroundShadow(dc, 50, 92, 30);
        Brush rod = Paint("#8A6A2E");
        dc.DrawRoundedRectangle(rod, null, new Rect(14, 14, 72, 5), 2.5, 2.5);
        Brush fabric = Paint("#5C3A78");
        Point[] hem =
        [
            new(20, 19), new(80, 19), new(80, 78), new(68, 88), new(56, 78),
            new(44, 88), new(32, 78), new(20, 88)
        ];
        dc.DrawGeometry(fabric, null, Polygon(hem));
        Pen fold = new(Paint("#4A2E62"), 1.6);
        dc.DrawLine(fold, new Point(38, 22), new Point(38, 80));
        dc.DrawLine(fold, new Point(50, 22), new Point(50, 84));
        dc.DrawLine(fold, new Point(62, 22), new Point(62, 80));
        // A shy pair of eyes peeking through the gap — flavor only, always visible.
        dc.DrawEllipse(Paint("#FFF6C2"), null, new Point(46, 52), 3, 4);
        dc.DrawEllipse(Paint("#FFF6C2"), null, new Point(56, 52), 3, 4);
    }

    private static void TreeStump(DrawingContext dc)
    {
        GroundShadow(dc, 50, 90, 30);
        Brush bark = Paint("#8C5A34");
        dc.DrawRoundedRectangle(bark, null, new Rect(20, 46, 60, 40), 10, 10);
        dc.DrawEllipse(Paint("#B87B45"), null, new Point(50, 46), 30, 14);
        dc.DrawEllipse(Paint("#DDA463"), null, new Point(50, 46), 20, 9);
        dc.DrawEllipse(Paint("#241A14"), null, new Point(50, 48), 11, 7);
    }

    private static void Grave(DrawingContext dc)
    {
        GroundShadow(dc, 50, 92, 26);
        Brush stone = Paint("#8B8B96");
        StreamGeometry geometry = new();
        using (StreamGeometryContext c = geometry.Open())
        {
            c.BeginFigure(new Point(28, 90), true, true);
            c.LineTo(new Point(28, 40), true, false);
            c.ArcTo(new Point(72, 40), new Size(22, 22), 0, false, SweepDirection.Clockwise, true, false);
            c.LineTo(new Point(72, 90), true, false);
        }
        dc.DrawGeometry(stone, new Pen(Paint("#6E6E78"), 1.5), geometry);
        Pen etch = new(Paint("#6E6E78"), 2) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawLine(etch, new Point(42, 58), new Point(58, 58));
        dc.DrawLine(etch, new Point(50, 50), new Point(50, 66));
        Brush grass = Paint("#5FA35A");
        dc.DrawGeometry(grass, null, Polygon(new Point(16, 90), new Point(20, 74), new Point(24, 90)));
        dc.DrawGeometry(grass, null, Polygon(new Point(76, 90), new Point(80, 74), new Point(84, 90)));
    }

    private static void AtticTrunk(DrawingContext dc)
    {
        GroundShadow(dc, 50, 88, 30);
        Brush wood = Paint("#8A5A2E");
        dc.DrawRoundedRectangle(wood, null, new Rect(16, 50, 68, 34), 6, 6);
        dc.DrawGeometry(Paint("#A3703E"), null,
            Polygon(new Point(16, 50), new Point(24, 34), new Point(76, 34), new Point(84, 50)));
        Brush metal = Paint("#D8B35A");
        dc.DrawRoundedRectangle(metal, null, new Rect(46, 44, 8, 14), 2, 2);
        dc.DrawRoundedRectangle(metal, null, new Rect(20, 54, 6, 24), 2, 2);
        dc.DrawRoundedRectangle(metal, null, new Rect(74, 54, 6, 24), 2, 2);
    }

    private static void Cave(DrawingContext dc)
    {
        GroundShadow(dc, 50, 92, 34);
        Brush rock = Paint("#5A5A66");
        StreamGeometry geometry = new();
        using (StreamGeometryContext c = geometry.Open())
        {
            c.BeginFigure(new Point(12, 90), true, true);
            c.LineTo(new Point(16, 46), true, false);
            c.BezierTo(new Point(20, 18), new Point(80, 18), new Point(84, 46), true, false);
            c.LineTo(new Point(88, 90), true, false);
        }
        dc.DrawGeometry(rock, null, geometry);
        dc.DrawEllipse(Paint("#100C16"), null, new Point(50, 66), 22, 26);
        Brush pebble = Paint("#77778A");
        dc.DrawEllipse(pebble, null, new Point(22, 88), 5, 3.4);
        dc.DrawEllipse(pebble, null, new Point(80, 86), 6, 4);
    }

    private static void Hammock(DrawingContext dc)
    {
        GroundShadow(dc, 50, 92, 34);
        Brush post = Paint("#7A5636");
        dc.DrawRoundedRectangle(post, null, new Rect(10, 20, 9, 72), 4, 4);
        dc.DrawRoundedRectangle(post, null, new Rect(81, 20, 9, 72), 4, 4);
        StreamGeometry net = new();
        using (StreamGeometryContext c = net.Open())
        {
            c.BeginFigure(new Point(17, 38), true, true);
            c.BezierTo(new Point(40, 78), new Point(60, 78), new Point(83, 38), true, false);
            c.LineTo(new Point(83, 48), true, false);
            c.BezierTo(new Point(60, 86), new Point(40, 86), new Point(17, 48), true, false);
        }
        dc.DrawGeometry(Paint("#E8A34E"), null, net);
        Pen stripe = new(Paint("#C97F2E"), 1.8);
        for (int i = 1; i < 6; i++)
        {
            double t = i / 6.0;
            dc.DrawLine(stripe, new Point(20 + t * 60, 40 + Math.Sin(t * Math.PI) * 40), new Point(20 + t * 60, 46 + Math.Sin(t * Math.PI) * 40));
        }
    }

    private static void Campfire(DrawingContext dc)
    {
        GroundShadow(dc, 50, 90, 28);
        Brush pebble = Paint("#8C8C97");
        foreach (double dx in new[] { -24, -14, -4, 6, 16, 24 }) dc.DrawEllipse(pebble, null, new Point(50 + dx, 86), 7, 5);
        Brush log = Paint("#6E4423");
        dc.PushTransform(new RotateTransform(22, 50, 78));
        dc.DrawRoundedRectangle(log, null, new Rect(28, 74, 44, 9), 4, 4);
        dc.Pop();
        dc.PushTransform(new RotateTransform(-22, 50, 78));
        dc.DrawRoundedRectangle(log, null, new Rect(28, 74, 44, 9), 4, 4);
        dc.Pop();
        Flame(dc, Paint("#F6A23D"), 26, 44);
        Flame(dc, Paint("#F9432E"), 18, 40);
        Flame(dc, Paint("#FFD866"), 12, 34);
    }

    private static void Flame(DrawingContext dc, Brush fill, double width, double height)
    {
        StreamGeometry flame = new();
        using (StreamGeometryContext c = flame.Open())
        {
            c.BeginFigure(new Point(50, 74), true, true);
            c.BezierTo(new Point(50 - width * .55, 74 - height * .3), new Point(50 - width * .35, 74 - height * .95), new Point(50, 74 - height), true, false);
            c.BezierTo(new Point(50 + width * .35, 74 - height * .95), new Point(50 + width * .55, 74 - height * .3), new Point(50, 74), true, false);
        }
        dc.DrawGeometry(fill, null, flame);
    }

    private static void Gym(DrawingContext dc)
    {
        GroundShadow(dc, 50, 90, 32);
        dc.DrawRoundedRectangle(Paint("#4B4B58"), null, new Rect(16, 82, 68, 8), 3, 3);
        Brush bar = Paint("#3A3A44");
        dc.DrawRoundedRectangle(bar, null, new Rect(22, 56, 56, 6), 3, 3);
        Brush plate = Paint("#22222A");
        dc.DrawEllipse(plate, null, new Point(24, 59), 11, 11);
        dc.DrawEllipse(plate, null, new Point(76, 59), 11, 11);
        dc.DrawEllipse(Paint("#4A4A56"), null, new Point(24, 59), 5, 5);
        dc.DrawEllipse(Paint("#4A4A56"), null, new Point(76, 59), 5, 5);
    }

    private static void CafeTable(DrawingContext dc)
    {
        GroundShadow(dc, 50, 90, 22);
        Brush leg = Paint("#5A4632");
        dc.DrawRoundedRectangle(leg, null, new Rect(47, 56, 6, 30), 2, 2);
        dc.DrawEllipse(Paint("#FFF3E2"), new Pen(Paint("#E8D3AE"), 1.5), new Point(50, 56), 26, 9);
        dc.DrawRoundedRectangle(leg, null, new Rect(48, 18, 4, 22), 2, 2);
        Brush canopy = Paint("#E86A6A");
        StreamGeometry parasol = new();
        using (StreamGeometryContext c = parasol.Open())
        {
            c.BeginFigure(new Point(20, 22), true, true);
            c.BezierTo(new Point(28, 4), new Point(72, 4), new Point(80, 22), true, false);
        }
        dc.DrawGeometry(canopy, null, parasol);
        dc.DrawEllipse(Paint("#F2C6C6"), null, new Point(60, 60), 4, 3);
    }

    private static void HotSpring(DrawingContext dc)
    {
        GroundShadow(dc, 50, 92, 36);
        Brush rock = Paint("#8C8C97");
        dc.DrawEllipse(rock, null, new Point(50, 72), 38, 20);
        dc.DrawEllipse(Paint("#3D9FC2"), null, new Point(50, 72), 32, 15);
        dc.DrawEllipse(Paint("#6FC7E6"), null, new Point(50, 68), 24, 9);
        Pen steam = new(Paint("#CFEFFB"), 3) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        DrawSteam(dc, steam, 34, 54);
        DrawSteam(dc, steam, 50, 48);
        DrawSteam(dc, steam, 66, 54);
    }

    private static void DrawSteam(DrawingContext dc, Pen pen, double x, double topY)
    {
        StreamGeometry wisp = new();
        using StreamGeometryContext c = wisp.Open();
        c.BeginFigure(new Point(x, topY + 26), false, false);
        c.BezierTo(new Point(x - 8, topY + 14), new Point(x + 8, topY + 10), new Point(x, topY), true, false);
        dc.DrawGeometry(null, pen, wisp);
    }

    private static StreamGeometry PathGeometry(params Point[] points)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext c = geometry.Open();
        c.BeginFigure(points[0], false, false);
        for (int i = 1; i < points.Length; i++) c.LineTo(points[i], true, false);
        return geometry;
    }
}
