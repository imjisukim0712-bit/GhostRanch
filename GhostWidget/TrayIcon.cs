using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace GhostWidget;

/// <summary>Shows a taskbar tray icon whose right-click menu is the same
/// ContextMenu instance the ghost sprite uses, so both stay in sync.</summary>
internal sealed class TrayIcon : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly ContextMenu menu;
    private readonly FrameworkElement placementTarget;

    internal TrayIcon(ContextMenu menu, FrameworkElement placementTarget, string tooltip)
    {
        this.menu = menu;
        this.placementTarget = placementTarget;
        notifyIcon = new Forms.NotifyIcon { Icon = CreateIcon(), Text = tooltip, Visible = true };
        notifyIcon.MouseUp += NotifyIcon_MouseUp;
    }

    private void NotifyIcon_MouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Right) return;
        menu.PlacementTarget = placementTarget;
        menu.Placement = PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    private static Drawing.Icon CreateIcon()
    {
        using Drawing.Bitmap bitmap = new(32, 32);
        using (Drawing.Graphics graphics = Drawing.Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.Clear(Drawing.Color.Transparent);
            using Drawing.SolidBrush body = new(Drawing.Color.FromArgb(255, 176, 158, 255));
            graphics.FillEllipse(body, 2, 3, 28, 25);
            using Drawing.SolidBrush eye = new(Drawing.Color.FromArgb(255, 30, 25, 45));
            graphics.FillEllipse(eye, 9, 14, 5, 6);
            graphics.FillEllipse(eye, 18, 14, 5, 6);
        }
        nint hIcon = bitmap.GetHicon();
        try
        {
            using Drawing.Icon handleIcon = Drawing.Icon.FromHandle(hIcon);
            return (Drawing.Icon)handleIcon.Clone();
        }
        finally
        {
            DestroyIcon(hIcon);
        }
    }

    public void Dispose()
    {
        notifyIcon.MouseUp -= NotifyIcon_MouseUp;
        notifyIcon.Visible = false;
        notifyIcon.Icon?.Dispose();
        notifyIcon.Dispose();
    }

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(nint handle);
}
