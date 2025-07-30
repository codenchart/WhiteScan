using System.Text.Json;
using System.IO;
using WhiteScan.Models;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace WhiteScan.Services;

public interface IConfigurationService
{
    Task<ScanConfiguration?> LoadConfigurationAsync();
    Task SaveConfigurationAsync(ScanConfiguration configuration);
    ScanConfiguration GetDefaultConfiguration();
    List<string> ValidateConfiguration(ScanConfiguration configuration);
    Task<ScanConfiguration> FixConfigurationAsync(ScanConfiguration configuration);
}

public class ConfigurationService : IConfigurationService
{
    private readonly ILogger<ConfigurationService> _logger;
    private readonly string _configPath;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
        _configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
    }

    public async Task<ScanConfiguration?> LoadConfigurationAsync()
    {
        try
        {
            if (!File.Exists(_configPath))
            {
                _logger.LogInformation("Configuration file not found, creating default");
                var defaultConfig = GetDefaultConfiguration();
                await SaveConfigurationAsync(defaultConfig);
                return defaultConfig;
            }

            var json = await File.ReadAllTextAsync(_configPath);
            var config = JsonSerializer.Deserialize<ScanConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (config == null)
            {
                _logger.LogWarning("Failed to deserialize configuration, using default");
                return GetDefaultConfiguration();
            }

            config = await FixConfigurationAsync(config);
            
            _logger.LogInformation("Configuration loaded successfully");
            return config;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            return GetDefaultConfiguration();
        }
    }

    public async Task SaveConfigurationAsync(ScanConfiguration configuration)
    {
        try
        {
            var json = JsonSerializer.Serialize(configuration, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            await File.WriteAllTextAsync(_configPath, json);
            _logger.LogInformation("Configuration saved successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            throw;
        }
    }

    public ScanConfiguration GetDefaultConfiguration()
    {
        return new ScanConfiguration
        {
            Hostname = "cp.cloudflare.com",
            Ports = new List<int> { 80, 443 },
            Path = "/",
            Ping = true,
            MaxPing = 300,
            Goroutines = Math.Max(1, Environment.ProcessorCount),
            Scans = 10000,
            Maxlatency = 1000,
            IplistPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ipv4.txt"),
            CSV = true
        };
    }

    public List<string> ValidateConfiguration(ScanConfiguration configuration)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.Hostname))
        {
            errors.Add("Hostname cannot be empty");
        }

        if (configuration.Ports == null || !configuration.Ports.Any())
        {
            errors.Add("At least one port must be specified");
        }
        else if (configuration.Ports.Any(p => p <= 0 || p > 65535))
        {
            errors.Add("Ports must be between 1 and 65535");
        }

        if (configuration.Goroutines <= 0)
        {
            errors.Add("Goroutines must be greater than 0");
        }

        if (configuration.Scans <= 0)
        {
            errors.Add("Scans must be greater than 0");
        }

        if (configuration.MaxPing <= 0)
        {
            errors.Add("MaxPing must be greater than 0");
        }

        if (configuration.Maxlatency <= 0)
        {
            errors.Add("Maxlatency must be greater than 0");
        }

        if (!string.IsNullOrWhiteSpace(configuration.IplistPath) && !File.Exists(configuration.IplistPath))
        {
            errors.Add($"IP list file not found: {configuration.IplistPath}");
        }

        return errors;
    }

    public async Task<ScanConfiguration> FixConfigurationAsync(ScanConfiguration configuration)
    {
        var errors = ValidateConfiguration(configuration);
        if (!errors.Any())
        {
            return configuration;
        }

        _logger.LogInformation("Fixing configuration errors: {Errors}", string.Join(", ", errors));

        if (string.IsNullOrWhiteSpace(configuration.Hostname))
        {
            configuration.Hostname = "cp.cloudflare.com";
        }

        if (configuration.Ports == null || !configuration.Ports.Any())
        {
            configuration.Ports = new List<int> { 80, 443 };
        }

        if (configuration.Goroutines <= 0)
        {
            configuration.Goroutines = Math.Max(1, Environment.ProcessorCount);
        }

        if (configuration.Scans <= 0)
        {
            configuration.Scans = 10000;
        }

        if (configuration.MaxPing <= 0)
        {
            configuration.MaxPing = 300;
        }

        if (configuration.Maxlatency <= 0)
        {
            configuration.Maxlatency = 1000;
        }

        if (string.IsNullOrWhiteSpace(configuration.IplistPath))
        {
            configuration.IplistPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ipv4.txt");
        }

        await SaveConfigurationAsync(configuration);
        
        _logger.LogInformation("Configuration fixed and saved");
        return configuration;
    }
} 