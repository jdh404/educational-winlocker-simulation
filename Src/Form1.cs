using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace Test_win
{
    /// <summary>
    /// Главная форма экрана блокировки - полупрозрачный фон с системой хука для перехвата клавиш Win
    /// </summary>
    public partial class LockForm : Form
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        
        private static readonly Color BackgroundColor = Color.FromArgb(20, 20, 22);
        private const double BackgroundOpacity = 0.65;

        private ControlForm controlWindow;
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc keyboardHookCallback;
        private IntPtr keyboardHookId = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        /// <summary>
        /// Устанавливает низкоуровневый хук клавиатуры для перехвата нажатий клавиши Win
        /// </summary>
        private void SetKeyboardHook()
        {
            keyboardHookCallback = HookCallback;
            
            using (Process currentProcess = Process.GetCurrentProcess())
            using (ProcessModule mainModule = currentProcess.MainModule)
            {
                keyboardHookId = SetWindowsHookEx(
                    WH_KEYBOARD_LL, 
                    keyboardHookCallback, 
                    GetModuleHandle(mainModule.ModuleName), 
                    0);
            }
        }

        /// <summary>
        /// Удаляет установленный хук клавиатуры
        /// </summary>
        private void RemoveKeyboardHook()
        {
            if (keyboardHookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(keyboardHookId);
                keyboardHookId = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Функция обратного вызова для обработки событий клавиатуры.
        /// Блокирует клавиши Windows (Win+X комбинации)
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int virtualKeyCode = Marshal.ReadInt32(lParam);
                if (virtualKeyCode == VK_LWIN || virtualKeyCode == VK_RWIN)
                    return (IntPtr)1; // Мгновенный возврат для предотвращения фризов ОС
            }
            return CallNextHookEx(keyboardHookId, nCode, wParam, lParam);
        }
        
        /// <summary>
        /// Конструктор главной формы блокировки
        /// </summary>
        public LockForm()
        {
            InitializeComponent();
            SetupBackgroundUI();
            this.FormClosing += LockForm_FormClosing;
            this.Shown += LockForm_Shown;
            this.LocationChanged += KeepControlFormCentered;
            this.SizeChanged += KeepControlFormCentered;
        }

        private void InitializeComponent()
        {
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.AutoScaleMode = AutoScaleMode.Dpi; // Включена поддержка High-DPI экранов
        }

        private void SetupBackgroundUI()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackColor = BackgroundColor;
            this.Opacity = BackgroundOpacity;
        }
        
        private void LockForm_Shown(object sender, EventArgs e)
        {
            SetKeyboardHook();
            controlWindow = new ControlForm(this);
            controlWindow.Show(this);
            KeepControlFormCentered(null, null);
        }

        private void KeepControlFormCentered(object sender, EventArgs e)
        {
            if (controlWindow != null && !controlWindow.IsDisposed)
            {
                int newX = this.Location.X + (this.Width - controlWindow.Width) / 2;
                int newY = this.Location.Y + (this.Height - controlWindow.Height) / 2;
                controlWindow.Location = new Point(newX, newY);
            }
        }

        private void LockForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Блокируем закрытие ТОЛЬКО если это прямое действие пользователя (Alt+F4 или Диспетчер задач как окно)
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
            }
            else
            {
                // Если закрывает сама программа (ApplicationExit / FormManager) — корректно чистим хуки
                RemoveKeyboardHook();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            RemoveKeyboardHook();
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_CLOSE = 0x0010;
            if (m.Msg == WM_CLOSE) 
                return; // Игнорируем внешние Win32-запросы на закрытие окна
            base.WndProc(ref m);
        }
    }

    /// <summary>
    /// Форма ввода пароля - полностью непрозрачное окно с интерфейсом входа
    /// </summary>
    public class ControlForm : Form
    {
        private const string UnlockPassword = "1234";
        private static readonly Color FormBackgroundColor = Color.FromArgb(36, 36, 38);
        private static readonly Color TextBoxBackgroundColor = Color.FromArgb(48, 48, 50);
        private static readonly Color ButtonColor = Color.FromArgb(0, 122, 255);
        private static readonly Color TitleColor = Color.FromArgb(0, 122, 255);
        private static readonly Color SubtextColor = Color.FromArgb(142, 142, 147);
        
        private const int FormWidth = 500;
        private const int FormHeight = 400;
        private const int TextBoxWidth = 260;
        private const int TextBoxHeight = 36;
        private const int ButtonWidth = 260;
        private const int ButtonHeight = 45;
        private const int TitleHeight = 80;

        private TextBox passwordInput;
        private Button unlockButton;
        private Label titleLabel;
        private Label instructionLabel;
        private Form backgroundForm;
        
        public ControlForm(Form ownerBackground)
        {
            backgroundForm = ownerBackground;
            this.Size = new Size(FormWidth, FormHeight);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.AutoScaleMode = AutoScaleMode.Dpi; // Защита интерфейса от размытия и кривых шрифтов
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 1.0;
            this.BackColor = FormBackgroundColor;
            InitializeUI();
            this.Shown += (s, e) => passwordInput.Focus();
        }

        private void InitializeUI()
        {
            titleLabel = new Label();
            titleLabel.Text = "DEMO LOCK SYSTEM";
            titleLabel.ForeColor = TitleColor;
            titleLabel.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            titleLabel.TextAlign = ContentAlignment.MiddleCenter;
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = TitleHeight;

            instructionLabel = new Label();
            instructionLabel.Text = "Safe UI testing environment active.\r\nTo exit, please enter the designated PIN-code:";
            instructionLabel.ForeColor = SubtextColor;
            instructionLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            instructionLabel.TextAlign = ContentAlignment.MiddleCenter;
            instructionLabel.Location = new Point(20, 110);
            instructionLabel.Size = new Size(460, 50);

            passwordInput = new TextBox();
            passwordInput.Font = new Font("Segoe UI", 16, FontStyle.Regular);
            passwordInput.PasswordChar = '●';
            passwordInput.TextAlign = HorizontalAlignment.Center;
            passwordInput.Size = new Size(TextBoxWidth, TextBoxHeight);
            passwordInput.Location = new Point((this.Width - TextBoxWidth) / 2, 200);
            passwordInput.BackColor = TextBoxBackgroundColor;
            passwordInput.ForeColor = Color.White;
            passwordInput.BorderStyle = BorderStyle.FixedSingle;
            passwordInput.KeyPress += PasswordInput_KeyPress;

            unlockButton = new Button();
            unlockButton.Text = "Verify PIN";
            unlockButton.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            unlockButton.Size = new Size(ButtonWidth, ButtonHeight);
            unlockButton.Location = new Point((this.Width - ButtonWidth) / 2, 260);
            unlockButton.BackColor = ButtonColor;
            unlockButton.ForeColor = Color.White;
            unlockButton.FlatStyle = FlatStyle.Flat;
            unlockButton.FlatAppearance.BorderSize = 0;
            unlockButton.Cursor = Cursors.Hand;
            unlockButton.Click += UnlockButton_Click;

            this.Controls.Add(titleLabel);
            this.Controls.Add(instructionLabel);
            this.Controls.Add(passwordInput);
            this.Controls.Add(unlockButton);
        }
        
        private void PasswordInput_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                VerifyPassword();
                e.Handled = true;
            }
        }

        private void UnlockButton_Click(object sender, EventArgs e)
        {
            VerifyPassword();
        }

        private void VerifyPassword()
        {
            if (passwordInput.Text == UnlockPassword)
            {
                CustomMessageBox.Show(this, "Access granted! The testing session closed successfully.", "Success", isError: false);
                
                // Сначала выключаем глобальное приложение, чтобы цепочка выгрузки Windows Forms отработала штатно
                Application.Exit();
            }
            else
            {
                CustomMessageBox.Show(this, "Invalid PIN entered.\r\nPlease try again.", "Authentication Error", isError: true);
                passwordInput.Clear();
                passwordInput.Focus();
            }
        }
    }

    /// <summary>
    /// Пользовательское диалоговое окно сообщений
    /// </summary>
    public class CustomMessageBox : Form
    {
        private const int DialogWidth = 400;
        private const int DialogHeight = 200;
        private static readonly Color DialogBackgroundColor = Color.FromArgb(48, 48, 50);
        private static readonly Color OkButtonColor = Color.FromArgb(68, 68, 70);
        private static readonly Color SuccessBorderColor = Color.FromArgb(0, 122, 255);
        private static readonly Color ErrorBorderColor = Color.FromArgb(255, 69, 58);
        private const int BorderThickness = 2;
        
        public CustomMessageBox(string text, string title, bool isError)
        {
            this.Size = new Size(DialogWidth, DialogHeight);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual; // Переключено на ручное точное позиционирование
            this.AutoScaleMode = AutoScaleMode.Dpi;
            this.ShowInTaskbar = false;
            this.TopMost = true;

            Label titleLabel = new Label();
            titleLabel.Text = title.ToUpper();
            titleLabel.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            titleLabel.ForeColor = isError ? ErrorBorderColor : SuccessBorderColor;
            titleLabel.Location = new Point(20, 20);
            titleLabel.Size = new Size(360, 25);

            Label messageLabel = new Label();
            messageLabel.Text = text;
            messageLabel.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            messageLabel.ForeColor = Color.White;
            messageLabel.Location = new Point(20, 55);
            messageLabel.Size = new Size(360, 60);
            messageLabel.AutoSize = true;
            messageLabel.MaximumSize = new Size(360, 0);

            Button okButton = new Button();
            okButton.Text = "OK";
            okButton.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            okButton.Size = new Size(100, 35);
            okButton.Location = new Point(280, 145);
            okButton.BackColor = OkButtonColor;
            okButton.ForeColor = Color.White;
            okButton.FlatStyle = FlatStyle.Flat;
            okButton.FlatAppearance.BorderSize = 0;
            okButton.Cursor = Cursors.Hand;
            okButton.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

            this.Controls.Add(titleLabel);
            this.Controls.Add(messageLabel);
            this.Controls.Add(okButton);

            this.Paint += (s, e) =>
            {
                Color borderColor = isError ? ErrorBorderColor : SuccessBorderColor;
                using (Pen borderPen = new Pen(borderColor, BorderThickness))
                    e.Graphics.DrawRectangle(borderPen, 1, 1, this.Width - 2, this.Height - 2);
            };
        }
        
        /// <summary>
        /// Показывает диалоговое окно с гарантированным центрированием относительно родителя
        /// </summary>
        public static void Show(Form owner, string text, string title, bool isError = false)
        {
            using (var messageDialog = new CustomMessageBox(text, title, isError))
            {
                messageDialog.Owner = owner;
                
                // Программный точный расчет координат центра родительского окна
                int centerX = owner.Location.X + (owner.Width - messageDialog.Width) / 2;
                int centerY = owner.Location.Y + (owner.Height - messageDialog.Height) / 2;
                messageDialog.Location = new Point(centerX, centerY);

                messageDialog.ShowDialog(owner);
            }
        }
    }
}
