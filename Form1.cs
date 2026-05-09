using System;
using System.Drawing;
using System.Windows.Forms;
using SysInfoApp.Helpers;
using SysInfoApp.Pages;

namespace SysInfoApp
{
    public partial class Form1 : Form
    {
        private SoftwarePage _softwarePage = null!;

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

            _softwarePage = new SoftwarePage();
            tabs.TabPages.Add(new SystemPage());
            tabs.TabPages.Add(new NetworkPage());
            tabs.TabPages.Add(_softwarePage);

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

            var btnClose = new Button
            {
                Text  = "Cerrar",
                Width = 90,
                Dock  = DockStyle.Right,
                Font  = new Font("Segoe UI", 9f)
            };
            btnClose.Click += (_, _) => this.Close();

            bottomPanel.Controls.Add(btnExport);
            bottomPanel.Controls.Add(btnClose);

            // ── Ensamblar ────────────────────────────────────────────────
            this.Controls.Add(tabs);
            this.Controls.Add(bottomPanel);
            this.Controls.Add(status);
        }
    }
}