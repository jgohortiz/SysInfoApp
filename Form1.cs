using System;
using System.Drawing;
using System.Windows.Forms;
using SysInfoApp.Helpers;
using SysInfoApp.Pages;

namespace SysInfoApp
{
    public partial class Form1 : Form
    {
        private SystemPage   _systemPage   = null!;
        private HardwarePage _hardwarePage = null!;
        private NetworkPage  _networkPage  = null!;
        private PrintersPage _printersPage = null!;
        private SoftwarePage _softwarePage = null!;
        private BatteryPage? _batteryPage;

        public Form1()
        {
            this.SuspendLayout();
            this.Text          = "Información del equipo";
            this.Size          = new Size(800, 600);
            this.MinimumSize   = new Size(700, 500);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font          = new Font("Segoe UI", 9f);
            this.Padding       = new Padding(10, 10, 10, 0);
            this.ResumeLayout(false);

            BuildUI();
        }

        private void BuildUI()
        {
            // ── TabControl principal ─────────────────────────────────────
            var tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9f)
            };

            _systemPage   = new SystemPage();
            _hardwarePage = new HardwarePage();
            _networkPage  = new NetworkPage();
            _printersPage = new PrintersPage();
            _softwarePage = new SoftwarePage();

            tabs.TabPages.Add(_systemPage);       // 1. Sistema   — siempre primero
            tabs.TabPages.Add(_softwarePage);     // 2. Aplicaciones
            if (HasBattery())                     // 3. Batería   — solo si hay
            {
                _batteryPage = new BatteryPage();
                tabs.TabPages.Add(_batteryPage);
            }
            tabs.TabPages.Add(_hardwarePage);     // 4. Hardware
            tabs.TabPages.Add(_printersPage);     // 5. Impresoras
            tabs.TabPages.Add(_networkPage);      // 6. Red

            // ── StatusStrip ──────────────────────────────────────────────
            var status = new StatusStrip();
            status.Items.Add(new ToolStripStatusLabel
            {
                Text   = $"Usuario: {Environment.UserName}",
                Spring = false
            });
            status.Items.Add(new ToolStripStatusLabel { Spring = true });
            status.Items.Add(new ToolStripStatusLabel
            {
                Text = DateTime.Now.ToString("dd/MM/yyyy  HH:mm")
            });

            // ── Panel inferior con botones ───────────────────────────────
            var bottomPanel = new Panel
            {
                Dock    = DockStyle.Bottom,
                Height  = 40,
                Padding = new Padding(8, 6, 8, 6)
            };

            var btnExport = new Button
            {
                Text  = "Exportar",
                Width = 90,
                Dock  = DockStyle.Left,
                Font  = new Font("Segoe UI", 9f)
            };
            btnExport.Click += (_, _) =>
                ExportHelper.ExportToTxt(_softwarePage.VisibleItems);

            var btnRefresh = new Button
            {
                Text  = "Refrescar",
                Width = 90,
                Dock  = DockStyle.Left,
                Font  = new Font("Segoe UI", 9f)
            };
            btnRefresh.Click += (_, _) => RefreshAll();

            var btnClose = new Button
            {
                Text  = "Cerrar",
                Width = 90,
                Dock  = DockStyle.Right,
                Font  = new Font("Segoe UI", 9f)
            };
            btnClose.Click += (_, _) => this.Close();

            bottomPanel.Controls.Add(btnRefresh);
            bottomPanel.Controls.Add(btnExport);
            bottomPanel.Controls.Add(btnClose);

            // ── Ensamblar ────────────────────────────────────────────────
            this.Controls.Add(tabs);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(status);
        }

        /// <summary>
        /// Detecta si el equipo tiene batería usando PowerStatus.
        /// No requiere admin.
        /// </summary>
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

        private void RefreshAll()
        {
            _hardwarePage.RefreshData();
            _networkPage.RefreshData();
            _printersPage.RefreshData();
            _batteryPage?.RefreshData();
        }
    }
}