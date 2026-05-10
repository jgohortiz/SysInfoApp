using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Forms;

namespace SysInfoApp.Pages
{
    public class NetworkPage : TabPage
    {
        private ListView _list = null!;

        public NetworkPage()
        {
            this.Text    = "Red";
            this.Padding = new Padding(0);
            BuildUI();
        }

        /// <summary>Recarga la lista de adaptadores activos.</summary>
        public void RefreshData()
        {
            var items = new System.Collections.Generic.List<ListViewItem>();

            var activos = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up
                         && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

            foreach (NetworkInterface nic in activos)
            {
                string raw = nic.GetPhysicalAddress().ToString();
                string mac = raw.Length == 12
                    ? string.Join(":", System.Linq.Enumerable.Range(0, 6)
                        .Select(i => raw.Substring(i * 2, 2)))
                    : "—";

                var ips = nic.GetIPProperties().UnicastAddresses
                    .Where(a => a.Address.AddressFamily ==
                                System.Net.Sockets.AddressFamily.InterNetwork
                             && !System.Net.IPAddress.IsLoopback(a.Address))
                    .Select(a => a.Address.ToString())
                    .ToList();

                if (ips.Count == 0) continue;

                var item = new ListViewItem(nic.Name);
                item.SubItems.Add(string.Join(", ", ips));
                item.SubItems.Add(mac);
                item.SubItems.Add(nic.OperationalStatus.ToString());
                items.Add(item);
            }

            if (_list.InvokeRequired)
                _list.Invoke(() => ReloadList(items));
            else
                ReloadList(items);
        }

        private void BuildUI()
        {
            var group = new GroupBox
            {
                Text    = "Adaptadores de red activos",
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

            _list.Columns.Add("Adaptador",  200);
            _list.Columns.Add("IP (IPv4)",  150);
            _list.Columns.Add("MAC",        150);
            _list.Columns.Add("Estado",     100);

            group.Controls.Add(_list);

            var wrapper = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(12)
            };
            wrapper.Controls.Add(group);
            this.Controls.Add(wrapper);

            RefreshData(); // carga inicial
        }

        private void ReloadList(
            System.Collections.Generic.List<ListViewItem> items)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var i in items) _list.Items.Add(i);
            _list.EndUpdate();
        }
    }
}