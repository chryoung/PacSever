using System;
using System.Text.Json;
using System.Windows.Forms;
using System.IO;

namespace PacServer;

public partial class ConfigForm : Form
{
    public event Func<string, string, Task>? OnConfigSaved;
    private readonly string configFilePath = "config.json";
    private readonly ConfigFile defaultConfigFile = new()
    {
        PacUrl = string.Empty,
        ProxyServer = @"http://127.0.0.1:8080",
    };

    TextBox txtPacUrl = new TextBox { Dock = DockStyle.Fill };
    TextBox txtProxy = new TextBox { Dock = DockStyle.Fill };

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

        OnConfigSaved += async (pacUrl, proxyServer) => await SaveConfigFileAsync(pacUrl, proxyServer);

        var lblPacUrl = new Label { Text = "PAC URL:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var lblProxy = new Label { Text = "HTTP Proxy Server:", AutoSize = true, TextAlign = ContentAlignment.MiddleLeft };
        var btnSave = new Button { Text = "Save", AutoSize = true };

        layout.Controls.Add(lblPacUrl, 0, 0);
        layout.Controls.Add(txtPacUrl, 1, 0);
        layout.Controls.Add(lblProxy, 0, 1);
        layout.Controls.Add(txtProxy, 1, 1);
        layout.Controls.Add(btnSave, 0, 2);
        layout.SetColumnSpan(btnSave, 2);

        Controls.Add(layout);

        btnSave.Click += async (s, e) =>
        {
            if (OnConfigSaved != null)
            {
                await OnConfigSaved(txtPacUrl.Text, txtProxy.Text);
            }

            Close();
        };

        MinimumSize = new System.Drawing.Size(800, 100);
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        ConfigForm_Load(this, e);
    }

    private async void ConfigForm_Load(object sender, EventArgs e)
    {
        ConfigFile config = await ReadConfigFileAsync();
        txtPacUrl.Text = config.PacUrl;
        txtProxy.Text = config.ProxyServer;
    }

    private async Task<ConfigFile> ReadConfigFileAsync()
    {
        try
        {
            string json = await File.ReadAllTextAsync(configFilePath);
            var configFile = JsonSerializer.Deserialize<ConfigFile>(json);

            return configFile ?? defaultConfigFile;
        }
        catch
        {
            return defaultConfigFile;
        }
    }

    private async Task SaveConfigFileAsync(string pacUrl, string proxyServer)
    {
        ConfigFile config = new ConfigFile
        {
            PacUrl = pacUrl,
            ProxyServer = proxyServer
        };

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(configFilePath, json);
    }
}