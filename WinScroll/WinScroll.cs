﻿using System;
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
//using Newtonsoft.Json;

namespace WinScroll
{
    public partial class WinScroll : Form
    {
        private System.Windows.Forms.Timer timer;
        private Rectangle captureRectangle;
        private Point p = new Point();

        private const string versionString = "0.3";
        private const string aboutURL = "http://www.github.com/Petethegoat";
        //private const string apiURL = "https://api.github.com/repos/petethegoat/winscroll/releases/latest";
        private const string registry = "SOFTWARE\\WinScroll";
        private int fullLeft;
        private int upperRight;
        private int lowerRight;
        private int fullRight;
        private int doubleUpper;
        private int secondLower;

        public WinScroll()
        {
            fullLeft = "full_left".GetHashCode();
            upperRight = "upper_right".GetHashCode();
            lowerRight = "lower_right".GetHashCode();
            fullRight = "full_right".GetHashCode();

            doubleUpper = "double_upper".GetHashCode();
            secondLower = "second_lower".GetHashCode();

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

        public void Init()
        {
            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(Tick);
            timer.Interval = 10;

            captureRectangle = new Rectangle((int)captureX.Value, (int)captureY.Value, (int)captureWidth.Value, (int)captureHeight.Value);

            //Startup
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", false);
            if(rk != null && rk.GetValue("WinScroll") != null)
            {
                startupCheck.Checked = true;
                WindowState = FormWindowState.Minimized;
                
            }
            //Loading
            rk = Registry.CurrentUser.CreateSubKey(registry);
            rk = Registry.CurrentUser.OpenSubKey(registry, false);
            if(rk != null)
            {
                object o = rk.GetValue("HideTrayIcon");
                if(o != null && o.ToString() == "true")
                {
                    trayCheck.Checked = true;
                    notifyIcon.Visible = !trayCheck.Checked;
                }
                o = rk.GetValue("WindowSnapping");
                if(o != null && o.ToString() == "true")
                {
                    windowCheck.Checked = true;
                    RegisterHotkeys();
                }
            }

            //allow us to be run from explorer's 'Run' - http://stackoverflow.com/a/4822749
            rk = Registry.LocalMachine.CreateSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\winscroll.exe");
            if(rk != null)
            {
                rk.SetValue("", Application.ExecutablePath);
                rk.SetValue("Path", Path.GetDirectoryName(Application.ExecutablePath));
            }

            //check for updates!!!! wowowwwow
            /*
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            using(WebClient c = new WebClient())
            {
                string data = c.DownloadString(apiURL);
                var json = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(data);
                string latestVersion = json["tag_name"];
                Debug.WriteLine(latestVersion);
            }
            */
        }

        private void Tick(object sender, EventArgs e)
        {
            NativeMethods.GetCursorPos(out p);
            labelCoords.Text = p.X.ToString() + ", " + p.Y.ToString();
            UpdateCapture(captureCheck.Checked);
        }

        private void UpdateCapture(bool capture)
        {    
            if(capture)
            {
                NativeMethods.ClipCursor(ref captureRectangle);
                NativeMethods.MoveWindow(Handle, (int) captureX.Value, (int) captureY.Value, 400, 220, true);
            }
            else
            {
                NativeMethods.ClipCursor(IntPtr.Zero);
            }
        }

        private void CaptureBounds(object sender, EventArgs e)
        {
            if(string.IsNullOrEmpty(((Control)captureX).Text))
                ((Control)captureX).Text = captureX.Value.ToString();

            if(string.IsNullOrEmpty(((Control)captureY).Text))
                ((Control)captureY).Text = captureY.Value.ToString();

            if(string.IsNullOrEmpty(((Control)captureWidth).Text))
                ((Control)captureWidth).Text = captureWidth.Value.ToString();

            if(string.IsNullOrEmpty(((Control)captureHeight).Text))
                ((Control)captureHeight).Text = captureHeight.Value.ToString();

            if(captureX.Value >= captureWidth.Value)
            {
                captureWidth.Value = captureX.Value + 1;
            }
            if(captureY.Value >= captureHeight.Value)
            {
                captureHeight.Value = captureY.Value + 1;
            }
            captureRectangle = new Rectangle((int)captureX.Value, (int)captureY.Value, (int)captureWidth.Value, (int)captureHeight.Value);
        }

        private void captureCheckChanged(object sender, EventArgs e)
        {
            UpdateCapture(captureCheck.Checked);
            if(captureCheck.Checked)
                timer.Start();
            else
                timer.Stop();
        }

        private void trayCheckChanged(object sender, EventArgs e)
        {
            notifyIcon.Visible = !trayCheck.Checked;
            RegistryKey rk = Registry.CurrentUser.OpenSubKey(registry, true);
            if(rk != null)
            {
                rk.SetValue("HideTrayIcon", trayCheck.Checked ? "true" : "false");
            }
        }

        private void startupCheckChanged(object sender, EventArgs e)
        {
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if(startupCheck.Checked)
                rk.SetValue("WinScroll", Application.ExecutablePath.ToString());
            else
                rk.DeleteValue("WinScroll", false);
        }

        private void windowShow(object sender, EventArgs e)
        {
            ShowWindow();
        }

        private void trayExit(object sender, EventArgs e)
        {
            Close();
        }

        private void formResize(object sender, EventArgs e)
        {
            //Debug.WriteLine(WindowState.ToString() + ", " + Visible.ToString());
            if(WindowState == FormWindowState.Minimized)
            {
                Hide();
            }
            //Debug.WriteLine(WindowState.ToString() + ", " + Visible.ToString());
        }

        private void aboutLinkClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(aboutURL);
        }


