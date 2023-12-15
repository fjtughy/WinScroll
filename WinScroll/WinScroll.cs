using System;
using System.Net;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Diagnostics.CodeAnalysis;

namespace WinScroll;

public partial class WinScroll : Form
{
    private System.Windows.Forms.Timer timer;
    private Rectangle captureRectangle;
    private Point p = new Point();

    private const string versionString = "0.5";
    private const string aboutURL = "https://github.com/fjtughy/WinScroll";
    private const string registry = "SOFTWARE\\WinScroll";
    private int leftArrow;
    private int upArrow;
    private int downArrow;
    private int rightArrow;

    public WinScroll()
    {
        leftArrow = "full_left".GetHashCode();
        upArrow = "upper_right".GetHashCode();
        downArrow = "lower_right".GetHashCode();
        rightArrow = "full_right".GetHashCode();

        InitializeComponent();
        Init();

        //SizeChanged += new System.EventHandler(formResize);
        Layout += new LayoutEventHandler(formResize);
        notifyIcon.DoubleClick += new System.EventHandler(windowShow);
        optionsToolStripMenuItem.Click += new System.EventHandler(windowShow);
        exitToolStripMenuItem.Click += new System.EventHandler(trayExit);

        captureX.LostFocus += new System.EventHandler(CaptureBounds);
        captureY.LostFocus += new System.EventHandler(CaptureBounds);
        captureWidth.LostFocus += new System.EventHandler(CaptureBounds);
        captureHeight.LostFocus += new System.EventHandler(CaptureBounds);

        captureCheck.CheckedChanged += new System.EventHandler(captureCheckChanged);
        trayCheck.CheckedChanged += new System.EventHandler(trayCheckChanged);
        startupCheck.CheckedChanged += new System.EventHandler(startupCheckChanged);
        windowCheck.CheckedChanged += new System.EventHandler(windowCheckChanged);

        aboutLink.Click += new System.EventHandler(aboutLinkClicked);
    }

    [MemberNotNull(nameof(timer))]
    private void Init()
    {
        timer = new System.Windows.Forms.Timer();
        timer.Tick += new EventHandler(Tick);
        timer.Interval = 10;

        captureRectangle = new Rectangle((int)captureX.Value, (int)captureY.Value, (int)captureWidth.Value, (int)captureHeight.Value);

        //Startup
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
        if (rk != null && rk.GetValue("WinScroll") != null)
        {
            startupCheck.Checked = true;
            WindowState = FormWindowState.Minimized;
        }
        //Loading
        rk = Registry.CurrentUser.CreateSubKey(registry);
        rk = Registry.CurrentUser.OpenSubKey(registry, false);
        if (rk != null)
        {
            object? o = rk.GetValue("HideTrayIcon");
            if (o != null && o.ToString() == "true")
            {
                trayCheck.Checked = true;
                notifyIcon.Visible = !trayCheck.Checked;
            }
            o = rk.GetValue("WindowSnapping");
            if (o != null && o.ToString() == "true")
            {
                windowCheck.Checked = true;
                RegisterHotkeys();
            }
        }

        //allow us to be run from explorer's 'Run' - http://stackoverflow.com/a/4822749
        rk = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\winscroll.exe");
        if (rk != null)
        {
            rk.SetValue("", Application.ExecutablePath);
            rk.SetValue("Path", Path.GetDirectoryName(Application.ExecutablePath) ?? "");
        }

        captureX.Value = Properties.Settings.Default.CaptureX;
        captureY.Value = Properties.Settings.Default.CaptureY;
        captureWidth.Value = Properties.Settings.Default.CaptureWidth;
        captureHeight.Value = Properties.Settings.Default.CaptureHeight;
        UpdateCaptureRect();
    }

    private void Tick(object? sender, EventArgs e)
    {
        NativeMethods.GetCursorPos(out p);
        labelCoords.Text = p.X.ToString() + ", " + p.Y.ToString();
        UpdateCapture(captureCheck.Checked);
    }

    private void UpdateCapture(bool capture)
    {
        if (capture)
        {
            NativeMethods.ClipCursor(ref captureRectangle);
            NativeMethods.MoveWindow(Handle, (int)captureX.Value, (int)captureY.Value, 464, 249, true);
        }
        else
        {
            NativeMethods.ClipCursor(IntPtr.Zero);
        }
    }

    private void CaptureBounds(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(((Control)captureX).Text))
            ((Control)captureX).Text = captureX.Value.ToString();

        if (string.IsNullOrEmpty(((Control)captureY).Text))
            ((Control)captureY).Text = captureY.Value.ToString();

