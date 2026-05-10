using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using SysInfoApp.Helpers;

namespace SysInfoApp.Pages
{
    public class SystemPage : TabPage
    {
        private Label _uptimeValue = null!;
        private System.Windows.Forms.Timer _timer = null!;

        public SystemPage()
        {
            this.Text    = "Sistema";
            this.Padding = new Padding(0);
            BuildUI();
            StartTimer();
        }

        private void BuildUI()
        {
            string? ssid    = WmiHelper.GetWifiSSID();
            bool    hasWifi = ssid != null;

            int rowCount    = hasWifi ? 8 : 7;
            int groupHeight = hasWifi ? 300 : 265;
            int wrapHeight  = hasWifi ? 316 : 281;

            // ── GroupBox principal ───────────────────────────────────────
            var group = new GroupBox
            {
                Text    = "Información del equipo",
                Dock    = DockStyle.Top,
                Height  = groupHeight,
                Padding = new Padding(12, 8, 12, 8),
                Font    = new System.Drawing.Font("Segoe UI", 9f)
            };

            var grid = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = rowCount,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rowCount; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rowCount));

            var rowList = new System.Collections.Generic.List<(string Label, string Value)>
            {
                ("Hostname:",    Dns.GetHostName()),
                ("Modelo:",      WmiHelper.GetValue("Win32_ComputerSystem", "Model")),
                ("Serial:",      WmiHelper.GetSerial()),
                ("Usuario:",     Environment.UserName),
                ("SO:",          WmiHelper.GetOsVersion()),
                ("Uptime:",      WmiHelper.FormatUptime(WmiHelper.GetUptimeSeconds())),
                ("IP(s):",       GetActiveIPs()),
            };

            if (hasWifi)
                rowList.Add(("Wi-Fi (SSID):", ssid!));

            for (int i = 0; i < rowList.Count; i++)
            {
                var lbl = new Label
                {
                    Text      = rowList[i].Label,
                    Dock      = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                    Font      = new System.Drawing.Font("Segoe UI", 9f,
                                    System.Drawing.FontStyle.Bold)
                };
                var val = new Label
                {
                    Text      = rowList[i].Value,
                    Dock      = DockStyle.Fill,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font      = new System.Drawing.Font("Segoe UI", 9f)
                };

                if (rowList[i].Label == "Uptime:") _uptimeValue = val;

                grid.Controls.Add(lbl, 0, i);
                grid.Controls.Add(val, 1, i);
            }

            group.Controls.Add(grid);

            var wrapper = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = wrapHeight,
                Padding = new Padding(12, 12, 12, 0)
            };
            wrapper.Controls.Add(group);
            this.Controls.Add(wrapper);

            // ── Sección soporte remoto ───────────────────────────────────
            string? supportIP = GetFirstValidIP();
            if (supportIP != null)
            {
                var supportGroup = new GroupBox
                {
                    Text    = "Soporte remoto",
                    Dock    = DockStyle.Top,
                    Height  = 80,
                    Padding = new Padding(4, 4, 4, 4),
                    Font    = new System.Drawing.Font("Segoe UI", 9f)
                };

                var textPart = new Label
                {
                    Text      = "Para soporte remoto, la dirección IP es:",
                    Dock      = DockStyle.Top,
                    Height    = 20,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font      = new System.Drawing.Font("Segoe UI", 9f),
                    Padding   = new Padding(4, 0, 0, 0)
                };

                var ipPart = new Label
                {
                    Text      = supportIP,
                    Dock      = DockStyle.Top,
                    Height    = 32,
                    TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                    Font      = new System.Drawing.Font("Segoe UI", 16f,
                                    System.Drawing.FontStyle.Bold),
                    Padding   = new Padding(4, 0, 0, 0)
                };

                supportGroup.Controls.Add(ipPart);
                supportGroup.Controls.Add(textPart);

                var supportWrapper = new Panel
                {
                    Dock    = DockStyle.Top,
                    Height  = 96,
                    Padding = new Padding(12, 8, 12, 0)
                };
                supportWrapper.Controls.Add(supportGroup);

                this.Controls.Add(supportWrapper);
                this.Controls.SetChildIndex(supportWrapper, 0);
                this.Controls.SetChildIndex(wrapper, 0);
            }

            // ── Créditos ─────────────────────────────────────────────────
            var creditPanel = new Panel
            {
                Dock    = DockStyle.Bottom,
                Height  = 24,
                Padding = new Padding(0, 0, 12, 0)
            };

            var creditLink = new LinkLabel
            {
                Text      = "404q9l - FoxShell",
                Dock      = DockStyle.Right,
                AutoSize  = true,
                Font      = new System.Drawing.Font("Segoe UI", 8f),
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Margin    = new Padding(0, 4, 0, 0)
            };
            creditLink.LinkClicked += (_, _) =>
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName        = "https://github.com/jgohortiz",
                    UseShellExecute = true
                });

            creditPanel.Controls.Add(creditLink);
            this.Controls.Add(creditPanel);
        }

        private void StartTimer()
        {
            _timer          = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick    += (_, _) =>
            {
                if (_uptimeValue != null)
                    _uptimeValue.Text =
                        WmiHelper.FormatUptime(WmiHelper.GetUptimeSeconds());
            };
            _timer.Start();
        }

        private static string? GetFirstValidIP()
        {
            return NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily ==
                            System.Net.Sockets.AddressFamily.InterNetwork
                         && !IPAddress.IsLoopback(a.Address)
                         && !a.Address.ToString().StartsWith("169.254"))
                .Select(a => a.Address.ToString())
                .FirstOrDefault();
        }

        private static string GetActiveIPs()
        {
            var ips = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily ==
                            System.Net.Sockets.AddressFamily.InterNetwork
                         && !IPAddress.IsLoopback(a.Address))
                .Select(a => a.Address.ToString())
                .ToList();

            return ips.Count > 0 ? string.Join(", ", ips) : "—";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { _timer?.Stop(); _timer?.Dispose(); }
            base.Dispose(disposing);
        }
    }
}