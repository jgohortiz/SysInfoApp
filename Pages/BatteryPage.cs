using System;
using System.Management;
using System.Windows.Forms;

namespace SysInfoApp.Pages
{
    public class BatteryPage : TabPage
    {
        private TableLayoutPanel _grid      = null!;
        private Label _lblPct              = null!;
        private Label _lblStatus           = null!;
        private Label _lblTime             = null!;
        private Label _lblHealth           = null!;
        private Label _lblCapacityFull     = null!;
        private Label _lblCapacityDesign   = null!;
        private Label _lblVoltage          = null!;
        private Label _lblChemistry        = null!;
        private ProgressBar _progressBar   = null!;
        private System.Windows.Forms.Timer _timer = null!;

        public BatteryPage()
        {
            this.Text    = "Batería";
            this.Padding = new Padding(0);
            BuildUI();
            RefreshData();
            StartTimer();
        }

        public void RefreshData()
        {
            UpdateBattery();
        }

        private void BuildUI()
        {
            var wrapper = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(12)
            };

            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 1,
                RowCount    = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,  100));

            layout.Controls.Add(BuildChargeGroup(), 0, 0);
            layout.Controls.Add(BuildDetailsGroup(), 0, 1);

            wrapper.Controls.Add(layout);
            this.Controls.Add(wrapper);
        }

        // ════════════════════════════════════════════════════════════════
        //  Sección carga — barra de progreso + porcentaje + estado
        // ════════════════════════════════════════════════════════════════
        private GroupBox BuildChargeGroup()
        {
            var group = new GroupBox
            {
                Text    = "Carga actual",
                Dock    = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                Font    = new System.Drawing.Font("Segoe UI", 9f),
                Margin  = new Padding(0, 0, 0, 6)
            };

            var inner = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 2,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inner.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 60));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
            inner.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            _progressBar = new ProgressBar
            {
                Dock    = DockStyle.Fill,
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous
            };

            _lblPct = new Label
            {
                Text      = "—",
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Font      = new System.Drawing.Font("Segoe UI", 14f,
                                System.Drawing.FontStyle.Bold)
            };

            _lblStatus = new Label
            {
                Text      = "—",
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font      = new System.Drawing.Font("Segoe UI", 9f)
            };

            _lblTime = new Label
            {
                Text      = "—",
                Dock      = DockStyle.Fill,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Font      = new System.Drawing.Font("Segoe UI", 9f)
            };

            inner.Controls.Add(_progressBar, 0, 0);
            inner.Controls.Add(_lblPct,      1, 0);
            inner.SetRowSpan(_lblPct, 1);
            inner.Controls.Add(_lblStatus,   0, 1);
            inner.Controls.Add(_lblTime,     1, 1);

            group.Controls.Add(inner);
            return group;
        }

        // ════════════════════════════════════════════════════════════════
        //  Sección detalles — datos técnicos de la batería
        // ════════════════════════════════════════════════════════════════
        private GroupBox BuildDetailsGroup()
        {
            var group = new GroupBox
            {
                Text    = "Detalles",
                Dock    = DockStyle.Fill,
                Padding = new Padding(12, 8, 12, 8),
                Font    = new System.Drawing.Font("Segoe UI", 9f),
                Margin  = new Padding(0, 0, 0, 0)
            };

            _grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = 6,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            _grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 6; i++)
                _grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6));

            _lblHealth        = MakeValueLabel("—");
            _lblCapacityFull  = MakeValueLabel("—");
            _lblCapacityDesign= MakeValueLabel("—");
            _lblVoltage       = MakeValueLabel("—");
            _lblChemistry     = MakeValueLabel("—");

            _grid.Controls.Add(MakeFieldLabel("Estado de salud:"),      0, 0);
            _grid.Controls.Add(_lblHealth,                              1, 0);
            _grid.Controls.Add(MakeFieldLabel("Capacidad actual:"),     0, 1);
            _grid.Controls.Add(_lblCapacityFull,                        1, 1);
            _grid.Controls.Add(MakeFieldLabel("Capacidad de diseño:"),  0, 2);
            _grid.Controls.Add(_lblCapacityDesign,                      1, 2);
            _grid.Controls.Add(MakeFieldLabel("Voltaje:"),              0, 3);
            _grid.Controls.Add(_lblVoltage,                             1, 3);
            _grid.Controls.Add(MakeFieldLabel("Química:"),              0, 4);
            _grid.Controls.Add(_lblChemistry,                           1, 4);

            group.Controls.Add(_grid);
            return group;
        }

        // ════════════════════════════════════════════════════════════════
        //  Lógica de actualización
        // ════════════════════════════════════════════════════════════════
        private void UpdateBattery()
        {
            // ── PowerStatus — no requiere admin ──────────────────────────
            var ps  = SystemInformation.PowerStatus;
            float pct = ps.BatteryLifePercent;
            int   pctInt = pct >= 0 && pct <= 1
                ? (int)Math.Round(pct * 100)
                : 0;

            string status = ps.PowerLineStatus switch
            {
                PowerLineStatus.Online  => "Conectado a la corriente",
                PowerLineStatus.Offline => "Usando batería",
                _                       => "Desconocido"
            };

            // Tiempo restante (-1 = desconocido / cargando)
            int secsRemaining = ps.BatteryLifeRemaining;
            string timeText = secsRemaining > 0
                ? $"Tiempo restante: {secsRemaining / 3600}h " +
                  $"{(secsRemaining % 3600) / 60}m"
                : ps.PowerLineStatus == PowerLineStatus.Online
                    ? "Cargando..."
                    : "Calculando...";

            // ── WMI Win32_Battery — accesible sin admin ──────────────────
            string chemistry   = "Indefinido";
            string voltage     = "Indefinido";
            string fullCap     = "Indefinido";
            string designCap   = "Indefinido";
            string health      = "Indefinido";

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_Battery");

                foreach (ManagementObject bat in searcher.Get())
                {
                    // Química de la batería
                    chemistry = (bat["Chemistry"]?.ToString()) switch
                    {
                        "1"  => "Otra",
                        "2"  => "Desconocida",
                        "3"  => "Plomo-Ácido",
                        "4"  => "Níquel-Cadmio",
                        "5"  => "Níquel-Hidruro",
                        "6"  => "Litio-Ion",
                        "7"  => "Zinc-Aire",
                        "8"  => "Litio-Polímero",
                        _    => "—"
                    };

                    // Voltaje en mV → V
                    if (bat["DesignVoltage"] is uint mv && mv > 0)
                        voltage = $"{mv / 1000.0:F2} V";

                    // Capacidades en mWh
                    if (bat["FullChargeCapacity"] is uint fc && fc > 0)
                        fullCap = $"{fc} mWh";

                    if (bat["DesignCapacity"] is uint dc && dc > 0)
                    {
                        designCap = $"{dc} mWh";

                        // Salud = capacidad actual / capacidad de diseño
                        if (bat["FullChargeCapacity"] is uint fc2 && fc2 > 0)
                        {
                            double h = fc2 * 100.0 / dc;
                            health = $"{h:F1}%  " +
                                     (h >= 80 ? "✔ Buena" :
                                      h >= 50 ? "⚠ Desgastada" : "✘ Reemplazar");
                        }
                    }

                    break; // solo primera batería
                }
            }
            catch { }

            // ── Aplicar a controles ──────────────────────────────────────
            void Apply()
            {
                _progressBar.Value    = Math.Max(0, Math.Min(100, pctInt));
                _lblPct.Text          = $"{pctInt}%";
                _lblStatus.Text       = status;
                _lblTime.Text         = timeText;
                _lblHealth.Text       = health;
                _lblCapacityFull.Text = fullCap;
                _lblCapacityDesign.Text = designCap;
                _lblVoltage.Text      = voltage;
                _lblChemistry.Text    = chemistry;
            }

            if (_progressBar.InvokeRequired)
                _progressBar.Invoke(Apply);
            else
                Apply();
        }

        private void StartTimer()
        {
            _timer          = new System.Windows.Forms.Timer { Interval = 30000 };
            _timer.Tick    += (_, _) => UpdateBattery();
            _timer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer?.Stop(); _timer?.Dispose(); }
            base.Dispose(disposing);
        }

        // ── Helpers ──────────────────────────────────────────────────────
        private static Label MakeFieldLabel(string text) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleRight,
            Font      = new System.Drawing.Font("Segoe UI", 9f,
                            System.Drawing.FontStyle.Bold)
        };

        private static Label MakeValueLabel(string text) => new Label
        {
            Text      = text,
            Dock      = DockStyle.Fill,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
            Font      = new System.Drawing.Font("Segoe UI", 9f)
        };
    }
}