using System.Text.Json.Serialization;

namespace WhiteScan.Models;

/// <summary>
/// ساده‌شده برای اسکن کلودفلر - فقط فیچرهای ضروری
/// </summary>
public class ScanConfiguration
{
    [JsonPropertyName("Hostname")]
    public string Hostname { get; set; } = "cp.cloudflare.com";

    [JsonPropertyName("Ports")]
    public List<int> Ports { get; set; } = new() { 80, 443 };

    [JsonPropertyName("Path")]
    public string Path { get; set; } = "/";

    [JsonPropertyName("Ping")]
    public bool Ping { get; set; } = true;

    [JsonPropertyName("MaxPing")]
    public int MaxPing { get; set; } = 300;

    [JsonPropertyName("Goroutines")]
    public int Goroutines { get; set; } = 4;

    [JsonPropertyName("Scans")]
    public int Scans { get; set; } = 10000;

    [JsonPropertyName("Maxlatency")]
    public long Maxlatency { get; set; } = 1000;

    [JsonPropertyName("IplistPath")]
    public string IplistPath { get; set; } = "ipv4.txt";

    [JsonPropertyName("CSV")]
    public bool CSV { get; set; } = true;

    // فیلدهای اضافی که ممکنه در simple_scanner لازم باشه
    [JsonPropertyName("maxPing")]
    public int MaxPingLowercase => MaxPing;

    [JsonPropertyName("maxlatency")]
    public int MaxlatencyInt => (int)Maxlatency;

    [JsonPropertyName("goroutines")]
    public int GoroutinesLowercase => Goroutines;

    [JsonPropertyName("scans")]
    public int ScansLowercase => Scans;

    [JsonPropertyName("hostname")]
    public string HostnameLowercase => Hostname;

    [JsonPropertyName("ports")]
    public List<int> PortsLowercase => Ports;

    [JsonPropertyName("path")]
    public string PathLowercase => Path;

    [JsonPropertyName("ping")]
    public bool PingLowercase => Ping;

    [JsonPropertyName("iplistPath")]
    public string IplistPathCamel => IplistPath;

    [JsonPropertyName("csv")]
    public bool CsvLowercase => CSV;
} 