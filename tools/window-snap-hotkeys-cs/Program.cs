using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WindowSnapHotkeys
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApplicationContext());
        }
    }

    internal sealed class TrayApplicationContext : ApplicationContext
    {
        private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string StartupValueName = "WindowSnapHotkeys";

        private const int WhKeyboardLl = 13;

        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;

        private const uint LlkhfAltDown = 0x20;

        private const int VkA = 0x41;
        private const int VkD = 0x44;
        private const int VkMenu = 0x12;

        private const int SwRestore = 9;
        private const uint MonitorDefaultToNearest = 2;
        private const uint SwpNoZorder = 0x0004;

        private static readonly LowLevelKeyboardProc KeyboardProc = KeyboardHookCallback;

        private static TrayApplicationContext _instance;

        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly StatusForm _statusForm;
        private readonly Icon _trayIcon;
        private ToolStripMenuItem _startupToggleItem;

        private IntPtr _hookHandle;
        private bool _leftHandled;
        private bool _rightHandled;
        private bool _isExiting;
        private bool _isDisposed;

        public TrayApplicationContext()
        {
            _instance = this;

            _statusForm = new StatusForm();
            _statusForm.HideRequested += HideStatusWindow;
            _statusForm.FormClosing += OnStatusFormClosing;

            var autoStartRegistered = EnsureStartupRegistration();

            _trayMenu = BuildTrayMenu();
            _trayIcon = CreateMoonTrayIcon();
            _notifyIcon = new NotifyIcon
            {
                Icon = _trayIcon,
                Text = "Window Snap Hotkeys",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            _notifyIcon.DoubleClick += OnTrayIconDoubleClick;

            if (!InstallKeyboardHook())
            {
                MessageBox.Show(
                    "无法安装全局键盘钩子，程序将退出。",
                    "Window Snap Hotkeys",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                ExitApplication();
                return;
            }

            _notifyIcon.ShowBalloonTip(
                2000,
                "Window Snap Hotkeys",
                autoStartRegistered
                    ? "程序已在系统托盘运行：Alt+A 左贴靠，Alt+D 右贴靠。"
                    : "程序已在系统托盘运行（开机自启注册失败，可手动设置）。",
                ToolTipIcon.Info);
        }

        private static bool EnsureStartupRegistration()
        {
            try
            {
                var executablePath = Application.ExecutablePath;
                if (string.IsNullOrEmpty(executablePath))
                {
                    return false;
                }

                var startupValue = "\"" + executablePath + "\"";

                using (var runKey = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, true))
                {
                    if (runKey == null)
                    {
                        return false;
                    }

                    var currentValue = runKey.GetValue(StartupValueName) as string;
                    if (string.Equals(currentValue, startupValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    runKey.SetValue(StartupValueName, startupValue, RegistryValueKind.String);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool IsStartupRegistered()
        {
            try
            {
                var executablePath = Application.ExecutablePath;
                if (string.IsNullOrEmpty(executablePath))
                {
                    return false;
                }

                var startupValue = "\"" + executablePath + "\"";

                using (var runKey = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, false))
                {
                    if (runKey == null)
                    {
                        return false;
                    }

                    var currentValue = runKey.GetValue(StartupValueName) as string;
                    return string.Equals(currentValue, startupValue, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool SetStartupRegistration(bool enable)
        {
            try
            {
                using (var runKey = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, true))
                {
                    if (runKey == null)
                    {
                        return false;
                    }

                    if (enable)
                    {
                        var executablePath = Application.ExecutablePath;
                        if (string.IsNullOrEmpty(executablePath))
                        {
                            return false;
                        }

                        var startupValue = "\"" + executablePath + "\"";
                        runKey.SetValue(StartupValueName, startupValue, RegistryValueKind.String);
                        return true;
                    }

                    if (runKey.GetValue(StartupValueName) != null)
                    {
                        runKey.DeleteValue(StartupValueName, false);
                    }

                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private static Icon CreateMoonTrayIcon()
        {
            var bitmap = new Bitmap(32, 32);
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.Clear(Color.Transparent);
                graphics.SmoothingMode = SmoothingMode.AntiAlias;

                using (var moonPath = new GraphicsPath())
                using (var cutoutPath = new GraphicsPath())
                using (var moonBrush = new SolidBrush(Color.FromArgb(255, 255, 214, 102)))
                {
                    moonPath.AddEllipse(3f, 3f, 24f, 24f);
                    cutoutPath.AddEllipse(13f, 1f, 20f, 20f);

                    using (var moonRegion = new Region(moonPath))
                    {
                        moonRegion.Exclude(cutoutPath);
                        graphics.FillRegion(moonBrush, moonRegion);
                    }
                }

                using (var starBrush = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
                {
                    graphics.FillRectangle(starBrush, 22, 22, 3, 3);
                    graphics.FillRectangle(starBrush, 18, 6, 2, 2);
                }
            }

            var iconHandle = bitmap.GetHicon();
            try
            {
                using (var handleIcon = Icon.FromHandle(iconHandle))
                {
                    return (Icon)handleIcon.Clone();
                }
            }
            finally
            {
                DestroyIcon(iconHandle);
                bitmap.Dispose();
            }
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            var menu = new ContextMenuStrip();

            var showItem = new ToolStripMenuItem("显示窗口");
            showItem.Click += delegate { ShowStatusWindow(); };

            var hideItem = new ToolStripMenuItem("隐藏窗口");
            hideItem.Click += delegate { HideStatusWindow(); };

            _startupToggleItem = new ToolStripMenuItem();
            _startupToggleItem.Click += delegate { ToggleStartupRegistration(); };

            var exitItem = new ToolStripMenuItem("退出程序");
            exitItem.Click += delegate { ExitApplication(); };

            menu.Items.Add(showItem);
            menu.Items.Add(hideItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(_startupToggleItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            UpdateStartupMenuLabel();

            return menu;
        }

        private void ToggleStartupRegistration()
        {
            var currentlyEnabled = IsStartupRegistered();
            var targetEnabled = !currentlyEnabled;
            var success = SetStartupRegistration(targetEnabled);

            UpdateStartupMenuLabel();

            _notifyIcon.ShowBalloonTip(
                1500,
                "Window Snap Hotkeys",
                success
                    ? (targetEnabled ? "已启用开机自启。" : "已禁用开机自启。")
                    : "开机自启设置失败，请手动检查。",
                success ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private void UpdateStartupMenuLabel()
        {
            if (_startupToggleItem == null)
            {
                return;
            }

            _startupToggleItem.Text = IsStartupRegistered()
                ? "禁用开机自启"
                : "启用开机自启";
        }

        private void OnTrayIconDoubleClick(object sender, EventArgs e)
        {
            if (_statusForm.Visible)
            {
                HideStatusWindow();
            }
            else
            {
                ShowStatusWindow();
            }
        }

        private void ShowStatusWindow()
        {
            if (_statusForm.IsDisposed)
            {
                return;
            }

            _statusForm.Show();
            _statusForm.WindowState = FormWindowState.Normal;
            _statusForm.ShowInTaskbar = true;
            _statusForm.Activate();
        }

        private void HideStatusWindow()
        {
            if (_statusForm.IsDisposed)
            {
                return;
            }

            _statusForm.ShowInTaskbar = false;
            _statusForm.Hide();
        }

        private void OnStatusFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isExiting)
            {
                return;
            }

            e.Cancel = true;
            HideStatusWindow();
        }

        private bool InstallKeyboardHook()
        {
            _hookHandle = SetWindowsHookEx(WhKeyboardLl, KeyboardProc, GetModuleHandle(null), 0);
            return _hookHandle != IntPtr.Zero;
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (_instance == null)
            {
                return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
            }

            return _instance.HandleKeyboardHook(nCode, wParam, lParam);
        }

        private IntPtr HandleKeyboardHook(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
            {
                return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
            }

            var message = wParam.ToInt32();
            var isKeyDown = message == WmKeyDown || message == WmSysKeyDown;
            var isKeyUp = message == WmKeyUp || message == WmSysKeyUp;

            var keyboardData = (KbdLlHookStruct)Marshal.PtrToStructure(lParam, typeof(KbdLlHookStruct));
            var vkCode = (int)keyboardData.vkCode;

            if (isKeyDown && IsAltPressed(keyboardData.flags))
            {
                if (vkCode == VkA)
                {
                    if (!_leftHandled)
                    {
                        SnapForegroundWindow(SnapSide.Left);
                        _leftHandled = true;
                    }

                    return (IntPtr)1;
                }

                if (vkCode == VkD)
                {
                    if (!_rightHandled)
                    {
                        SnapForegroundWindow(SnapSide.Right);
                        _rightHandled = true;
                    }

                    return (IntPtr)1;
                }
            }

            if (isKeyUp)
            {
                if (vkCode == VkA && _leftHandled)
                {
                    _leftHandled = false;
                    return (IntPtr)1;
                }

                if (vkCode == VkD && _rightHandled)
                {
                    _rightHandled = false;
                    return (IntPtr)1;
                }

                if (vkCode == VkMenu)
                {
                    _leftHandled = false;
                    _rightHandled = false;
                }
            }

            return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
        }

        private static bool IsAltPressed(uint flags)
        {
            if ((flags & LlkhfAltDown) != 0)
            {
                return true;
            }

            return (GetAsyncKeyState(VkMenu) & 0x8000) != 0;
        }

        private static void SnapForegroundWindow(SnapSide side)
        {
            var windowHandle = GetForegroundWindow();
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            if (IsIconic(windowHandle) || IsZoomed(windowHandle))
            {
                ShowWindow(windowHandle, SwRestore);
            }

            var monitorHandle = MonitorFromWindow(windowHandle, MonitorDefaultToNearest);
            if (monitorHandle == IntPtr.Zero)
            {
                return;
            }

            var monitorInfo = new MonitorInfo();
            monitorInfo.cbSize = Marshal.SizeOf(typeof(MonitorInfo));

            if (!GetMonitorInfo(monitorHandle, ref monitorInfo))
            {
                return;
            }

            var workArea = monitorInfo.rcWork;
            var totalWidth = workArea.Right - workArea.Left;
            var totalHeight = workArea.Bottom - workArea.Top;

            if (totalWidth <= 1 || totalHeight <= 1)
            {
                return;
            }

            var leftWidth = totalWidth / 2;

            int targetX;
            int targetWidth;

            if (side == SnapSide.Left)
            {
                targetX = workArea.Left;
                targetWidth = leftWidth;
            }
            else
            {
                targetX = workArea.Left + leftWidth;
                targetWidth = totalWidth - leftWidth;
            }

            SetWindowPos(
                windowHandle,
                IntPtr.Zero,
                targetX,
                workArea.Top,
                targetWidth,
                totalHeight,
                SwpNoZorder);
        }

        private void ExitApplication()
        {
            if (_isExiting)
            {
                return;
            }

            _isExiting = true;
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            DisposeResources();
            base.ExitThreadCore();
        }

        private void DisposeResources()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            if (_hookHandle != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookHandle);
                _hookHandle = IntPtr.Zero;
            }

            _instance = null;

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            if (_trayMenu != null)
            {
                _trayMenu.Dispose();
            }

            if (_statusForm != null && !_statusForm.IsDisposed)
            {
                _statusForm.FormClosing -= OnStatusFormClosing;
                _statusForm.Dispose();
            }

            if (_trayIcon != null)
            {
                _trayIcon.Dispose();
            }
        }

        private enum SnapSide
        {
            Left,
            Right
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct MonitorInfo
        {
            public int cbSize;
            public Rect rcMonitor;
            public Rect rcWork;
            public int dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KbdLlHookStruct
        {
            public uint vkCode;
            public uint scanCode;
            public uint flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool IsZoomed(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);
    }

    internal sealed class StatusForm : Form
    {
        public event Action HideRequested;

        public StatusForm()
        {
            Text = "Window Snap Hotkeys";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = true;
            Width = 420;
            Height = 220;

            var descriptionLabel = new Label
            {
                AutoSize = false,
                Left = 20,
                Top = 20,
                Width = 360,
                Height = 90,
                Text = "程序正在系统托盘运行。\r\n\r\nAlt + A：当前窗口贴靠左半屏\r\nAlt + D：当前窗口贴靠右半屏\r\n双击托盘图标：显示/隐藏本窗口"
            };

            var hideButton = new Button
            {
                Text = "隐藏到托盘",
                Left = 140,
                Top = 130,
                Width = 120,
                Height = 32
            };
            hideButton.Click += delegate
            {
                var handler = HideRequested;
                if (handler != null)
                {
                    handler();
                }
            };

            Controls.Add(descriptionLabel);
            Controls.Add(hideButton);
        }
    }
}
