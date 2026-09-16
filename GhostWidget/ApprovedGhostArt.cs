using System.Windows;
using System.Windows.Media;

namespace GhostWidget;

/// <summary>
/// Native artwork for the 30 expansion ghosts.  Each design is a small composition instead
/// of a sampled tile, so the outlines remain clean when a desktop pet is resized or animated.
/// </summary>
internal static class ApprovedGhostArt
{
    private static readonly Brush Blue = Paint("#1117F5");
    private static readonly Brush Purple = Paint("#5A20E8");
    private static readonly Brush Red = Paint("#F22637");
    private static readonly Brush Ink = Paint("#17161D");
    private static readonly Brush White = Brushes.White;

    internal static void Draw(DrawingContext dc, int index)
    {
        switch (index)
        {
            case 1: Sheet(dc); break;
            case 2: Skull(dc); break;
            case 3: Mummy(dc); break;
            case 4: Devil(dc); break;
            case 5: Reaper(dc); break;
            case 6: Cat(dc); break;
            case 7: Dog(dc); break;
            case 8: Octopus(dc); break;
            case 9: Ufo(dc); break;
            case 10: Television(dc); break;
            case 11: Book(dc); break;
            case 12: Teapot(dc); break;
            case 13: Balloon(dc); break;
            case 14: Flask(dc); break;
            case 15: Cake(dc); break;
            case 16: Mushroom(dc); break;
            case 17: Coral(dc); break;
            case 18: Snowman(dc); break;
            case 19: Umbrella(dc); break;
            case 20: Lantern(dc); break;
            case 21: Origami(dc); break;
            case 22: Spider(dc); break;
            case 23: Snail(dc); break;
            case 24: Dragon(dc); break;
            case 25: Crown(dc); break;
            case 26: Knight(dc); break;
            case 27: Jester(dc); break;
            case 28: Butterfly(dc); break;
            case 29: Crystal(dc); break;
            case 30: Meteor(dc); break;
            default: Sheet(dc); break;
        }
    }

