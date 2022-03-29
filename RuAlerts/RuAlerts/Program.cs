using System.IO;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.Win32;

namespace RuAlerts
{
    class RuAlerts
    {
        static void Main(string[] args)
        {
            string pathFile = AppDomain.CurrentDomain.BaseDirectory + "alertOptions.txt";
            string[] options = File.ReadLines(pathFile).ToArray();

            if (options[1].Remove(0, 9) == "True")
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.SetValue("Ruben Clients Alerts", System.Windows.Forms.Application.ExecutablePath.ToString());
                options[1] = "Startup: Actived";
                File.WriteAllLines(pathFile, options);
            }
            else if (options[1].Remove(0, 10) == "False")
            {
                RegistryKey reg = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                reg.DeleteValue("Ruben Clients Alerts");
            }

            if (options[0].Remove(0, 8) == "False")
            {
                Environment.Exit(0);
            }
            else
            {
                int interval = 12 * 60 * 60 * 1000;
                for(int i = 0; i < options.Length; i++)
                {
                    Notification();
                    Thread.Sleep(interval);
                }
            }
        }
                
        private static void Notification()
        {
            string pathFile = AppDomain.CurrentDomain.BaseDirectory + "alertOptions.txt";
            string[] options = File.ReadLines(pathFile).ToArray();

            new ToastContentBuilder()
                .AddText("Alerta por vencimiento de clientes")
                .AddText(options[2])
                .AddText(options[3])
                .Show();
        }
    }
}