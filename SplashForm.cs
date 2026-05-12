using System;
using System.Drawing;
using System.Windows.Forms;

namespace SysInfoApp
{
    public class SplashForm : Form
    {
        private Label  _statusLabel = null!;
        private Label  _appLabel    = null!;
        private Label  _subLabel    = null!;
        private ProgressBar _bar    = null!;

        public SplashForm()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Size            = new Size(420, 220);
            this.BackColor       = SystemColors.Window;
            this.TopMost         = true;

            // Borde simulado con panel exterior
            var border = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = SystemColors.ControlDark,
                Padding   = new Padding(1)
            };

            var inner = new Panel
            {
                Dock      = DockStyle.Fill,
                BackColor = SystemColors.Window,
                Padding   = new Padding(28, 24, 28, 20)
            };

            // Nombre de la app
            _appLabel = new Label
            {
                Text      = "Información del equipo",
                Dock      = DockStyle.Top,
                Height    = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 16f, FontStyle.Bold),
                ForeColor = SystemColors.ControlText
            };

            // Subtítulo
            _subLabel = new Label
            {
                Text      = "404q9l - FoxShell",
                Dock      = DockStyle.Top,
                Height    = 22,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 9f),
                ForeColor = SystemColors.GrayText
            };

            // Separador
            var sep = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 12,
                BackColor = SystemColors.Window
            };

            // Barra de progreso
            _bar = new ProgressBar
            {
                Dock    = DockStyle.Top,
                Height  = 6,
                Minimum = 0,
                Maximum = 100,
                Value   = 0,
                Style   = ProgressBarStyle.Continuous
            };

            // Espacio
            var spacer = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 10,
                BackColor = SystemColors.Window
            };

            // Estado actual
            _statusLabel = new Label
            {
                Text      = "Iniciando...",
                Dock      = DockStyle.Top,
                Height    = 20,
                TextAlign = ContentAlignment.MiddleLeft,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = SystemColors.GrayText
            };

            // Ensamblar en orden inverso (Dock=Top apila de abajo hacia arriba)
            inner.Controls.Add(_statusLabel);
            inner.Controls.Add(spacer);
            inner.Controls.Add(_bar);
            inner.Controls.Add(sep);
            inner.Controls.Add(_subLabel);
            inner.Controls.Add(_appLabel);

            border.Controls.Add(inner);
            this.Controls.Add(border);
        }

        /// <summary>
        /// Actualiza el mensaje de estado y el porcentaje de la barra.
        /// Se puede llamar desde cualquier hilo.
        /// </summary>
        public void SetStatus(string message, int percent)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(() => SetStatus(message, percent));
                return;
            }

            _statusLabel.Text = message;
            _bar.Value        = Math.Max(0, Math.Min(100, percent));
            Application.DoEvents();
        }
    }
}