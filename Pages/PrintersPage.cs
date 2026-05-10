using System;
using System.Management;
using System.Windows.Forms;

namespace SysInfoApp.Pages
{
    public class PrintersPage : TabPage
    {
        private ListView _list    = null!;
        private Label    _countLbl = null!;

        public PrintersPage()
        {
            this.Text    = "Impresoras";
            this.Padding = new Padding(0);
            BuildUI();
            LoadPrinters();
        }

        public void RefreshData()
        {
            LoadPrinters();
        }

        private void BuildUI()
        {
            // ── Barra superior con contador ──────────────────────────────
            var topBar = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 36,
                Padding = new Padding(12, 8, 12, 0)
            };

            _countLbl = new Label
            {
                Width     = 200,
                Dock      = DockStyle.Right,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Font      = new System.Drawing.Font("Segoe UI", 9f)
            };

            topBar.Controls.Add(_countLbl);

            // ── GroupBox + ListView ──────────────────────────────────────
            var group = new GroupBox
            {
                Text    = "Impresoras instaladas",
                Dock    = DockStyle.Fill,
                Padding = new Padding(8),
                Font    = new System.Drawing.Font("Segoe UI", 9f)
            };

            _list = new ListView
            {
                Dock          = DockStyle.Fill,
                View          = View.Details,
                FullRowSelect = true,
                GridLines     = true,
                Font          = new System.Drawing.Font("Segoe UI", 9f)
            };

            _list.Columns.Add("Nombre",          240);
            _list.Columns.Add("Estado",          100);
            _list.Columns.Add("Predeterminada",   110);
            _list.Columns.Add("Red / Local",      90);
            _list.Columns.Add("Puerto",           140);
            _list.Columns.Add("Controlador",      200);

            _list.ColumnClick += (_, e) =>
            {
                _list.Sorting = _list.Sorting == SortOrder.Ascending
                    ? SortOrder.Descending : SortOrder.Ascending;
                _list.Sort();
            };

            group.Controls.Add(_list);

            var wrapper = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(12, 4, 12, 12)
            };
            wrapper.Controls.Add(group);

            this.Controls.Add(wrapper);
            this.Controls.Add(topBar);
        }

        private void LoadPrinters()
        {
            var items = new System.Collections.Generic.List<ListViewItem>();

            try
            {
                // Win32_Printer es accesible sin permisos de administrador
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, PrinterStatus, Default, Network, " +
                    "PortName, DriverName, WorkOffline " +
                    "FROM Win32_Printer");

                foreach (ManagementObject p in searcher.Get())
                {
                    string name      = p["Name"]?.ToString()          ?? "";
                    string port      = p["PortName"]?.ToString()      ?? "";
                    string driver    = p["DriverName"]?.ToString()    ?? "";
                    bool   isDefault = p["Default"] as bool?          ?? false;
                    bool   isNetwork = p["Network"] as bool?          ?? false;
                    bool   offline   = p["WorkOffline"] as bool?      ?? false;

                    // PrinterStatus:
                    // 1=Other, 2=Unknown, 3=Idle, 4=Printing,
                    // 5=Warmup, 6=Stopped, 7=Offline
                    string status = (p["PrinterStatus"]?.ToString()) switch
                    {
                        "3" => "Listo",
                        "4" => "Imprimiendo",
                        "5" => "Calentando",
                        "6" => "Detenida",
                        "7" => "Sin conexión",
                        _   => offline ? "Sin conexión" : "Desconocido"
                    };

                    var item = new ListViewItem(name);
                    item.SubItems.Add(status);
                    item.SubItems.Add(isDefault ? "✔ Sí" : "");
                    item.SubItems.Add(isNetwork ? "Red" : "Local");
                    item.SubItems.Add(port);
                    item.SubItems.Add(driver);

                    items.Add(item);
                }
            }
            catch { }

            // Fallback: usar System.Drawing.Printing si WMI falla
            if (items.Count == 0)
                items = LoadPrintersFallback();

            if (_list.InvokeRequired)
                _list.Invoke(() => ReloadList(items));
            else
                ReloadList(items);
        }

        /// <summary>
        /// Fallback usando System.Drawing.Printing.PrinterSettings.
        /// No requiere admin y funciona en cualquier equipo Windows.
        /// Solo obtiene nombre y si es predeterminada.
        /// </summary>
        private static System.Collections.Generic.List<ListViewItem>
            LoadPrintersFallback()
        {
            var items = new System.Collections.Generic.List<ListViewItem>();
            try
            {
                string defaultPrinter = new System.Drawing.Printing
                    .PrinterSettings().PrinterName;

                foreach (string name in
                    System.Drawing.Printing.PrinterSettings.InstalledPrinters)
                {
                    var item = new ListViewItem(name);
                    item.SubItems.Add("—");
                    item.SubItems.Add(name == defaultPrinter ? "✔ Sí" : "");
                    item.SubItems.Add("—");
                    item.SubItems.Add("—");
                    item.SubItems.Add("—");
                    items.Add(item);
                }
            }
            catch { }
            return items;
        }

        private void ReloadList(
            System.Collections.Generic.List<ListViewItem> items)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var i in items) _list.Items.Add(i);
            _list.EndUpdate();
            _countLbl.Text = $"{_list.Items.Count} impresora(s)";
        }
    }
}