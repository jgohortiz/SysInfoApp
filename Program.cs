using System;
using System.IO;
using System.Windows.Forms;
using SysInfoApp.Helpers;
using SysInfoApp.Pages;

namespace SysInfoApp
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // ── Modo línea de comandos ───────────────────────────────────
            string? exportPath    = null;
            string? exportName    = null;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals("--export", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < args.Length)
                {
                    exportPath = args[i + 1];
                    i++;
                }
                else if (args[i].Equals("--filename", StringComparison.OrdinalIgnoreCase)
                    && i + 1 < args.Length)
                {
                    exportName = args[i + 1];
                    i++;
                }
            }

            if (exportPath != null)
            {
                RunSilentExport(exportPath, exportName);
                return;
            }

            // ── Modo ventana normal ──────────────────────────────────────
            using var splash = new SplashForm();
            splash.Show();
            splash.Refresh();

            splash.SetStatus("Leyendo información del sistema...", 5);
            var systemPage = new SystemPage();
            splash.SetStatus("Información del sistema lista.", 15);

            splash.SetStatus("Consultando procesador y memoria...", 17);
            var hardwarePage = new HardwarePage();
            splash.SetStatus("Hardware listo.", 28);

            splash.SetStatus("Obteniendo adaptadores de red...", 30);
            var networkPage = new NetworkPage();
            splash.SetStatus("Red lista.", 38);

            splash.SetStatus("Cargando impresoras instaladas...", 40);
            var printersPage = new PrintersPage();
            splash.SetStatus("Impresoras listas.", 50);

            splash.SetStatus("Detectando dispositivos conectados...", 52);
            var devicesPage = new DevicesPage();
            splash.SetStatus("Dispositivos listos.", 64);

            splash.SetStatus("Verificando batería...", 66);
            BatteryPage? batteryPage = null;
            if (HasBattery())
            {
                batteryPage = new BatteryPage();
                splash.SetStatus("Batería lista.", 74);
            }
            else
            {
                splash.SetStatus("Sin batería detectada.", 74);
            }

            splash.SetStatus("Cargando aplicaciones instaladas...", 76);
            var softwarePage = new SoftwarePage();
            splash.SetStatus("Aplicaciones listas.", 93);

            splash.SetStatus("Preparando interfaz...", 95);
            var mainForm = new Form1(
                systemPage,
                hardwarePage,
                networkPage,
                printersPage,
                devicesPage,
                softwarePage,
                batteryPage);

            splash.SetStatus("Listo.", 100);
            splash.Refresh();

            System.Threading.Thread.Sleep(300);
            splash.Close();

            Application.Run(mainForm);
        }

        // ════════════════════════════════════════════════════════════════
        //  Exportación silenciosa sin abrir ventana
        // ════════════════════════════════════════════════════════════════
        private static void RunSilentExport(string folder, string? filename)
        {
            try
            {
                // Validar que la carpeta existe; crearla si no
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                // Construir nombre del archivo
                string hostname = System.Net.Dns.GetHostName();
                string name     = string.IsNullOrWhiteSpace(filename)
                    ? $"{hostname}_{DateTime.Now:yyyyMMdd_HHmmss}"
                    : SanitizeFilename(filename);

                string fullPath = Path.Combine(folder, $"{name}.md");

                // Cargar datos (sin UI)
                Console.WriteLine("Recopilando información del sistema...");
                var softwarePage = new SoftwarePage();
                var devicesPage  = new DevicesPage();

                // Exportar
                Console.WriteLine($"Generando archivo: {fullPath}");
                ExportHelper.ExportSilent(
                    softwarePage.VisibleItems,
                    devicesPage.VisibleItems,
                    fullPath);

                Console.WriteLine($"Exportacion completada: {fullPath}");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error al exportar: {ex.Message}");
                Environment.Exit(1);
            }
        }

        private static string SanitizeFilename(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        private static bool HasBattery()
        {
            try
            {
                var ps = SystemInformation.PowerStatus;
                return ps.BatteryChargeStatus != BatteryChargeStatus.NoSystemBattery
                    && ps.BatteryChargeStatus != BatteryChargeStatus.Unknown;
            }
            catch { }
            return false;
        }
    }
}