using System.Text.Json;
using System.Text.Json.Serialization;

namespace PacServer;

public class ConfigFile
{
    [JsonIgnore]
    private static readonly string configFilePath = "config.json";

    [JsonIgnore]
    private static readonly ConfigFile defaultConfigFile = new()
    {
        PacUrl = string.Empty,
        ProxyServer = @"http://127.0.0.1:8080",
    };

    [JsonPropertyName("pacUrl")]
    public required string PacUrl { get; set; }

    [JsonPropertyName("proxyServer")]
    public required string ProxyServer { get; set; }

    [JsonPropertyName("pacServerListeningPort")]
    public int PacServerListeningPort { get; set; } = 12345;

    private static readonly JsonSerializerOptions CachedJsonSerializerOptions = new() { WriteIndented = true };

    public static readonly Lazy<ConfigFile> Instance = new Lazy<ConfigFile>(() =>
    {
        if (!File.Exists(configFilePath))
        {
            return defaultConfigFile;
        }

        var json = File.ReadAllText(configFilePath);

        try
        {
            return JsonSerializer.Deserialize<ConfigFile>(json) ?? defaultConfigFile;
        }
        catch
        {
            MessageBox.Show("Failed to parse config file. Using default settings.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return defaultConfigFile;
        }
    });

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(this, CachedJsonSerializerOptions);
            File.WriteAllText(configFilePath, json);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to save config file: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
