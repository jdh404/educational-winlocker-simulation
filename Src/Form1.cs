using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices; // Поддержка WinAPI функций (P/Invoke)

namespace Test_win
{
    // 1. ГЛАВНАЯ ФОРМА-ФОН (Полупрозрачная тонировка экрана + хук для блокировки Win)
    public partial class LockForm : Form
    {
        private ControlForm controlWindow; // Ссылка на наше непрозрачное окошко ввода

        // =================== КОД ДЛЯ ХУКА КЛАВИАТУРЫ ===================
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private void SetWinHook()
        {
            _proc = HookCallback;
            using (System.Diagnostics.Process curProcess = System.Diagnostics.Process.GetCurrentProcess())
            using (System.Diagnostics.ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private void UnhookWinHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    // Блокируем клавишу Win, возвращая 1 вместо передачи системе
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }
        // =================== КОНЕЦ КОДА ДЛЯ ХУКА ===================

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
        }

        private void SetupBackgroundUI()
        {
            this.WindowState = FormWindowState.Maximized;
            this.FormBorderStyle = FormBorderStyle.None;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(20, 20, 22); 
            this.Opacity = 0.65; // Полупрозрачность для эффекта тонирования
        }

        private void LockForm_Shown(object sender, EventArgs e)
        {
            // Активируем хук при показе формы
            SetWinHook();

            controlWindow = new ControlForm(this);
            controlWindow.Show(this);
            KeepControlFormCentered(null, null);
        }

        private void KeepControlFormCentered(object sender, EventArgs e)
        {
            if (controlWindow != null && !controlWindow.IsDisposed)
            {
                controlWindow.Location = new Point(
                    this.Location.X + (this.Width - controlWindow.Width) / 2,
                    this.Location.Y + (this.Height - controlWindow.Height) / 2
                );
            }
        }

