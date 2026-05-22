using System;
using System.Windows.Forms;

namespace Test_win  // возможно, у вас другое имя, но не важно
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LockForm());  // Здесь будет запускаться ваша форма
        }
    }
}