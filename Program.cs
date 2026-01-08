using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoClick;

static class Program
{
    [STAThread]
    static void Main()
    {
        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext("kitty.ico"));
    }
}

public sealed class TrayApplicationContext : ApplicationContext
{
    // P/Invoke declaration for the Win32 API function
    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern uint SetThreadExecutionState(uint esFlags);

    // Flags to control system sleep and display behavior
    private const uint ES_CONTINUOUS = 0x80000000;
    private const uint ES_SYSTEM_REQUIRED = 0x00000001;
    private const uint ES_DISPLAY_REQUIRED = 0x00000002;

    private readonly NotifyIcon _notifyIcon;
    private readonly System.Windows.Forms.Timer _timer = new();
    private readonly Icon _appIcon;
    private const int UpdateIntervalSeconds = 60;

    public TrayApplicationContext(string iconPath)
    {
        _appIcon = LoadIcon(iconPath);

        _notifyIcon = new NotifyIcon
        {
            Icon = _appIcon,
            Text = "防休眠程序运行中...",
            Visible = true,
            ContextMenuStrip = new ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("退出", null, OnExit);

        PreventSleep();

        _timer.Interval = UpdateIntervalSeconds * 1000;
        _timer.Tick += (sender, args) => PreventSleep();
        _timer.Start();

        ShowBalloonTip("开始工作", "防休眠模式已开启, 电脑将不会自动息屏或休眠。", ToolTipIcon.Info);
        ShowStartupMessage();
    }

    private static Icon LoadIcon(string iconPath)
    {
        try
        {
            var candidates = new[]
            {
                iconPath,
                Path.Combine(AppContext.BaseDirectory, iconPath)
            };

            foreach (var path in candidates)
            {
                if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
                {
                    return new Icon(path);
                }
            }
        }
        catch
        {
        }

        return SystemIcons.Application;
    }

    private void PreventSleep()
    {
        SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED | ES_DISPLAY_REQUIRED);
    }

    private void AllowSleep()
    {
        SetThreadExecutionState(ES_CONTINUOUS);
    }

    private void ShowBalloonTip(string title, string message, ToolTipIcon icon, int timeout = 2000)
    {
        _notifyIcon.ShowBalloonTip(timeout, title, message, icon);
    }

    private void ShowStartupMessage()
    {
        using var form = new StartupMessageForm(_appIcon, "AutoClick", "防休眠程序已启动, 电脑将不会自动息屏或休眠。");
        form.ShowDialog();
    }

    private void OnExit(object? sender, EventArgs e)
    {
        _timer.Stop();
        AllowSleep();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _appIcon.Dispose();
        Application.Exit();
    }
}

internal sealed class StartupMessageForm : Form
{
    private readonly Image _iconImage;

    public StartupMessageForm(Icon icon, string title, string message)
    {
        Icon = icon;
        Text = title;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        TopMost = true;
        ClientSize = new Size(360, 150);

        _iconImage = icon.ToBitmap();

        var picture = new PictureBox
        {
            Image = _iconImage,
            SizeMode = PictureBoxSizeMode.StretchImage,
            Size = new Size(48, 48),
            Location = new Point(24, 40)
        };

        var label = new Label
        {
            Text = message,
            AutoSize = false,
            Location = new Point(90, 30),
            Size = new Size(240, 70)
        };

        var button = new Button
        {
            Text = "确定",
            DialogResult = DialogResult.OK,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Size = new Size(80, 30),
            Location = new Point(ClientSize.Width - 100, ClientSize.Height - 50)
        };

        Controls.AddRange(new Control[] { picture, label, button });
        AcceptButton = button;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _iconImage?.Dispose();
        }

        base.Dispose(disposing);
    }
}
