using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace SysInfoApp.Pages
{
    public class SoftwarePage : TabPage
    {
        private ListView _list     = null!;
        private TextBox  _search   = null!;
        private Label    _countLbl = null!;
        private List<ListViewItem> _all = new();

        public SoftwarePage()
        {
            this.Text    = "Aplicaciones";
            this.Padding = new Padding(0);
            BuildUI();
            LoadSoftware();
        }

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
                Width     = 120,
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
                Text    = "Programas instalados",
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

            _list.Columns.Add("Nombre",             260);
            _list.Columns.Add("Versión",            120);
            _list.Columns.Add("Publicador",         200);
            _list.Columns.Add("Fecha instalación",  130);

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

        private void LoadSoftware()
        {
            _list.BeginUpdate();
            _all.Clear();

            string[] keys =
            {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string keyPath in keys)
            {
                using var root = Registry.LocalMachine.OpenSubKey(keyPath);
                if (root == null) continue;

                foreach (string sub in root.GetSubKeyNames())
                {
                    RegistryKey? key;
                    try   { key = root.OpenSubKey(sub); }
                    catch { continue; }
                    if (key == null) continue;

                    string? name = key.GetValue("DisplayName")?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(name) || !seen.Add(name)) continue;

                    string ver     = key.GetValue("DisplayVersion")?.ToString() ?? "";
                    string pub     = key.GetValue("Publisher")?.ToString() ?? "";
                    string rawDate = key.GetValue("InstallDate")?.ToString() ?? "";

                    string date = "";
                    if (rawDate.Length == 8 &&
                        DateTime.TryParseExact(rawDate, "yyyyMMdd", null,
                            System.Globalization.DateTimeStyles.None, out var dt))
                        date = dt.ToString("dd/MM/yyyy");

                    var item = new ListViewItem(name);
                    item.SubItems.Add(ver);
                    item.SubItems.Add(pub);
                    item.SubItems.Add(date);
                    _all.Add(item);
                }
            }

            _all = _all.OrderBy(i => i.Text).ToList();
            foreach (var item in _all) _list.Items.Add(item);

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
                : _all.Where(i =>
                    i.Text.ToLower().Contains(q) ||
                    (i.SubItems.Count > 2 &&
                     i.SubItems[2].Text.ToLower().Contains(q)));

            foreach (var item in filtered) _list.Items.Add(item);
            _list.EndUpdate();
            UpdateCount();
        }

        private void UpdateCount() =>
            _countLbl.Text = $"{_list.Items.Count} aplicación(es)";
    }
}