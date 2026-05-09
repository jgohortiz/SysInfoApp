using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace SysInfoApp.Helpers
{
    public static class ExportHelper
    {
        public static void ExportToTxt(IEnumerable<ListViewItem> softwareItems)
        {
            string hostname = Dns.GetHostName();

            using var dlg = new SaveFileDialog
            {
                Title            = "Exportar información del sistema",
                Filter           = "Archivo Markdown (*.md)|*.md",
                FileName         = $"{hostname}_{DateTime.Now:yyyyMMdd_HHmmss}.md",
                InitialDirectory = Environment.GetFolderPath(
                                       Environment.SpecialFolder.Desktop)
            };

            if (dlg.ShowDialog() != DialogResult.OK) return;

            var sb = new StringBuilder();

            // ── Encabezado ───────────────────────────────────────────────
            sb.AppendLine($"# Información del equipo — {hostname}");
            sb.AppendLine();
            sb.AppendLine($"> Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();

            // ── Datos del sistema ────────────────────────────────────────
            sb.AppendLine("## Sistema");
            sb.AppendLine();
            sb.AppendLine("| Campo | Valor |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Hostname | {hostname} |");
            sb.AppendLine($"| Modelo | {WmiHelper.GetValue("Win32_ComputerSystem", "Model")} |");
            sb.AppendLine($"| Serial | {WmiHelper.GetValue("Win32_BIOS", "SerialNumber")} |");
            sb.AppendLine($"| Usuario | {Environment.UserName} |");
            sb.AppendLine($"| Sistema Op. | {WmiHelper.GetOsVersion()} |");
            sb.AppendLine($"| Uptime | {WmiHelper.FormatUptime(WmiHelper.GetUptimeSeconds())} |");
            sb.AppendLine();

            // ── Adaptadores de red ───────────────────────────────────────
            sb.AppendLine("## Adaptadores de Red");
            sb.AppendLine();
            sb.AppendLine("| Adaptador | IP (IPv4) | MAC | Estado |");
            sb.AppendLine("|---|---|---|---|");

            var activos = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (NetworkInterface nic in activos)
            {
                string raw = nic.GetPhysicalAddress().ToString();
                string mac = raw.Length == 12
                    ? string.Join(":", Enumerable.Range(0, 6)
                        .Select(i => raw.Substring(i * 2, 2)))
                    : "—";

                var ips = nic.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily ==
                                System.Net.Sockets.AddressFamily.InterNetwork
                             && !IPAddress.IsLoopback(a.Address))
                    .Select(a => a.Address.ToString())
                    .ToList();

                if (ips.Count == 0) continue;

                string ipText = string.Join(", ", ips);
                sb.AppendLine($"| {MdCell(nic.Name)} | {ipText} | {mac} | {nic.OperationalStatus} |");
            }

            sb.AppendLine();

            // ── Programas instalados ─────────────────────────────────────
            var items = softwareItems.ToList();

            sb.AppendLine("## Programas Instalados");
            sb.AppendLine();
            sb.AppendLine($"Total: **{items.Count} programas**");
            sb.AppendLine();
            sb.AppendLine("| Nombre | Versión | Publicador | Fecha instalación |");
            sb.AppendLine("|---|---|---|---|");

            foreach (ListViewItem item in items)
            {
                string name = MdCell(item.Text);
                string ver  = MdCell(item.SubItems.Count > 1 ? item.SubItems[1].Text : "");
                string pub  = MdCell(item.SubItems.Count > 2 ? item.SubItems[2].Text : "");
                string date = MdCell(item.SubItems.Count > 3 ? item.SubItems[3].Text : "");
                sb.AppendLine($"| {name} | {ver} | {pub} | {date} |");
            }

            sb.AppendLine();

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

            MessageBox.Show(
                $"Archivo guardado correctamente:\n\n{dlg.FileName}",
                "Exportación completada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Escapa caracteres especiales de Markdown dentro de celdas de tabla:
        /// reemplaza | por &#124; y saltos de línea por espacio.
        /// </summary>
        private static string MdCell(string value)
        {
            return value
                .Replace("|", "&#124;")
                .Replace("\r\n", " ")
                .Replace("\n", " ")
                .Replace("\r", " ")
                .Trim();
        }
    }
}