using WhiteScan.Models;

namespace WhiteScan.Services;

public interface INetworkScannerService
{
    event EventHandler<ScanResult>? ScanResultReceived;
    event EventHandler<ScanProgressEventArgs>? ScanProgressChanged;
    event EventHandler<bool>? ScanStatusChanged;

    Task<bool> StartScanAsync(ScanConfiguration configuration, CancellationToken cancellationToken = default);
    Task StopScanAsync();
    Task<List<string>> LoadIPListAsync(string filePath);
    bool IsScanning { get; }
    int TotalScans { get; }
    int CompletedScans { get; }
    int SuccessfulScans { get; }
    double ProgressPercentage { get; }
}

public class ScanProgressEventArgs : EventArgs
{
    public int TotalScans { get; set; }
    public int CompletedScans { get; set; }
    public int SuccessfulScans { get; set; }
    public double ProgressPercentage { get; set; }
    public string? CurrentTarget { get; set; }
} 