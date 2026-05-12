using System;
using System.Windows.Forms;
using SysInfoApp.Pages;

namespace SysInfoApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var splash = new SplashForm();
            splash.Show();
            splash.Refresh();

            // ── Paso 1: Sistema ──────────────────────────────────────────
            splash.SetStatus("Leyendo información del sistema...", 5);
            var systemPage = new SystemPage();
            splash.SetStatus("Información del sistema lista.", 18);

            // ── Paso 2: Hardware ─────────────────────────────────────────
            splash.SetStatus("Consultando procesador y memoria...", 20);
            var hardwarePage = new HardwarePage();
            splash.SetStatus("Hardware listo.", 36);

            // ── Paso 3: Red ──────────────────────────────────────────────
            splash.SetStatus("Obteniendo adaptadores de red...", 38);
            var networkPage = new NetworkPage();
            splash.SetStatus("Red lista.", 52);

            // ── Paso 4: Impresoras ───────────────────────────────────────
            splash.SetStatus("Cargando impresoras instaladas...", 54);
            var printersPage = new PrintersPage();
            splash.SetStatus("Impresoras listas.", 66);

            // ── Paso 5: Batería ──────────────────────────────────────────
            splash.SetStatus("Verificando batería...", 68);
            BatteryPage? batteryPage = null;
            if (HasBattery())
            {
                batteryPage = new BatteryPage();
                splash.SetStatus("Batería lista.", 76);
            }
            else
            {
                splash.SetStatus("Sin batería detectada.", 76);
            }

            // ── Paso 6: Aplicaciones (el más lento — lee el registro) ────
            splash.SetStatus("Cargando aplicaciones instaladas...", 78);
            var softwarePage = new SoftwarePage();
            splash.SetStatus("Aplicaciones listas.", 93);

            // ── Paso 7: Construir ventana principal ──────────────────────
            splash.SetStatus("Preparando interfaz...", 95);
            var mainForm = new Form1(
                systemPage,
                hardwarePage,
                networkPage,
                printersPage,
                softwarePage,
                batteryPage);

            splash.SetStatus("Listo.", 100);
            splash.Refresh();

            // Pequeña pausa para que se vea el 100% antes de cerrar
            System.Threading.Thread.Sleep(300);

            splash.Close();

            Application.Run(mainForm);
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