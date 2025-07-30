using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WhiteScan.Infrastructure;
using WhiteScan.Models;
using WhiteScan.Services;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WhiteScan.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ILogger<MainViewModel> _logger;
    private readonly INetworkScannerService _networkScanner;
    private readonly IConfigurationService _configService;

    private ScanConfiguration _configuration;
    private ObservableCollection<ScanResult> _scanResults;
    private bool _isScanning;
    private double _scanProgress;
    private string _statusMessage;
    private int _totalScans;
    private int _successfulScans;
    private int _failedScans;
    private int _loadedIPCount;
    private CancellationTokenSource? _scanCancellationTokenSource;
    private List<string> _successfulIPs = new();

    public MainViewModel(
        ILogger<MainViewModel> logger,
        INetworkScannerService networkScanner,
        IConfigurationService configService)
    {
        _logger = logger;
        _networkScanner = networkScanner;
        _configService = configService;

        _configuration = new ScanConfiguration();
        _scanResults = new ObservableCollection<ScanResult>();
        _statusMessage = "Ready to scan CloudFlare IPs";

        StartScanCommand = new RelayCommand(async () => await StartScanAsync(), () => CanStartScan);
        StopScanCommand = new RelayCommand(async () => await StopScanAsync(), () => CanStopScan);
        SaveConfigurationCommand = new RelayCommand(async () => await SaveConfigurationAsync());
        LoadConfigurationCommand = new RelayCommand(async () => await LoadConfigurationAsync());
        ResetConfigurationCommand = new RelayCommand(ResetConfiguration);
        ViewWhiteListCommand = new RelayCommand(ViewWhiteList);

        InitializeAsync();
    }

    #region Properties

    public ScanConfiguration Configuration
    {
        get => _configuration;
        set => SetProperty(ref _configuration, value);
    }

    public ObservableCollection<ScanResult> ScanResults
    {
        get => _scanResults;
        set => SetProperty(ref _scanResults, value);
    }

    public bool IsScanning
    {
        get => _isScanning;
        set
        {
            if (SetProperty(ref _isScanning, value))
            {
                UpdateCanExecuteCommands();
            }
        }
    }

    public double ScanProgress
    {
        get => _scanProgress;
        set => SetProperty(ref _scanProgress, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public int TotalScans
    {
        get => _totalScans;
        set => SetProperty(ref _totalScans, value);
    }

    public int SuccessfulScans
    {
        get => _successfulScans;
        set => SetProperty(ref _successfulScans, value);
    }

    public int FailedScans
    {
        get => _failedScans;
        set => SetProperty(ref _failedScans, value);
    }

    public int LoadedIPCount
    {
        get => _loadedIPCount;
        set => SetProperty(ref _loadedIPCount, value);
    }

    public string TotalScansText => $"Total: {TotalScans}";
    public string SuccessfulScansText => $"Success: {SuccessfulScans}";
    public string FailedScansText => $"Failed: {FailedScans}";

    #endregion

    #region Commands

    public ICommand StartScanCommand { get; }
    public ICommand StopScanCommand { get; }
    public ICommand SaveConfigurationCommand { get; }
    public ICommand LoadConfigurationCommand { get; }
    public ICommand ResetConfigurationCommand { get; }
    public ICommand ViewWhiteListCommand { get; }

    #endregion

    #region Command Implementation

    public bool CanStartScan => !IsScanning;
    public bool CanStopScan => IsScanning;

    private async Task StartScanAsync()
    {
        try
        {
            IsScanning = true;
            StatusMessage = "🚀 Starting scan...";
            
            _scanCancellationTokenSource = new CancellationTokenSource();
            
            ScanResults.Clear();
            _successfulIPs.Clear();
            TotalScans = 0;
            SuccessfulScans = 0;
            FailedScans = 0;
            ScanProgress = 0;

            _logger.LogInformation("🔍 Starting C# NetworkScannerService scan...");

            _networkScanner.ScanResultReceived += OnScanResultReceived;
            _networkScanner.ScanProgressChanged += OnScanProgressChanged;
            _networkScanner.ScanStatusChanged += OnScanStatusChanged;

            var allIps = await _networkScanner.LoadIPListAsync(Configuration.IplistPath);
            var scanCount = Math.Min(allIps?.Count ?? 0, Configuration.Scans);
            LoadedIPCount = scanCount;

            if (LoadedIPCount == 0)
            {
                StatusMessage = "❌ No IP addresses loaded";
                IsScanning = false;
                return;
            }

            StatusMessage = $"📋 Loaded {allIps?.Count ?? 0} IPs, scanning {scanCount} IPs...";

            var success = await _networkScanner.StartScanAsync(Configuration, _scanCancellationTokenSource.Token);
            
            if (!success)
            {
                StatusMessage = "❌ Failed to start scan";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting scan");
            StatusMessage = $"❌ Scan failed: {ex.Message}";
        }
        finally
        {
            _networkScanner.ScanResultReceived -= OnScanResultReceived;
            _networkScanner.ScanProgressChanged -= OnScanProgressChanged;
            _networkScanner.ScanStatusChanged -= OnScanStatusChanged;
        }
    }

    private async Task StopScanAsync()
    {
        try
        {
            StatusMessage = "🛑 Stopping scan...";
            _scanCancellationTokenSource?.Cancel();
            
            await _networkScanner.StopScanAsync();
            
            IsScanning = false;
            StatusMessage = "✅ Scan stopped successfully";
            _logger.LogInformation("✅ Scan stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping scan");
            StatusMessage = $"❌ Error stopping scan: {ex.Message}";
        }
    }

    #endregion

    #region Configuration Management

    private async Task SaveConfigurationAsync()
    {
        try
        {
            await _configService.SaveConfigurationAsync(Configuration);
            StatusMessage = "💾 Configuration saved successfully";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving configuration");
            StatusMessage = $"❌ Error saving configuration: {ex.Message}";
        }
    }

    private async Task LoadConfigurationAsync()
    {
        try
        {
            var config = await _configService.LoadConfigurationAsync();
            if (config != null)
            {
                Configuration = config;
                StatusMessage = "📂 Configuration loaded successfully";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration");
            StatusMessage = $"❌ Error loading configuration: {ex.Message}";
        }
    }

    private void ResetConfiguration()
    {
        Configuration = new ScanConfiguration();
        StatusMessage = "↻ Configuration reset to defaults";
    }

    #endregion

    #region NetworkScanner Event Handlers

    private void OnScanResultReceived(object? sender, ScanResult result)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // Set the number based on the current count
            result.Number = ScanResults.Count + 1;
            
            ScanResults.Add(result);
            
            if (ScanResults.Count > 100)
            {
                ScanResults.RemoveAt(0);
                // Re-number all items after removing
                for (int i = 0; i < ScanResults.Count; i++)
                {
                    ScanResults[i].Number = i + 1;
                }
            }
            
            if (result.Status == ScanStatus.Success)
            {
                SuccessfulScans++;
                _successfulIPs.Add(result.IpAddress);
                
                if (SuccessfulScans % 10 == 0)
                {
                    SaveWhiteList();
                }
            }
            else
                FailedScans++;
                
            TotalScans++;
        });
    }

    private void OnScanProgressChanged(object? sender, ScanProgressEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ScanProgress = e.ProgressPercentage / 100.0;
            StatusMessage = $"🔍 Scanning {e.CurrentTarget} - {e.CompletedScans}/{e.TotalScans} ({e.ProgressPercentage:F1}%)";
        });
    }

    private void OnScanStatusChanged(object? sender, bool isScanning)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!isScanning)
            {
                IsScanning = false;
                ScanProgress = 1.0;
                StatusMessage = $"✅ Scan completed - {SuccessfulScans} successful, {FailedScans} failed";
                
                if (_successfulIPs.Count > 0)
                {
                    SaveWhiteList();
                }
            }
        });
    }

    #endregion

    #region Initialization

    private async void InitializeAsync()
    {
        try
        {
            _logger.LogInformation("📂 Loading configuration...");
            StatusMessage = "📂 Loading configuration...";
            
            var config = await _configService.LoadConfigurationAsync();
            if (config != null)
            {
                Configuration = config;
            }
            
            _logger.LogInformation("✅ Configuration loaded successfully");
            StatusMessage = "✅ Ready to scan with C# NetworkScannerService";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during initialization");
            StatusMessage = $"❌ Initialization failed: {ex.Message}";
        }
    }

    #endregion

    #region Helper Methods

    private void UpdateCanExecuteCommands()
    {
    }

    public void ClearOldResults()
    {
        if (ScanResults.Count > 50)
        {
            var itemsToRemove = ScanResults.Take(ScanResults.Count - 50).ToList();
            foreach (var item in itemsToRemove)
            {
                ScanResults.Remove(item);
            }
            
            // Re-number all remaining items
            for (int i = 0; i < ScanResults.Count; i++)
            {
                ScanResults[i].Number = i + 1;
            }
        }
    }

    private async void SaveWhiteList()
    {
        try
        {
            var whiteListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "white list.txt");
            var uniqueIPs = _successfulIPs.Distinct().ToList();
            
            await File.WriteAllLinesAsync(whiteListPath, uniqueIPs);
            
            _logger.LogInformation("✅ White list saved with {Count} unique IPs", uniqueIPs.Count);
            StatusMessage = $"✅ White list saved: {uniqueIPs.Count} unique IPs";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving white list");
            StatusMessage = $"❌ Error saving white list: {ex.Message}";
        }
    }

    private void ViewWhiteList()
    {
        try
        {
            var whiteListPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "white list.txt");
            
            if (File.Exists(whiteListPath))
            {
                var lines = File.ReadAllLines(whiteListPath);
                var message = $"White List contains {lines.Length} IPs:\n\n{string.Join("\n", lines.Take(20))}";
                
                if (lines.Length > 20)
                {
                    message += $"\n\n... and {lines.Length - 20} more IPs";
                }
                
                MessageBox.Show(message, "White List", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("No white list file found. Run a scan first to generate one.", "White List", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing white list");
            MessageBox.Show($"Error viewing white list: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
} 