using System;
using System.Diagnostics;
using System.Management;

namespace SysInfoApp.Helpers
{
    public static class WmiHelper
    {
        public static string GetValue(string wmiClass, string property)
        {
            try
            {
                using var searcher =
                    new ManagementObjectSearcher($"SELECT {property} FROM {wmiClass}");
                foreach (ManagementObject obj in searcher.Get())
                    return obj[property]?.ToString()?.Trim() ?? "N/A";
            }
            catch { }
            return "N/A";
        }

        public static string GetOsVersion()
        {
            string caption = GetValue("Win32_OperatingSystem", "Caption");
            string build   = GetValue("Win32_OperatingSystem", "BuildNumber");
            if (caption == "N/A") caption = Environment.OSVersion.VersionString;
            return build != "N/A" ? $"{caption}  (Build {build})" : caption;
        }

        public static long GetUptimeSeconds()
        {
            try
            {
                string raw    = GetValue("Win32_OperatingSystem", "LastBootUpTime");
                DateTime boot = ManagementDateTimeConverter.ToDateTime(raw);
                return (long)(DateTime.Now - boot).TotalSeconds;
            }
            catch { }
            return Environment.TickCount64 / 1000;
        }

        public static string FormatUptime(long seconds)
        {
            var ts   = TimeSpan.FromSeconds(seconds);
            string d = ts.Days > 0 ? $"{ts.Days} día{(ts.Days != 1 ? "s" : "")}, " : "";
            return $"{d}{ts.Hours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }

        /// <summary>
        /// Devuelve el serial del BIOS.
        /// Intenta WMI primero; si falla por permisos usa un segundo
        /// scope explícito con impersonación de nivel de seguridad bajo.
        /// </summary>
        public static string GetSerial()
        {
            // Intento 1 — WMI estándar (funciona en la mayoría de equipos)
            string serial = GetValue("Win32_BIOS", "SerialNumber");
            if (serial != "N/A") return serial;

            // Intento 2 — WMI con opciones de conexión explícitas
            try
            {
                var options = new ConnectionOptions
                {
                    Impersonation  = ImpersonationLevel.Impersonate,
                    Authentication = AuthenticationLevel.PacketPrivacy,
                    EnablePrivileges = true
                };
                var scope    = new ManagementScope(@"\\.\root\cimv2", options);
                var query    = new ObjectQuery("SELECT SerialNumber FROM Win32_BIOS");
                using var searcher = new ManagementObjectSearcher(scope, query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    string? val = obj["SerialNumber"]?.ToString()?.Trim();
                    if (!string.IsNullOrEmpty(val)) return val;
                }
            }
            catch { }

            return "N/A";
        }

        /// <summary>
        /// Devuelve el SSID de la red Wi-Fi activa.
        /// Intenta WMI (requiere admin); si falla usa netsh (funciona sin admin).
        /// Retorna null si no hay Wi-Fi activa.
        /// </summary>
        public static string? GetWifiSSID()
        {
            // Intento 1 — WMI (requiere admin en algunos sistemas)
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    new ManagementScope(@"\\.\root\wmi"),
                    new ObjectQuery(
                        "SELECT * FROM MSNdis_80211_ServiceSetIdentifier"));

                foreach (ManagementObject obj in searcher.Get())
                {
                    var raw = obj["Ndis80211SSId"] as byte[];
                    if (raw == null) continue;

                    string ssid = System.Text.Encoding.UTF8
                        .GetString(raw)
                        .TrimEnd('\0')
                        .Trim();

                    if (!string.IsNullOrEmpty(ssid))
                        return ssid;
                }
            }
            catch { }

            // Intento 2 — netsh (no requiere admin)
            return GetWifiSSIDNetsh();
        }

        /// <summary>
        /// Obtiene el SSID ejecutando "netsh wlan show interfaces".
        /// Funciona sin permisos de administrador.
        /// </summary>
        private static string? GetWifiSSIDNetsh()
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName               = "netsh",
                    Arguments              = "wlan show interfaces",
                    RedirectStandardOutput = true,
                    UseShellExecute        = false,
                    CreateNoWindow         = true
                };

                using var proc = Process.Start(psi);
                if (proc == null) return null;

                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                foreach (string line in output.Split('\n'))
                {
                    // Busca la línea "    SSID                   : MiRed"
                    // y excluye "BSSID" para no confundirlos
                    string trimmed = line.Trim();
                    if (trimmed.StartsWith("SSID", StringComparison.OrdinalIgnoreCase)
                        && !trimmed.StartsWith("BSSID", StringComparison.OrdinalIgnoreCase))
                    {
                        int idx = trimmed.IndexOf(':');
                        if (idx >= 0)
                        {
                            string ssid = trimmed.Substring(idx + 1).Trim();
                            if (!string.IsNullOrEmpty(ssid))
                                return ssid;
                        }
                    }
                }
            }
            catch { }

            return null;
        }
    }
}