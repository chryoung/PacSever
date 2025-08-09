using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PacServer;

public partial class TrayAppContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private ToolStripMenuItem enableProxyMenuItem;
    private ConfigForm? configForm;
    private PacHttpServer pacServer;
    private string pacContent = "";
    private string pacUrl = "";
    private string proxyServer = "";

    // Add a new menu item for updating PAC in the constructor
    private ToolStripMenuItem updatePacMenuItem;

    // Add a constant for the PAC file path
    private const string LocalPacFilePath = "pacfile.pac";

    // Modify constructor to load PAC from disk if available
    public TrayAppContext()
    {
        trayIcon = new NotifyIcon()
        {
            Icon = Resource.TrayIcon,
            ContextMenuStrip = new ContextMenuStrip(),
            Visible = true,
            Text = "PAC Proxy Tray"
        };

        bool isPacEnabled = !string.IsNullOrEmpty(GetSystemPacProxy());

        enableProxyMenuItem = new ToolStripMenuItem("Enable Proxy", null, OnEnableProxyClicked) { CheckOnClick = true };
        trayIcon.ContextMenuStrip.Items.Add(enableProxyMenuItem);
        trayIcon.ContextMenuStrip.Items.Add("Config...", null, OnConfigClicked);
        enableProxyMenuItem.Checked = isPacEnabled;

        updatePacMenuItem = new ToolStripMenuItem("Update PAC", null, OnUpdatePacClicked);
        trayIcon.ContextMenuStrip.Items.Add(updatePacMenuItem);

        trayIcon.ContextMenuStrip.Items.Add("Exit", null, (s, e) => ExitThread());

        pacServer = new();

        trayIcon.DoubleClick += (s, e) => ShowConfig();

        // Load PAC file from disk if exists
        if (!File.Exists(LocalPacFilePath))
        {
            return;
        }

        pacContent = System.IO.File.ReadAllText(LocalPacFilePath);
        pacServer.SetPacContent(pacContent);
    }

    // Add a field to track last downloaded PAC URL
    private string lastDownloadedPacUrl = "";

    private void OnConfigClicked(object? sender, EventArgs e) => ShowConfig();

    // Update OnUpdatePacClicked to save PAC to disk
    private async void OnUpdatePacClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(pacUrl))
        {
            pacContent = await DownloadPacFile(pacUrl, proxyServer);
            lastDownloadedPacUrl = pacUrl;
            pacServer.SetPacContent(pacContent);
            // Save PAC to disk
            File.WriteAllText(LocalPacFilePath, pacContent);
        }
    }

    // Update ShowConfig to save PAC to disk when downloaded
    private void ShowConfig()
    {
        if (configForm == null || configForm.IsDisposed)
        {
            configForm = new ConfigForm();
            configForm.OnConfigSaved += async (url, proxy) =>
            {
                pacUrl = url;
                proxyServer = proxy;
                if (pacUrl != lastDownloadedPacUrl)
                {
                    pacContent = await DownloadPacFile(pacUrl, proxyServer);
                    if (!string.IsNullOrEmpty(pacContent))
                    {
                        lastDownloadedPacUrl = pacUrl;
                        pacContent = UpdatePacFileProxy(pacContent, proxyServer);
                        pacServer.SetPacContent(pacContent);
                        // Save PAC to disk
                        File.WriteAllText(LocalPacFilePath, pacContent);
                    }
                }
            };
        }
        configForm.Show();
        configForm.BringToFront();
    }

    // DownloadPacFile remains unchanged
    private async Task<string> DownloadPacFile(string url, string proxy)
    {
        var handler = new HttpClientHandler();
        if (!string.IsNullOrWhiteSpace(proxy))
        {
            handler.Proxy = new WebProxy(proxy);
        }

        using var client = new HttpClient(handler);

        try
        {
            var pacContent = await client.GetStringAsync(url);
            return pacContent;
        }
        catch
        {
            MessageBox.Show("Failed to download PAC file. Please check the URL and your network connection or the proxy setting.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return string.Empty; // Return empty content on failure
        }
    }

    static private string UpdatePacFileProxy(string pacContent, string proxy)
    {
        var httpProxyRegex = HttpProxyUrlRegex();
        var proxyRegex = ProxyPatternRegex();
        try
        {
            var httpProxy = httpProxyRegex.Match(proxy).Groups[1].Value;
            return proxyRegex.Replace(pacContent, $"""""PROXY {httpProxy}""""");
        }
        catch
        {
            MessageBox.Show("Invalid proxy format. Please use 'http://host:port' or 'https://host:port'.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

            return pacContent;
        }
    }

    private void OnEnableProxyClicked(object? sender, EventArgs e)
    {
        if (enableProxyMenuItem.Checked)
        {
            pacServer.Start();
            SetSystemPacProxy(pacServer.ServerUrl);
        }
        else
        {
            pacServer.Stop();
            SetSystemPacProxy(string.Empty);
        }
    }

    private void SetSystemPacProxy(string pacUrl)
    {
        // Windows only: set system PAC URL
        var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", true);
        if (string.IsNullOrEmpty(pacUrl))
        {
            key?.DeleteValue("AutoConfigURL", false);
        }
        else
        {
            key?.SetValue("AutoConfigURL", pacUrl);
        }
    }

    private string GetSystemPacProxy()
    {
        // Windows only: get system PAC URL
        var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Internet Settings", false);
        return key?.GetValue("AutoConfigURL") as string ?? string.Empty;
    }

    protected override void ExitThreadCore()
    {
        trayIcon.Visible = false;
        pacServer.Stop();
        base.ExitThreadCore();
    }

    [GeneratedRegex("""""PROXY\s+[^\s;""]+""""")]
    private static partial Regex ProxyPatternRegex();

    [GeneratedRegex(@"https?://(.+)/?")]
    private static partial Regex HttpProxyUrlRegex();
}