using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows.Forms;

namespace SysInfoApp.Helpers
{
    public static class ExportHelper
    {
        /// <summary>
        /// Exporta mostrando un SaveFileDialog al usuario.
        /// </summary>
        public static void ExportToTxt(
            IEnumerable<ListViewItem> softwareItems,
            IEnumerable<ListViewItem> deviceItems)
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

            var sb = BuildContent(hostname, softwareItems, deviceItems);
            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);

            MessageBox.Show(
                $"Archivo guardado correctamente:\n\n{dlg.FileName}",
                "Exportación completada",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        /// <summary>
        /// Exporta directamente a la ruta indicada sin mostrar diálogo.
        /// Usado cuando la aplicación se ejecuta por línea de comandos.
        /// </summary>
        public static void ExportSilent(
            IEnumerable<ListViewItem> softwareItems,
            IEnumerable<ListViewItem> deviceItems,
            string fullPath)
        {
            string hostname = Dns.GetHostName();
            var sb = BuildContent(hostname, softwareItems, deviceItems);
            File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        }

        // ════════════════════════════════════════════════════════════════
        //  Construcción del contenido — compartido por ambos métodos
        // ════════════════════════════════════════════════════════════════
        private static StringBuilder BuildContent(
            string hostname,
            IEnumerable<ListViewItem> softwareItems,
            IEnumerable<ListViewItem> deviceItems)
        {
            var sb = new StringBuilder();

            // ── Encabezado ───────────────────────────────────────────────
            sb.AppendLine($"# Información del equipo — {hostname}");
            sb.AppendLine();
            sb.AppendLine($"> Generado: {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine();

            // ── Sistema ──────────────────────────────────────────────────
            sb.AppendLine("## Sistema");
            sb.AppendLine();
            sb.AppendLine("| Campo | Valor |");
            sb.AppendLine("|---|---|");
            sb.AppendLine($"| Hostname | {hostname} |");
            sb.AppendLine($"| Modelo | {WmiHelper.GetValue("Win32_ComputerSystem", "Model")} |");
            sb.AppendLine($"| Serial | {WmiHelper.GetSerial()} |");
            sb.AppendLine($"| Usuario | {Environment.UserName} |");
            sb.AppendLine($"| SO | {WmiHelper.GetOsVersion()} |");
            sb.AppendLine($"| Uptime | {WmiHelper.FormatUptime(WmiHelper.GetUptimeSeconds())} |");
            sb.AppendLine();

            // ── Hardware ─────────────────────────────────────────────────
            sb.AppendLine("## Hardware");
            sb.AppendLine();

            sb.AppendLine("### Procesador");
            sb.AppendLine();
            sb.AppendLine("| Campo | Valor |");
            sb.AppendLine("|---|---|");

            string cpuName  = GetWmi("Win32_Processor", "Name");
            string cores    = GetWmi("Win32_Processor", "NumberOfCores");
            string logical  = GetWmi("Win32_Processor", "NumberOfLogicalProcessors");
            string speedMhz = GetWmi("Win32_Processor", "MaxClockSpeed");
            string arch     = GetWmi("Win32_Processor", "AddressWidth");

            string speed   = int.TryParse(speedMhz, out int mhz)
                ? $"{mhz / 1000.0:F2} GHz ({mhz} MHz)" : speedMhz;
            string archFmt = arch == "64" ? "64 bits" :
                             arch == "32" ? "32 bits" : arch;

            sb.AppendLine($"| Nombre | {MdCell(cpuName)} |");
            sb.AppendLine($"| Núcleos | {cores} físicos / {logical} lógicos |");
            sb.AppendLine($"| Velocidad | {speed} |");
            sb.AppendLine($"| Arquitectura | {archFmt} |");
            sb.AppendLine();

            sb.AppendLine("### Memoria RAM");
            sb.AppendLine();
            sb.AppendLine("| Campo | Valor |");
            sb.AppendLine("|---|---|");

            string totalRaw = GetWmi("Win32_ComputerSystem",  "TotalPhysicalMemory");
            string freeRaw  = GetWmi("Win32_OperatingSystem", "FreePhysicalMemory");
            string total    = FormatBytes(totalRaw);
            string free     = long.TryParse(freeRaw, out long freeKb)
                ? FormatBytes((freeKb * 1024).ToString()) : "N/A";
            string used     = "N/A";

            if (long.TryParse(totalRaw, out long totalBytes) && freeKb > 0)
            {
                long usedBytes = totalBytes - (freeKb * 1024);
                used = $"{FormatBytes(usedBytes.ToString())} " +
                       $"({usedBytes * 100.0 / totalBytes:F1}% en uso)";
            }

            sb.AppendLine($"| Total | {total} |");
            sb.AppendLine($"| En uso | {used} |");
            sb.AppendLine($"| Disponible | {free} |");
            sb.AppendLine();

            sb.AppendLine("### Discos");
            sb.AppendLine();
            sb.AppendLine("| Unidad | Etiqueta | Tipo | Sistema | Total | Disponible | Uso |");
            sb.AppendLine("|---|---|---|---|---|---|---|");

            try
            {
                using var ds = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_LogicalDisk " +
                    "WHERE DriveType=3 OR DriveType=2");

                foreach (ManagementObject disk in ds.Get())
                {
                    string drive  = disk["DeviceID"]?.ToString()   ?? "";
                    string label  = disk["VolumeName"]?.ToString()  ?? "";
                    string fs     = disk["FileSystem"]?.ToString()   ?? "";
                    string dtype  = disk["DriveType"]?.ToString() == "3"
                                    ? "Fijo" : "Extraíble";
                    long   size   = Convert.ToInt64(disk["Size"]      ?? 0L);
                    long   free2  = Convert.ToInt64(disk["FreeSpace"]  ?? 0L);
                    long   used2  = size - free2;
                    string pct    = size > 0
                        ? $"{used2 * 100.0 / size:F1}%" : "—";

                    sb.AppendLine(
                        $"| {MdCell(drive)} " +
                        $"| {MdCell(label)} " +
                        $"| {dtype} " +
                        $"| {fs} " +
                        $"| {(size  > 0 ? FormatBytes(size.ToString())  : "—")} " +
                        $"| {(free2 > 0 ? FormatBytes(free2.ToString()) : "—")} " +
                        $"| {pct} |");
                }
            }
            catch { }

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

                sb.AppendLine(
                    $"| {MdCell(nic.Name)} " +
                    $"| {string.Join(", ", ips)} " +
                    $"| {mac} " +
                    $"| {nic.OperationalStatus} |");
            }

            sb.AppendLine();

            // ── Dispositivos ─────────────────────────────────────────────
            var devices = deviceItems.ToList();

            sb.AppendLine("## Dispositivos Conectados");
            sb.AppendLine();
            sb.AppendLine($"Total: **{devices.Count} dispositivos**");
            sb.AppendLine();
            sb.AppendLine("| Nombre | Tipo | Fabricante | Estado | ID |");
            sb.AppendLine("|---|---|---|---|---|");

            foreach (ListViewItem item in devices)
            {
                string name  = MdCell(item.Text);
                string type  = MdCell(item.SubItems.Count > 1
                    ? item.SubItems[1].Text : "");
                string mfg   = MdCell(item.SubItems.Count > 2
                    ? item.SubItems[2].Text : "");
                string stat  = MdCell(item.SubItems.Count > 3
                    ? item.SubItems[3].Text : "");
                string id    = MdCell(item.SubItems.Count > 4
                    ? item.SubItems[4].Text : "");
                sb.AppendLine($"| {name} | {type} | {mfg} | {stat} | {id} |");
            }

            sb.AppendLine();

            // ── Aplicaciones instaladas ──────────────────────────────────
            var swItems = softwareItems.ToList();

            sb.AppendLine("## Aplicaciones Instaladas");
            sb.AppendLine();
            sb.AppendLine($"Total: **{swItems.Count} aplicaciones**");
            sb.AppendLine();
            sb.AppendLine("| Nombre | Versión | Publicador | Fecha instalación |");
            sb.AppendLine("|---|---|---|---|");

            foreach (ListViewItem item in swItems)
            {
                string name = MdCell(item.Text);
                string ver  = MdCell(item.SubItems.Count > 1
                    ? item.SubItems[1].Text : "");
                string pub  = MdCell(item.SubItems.Count > 2
                    ? item.SubItems[2].Text : "");
                string date = MdCell(item.SubItems.Count > 3
                    ? item.SubItems[3].Text : "");
                sb.AppendLine($"| {name} | {ver} | {pub} | {date} |");
            }

            sb.AppendLine();

            return sb;
        }

        // ── Helpers privados ─────────────────────────────────────────────

        private static string GetWmi(string wmiClass, string property)
        {
            try
            {
                using var searcher =
                    new ManagementObjectSearcher(
                        $"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                    return obj[property]?.ToString()?.Trim() ?? "N/A";
            }
            catch { }
            return "N/A";
        }

        private static string FormatBytes(string rawBytes)
        {
            if (!long.TryParse(rawBytes, out long bytes)) return "N/A";
            if (bytes >= 1_073_741_824)
                return $"{bytes / 1_073_741_824.0:F1} GB";
            if (bytes >= 1_048_576)
                return $"{bytes / 1_048_576.0:F1} MB";
            if (bytes >= 1_024)
                return $"{bytes / 1_024.0:F1} KB";
            return $"{bytes} B";
        }

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