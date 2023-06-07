using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Interop;
using WpfHook.ViewModel;
using ComboBox = System.Windows.Controls.ComboBox;
using Control = System.Windows.Forms.Control;
using MessageBox = System.Windows.MessageBox;
using Point = System.Drawing.Point;

namespace WpfHook
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 声明一个hook对象
        /// </summary>
        private HookHelper? _hook = null;

        /// <summary>
        /// 连点定时器
        /// </summary>
        private readonly System.Timers.Timer _timerClicker = new System.Timers.Timer();

        //当前鼠标类型
        private int _mouseTypeIndex = 0;

        //鼠标按下响应时长
        private int _clickLongTime = 0;

        //当前窗口句柄
        private IntPtr windowHandle;

        /// <summary>
        /// 窗口是否已经加载完毕
        /// </summary>
        private bool _isLoaded = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (PresentationSource.FromVisual(this) is HwndSource hwndSource)
                windowHandle = hwndSource.Handle;

            BtnInstallHook.IsEnabled = false;
            BtnUnInstall.IsEnabled = true;
            //初始化钩子对象
            _hook ??= new HookHelper();
            _hook.KeyDown += new System.Windows.Forms.KeyEventHandler(Hook_KeyDown!);
            //hook.KeyPress += new KeyPressEventHandler(Hook_KeyPress);
            _hook.KeyUp += new System.Windows.Forms.KeyEventHandler(Hook_KeyUp!);
            _hook.OnMouseActivity += new System.Windows.Forms.MouseEventHandler(Hook_OnMouseActivity!);

            bool r = _hook != null && _hook.Start();
            if (r)
            {
                BtnInstallHook.IsEnabled = false;
                BtnUnInstall.IsEnabled = true;
                //MessageBox.Show("安装钩子成功!");
            }
            else
            {
                MessageBox.Show("安装钩子失败!");
            }

            _timerClicker.Elapsed += ClickTick!;//事件处理
            HotKeyHandler(HotKey.Text);
            _isLoaded = true;
        }

        /// <summary>
        /// 注册热键
        /// </summary>
        private void HotKeyHandler(string text)
        {
            _key = HookHelper.GlobalAddAtom(text);
            var index = HotKey.SelectedIndex;
            bool success = HookHelper.RegisterHotKey(windowHandle, _key, HookHelper.KeyModifiers.None, _keysArray[index]);
            if (!success)
            {
                var error = text + "已经被其他软件占用，请更换热键";
                MessageBox.Show(error);
            }

            if(ExplainText != null)
                ExplainText.Text = @"使用热键" + text + @"开启关闭连点";
        }

        /// <summary>
        /// 启动点击
        /// </summary>
        private void RunTimer()
        {
            _timerClicker.Enabled = true;//设置启用定时器
            int.TryParse(ClickTime.Text, out var time);
            _timerClicker.Interval = time;//执行时间
            _timerClicker.Start();//开启定时器
        }

        /// <summary>
        /// 停止点击
        /// </summary>
        private void StopTimer()
        {
            _timerClicker.Enabled = false;//设置启用定时器
            _timerClicker.Stop();//开启定时器
        }

        /// <summary>
        /// F1-F12
        /// </summary>
        private readonly int[] _keysArray = {
            0x70,
            0x71,
            0x72,
            0x73,
            0x74,
            0x75,
            0x76,
            0x77,
            0x78,
            0x79,
            0x7A,
            0x7B
        };

        /// <summary>
        /// 当前热键
        /// </summary>
        private int _key = 0;

        private void SetHookButton_clicked(object sender, RoutedEventArgs e)
        {
            if (BtnInstallHook.IsEnabled)
            {
                bool r = _hook != null && _hook.Start();
                if (r)
                {
                    BtnInstallHook.IsEnabled = false;
                    BtnUnInstall.IsEnabled = true;
                    MessageBox.Show("安装钩子成功!");
                }
                else
                {
                    MessageBox.Show("安装钩子失败!");
                }
            }
        }

        private void UnHookButton_clicked(object sender, RoutedEventArgs e)
        {
            if (BtnUnInstall.IsEnabled)
            {
                _hook?.Stop();
                BtnUnInstall.IsEnabled = false;
                BtnInstallHook.IsEnabled = true;
                MessageBox.Show("卸载钩子成功!");
            }
        }

        /// <summary>
        /// 鼠标移动事件
        /// </summary>
        void Hook_OnMouseActivity(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            MouseText.Text = "X:" + e.X + " Y:" + e.Y;
        }

        /// <summary>
        /// 键盘抬起
        /// </summary>
        void Hook_KeyUp(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            KeyText.Text = "键盘抬起, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }

        /// <summary>
        /// 键盘输入
        /// </summary>
        void Hook_KeyPress(object sender, KeyPressEventArgs e)
        { }

        /// <summary>
        /// 键盘按下
        /// </summary>
        void Hook_KeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            KeyText.Text = "键盘按下, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }

        /// <summary>
        /// 监视Windows消息
        /// </summary>
        /// <param name="m"></param>
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromVisual(this) is HwndSource source)
                source.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case HookHelper.WM_HOTKEY:
                    int sid = wParam.ToInt32();
                    if (sid == _key)
                    {
                        if (_timerClicker.Enabled)
                            StopTimer();
                        else
                            RunTimer();
                    }
                    break;
            }
            return IntPtr.Zero;
        }

        /// <summary> 
        /// 定时器事件处理 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param> 
        private void ClickTick(object sender, EventArgs e)
        {
            Point point = Control.MousePosition;
            switch (_mouseTypeIndex)
            {
                case 0:
                    MouseLeftClick(point.X, point.Y);
                    break;
                case 1:
                    MouseMiddleClick(point.X, point.Y);
                    break;
                case 2:
                    MouseRightClick(point.X, point.Y);
                    break;
            }

        }

        /// <summary>
        /// 左键
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void MouseLeftClick(int x, int y)
        {
            //鼠标点击
            HookHelper.mouse_event(HookHelper.mouseeventf_leftdown, x, y, 0, 0);
            if (_clickLongTime > 0)
                Thread.Sleep(_clickLongTime);
            HookHelper.mouse_event(HookHelper.mouseeventf_leftup, x, y, 0, 0);
        }

        /// <summary>
        /// 中健
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void MouseMiddleClick(int x, int y)
        {
            //鼠标点击
            HookHelper.mouse_event(HookHelper.mouseeventf_middledown, x, y, 0, 0);
            if (_clickLongTime > 0)
                Thread.Sleep(_clickLongTime);
            HookHelper.mouse_event(HookHelper.mouseeventf_middleup, x, y, 0, 0);
        }

        /// <summary>
        /// 右健
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void MouseRightClick(int x, int y)
        {
            //鼠标点击
            HookHelper.mouse_event(HookHelper.mouseeventf_rightdown, x, y, 0, 0);
            if (_clickLongTime > 0)
                Thread.Sleep(_clickLongTime);
            HookHelper.mouse_event(HookHelper.mouseeventf_rightup, x, y, 0, 0);
        }

        private void MouseType_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _mouseTypeIndex = MouseType.SelectedIndex;
        }

        private void ClickLongTime_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(ClickLongTime.Text, out var time);
            _clickLongTime = time;
        }

        private void HotKey_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded)
            {
                HookHelper.UnregisterHotKey(windowHandle, _key);
                ComboBox? comboBox = sender as ComboBox; // 强制把 sender 转换为 ComboBox 类型
                ComboBoxItem? selectedItem = comboBox?.SelectedItem as ComboBoxItem; // 获取当前所选项目
                string? selectedItemContent = selectedItem?.Content.ToString(); // 获取当前所选项目的文本
                HotKeyHandler(selectedItemContent!);
            }
        }
    }
}
