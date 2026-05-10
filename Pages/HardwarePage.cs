using System;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace SysInfoApp.Pages
{
    public class HardwarePage : TabPage
    {
        // Referencias a labels que se actualizan al refrescar
        private Label _ramTotal  = null!;
        private Label _ramUsed   = null!;
        private Label _ramFree   = null!;
        private ListView _diskList = null!;

        public HardwarePage()
        {
            this.Text    = "Hardware";
            this.Padding = new Padding(0);
            BuildUI();
        }

        /// <summary>Actualiza RAM y discos sin reconstruir toda la UI.</summary>
        public void RefreshData()
        {
            UpdateRam();
            UpdateDisks();
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
                RowCount    = 3,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));

            layout.Controls.Add(BuildCpuGroup(),    0, 0);
            layout.Controls.Add(BuildMemoryGroup(), 0, 1);
            layout.Controls.Add(BuildDiskGroup(),   0, 2);

            wrapper.Controls.Add(layout);
            this.Controls.Add(wrapper);
        }

        // ════════════════════════════════════════════════════════════════
        //  CPU — estático, no cambia en tiempo de ejecución
        // ════════════════════════════════════════════════════════════════
        private static GroupBox BuildCpuGroup()
        {
            var group = new GroupBox
            {
                Text    = "Procesador",
                Dock    = DockStyle.Fill,
                Padding = new Padding(8),
                Font    = new System.Drawing.Font("Segoe UI", 9f),
                Margin  = new Padding(0, 0, 0, 6)
            };

            var grid = BuildGrid(4);

            string name    = GetWmi("Win32_Processor", "Name");
            string cores   = GetWmi("Win32_Processor", "NumberOfCores");
            string logical = GetWmi("Win32_Processor", "NumberOfLogicalProcessors");
            string speedMhz = GetWmi("Win32_Processor", "MaxClockSpeed");
            string arch    = GetWmi("Win32_Processor", "AddressWidth");

            string speed = int.TryParse(speedMhz, out int mhz)
                ? $"{mhz / 1000.0:F2} GHz  ({mhz} MHz)"
                : speedMhz;

            string archFmt = arch == "64" ? "64 bits" :
                             arch == "32" ? "32 bits" : arch;

            AddRow(grid, 0, "Nombre:",       name);
            AddRow(grid, 1, "Núcleos:",      $"{cores} físicos  /  {logical} lógicos");
            AddRow(grid, 2, "Velocidad:",    speed);
            AddRow(grid, 3, "Arquitectura:", archFmt);

            group.Controls.Add(grid);
            return group;
        }

        // ════════════════════════════════════════════════════════════════
        //  RAM — se construye una vez, se actualiza con RefreshData()
        // ════════════════════════════════════════════════════════════════
        private GroupBox BuildMemoryGroup()
        {
            var group = new GroupBox
            {
                Text    = "Memoria RAM",
                Dock    = DockStyle.Fill,
                Padding = new Padding(8),
                Font    = new System.Drawing.Font("Segoe UI", 9f),
                Margin  = new Padding(0, 0, 0, 6)
            };

            var grid = BuildGrid(3);

            // Labels con referencias para actualizar después
            _ramTotal = MakeValueLabel("");
            _ramUsed  = MakeValueLabel("");
            _ramFree  = MakeValueLabel("");

            grid.Controls.Add(MakeFieldLabel("Total:"),     0, 0);
            grid.Controls.Add(_ramTotal,                    1, 0);
            grid.Controls.Add(MakeFieldLabel("En uso:"),    0, 1);
            grid.Controls.Add(_ramUsed,                     1, 1);
            grid.Controls.Add(MakeFieldLabel("Disponible:"),0, 2);
            grid.Controls.Add(_ramFree,                     1, 2);

            group.Controls.Add(grid);
            UpdateRam(); // carga inicial
            return group;
        }

        private void UpdateRam()
        {
            string totalRaw = GetWmi("Win32_ComputerSystem",  "TotalPhysicalMemory");
            string freeRaw  = GetWmi("Win32_OperatingSystem", "FreePhysicalMemory");

            string total = FormatBytes(totalRaw);
            string free  = long.TryParse(freeRaw, out long freeKb)
                ? FormatBytes((freeKb * 1024).ToString())
                : "N/A";

            string used = "N/A";
            if (long.TryParse(totalRaw, out long totalBytes) && freeKb > 0)
            {
                long usedBytes = totalBytes - (freeKb * 1024);
                used = $"{FormatBytes(usedBytes.ToString())}  " +
                       $"({usedBytes * 100.0 / totalBytes:F1}% en uso)";
            }

            // Actualizar labels (puede llamarse desde hilo UI o timer)
            if (_ramTotal.InvokeRequired)
            {
                _ramTotal.Invoke(() => { _ramTotal.Text = total;
                                         _ramUsed.Text  = used;
                                         _ramFree.Text  = free; });
            }
            else
            {
                _ramTotal.Text = total;
                _ramUsed.Text  = used;
                _ramFree.Text  = free;
            }
        }

        // ════════════════════════════════════════════════════════════════
        //  Discos — se construye una vez, se actualiza con RefreshData()
        // ════════════════════════════════════════════════════════════════
        private GroupBox BuildDiskGroup()
        {
            var group = new GroupBox
            {
                Text    = "Discos",
                Dock    = DockStyle.Fill,
                Padding = new Padding(8),
                Font    = new System.Drawing.Font("Segoe UI", 9f),
                Margin  = new Padding(0, 0, 0, 0)
            };

            _diskList = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                Font          = new System.Drawing.Font("Segoe UI", 9f)
            };

            _diskList.Columns.Add("Unidad",     60);
            _diskList.Columns.Add("Etiqueta",  120);
            _diskList.Columns.Add("Tipo",       90);
            _diskList.Columns.Add("Sistema",    70);
            _diskList.Columns.Add("Total",      90);
            _diskList.Columns.Add("Disponible", 90);
            _diskList.Columns.Add("Uso",        70);

            group.Controls.Add(_diskList);
            UpdateDisks(); // carga inicial
            return group;
        }

        private void UpdateDisks()
        {
            var items = new System.Collections.Generic.List<ListViewItem>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT * FROM Win32_LogicalDisk WHERE DriveType=3 OR DriveType=2");

                foreach (ManagementObject disk in searcher.Get())
                {
                    string drive = disk["DeviceID"]?.ToString()  ?? "";
                    string label = disk["VolumeName"]?.ToString() ?? "";
                    string fs    = disk["FileSystem"]?.ToString()  ?? "";
                    string dtype = disk["DriveType"]?.ToString() == "3"
                                   ? "Fijo" : "Extraíble";

                    long size  = Convert.ToInt64(disk["Size"]      ?? 0L);
                    long free2 = Convert.ToInt64(disk["FreeSpace"]  ?? 0L);
                    long used2 = size - free2;
                    string pct = size > 0
                        ? $"{used2 * 100.0 / size:F1}%"
                        : "—";

                    var item = new ListViewItem(drive);
                    item.SubItems.Add(label);
                    item.SubItems.Add(dtype);
                    item.SubItems.Add(fs);
                    item.SubItems.Add(size  > 0 ? FormatBytes(size.ToString())  : "—");
                    item.SubItems.Add(free2 > 0 ? FormatBytes(free2.ToString()) : "—");
                    item.SubItems.Add(pct);
                    items.Add(item);
                }
            }
            catch { }

            if (_diskList.InvokeRequired)
                _diskList.Invoke(() => ReloadDiskList(items));
            else
                ReloadDiskList(items);
        }

        private void ReloadDiskList(
            System.Collections.Generic.List<ListViewItem> items)
        {
            _diskList.BeginUpdate();
            _diskList.Items.Clear();
            foreach (var i in items) _diskList.Items.Add(i);
            _diskList.EndUpdate();
        }

        // ════════════════════════════════════════════════════════════════
        //  Helpers compartidos
        // ════════════════════════════════════════════════════════════════
        private static TableLayoutPanel BuildGrid(int rows)
        {
            var grid = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                ColumnCount = 2,
                RowCount    = rows,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < rows; i++)
                grid.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows));
            return grid;
        }

        private static void AddRow(TableLayoutPanel grid, int row,
                                   string label, string value)
        {
            grid.Controls.Add(MakeFieldLabel(label), 0, row);
            grid.Controls.Add(MakeValueLabel(value), 1, row);
        }

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
    }
}