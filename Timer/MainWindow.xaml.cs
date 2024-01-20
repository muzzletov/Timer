
/**
 * Copyright 2024 muzzletov 
 * 
 **/
using System.Configuration;
using System.Diagnostics;
using System.Media;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Timer.Properties;

#nullable disable
namespace Timer
{
    public partial class MainWindow : Window, IComponentConnector
    {

        private static MainWindow.LowLevelKeyboardProc kProc = new(MainWindow.KeyboardHookCallback);
        private static MainWindow.LowLevelMouseProc mProc = new(MainWindow.MouseHookCallback);
        private static MainWindow self;
        private static IntPtr _mouseHookID = IntPtr.Zero;
        private static IntPtr _keyboardHookID = IntPtr.Zero;
        private SolidColorBrush BREAK_COLOR = (SolidColorBrush)new BrushConverter().ConvertFrom((object)"#58cf39");
        private SolidColorBrush FOCUS_COLOR = (SolidColorBrush)new BrushConverter().ConvertFrom((object)"#fff");
        private SolidColorBrush ERROR_COLOR = (SolidColorBrush)new BrushConverter().ConvertFrom((object)"#bf0000");
        private int seconds = Settings.Default.Minutes * 60;
        private bool inverted = true;
        private bool locationChanged_;
        private bool break_;
        private DispatcherTimer timer;
        private long enterMillis = 0;

