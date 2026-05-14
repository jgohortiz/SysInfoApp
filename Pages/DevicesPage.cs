using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace SysInfoApp.Pages
{
    public class DevicesPage : TabPage
    {
        private ListView _list     = null!;
        private Label    _countLbl = null!;
        private TextBox  _search   = null!;
        private List<ListViewItem> _all = new();

        public DevicesPage()
        {
            this.Text    = "Dispositivos";
            this.Padding = new Padding(0);
            BuildUI();
            LoadDevices();
        }

        public void RefreshData() => LoadDevices();

        public IEnumerable<ListViewItem> VisibleItems =>
            _list.Items.Cast<ListViewItem>();

        private void BuildUI()
        {
            // ── Barra de búsqueda ────────────────────────────────────────
            var searchPanel = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 36,
                Padding = new Padding(12, 6, 12, 0)
            };

            var searchLbl = new Label
            {
                Text      = "Buscar:",
                Width     = 52,
                Dock      = DockStyle.Left,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft,
                Font      = new System.Drawing.Font("Segoe UI", 9f)
            };

            _search = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new System.Drawing.Font("Segoe UI", 9f)
            };
            _search.TextChanged += OnSearch;

            _countLbl = new Label
            {
                Width     = 140,
                Dock      = DockStyle.Right,
                TextAlign = System.Drawing.ContentAlignment.MiddleRight,
                Font      = new System.Drawing.Font("Segoe UI", 9f)
            };

            searchPanel.Controls.Add(_search);
            searchPanel.Controls.Add(searchLbl);
            searchPanel.Controls.Add(_countLbl);

            // ── GroupBox + ListView ──────────────────────────────────────
            var group = new GroupBox
            {
                Text    = "Dispositivos conectados",
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
                Font          = new System.Drawing.Font("Segoe UI", 9f),
                Sorting       = SortOrder.Ascending
            };

            _list.Columns.Add("Nombre",     240);
            _list.Columns.Add("Tipo",       130);
            _list.Columns.Add("Fabricante", 160);
            _list.Columns.Add("Estado",      90);
            _list.Columns.Add("ID",         220);

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
            this.Controls.Add(searchPanel);
        }

        private void LoadDevices()
        {
            var items = new List<ListViewItem>();

            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, PNPClass, Manufacturer, Status, DeviceID " +
                    "FROM Win32_PnPEntity " +
                    "WHERE PNPClass IS NOT NULL");

                foreach (ManagementObject dev in searcher.Get())
                {
                    string name   = dev["Name"]?.ToString()?.Trim()         ?? "";
                    string type   = dev["PNPClass"]?.ToString()?.Trim()     ?? "";
                    string mfg    = dev["Manufacturer"]?.ToString()?.Trim() ?? "";
                    string status = dev["Status"]?.ToString()?.Trim()       ?? "";
                    string id     = dev["DeviceID"]?.ToString()?.Trim()     ?? "";

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // ── Traducir estado ──────────────────────────────────
                    status = status.ToUpper() switch
                    {
                        "OK"         => "OK",
                        "ERROR"      => "Error",
                        "DEGRADED"   => "Degradado",
                        "UNKNOWN"    => "Desconocido",
                        "PRED FAIL"  => "Fallo previsto",
                        "STARTING"   => "Iniciando",
                        "STOPPING"   => "Deteniendo",
                        "SERVICE"    => "En servicio",
                        "STRESSED"   => "Sobrecargado",
                        "NONRECOVER" => "No recuperable",
                        "NO CONTACT" => "Sin contacto",
                        "LOST COMM"  => "Sin comunicación",
                        _            => status
                    };

                    // ── Traducir tipo de dispositivo ─────────────────────
                    type = type switch
                    {
                        "USB"             => "USB",
                        "Bluetooth"       => "Bluetooth",
                        "Net"             => "Red",
                        "AudioEndpoint"   => "Audio",
                        "HIDClass"        => "HID / Entrada",
                        "Keyboard"        => "Teclado",
                        "Mouse"           => "Mouse",
                        "Monitor"         => "Monitor",
                        "DiskDrive"       => "Disco",
                        "CDROM"           => "CD / DVD",
                        "Processor"       => "Procesador",
                        "PrintQueue"      => "Impresora",
                        "Camera"          => "Cámara",
                        "Ports"           => "Puerto serie / COM",
                        "Battery"         => "Batería",
                        "Display"         => "Pantalla",
                        "Image"           => "Imagen / Escáner",
                        "Media"           => "Multimedia",
                        "SCSIAdapter"     => "Controladora SCSI",
                        "HDC"             => "Controladora IDE",
                        "System"          => "Sistema",
                        "Computer"        => "Equipo",
                        "Volume"          => "Volumen",
                        "MTD"             => "Almacenamiento",
                        "SmartCardReader" => "Lector tarjeta",
                        "Biometric"       => "Biométrico",
                        "Sensor"          => "Sensor",
                        "WSD"             => "Dispositivo WSD",
                        "SoftwareDevice"  => "Dispositivo virtual",
                        "Extension"       => "Extensión",
                        _                 => type
                    };

                    var item = new ListViewItem(name);
                    item.SubItems.Add(type);
                    item.SubItems.Add(mfg);
                    item.SubItems.Add(status);
                    item.SubItems.Add(id);
                    items.Add(item);
                }
            }
            catch { }

            // Ordenar por tipo y luego por nombre
            items.Sort((a, b) =>
            {
                int cmp = string.Compare(
                    a.SubItems[1].Text,
                    b.SubItems[1].Text,
                    StringComparison.OrdinalIgnoreCase);
                return cmp != 0 ? cmp : string.Compare(
                    a.Text, b.Text,
                    StringComparison.OrdinalIgnoreCase);
            });

            if (_list.InvokeRequired)
                _list.Invoke(() => ReloadList(items));
            else
                ReloadList(items);
        }

        private void ReloadList(List<ListViewItem> items)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            _all = items;
            foreach (var i in _all) _list.Items.Add(i);
            _list.EndUpdate();
            UpdateCount();
        }

        private void OnSearch(object? sender, EventArgs e)
        {
            string q = _search.Text.Trim().ToLower();
            _list.BeginUpdate();
            _list.Items.Clear();

            var filtered = string.IsNullOrEmpty(q)
                ? _all
                : _all.FindAll(i =>
                    i.Text.ToLower().Contains(q) ||
                    (i.SubItems.Count > 1 &&
                     i.SubItems[1].Text.ToLower().Contains(q)) ||
                    (i.SubItems.Count > 2 &&
                     i.SubItems[2].Text.ToLower().Contains(q)));

            foreach (var i in filtered) _list.Items.Add(i);
            _list.EndUpdate();
            UpdateCount();
        }

        private void UpdateCount() =>
            _countLbl.Text = $"{_list.Items.Count} dispositivo(s)";
    }
}