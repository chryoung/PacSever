using System.Text.Json.Serialization;

namespace PacServer;

public class ConfigFile
{
    [JsonPropertyName("pacUrl")]
    public required string PacUrl { get; set; }

    [JsonPropertyName("proxyServer")]
    public required string ProxyServer { get; set; }
}