        private void windowCheckChanged(object sender, EventArgs e)
        {
            RegisterHotkeys();

            RegistryKey rk = Registry.CurrentUser.OpenSubKey(registry, true);
            if(rk != null)
            {
                rk.SetValue("WindowSnapping", windowCheck.Checked ? "true" : "false");
            }
        }

        private void RegisterHotkeys()
        {
            if(windowCheck.Checked)
            {
                Keys k = Keys.Control | Keys.Alt | Keys.Left;
                Macro.RegisterHotKey(this, k, fullLeft);

                k = Keys.Control | Keys.Alt | Keys.Up;
                Macro.RegisterHotKey(this, k, upperRight);

                k = Keys.Control | Keys.Alt | Keys.Down;
                Macro.RegisterHotKey(this, k, lowerRight);

                k = Keys.Control | Keys.Alt | Keys.Right;
                Macro.RegisterHotKey(this, k, fullRight);
            }
            else
            {
                Macro.UnregisterHotKey(this, fullLeft);
                Macro.UnregisterHotKey(this, upperRight);
                Macro.UnregisterHotKey(this, lowerRight);
                Macro.UnregisterHotKey(this, fullRight);
            }
        }

        /*
        public static IntPtr WinGetHandle(string wName)
        {
            IntPtr hWnd = IntPtr.Zero;

            foreach(Process pList in Process.GetProcesses())
                if(pList.MainWindowTitle.Contains(wName))
                    hWnd = pList.MainWindowHandle;

            return hWnd;
        }

        public static int HandleGetName(IntPtr hWnd)
        {
            foreach(Process pList in Process.GetProcesses())
                if(pList.MainWindowHandle == hWnd)
                    return pList.Id;

            return -1;
        }
        */

        protected override void WndProc(ref Message m)
        {
            if(m.Msg == Macro.WM_HOTKEY)
            {
                int col;
                int row;
                int w = 640;
                int h = 480;
                int x = 0;
                int y = 0;
                IntPtr window = NativeMethods.GetForegroundWindow();

                /*
                Debug.Print(HandleGetName(window).ToString());
                if(window.ToInt64() == 131262)  //don't move the start menu! - needs work, apparently these IDs aren't consistent
                {
                    return;
                }
                */
                 
                Point p;
                NativeMethods.GetCursorPos(out p);
                Screen activeScreen = Screen.FromPoint(p);

                int screenWidth = activeScreen.WorkingArea.Width;
                int screenHeight = activeScreen.WorkingArea.Height;
                /*
                RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\Advanced", false);
                if(screenHeight != activeScreen.Bounds.Height)  //make sure the monitor we're on actually has the taskbar!
                {
                    if(rk != null && (int) rk.GetValue("TaskbarSmallIcons") == 1)
                    {
                        Debug.WriteLine((int)rk.GetValue("TaskbarSmallIcons"));
                        //screenHeight += 2;  //windows seems to misreport the actual working area when small icons is enabled
                        Debug.WriteLine(screenHeight);
                    }
                }*/

                if((int)m.WParam == fullLeft)
                {
                    col = (screenWidth / (int)columns.Value);
                    row = (screenHeight / (int)rows.Value);
                    w = col * (int)colsNum.Value;
                    h = (row * (int)rows.Value) + (screenHeight % (int)rows.Value);
                    x = activeScreen.Bounds.Left;   
                    y = 0;
                }
                else if((int)m.WParam == upperRight)
                {
                    col = (screenWidth / (int)columns.Value);
                    row = (screenHeight / (int)rows.Value);
                    w = col * 3;
                    h = row * 5;
                    x = activeScreen.Bounds.Left + col * 9;
                    y = 0;

                    Rect rect = new Rect();
                    NativeMethods.GetWindowRect(window, ref rect);
                    if(rect.Left == x && rect.Top == y && rect.Right == x + w && rect.Bottom == y + h)
                    {
                        col = (screenWidth / (int)columns.Value);
                        row = (screenHeight / (int)rows.Value);
                        w = col * 6;
                        h = row * 5;
                        x = activeScreen.Bounds.Left + col * 6;
                        y = 0;
                    }
                }
                else if((int)m.WParam == lowerRight)
                {
                    col = (screenWidth / (int)columns.Value);
                    row = (screenHeight / (int)rows.Value);
                    w = col * 3;
                    h = (row * 3) + (screenHeight % (int)rows.Value);
                    x = activeScreen.Bounds.Left + col * 9;
                    y = row * 5;

                    Rect rect = new Rect();
                    NativeMethods.GetWindowRect(window, ref rect);
                    if(rect.Left == x && rect.Top == y && rect.Right == x + w && rect.Bottom == y + h)
                    {
                        col = (screenWidth / (int)columns.Value);
                        row = (screenHeight / (int)rows.Value);
                        w = col * 3;
                        h = (row * 3) + (screenHeight % (int)rows.Value);
                        x = activeScreen.Bounds.Left + col * 6;
                        y = row * 5;
                    }
                }
                else if((int)m.WParam == fullRight)
                {
                    col = (screenWidth / (int)columns.Value);
                    row = (screenHeight / (int)rows.Value);
                    w = col * 3;
                    h = row * 8;
                    x = activeScreen.Bounds.Left + col * 9;
                    y = 0;
                }
                NativeMethods.MoveWindow(window, x, y, w, h, true);
            }
            else if(m.Msg == NativeMethods.WM_SHOWME)
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
            //TopMost = true;
            WindowState = FormWindowState.Normal;
        }
    }
}