        if (string.IsNullOrEmpty(((Control)captureWidth).Text))
            ((Control)captureWidth).Text = captureWidth.Value.ToString();

        if (string.IsNullOrEmpty(((Control)captureHeight).Text))
            ((Control)captureHeight).Text = captureHeight.Value.ToString();

        if (captureX.Value >= captureWidth.Value)
        {
            captureWidth.Value = captureX.Value + 1;
        }
        if (captureY.Value >= captureHeight.Value)
        {
            captureHeight.Value = captureY.Value + 1;
        }
        UpdateCaptureRect();
    }

    private void UpdateCaptureRect()
    {
        captureRectangle = new Rectangle((int)captureX.Value, (int)captureY.Value, (int)captureWidth.Value, (int)captureHeight.Value);
    }

    private void captureCheckChanged(object? sender, EventArgs e)
    {
        UpdateCapture(captureCheck.Checked);
        if (captureCheck.Checked)
            timer.Start();
        else
            timer.Stop();
    }

    private void trayCheckChanged(object? sender, EventArgs e)
    {
        notifyIcon.Visible = !trayCheck.Checked;
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey(registry, true);
        if (rk != null)
        {
            rk.SetValue("HideTrayIcon", trayCheck.Checked ? "true" : "false");
        }
    }

    private void startupCheckChanged(object? sender, EventArgs e)
    {
        RegistryKey? rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        if (null == rk)
        {
            return; //The subkey request failed.
        }

        if (startupCheck.Checked)
            rk.SetValue("WinScroll", Application.ExecutablePath.ToString());
        else
            rk.DeleteValue("WinScroll", false);
    }

    private void windowShow(object? sender, EventArgs e)
    {
        ShowWindow();
    }

    private void trayExit(object? sender, EventArgs e)
    {
        Close();
    }

    private void formResize(object? sender, EventArgs e)
    {
        //Debug.WriteLine(WindowState.ToString() + ", " + Visible.ToString());
        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
        //Debug.WriteLine(WindowState.ToString() + ", " + Visible.ToString());
    }

    private void aboutLinkClicked(object? sender, EventArgs e)
    {
        System.Diagnostics.Process.Start(aboutURL);
    }


    private void windowCheckChanged(object? sender, EventArgs e)
    {
        RegisterHotkeys();

        RegistryKey? rk = Registry.CurrentUser.OpenSubKey(registry, true);
        if (rk != null)
        {
            rk.SetValue("WindowSnapping", windowCheck.Checked ? "true" : "false");
        }
    }

    private void RegisterHotkeys()
    {
        if (windowCheck.Checked)
        {
            Keys k = Keys.Control | Keys.Alt | Keys.Left;
            Macro.RegisterHotKey(this, k, leftArrow);

            k = Keys.Control | Keys.Alt | Keys.Up;
            Macro.RegisterHotKey(this, k, upArrow);

            k = Keys.Control | Keys.Alt | Keys.Down;
            Macro.RegisterHotKey(this, k, downArrow);

            k = Keys.Control | Keys.Alt | Keys.Right;
            Macro.RegisterHotKey(this, k, rightArrow);
        }
        else
        {
            Macro.UnregisterHotKey(this, leftArrow);
            Macro.UnregisterHotKey(this, upArrow);
            Macro.UnregisterHotKey(this, downArrow);
            Macro.UnregisterHotKey(this, rightArrow);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Macro.WM_HOTKEY)
        {
            if ((int)m.WParam == downArrow)
                Hide();
            else if ((int)m.WParam == upArrow)
                ShowWindow();
        }
        else if (m.Msg == NativeMethods.WM_SHOWME)
        {
            ShowWindow();
        }
        base.WndProc(ref m);
    }

    private void ShowWindow()
    {
        WindowState = FormWindowState.Maximized;    //once again, not quite sure why this is necessary, but setting the state to normal straight away doesn't unhide correctly.
        Show();
        BringToFront();
        TopMost = true;     //ensure we're  right at the front!
        TopMost = false;    //don't stay in front though, that'd be rude.
        Activate();         //and make sure we're focused after we've been called.
        WindowState = FormWindowState.Normal;
    }

    private void OnClose(object sender, FormClosedEventArgs e)
    {
        Properties.Settings.Default.CaptureX = captureX.Value;
        Properties.Settings.Default.CaptureY = captureY.Value;
        Properties.Settings.Default.CaptureWidth = captureWidth.Value;
        Properties.Settings.Default.CaptureHeight = captureHeight.Value;
        Properties.Settings.Default.Save();
    }
}