        private static IntPtr SetHook(MainWindow.LowLevelKeyboardProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule mainModule = currentProcess.MainModule)
                    return MainWindow._keyboardHookID = MainWindow.SetWindowsHookEx(13, MainWindow.kProc, MainWindow.GetModuleHandle(mainModule.ModuleName), 0U);
            }
        }

        private static IntPtr SetHook(MainWindow.LowLevelMouseProc proc)
        {
            using (Process currentProcess = Process.GetCurrentProcess())
            {
                using (ProcessModule mainModule = currentProcess.MainModule)
                    return MainWindow.SetWindowsHookEx(14, MainWindow.mProc, MainWindow.GetModuleHandle(mainModule.ModuleName), 0U);
            }
        }

        private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            self.counterLabel.Foreground = (Brush)self.ERROR_COLOR;
            return nCode >= 0 && wParam == (IntPtr)256 ? (IntPtr)1 : MainWindow.CallNextHookEx(MainWindow._keyboardHookID, nCode, wParam, lParam);
        }

        private static IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            self.counterLabel.Foreground = (Brush)self.ERROR_COLOR;
            return nCode >= 0 && (wParam == (IntPtr)512 || wParam == (IntPtr)516 || wParam == (IntPtr)522 || wParam == (IntPtr)512 || wParam == (IntPtr)517 || wParam == (IntPtr)513 || wParam == (IntPtr)514) ? (IntPtr)1 : MainWindow.CallNextHookEx(MainWindow._mouseHookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
          int idHook,
          MainWindow.LowLevelKeyboardProc lpfn,
          IntPtr hMod,
          uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
          int idHook,
          MainWindow.LowLevelMouseProc lpfn,
          IntPtr hMod,
          uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
          IntPtr hhk,
          int nCode,
          IntPtr wParam,
          IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public void onExit() => ((SettingsBase)Settings.Default).Save();

        public MainWindow()
        {
            InitializeComponent();
            self = this;
            this.Deactivated += new EventHandler(this.deactivated);
            this.LocationChanged += new EventHandler(this.locationChanged);
            this.counterLabel.Foreground = (Brush)this.FOCUS_COLOR;
            counterLabel.MouseDown += new MouseButtonEventHandler(this.drag);
            this.MouseUp += new MouseButtonEventHandler(this.dragEnd);
            counterLabel.MouseDoubleClick += new MouseButtonEventHandler(this.reset);
            this.KeyDown += new KeyEventHandler(this.enter);
            this.timer = new DispatcherTimer();
            this.timer.Interval = TimeSpan.FromSeconds(1.0);
            this.timer.Tick += new EventHandler(this.updateOnTick);
            this.timer.Start();
            
            if (Settings.Default.Left == -1.0)
            {
                Settings.Default.Left = SystemParameters.WorkArea.Width * 0.02;
                Settings.Default.Top = SystemParameters.WorkArea.Height * 0.85;
            }

            this.Left = Settings.Default.Left;
            this.Top = Settings.Default.Top;
        }

        private void drag(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) this.DragMove();
        }

        private void dragEnd(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left || !this.locationChanged_)
                return;

            this.locationChanged_ = false;
            Settings.Default.Left = this.Left;
            Settings.Default.Top = this.Top;
            ((SettingsBase)Settings.Default).Save();
        }

        private void locationChanged(object sender, EventArgs e) => this.locationChanged_ = true;

        private void deactivated(object sender, EventArgs e) => ((Window)sender).Topmost = true;

        private void invert(object sender, EventArgs e)
        {
            this.inverted = !this.inverted;
            this.updateOnTick((object)null, (EventArgs)null);
        }
        private void enter(object sender, KeyEventArgs e)
        {
            var key = ((int)e.Key) - 34;
            var isInteger = key > -1 && key < 10;

            if (!isInteger)
            {
                return;
            }

            long millis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (millis-this.enterMillis > 1000)
            {
                Settings.Default.Minutes = key;
            } else
            {
                Settings.Default.Minutes = Settings.Default.Minutes * 10 + key;
            }
            
            this.enterMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            this.seconds = Settings.Default.Minutes * 60;
            ((SettingsBase)Settings.Default).Save();
        }

        private void reset(object sender, EventArgs e)
        {
            this.break_ = false;
            this.seconds = Settings.Default.Minutes*60;
            this.counterLabel.Foreground = (Brush)this.FOCUS_COLOR;
            this.timer.Start();
            this.updateOnTick((object)null, (EventArgs)null);
        }

        private void updateOnTick(object sender, EventArgs e)
        {
            if (this.seconds-- <= 0)
            {
                this.seconds = !this.break_ ? 300 : Settings.Default.Minutes*60;
                this.break_ = !this.break_;
                SystemSounds.Exclamation.Play();

                if (this.break_)
                {
                    MainWindow._keyboardHookID = MainWindow.SetHook(MainWindow.kProc);
                    MainWindow._mouseHookID = MainWindow.SetHook(MainWindow.mProc);
                    this.counterLabel.Foreground = (Brush)this.BREAK_COLOR;
                }
                else
                {
                    MainWindow.UnhookWindowsHookEx(MainWindow._keyboardHookID);
                    MainWindow.UnhookWindowsHookEx(MainWindow._mouseHookID);
                    this.counterLabel.Foreground = (Brush)this.FOCUS_COLOR;
                }
            }
            int num1 = this.seconds / 60;
            int num2 = this.seconds % 60;

            string[] strArray = new string[5]
            {
                (num1 / 10).ToString(),
                null,
                null,
                null,
                null
            };
            int num3 = num1 % 10;
            strArray[1] = num3.ToString();
            strArray[2] = ":";
            num3 = num2 / 10;
            strArray[3] = num3.ToString();
            num3 = num2 % 10;
            strArray[4] = num3.ToString();
            string str = string.Concat(strArray);
            counterLabel.Content = (object)str;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private enum MouseMessages
        {
            WM_MOUSEMOVE = 512,
            WM_LBUTTONDOWN = 513,
            WM_LBUTTONUP = 514,
            WM_RBUTTONDOWN = 516,
            WM_RBUTTONUP = 517,
            WM_MOUSEWHEEL = 522,
        }

        private struct POINT
        {
            public int x;
            public int y;
        }

        private struct MSLLHOOKSTRUCT
        {
            public MainWindow.POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
    }
}