        private void LockForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Запрещаем закрывать форму вручную пользователю
            }
            else
            {
                UnhookWinHook(); // Снимаем хук при системном выходе (Application.Exit)
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnhookWinHook(); // Страховка снятия хука при уничтожении окна
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_CLOSE = 0x0010;
            if (m.Msg == WM_CLOSE) return;
            base.WndProc(ref m);
        }
    }

    // 2. ФОРМА ИНТЕРФЕЙСА (Полностью непрозрачная, центрируется поверх фона)
    public class ControlForm : Form
    {
        private TextBox txtPassword;
        private Button btnUnlock;
        private Label lblMessage;
        private Label lblInstruction;
        private const string UnlockPassword = "1234";
        private Form _ownerBackground;

        public ControlForm(Form ownerBackground)
        {
            _ownerBackground = ownerBackground;
            
            this.Size = new Size(500, 400);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual; 
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.Opacity = 1.0; // Плотное, непрозрачное окно интерфейса
            this.BackColor = Color.FromArgb(36, 36, 38);

            // Главный заголовок
            lblMessage = new Label();
            lblMessage.Text = "DEMO LOCK SYSTEM";
            lblMessage.ForeColor = Color.FromArgb(0, 122, 255);
            lblMessage.Font = new Font("Segoe UI", 20, FontStyle.Bold);
            lblMessage.TextAlign = ContentAlignment.MiddleCenter;
            lblMessage.Dock = DockStyle.Top;
            lblMessage.Height = 80;

            // Текст инструкции
            lblInstruction = new Label();
            lblInstruction.Text = "Safe UI testing environment active.\nTo exit, please enter the designated PIN-code:";
            lblInstruction.ForeColor = Color.FromArgb(142, 142, 147);
            lblInstruction.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            lblInstruction.TextAlign = ContentAlignment.MiddleCenter;
            lblInstruction.Location = new Point(20, 110);
            lblInstruction.Size = new Size(460, 50);

            // Поле для ввода пароля
            txtPassword = new TextBox();
            txtPassword.Font = new Font("Segoe UI", 16, FontStyle.Regular);
            txtPassword.PasswordChar = '●';
            txtPassword.TextAlign = HorizontalAlignment.Center;
            txtPassword.Size = new Size(260, 36);
            txtPassword.Location = new Point((this.Width - 260) / 2, 200);
            txtPassword.BackColor = Color.FromArgb(48, 48, 50);
            txtPassword.ForeColor = Color.White;
            txtPassword.BorderStyle = BorderStyle.FixedSingle;
            txtPassword.KeyPress += TxtPassword_KeyPress;

            // Кнопка проверки
            btnUnlock = new Button();
            btnUnlock.Text = "Verify PIN";
            btnUnlock.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            btnUnlock.Size = new Size(260, 45);
            btnUnlock.Location = new Point((this.Width - 260) / 2, 260);
            btnUnlock.BackColor = Color.FromArgb(0, 122, 255);
            btnUnlock.ForeColor = Color.White;
            btnUnlock.FlatStyle = FlatStyle.Flat;
            btnUnlock.FlatAppearance.BorderSize = 0;
            btnUnlock.Cursor = Cursors.Hand;
            btnUnlock.Click += BtnUnlock_Click;

            this.Controls.Add(lblMessage);
            this.Controls.Add(lblInstruction);
            this.Controls.Add(txtPassword);
            this.Controls.Add(btnUnlock);

            this.Shown += (s, e) => txtPassword.Focus();
        }

        private void TxtPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                CheckPassword();
                e.Handled = true;
            }
        }

        private void BtnUnlock_Click(object sender, EventArgs e)
        {
            CheckPassword();
        }

        private void CheckPassword()
        {
            if (txtPassword.Text == UnlockPassword)
            {
                CustomMessageBox.Show(this, "Access granted! The testing session closed successfully.", "Success", false);
                
                // Корректно закрываем приложение: убираем флаг отмены, гасим формы и выходим
                if (_ownerBackground != null)
                {
                    _ownerBackground.Close(); 
                }
                Application.Exit();
            }
            else
            {
                CustomMessageBox.Show(this, "Invalid PIN entered.\nPlease try again.", "Authentication Error", true);
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }

    // 3. СТИЛИЗОВАННЫЙ MESSAGEBOX (Полностью непрозрачный)
    public class CustomMessageBox : Form
    {
        public CustomMessageBox(string text, string title, bool isError)
        {
            this.Size = new Size(400, 200);
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.CenterParent; 
            this.BackColor = Color.FromArgb(48, 48, 50); 
            this.ShowInTaskbar = false;
            this.TopMost = true; 

            Label lblTitle = new Label();
            lblTitle.Text = title.ToUpper();
            lblTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            lblTitle.ForeColor = isError ? Color.FromArgb(255, 69, 58) : Color.FromArgb(0, 122, 255); 
            lblTitle.Location = new Point(20, 20);
            lblTitle.Size = new Size(360, 25);

            Label lblText = new Label();
            lblText.Text = text;
            lblText.Font = new Font("Segoe UI", 11, FontStyle.Regular);
            lblText.ForeColor = Color.White;
            lblText.Location = new Point(20, 55);
            lblText.Size = new Size(360, 60);

            Button btnOk = new Button();
            btnOk.Text = "OK";
            btnOk.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnOk.Size = new Size(100, 35);
            btnOk.Location = new Point(280, 145);
            btnOk.BackColor = Color.FromArgb(68, 68, 70);
            btnOk.ForeColor = Color.White;
            btnOk.FlatStyle = FlatStyle.Flat;
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Cursor = Cursors.Hand;
            btnOk.Click += (s, e) => { this.DialogResult = DialogResult.OK; this.Close(); };

            this.Controls.Add(lblTitle);
            this.Controls.Add(lblText);
            this.Controls.Add(btnOk);

            this.Paint += (s, e) =>
            {
                Color borderColor = isError ? Color.FromArgb(255, 69, 58) : Color.FromArgb(0, 122, 255);
                using (Pen pen = new Pen(borderColor, 2))
                {
                    e.Graphics.DrawRectangle(pen, 1, 1, this.Width - 2, this.Height - 2);
                }
            };
        }

        public static void Show(Form owner, string text, string title, bool isError = false)
        {
            using (var msgBox = new CustomMessageBox(text, title, isError))
            {
                msgBox.Owner = owner; 
                msgBox.ShowDialog(owner);
            }
        }
    }
}