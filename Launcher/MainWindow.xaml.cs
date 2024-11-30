using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Threading;

namespace Launcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon trayIcon;
        private WindowsHook windowsHook;
        private LauncherWindow currentLauncher;

        public MainWindow()
        {
            InitializeComponent();
            InitializeTrayIcon();
            InitializeHook();
            this.Hide();
        }

        private void InitializeTrayIcon()
        {
            trayIcon = new NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Visible = true,
                Text = "Launcher"
            };
        }

        private void InitializeHook()
        {
            windowsHook = new WindowsHook();
            windowsHook.StartButtonMiddleClick += OnStartButtonMiddleClick;
        }

        private void OnStartButtonMiddleClick(object sender, EventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                if (currentLauncher != null)
                {
                    currentLauncher.isClosing = true;
                    currentLauncher.Close();
                    currentLauncher = null;
                }
                else
                {
                    currentLauncher = new LauncherWindow();
                    currentLauncher.Closed += (s, args) => currentLauncher = null;
                    currentLauncher.Show();
                }
            });
        }

        protected override void OnClosed(EventArgs e)
        {
            currentLauncher?.Close();
            windowsHook?.Dispose();
            trayIcon.Dispose();
            base.OnClosed(e);
        }
    }

    public class WindowsHook : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MBUTTONDOWN = 0x0207;

        public event EventHandler StartButtonMiddleClick;

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelMouseProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll")]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public WindowsHook()
        {
            _proc = HookCallback;
            _hookID = SetHook(_proc);
        }

        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MBUTTONDOWN)
            {
                var startButton = FindWindow("Shell_TrayWnd", null);
                if (IsMouseOverStartButton())
                {
                    StartButtonMiddleClick?.Invoke(this, EventArgs.Empty);
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        private bool IsMouseOverStartButton()
        {
            var startButton = FindWindow("Shell_TrayWnd", null);
            if (startButton == IntPtr.Zero) return false;

            POINT cursorPos;
            RECT windowRect;

            if (!GetCursorPos(out cursorPos)) return false;
            if (!GetWindowRect(startButton, out windowRect)) return false;

            return cursorPos.X >= windowRect.Left && cursorPos.X <= windowRect.Right &&
                   cursorPos.Y >= windowRect.Top && cursorPos.Y <= windowRect.Bottom;
        }

        public void Dispose()
        {
            UnhookWindowsHookEx(_hookID);
        }
    }
}