    private static Brush Paint(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)!);

    private static Geometry Polygon(params Point[] points)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext context = geometry.Open();
        context.BeginFigure(points[0], true, true);
        for (int i = 1; i < points.Length; i++) context.LineTo(points[i], true, false);
        return geometry;
    }

    private static Geometry GhostBody(double left = 22, double top = 20, double width = 54, double height = 69)
    {
        double right = left + width;
        double bottom = top + height;
        StreamGeometry geometry = new();
        using StreamGeometryContext c = geometry.Open();
        c.BeginFigure(new Point(left, bottom - 5), true, true);
        c.LineTo(new Point(left, top + 25), true, false);
        c.BezierTo(new Point(left, top + 7), new Point(left + 11, top), new Point(left + width / 2, top), true, false);
        c.BezierTo(new Point(right - 11, top), new Point(right, top + 7), new Point(right, top + 25), true, false);
        c.LineTo(new Point(right, bottom - 6), true, false);
        c.BezierTo(new Point(right, bottom + 2), new Point(right - 11, bottom + 4), new Point(right - 14, bottom - 5), true, false);
        c.BezierTo(new Point(right - 18, bottom + 5), new Point(left + 25, bottom + 5), new Point(left + 22, bottom - 5), true, false);
        c.BezierTo(new Point(left + 17, bottom + 4), new Point(left, bottom + 3), new Point(left, bottom - 5), true, false);
        return geometry;
    }

    private static void Face(DrawingContext dc, double leftX = 39, double rightX = 59, double y = 52, double rx = 6.6, double ry = 11,
        Brush? leftPupil = null, Brush? rightPupil = null)
    {
        dc.DrawEllipse(White, null, new Point(leftX, y), rx, ry);
        dc.DrawEllipse(White, null, new Point(rightX, y), rx, ry);
        dc.DrawEllipse(leftPupil ?? Blue, null, new Point(leftX - .8, y + 3.2), rx * .52, ry * .48);
        dc.DrawEllipse(rightPupil ?? Blue, null, new Point(rightX - .8, y + 3.2), rx * .52, ry * .48);
    }

    private static void SmallFeet(DrawingContext dc, Brush brush, double y = 82)
    {
        dc.DrawEllipse(brush, null, new Point(32, y), 8.5, 9);
        dc.DrawEllipse(brush, null, new Point(49, y + 2), 9, 8);
        dc.DrawEllipse(brush, null, new Point(66, y), 8.5, 9);
    }

    private static void Sheet(DrawingContext dc)
    {
        Brush body = Paint("#F7F7F5");
        dc.DrawGeometry(body, null, GhostBody(19, 19, 60, 68));
        Face(dc, 40, 59, 51);
    }

    private static void Skull(DrawingContext dc)
    {
        Brush bone = Paint("#E4E4E4");
        dc.DrawEllipse(bone, null, new Point(49, 48), 28, 29);
        dc.DrawRoundedRectangle(bone, null, new Rect(29, 50, 40, 32), 12, 12);
        SmallFeet(dc, bone, 78);
        Face(dc, 39, 59, 50, 6, 10.5);
        dc.DrawGeometry(Ink, null, Polygon(new Point(49, 64), new Point(44, 71), new Point(49, 75), new Point(54, 71)));
    }

    private static void Mummy(DrawingContext dc)
    {
        Brush wrap = Paint("#E8D6AF");
        Brush shadow = Paint("#C8B78F");
        dc.DrawRoundedRectangle(wrap, null, new Rect(25, 22, 48, 62), 16, 16);
        SmallFeet(dc, wrap, 78);
        Pen band = new(shadow, 4) { StartLineCap = PenLineCap.Round };
        dc.DrawLine(band, new Point(27, 37), new Point(70, 29));
        dc.DrawLine(band, new Point(26, 53), new Point(72, 43));
        dc.DrawLine(band, new Point(25, 70), new Point(71, 61));
        Face(dc, 39, 59, 48, 6.3, 10.5);
    }

    private static void Devil(DrawingContext dc)
    {
        Brush body = Paint("#F32929");
        Brush horn = Paint("#FFD84D");
        dc.DrawGeometry(body, null, GhostBody());
        dc.DrawGeometry(horn, null, Polygon(new Point(27, 28), new Point(28, 11), new Point(39, 26)));
        dc.DrawGeometry(horn, null, Polygon(new Point(59, 26), new Point(70, 10), new Point(71, 29)));
        Face(dc, 39, 59, 50);
    }

    private static void Reaper(DrawingContext dc)
    {
        Brush robe = Paint("#37353E");
        Brush hood = Paint("#1B1A21");
        dc.DrawGeometry(robe, null, GhostBody(21, 26, 56, 63));
        dc.DrawEllipse(hood, null, new Point(49, 37), 28, 28);
        dc.DrawEllipse(robe, null, new Point(49, 39), 21, 21);
        Face(dc, 40, 59, 50, 5.7, 9.5);
    }

    private static void Cat(DrawingContext dc)
    {
        Brush body = Paint("#F28B43");
        dc.DrawGeometry(body, null, GhostBody(25, 27, 49, 59));
        dc.DrawGeometry(body, null, Polygon(new Point(27, 35), new Point(29, 13), new Point(42, 29)));
        dc.DrawGeometry(body, null, Polygon(new Point(57, 29), new Point(69, 13), new Point(72, 36)));
        dc.DrawGeometry(null, new Pen(body, 8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
            Path(new Point(70, 68), new Point(88, 69), new Point(85, 84), new Point(76, 81)));
        Face(dc, 40, 58, 53);
    }

    private static void Dog(DrawingContext dc)
    {
        Brush body = Paint("#B97A43");
        Brush ear = Paint("#9B5F34");
        dc.DrawGeometry(body, null, GhostBody(25, 27, 49, 59));
        dc.DrawEllipse(ear, null, new Point(26, 47), 10, 17);
        dc.DrawEllipse(ear, null, new Point(72, 47), 10, 17);
        dc.DrawGeometry(null, new Pen(body, 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round },
            Path(new Point(72, 71), new Point(87, 79), new Point(83, 88), new Point(76, 84)));
        Face(dc, 40, 58, 52);
    }

    private static void Octopus(DrawingContext dc)
    {
        Brush body = Paint("#35CDC2");
        dc.DrawEllipse(body, null, new Point(49, 48), 27, 27);
        dc.DrawRoundedRectangle(body, null, new Rect(20, 48, 58, 30), 18, 18);
        foreach (double x in new[] { 27d, 38d, 49d, 60d, 71d })
            dc.DrawEllipse(body, null, new Point(x, 77), 5.5, 16);
        Face(dc, 40, 58, 52);
    }

    private static void Ufo(DrawingContext dc)
    {
        Brush metal = Paint("#C7CCD5");
        Brush dark = Paint("#858B96");
        Brush lamp = Paint("#F5DA35");
        dc.DrawEllipse(metal, null, new Point(49, 71), 39, 13);
        dc.DrawEllipse(dark, null, new Point(49, 69), 31, 7);
        dc.DrawEllipse(metal, null, new Point(49, 52), 19, 17);
        dc.DrawEllipse(dark, null, new Point(49, 47), 4, 4);
        Face(dc, 42, 56, 56, 4.6, 8);
        foreach (double x in new[] { 25d, 42d, 61d, 75d }) dc.DrawEllipse(lamp, null, new Point(x, 73), 3.5, 3.5);
    }

    private static void Television(DrawingContext dc)
    {
        Brush body = Paint("#8061C8");
        Brush screen = Paint("#C6C2D4");
        dc.DrawRoundedRectangle(body, null, new Rect(19, 31, 60, 49), 8, 8);
        dc.DrawRoundedRectangle(screen, null, new Rect(26, 39, 46, 27), 4, 4);
        dc.DrawLine(new Pen(body, 4) { StartLineCap = PenLineCap.Round }, new Point(39, 31), new Point(30, 19));
        dc.DrawLine(new Pen(body, 4) { StartLineCap = PenLineCap.Round }, new Point(59, 31), new Point(68, 19));
        SmallFeet(dc, body, 77);
        Face(dc, 40, 58, 52, 5.5, 8.5);
        dc.DrawEllipse(White, null, new Point(70, 43), 2.5, 2.5);
    }

    private static void Book(DrawingContext dc)
    {
        Brush page = Paint("#FFF7D8");
        Brush cover = Paint("#EF3C40");
        dc.DrawGeometry(cover, null, Polygon(new Point(15, 34), new Point(48, 39), new Point(48, 82), new Point(15, 75)));
        dc.DrawGeometry(cover, null, Polygon(new Point(50, 39), new Point(83, 34), new Point(83, 75), new Point(50, 82)));
        dc.DrawGeometry(page, null, Polygon(new Point(20, 36), new Point(48, 41), new Point(48, 78), new Point(20, 72)));
        dc.DrawGeometry(page, null, Polygon(new Point(50, 41), new Point(78, 36), new Point(78, 72), new Point(50, 78)));
        Face(dc, 39, 59, 56, 5.7, 9.5);
    }

    private static void Teapot(DrawingContext dc)
    {
        Brush pink = Paint("#F3A2C8");
        Brush pale = Paint("#FFD2E4");
        dc.DrawEllipse(pink, null, new Point(49, 59), 26, 25);
        dc.DrawEllipse(pink, null, new Point(24, 58), 13, 16);
        dc.DrawGeometry(pink, null, Polygon(new Point(70, 55), new Point(88, 49), new Point(78, 67)));
        dc.DrawEllipse(pink, null, new Point(49, 29), 11, 5);
        dc.DrawRoundedRectangle(pink, null, new Rect(40, 23, 18, 9), 4, 4);
        SmallFeet(dc, pink, 77);
        Face(dc, 40, 58, 58);
        dc.DrawEllipse(pale, null, new Point(38, 44), 4, 3);
    }

    private static void Balloon(DrawingContext dc)
    {
        Brush red = Paint("#F73535");
        dc.DrawEllipse(red, null, new Point(49, 51), 24, 30);
        dc.DrawGeometry(red, null, Polygon(new Point(46, 79), new Point(52, 79), new Point(49, 85)));
        dc.DrawGeometry(null, new Pen(Paint("#D8D9DD"), 1.5), Path(new Point(49, 85), new Point(54, 96), new Point(49, 103)));
        Face(dc, 40, 58, 52);
        dc.DrawEllipse(Paint("#FF7E7E"), null, new Point(40, 37), 4, 8);
    }

    private static void Flask(DrawingContext dc)
    {
        Brush glass = Paint("#79DC8E");
        Brush cork = Paint("#8C5A30");
        dc.DrawRoundedRectangle(glass, null, new Rect(27, 37, 44, 49), 15, 15);
        dc.DrawRectangle(glass, null, new Rect(42, 22, 14, 21));
        dc.DrawRoundedRectangle(cork, null, new Rect(39, 17, 20, 10), 3, 3);
        SmallFeet(dc, glass, 80);
        Face(dc, 40, 58, 58);
        dc.DrawEllipse(Paint("#BFF5BD"), null, new Point(39, 47), 4, 8);
    }

    private static void Cake(DrawingContext dc)
    {
        Brush cream = Paint("#FFF7E9");
        Brush frosting = Paint("#F6A5C7");
        dc.DrawGeometry(cream, null, Polygon(new Point(25, 38), new Point(72, 49), new Point(72, 84), new Point(25, 84)));
        dc.DrawGeometry(frosting, null, Polygon(new Point(25, 38), new Point(72, 49), new Point(72, 59), new Point(25, 51)));
        dc.DrawRectangle(frosting, null, new Rect(25, 68, 47, 9));
        dc.DrawEllipse(Red, null, new Point(49, 28), 7, 7);
        dc.DrawGeometry(Paint("#7BB650"), null, Polygon(new Point(50, 28), new Point(57, 20), new Point(58, 27)));
        Face(dc, 40, 58, 59);
    }

    private static void Mushroom(DrawingContext dc)
    {
        Brush cap = Paint("#EF3737");
        Brush stem = Paint("#FFF5DC");
        dc.DrawEllipse(cap, null, new Point(49, 39), 33, 20);
        dc.DrawRoundedRectangle(stem, null, new Rect(30, 44, 38, 39), 13, 13);
        SmallFeet(dc, stem, 78);
        foreach (Point spot in new[] { new Point(33, 35), new Point(52, 29), new Point(67, 38) }) dc.DrawEllipse(White, null, spot, 4, 4);
        Face(dc, 40, 58, 59);
    }

    private static void Coral(DrawingContext dc)
    {
        Brush coral = Paint("#F18478");
        Pen branch = new(coral, 7) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        dc.DrawRoundedRectangle(coral, null, new Rect(32, 53, 35, 34), 14, 14);
        SmallFeet(dc, coral, 80);
        dc.DrawLine(branch, new Point(42, 57), new Point(33, 32)); dc.DrawLine(branch, new Point(33, 40), new Point(22, 34)); dc.DrawLine(branch, new Point(34, 43), new Point(21, 51));
        dc.DrawLine(branch, new Point(49, 55), new Point(50, 24)); dc.DrawLine(branch, new Point(50, 35), new Point(41, 27)); dc.DrawLine(branch, new Point(50, 40), new Point(61, 30));
        dc.DrawLine(branch, new Point(58, 57), new Point(70, 32)); dc.DrawLine(branch, new Point(68, 39), new Point(78, 31)); dc.DrawLine(branch, new Point(67, 45), new Point(80, 51));
        Face(dc, 41, 58, 65);
    }

    private static void Snowman(DrawingContext dc)
    {
        Brush snow = Paint("#FAFAFA");
        dc.DrawEllipse(snow, null, new Point(49, 67), 25, 25);
        dc.DrawEllipse(snow, null, new Point(49, 43), 19, 19);
        SmallFeet(dc, snow, 83);
        Face(dc, 43, 55, 43, 4.7, 8);
        dc.DrawGeometry(Paint("#F39D30"), null, Polygon(new Point(49, 52), new Point(63, 56), new Point(49, 58)));
        dc.DrawEllipse(Ink, null, new Point(49, 69), 3, 3); dc.DrawEllipse(Ink, null, new Point(49, 79), 3, 3);
    }

    private static void Umbrella(DrawingContext dc)
    {
        Brush blue = Paint("#3D91DF");
        dc.DrawGeometry(blue, null, Polygon(new Point(15, 54), new Point(27, 28), new Point(49, 20), new Point(71, 28), new Point(83, 54)));
        dc.DrawEllipse(blue, null, new Point(27, 53), 12, 8); dc.DrawEllipse(blue, null, new Point(49, 53), 12, 8); dc.DrawEllipse(blue, null, new Point(71, 53), 12, 8);
        dc.DrawLine(new Pen(Paint("#E8EFFC"), 3) { StartLineCap = PenLineCap.Round }, new Point(49, 53), new Point(49, 91));
        dc.DrawGeometry(null, new Pen(Paint("#E8EFFC"), 3) { StartLineCap = PenLineCap.Round }, Path(new Point(49, 91), new Point(49, 101), new Point(59, 101), new Point(60, 95)));
        Face(dc, 40, 58, 63);
    }

    private static void Lantern(DrawingContext dc)
    {
        Brush gold = Paint("#F2CC40");
        Brush dark = Paint("#47474E");
        dc.DrawRoundedRectangle(gold, null, new Rect(28, 33, 42, 53), 20, 20);
        dc.DrawRoundedRectangle(dark, null, new Rect(31, 25, 36, 11), 4, 4);
        dc.DrawRoundedRectangle(dark, null, new Rect(31, 82, 36, 9), 4, 4);
        dc.DrawEllipse(null, new Pen(dark, 5), new Point(49, 24), 14, 9);
        Face(dc, 40, 58, 58);
        dc.DrawEllipse(Paint("#FFF096"), null, new Point(37, 46), 4, 8);
    }

    private static void Origami(DrawingContext dc)
    {
        Brush pink = Paint("#F18AC2");
        Brush pale = Paint("#FFA7D8");
        dc.DrawGeometry(pink, null, Polygon(new Point(12, 55), new Point(38, 45), new Point(48, 15), new Point(61, 47), new Point(86, 58), new Point(62, 84), new Point(48, 92), new Point(31, 78)));
        dc.DrawGeometry(pale, null, Polygon(new Point(38, 45), new Point(48, 15), new Point(50, 59), new Point(31, 78)));
        Face(dc, 39, 58, 64, 5.6, 9.2);
    }

    private static void Spider(DrawingContext dc)
    {
        Brush body = Paint("#383944");
        Pen leg = new(body, 6) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round, LineJoin = PenLineJoin.Round };
        dc.DrawEllipse(body, null, new Point(49, 59), 20, 23);
        dc.DrawRoundedRectangle(body, null, new Rect(29, 56, 40, 27), 14, 14);
        foreach (int side in new[] { -1, 1 })
        {
            dc.DrawGeometry(null, leg, Path(new Point(35 + side * 2, 60), new Point(17 * (side == -1 ? 1 : 0) + (side == 1 ? 81 : 17), 48), new Point(side == -1 ? 12 : 86, 34)));
            dc.DrawGeometry(null, leg, Path(new Point(34 + side * 2, 67), new Point(side == -1 ? 15 : 83, 68), new Point(side == -1 ? 9 : 89, 82)));
        }
        Face(dc, 42, 56, 59, 4.8, 8.5);
    }

    private static void Snail(DrawingContext dc)
    {
        Brush brown = Paint("#AF7640");
        Brush shell = Paint("#9A6232");
        dc.DrawRoundedRectangle(brown, null, new Rect(21, 58, 57, 28), 14, 14);
        dc.DrawEllipse(shell, null, new Point(60, 55), 24, 24);
        dc.DrawEllipse(null, new Pen(Paint("#72431F"), 3), new Point(60, 55), 13, 13);
        dc.DrawLine(new Pen(brown, 3) { StartLineCap = PenLineCap.Round }, new Point(34, 59), new Point(28, 39));
        dc.DrawLine(new Pen(brown, 3) { StartLineCap = PenLineCap.Round }, new Point(42, 59), new Point(48, 39));
        Face(dc, 31, 42, 59, 4.6, 8);
    }

    private static void Dragon(DrawingContext dc)
    {
        Brush green = Paint("#73DC87");
        Brush spike = Paint("#35B14D");
        dc.DrawGeometry(green, null, GhostBody(27, 28, 48, 58));
        dc.DrawGeometry(green, null, Polygon(new Point(67, 62), new Point(89, 51), new Point(83, 75), new Point(94, 84), new Point(72, 80)));
        dc.DrawGeometry(green, null, Polygon(new Point(33, 54), new Point(18, 32), new Point(31, 33), new Point(43, 48)));
        foreach (double x in new[] { 38d, 49d, 60d }) dc.DrawGeometry(spike, null, Polygon(new Point(x, 30), new Point(x + 5, 15), new Point(x + 10, 30)));
        Face(dc, 41, 58, 53);
    }

    private static void Crown(DrawingContext dc)
    {
        Brush gold = Paint("#F1D23B");
        dc.DrawGeometry(gold, null, GhostBody(25, 31, 49, 55));
        dc.DrawGeometry(gold, null, Polygon(new Point(25, 38), new Point(25, 18), new Point(38, 30), new Point(49, 10), new Point(59, 30), new Point(74, 18), new Point(74, 38)));
        dc.DrawEllipse(Red, null, new Point(49, 23), 4, 5);
        Face(dc, 40, 58, 55);
    }

    private static void Knight(DrawingContext dc)
    {
        Brush steel = Paint("#B9C1CA");
        Brush dark = Paint("#66707A");
        dc.DrawGeometry(steel, null, GhostBody(25, 33, 49, 54));
        dc.DrawGeometry(steel, null, Polygon(new Point(22, 43), new Point(25, 25), new Point(49, 15), new Point(73, 25), new Point(77, 43)));
        dc.DrawGeometry(dark, null, Polygon(new Point(29, 38), new Point(49, 28), new Point(69, 38), new Point(65, 47), new Point(33, 47)));
        dc.DrawGeometry(Red, null, Polygon(new Point(48, 16), new Point(55, 5), new Point(67, 17)));
        Face(dc, 40, 58, 58);
    }

    private static void Jester(DrawingContext dc)
    {
        Brush violet = Paint("#9A3CE0");
        Brush red = Paint("#F0333F");
        Brush gold = Paint("#FFE149");
        dc.DrawGeometry(violet, null, GhostBody(25, 36, 25, 51));
        dc.DrawGeometry(red, null, GhostBody(49, 36, 25, 51));
        dc.DrawGeometry(violet, null, Polygon(new Point(47, 39), new Point(21, 22), new Point(30, 11), new Point(55, 29)));
        dc.DrawGeometry(red, null, Polygon(new Point(52, 39), new Point(77, 22), new Point(67, 10), new Point(44, 29)));
        dc.DrawEllipse(gold, null, new Point(25, 14), 5, 5); dc.DrawEllipse(gold, null, new Point(72, 14), 5, 5);
        Face(dc, 40, 58, 57, 6, 10, Purple, Red);
    }

    private static void Butterfly(DrawingContext dc)
    {
        Brush pink = Paint("#F18DC6");
        Brush pale = Paint("#FAAFD9");
        dc.DrawEllipse(pink, null, new Point(28, 52), 20, 25);
        dc.DrawEllipse(pink, null, new Point(70, 52), 20, 25);
        dc.DrawEllipse(pale, null, new Point(30, 74), 18, 13);
        dc.DrawEllipse(pale, null, new Point(68, 74), 18, 13);
        dc.DrawRoundedRectangle(pink, null, new Rect(39, 42, 20, 45), 10, 10);
        dc.DrawLine(new Pen(pink, 2) { StartLineCap = PenLineCap.Round }, new Point(44, 43), new Point(36, 28));
        dc.DrawLine(new Pen(pink, 2) { StartLineCap = PenLineCap.Round }, new Point(54, 43), new Point(62, 28));
        dc.DrawEllipse(Paint("#FFE75A"), null, new Point(35, 27), 2.5, 2.5); dc.DrawEllipse(Paint("#FFE75A"), null, new Point(63, 27), 2.5, 2.5);
        Face(dc, 42, 56, 60, 4.8, 8.5);
    }

    private static void Crystal(DrawingContext dc)
    {
        Brush ice = Paint("#9DE1FF");
        Brush bright = Paint("#D3F4FF");
        dc.DrawGeometry(ice, null, GhostBody(28, 40, 42, 47));
        foreach (Point[] shard in new[]
        {
            new[] { new Point(27, 47), new Point(20, 23), new Point(39, 42) },
            new[] { new Point(40, 40), new Point(46, 13), new Point(56, 41) },
            new[] { new Point(58, 42), new Point(72, 20), new Point(73, 51) },
            new[] { new Point(69, 56), new Point(86, 36), new Point(78, 69) }
        }) dc.DrawGeometry(bright, null, Polygon(shard));
        Face(dc, 40, 58, 60);
    }

    private static void Meteor(DrawingContext dc)
    {
        Brush yellow = Paint("#F5D836");
        dc.DrawGeometry(yellow, null, Star(new Point(46, 62), 28, 13));
        dc.DrawGeometry(yellow, null, Polygon(new Point(58, 51), new Point(88, 30), new Point(73, 54), new Point(94, 50), new Point(67, 66)));
        Face(dc, 39, 53, 64, 5.4, 8.6);
    }

    private static StreamGeometry Path(params Point[] points)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext c = geometry.Open();
        c.BeginFigure(points[0], false, false);
        for (int i = 1; i < points.Length; i++) c.LineTo(points[i], true, false);
        return geometry;
    }

    private static Geometry Star(Point center, double outer, double inner)
    {
        StreamGeometry geometry = new();
        using StreamGeometryContext c = geometry.Open();
        for (int i = 0; i < 10; i++)
        {
            double angle = -Math.PI / 2 + i * Math.PI / 5;
            double radius = i % 2 == 0 ? outer : inner;
            Point point = new(center.X + Math.Cos(angle) * radius, center.Y + Math.Sin(angle) * radius);
            if (i == 0) c.BeginFigure(point, true, true); else c.LineTo(point, true, false);
        }
        return geometry;
    }
}
