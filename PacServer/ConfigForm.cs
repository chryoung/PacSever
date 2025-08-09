using System;
using System.Text.Json;
using System.Windows.Forms;
using System.IO;

namespace PacServer;

public partial class ConfigForm : Form
{
    public event Func<string, string, int, Task>? OnConfigSaved;

    TextBox txtPacUrl = new TextBox { Dock = DockStyle.Fill };
    TextBox txtProxy = new TextBox { Dock = DockStyle.Fill };
    TextBox txtPort = new TextBox { Dock = DockStyle.Fill };

    public ConfigForm()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);

        Text = "PAC Proxy Config";

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true,
            Padding = new Padding(10)
        };

        OnConfigSaved += async (pacUrl, proxyServer, port) =>
        {
            ConfigFile.Instance.Value.PacUrl = pacUrl;
            ConfigFile.Instance.Value.ProxyServer = proxyServer;
            ConfigFile.Instance.Value.PacServerListeningPort = port;

            ConfigFile.Instance.Value.Save();
            await Task.CompletedTask;
        };

        var lblPacUrl = new Label { Text = "PAC URL:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var lblProxy = new Label { Text = "HTTP Proxy Server:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var lblPort = new Label { Text = "PAC Server Listening Port:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var btnSave = new Button { Text = "Save", AutoSize = true };
        txtPacUrl.Text = ConfigFile.Instance.Value.PacUrl;
        txtProxy.Text = ConfigFile.Instance.Value.ProxyServer;
        txtPort.Text = ConfigFile.Instance.Value.PacServerListeningPort.ToString();

        layout.Controls.Add(lblPacUrl, 0, 0);
        layout.Controls.Add(txtPacUrl, 1, 0);
        layout.Controls.Add(lblProxy, 0, 1);
        layout.Controls.Add(txtProxy, 1, 1);
        layout.Controls.Add(lblPort, 0, 2);
        layout.Controls.Add(txtPort, 1, 2);
        layout.Controls.Add(btnSave, 0, 3);
        layout.SetColumnSpan(btnSave, 2);

        Controls.Add(layout);

        btnSave.Click += async (s, e) =>
        {
            if (OnConfigSaved != null)
            {
                if (int.TryParse(txtPort.Text, out int port))
                {
                    await OnConfigSaved(txtPacUrl.Text, txtProxy.Text, port);
                    if (port <= 0 || port > 65535)
                    {
                        MessageBox.Show("Port number must be between 1 and 65535.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Invalid port number. Please enter a valid integer.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            Close();
        };

        MinimumSize = new System.Drawing.Size(800, 100);
    }
}