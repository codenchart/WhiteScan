using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text.Json;
using WhiteScan.Models;
using System.IO;

namespace WhiteScan.Services
{
    public class NetworkScannerService : INetworkScannerService, IDisposable
    {
        private readonly ILogger<NetworkScannerService> _logger;
        private readonly HttpClient _httpClient;
        private readonly object _progressLock = new();
        private int _progressCounter = 0;
        private bool _isScanning = false;
        private CancellationTokenSource? _cancellationTokenSource;
        private int _totalScans = 0;
        private int _completedScans = 0;
        private int _successfulScans = 0;
        private double _progressPercentage = 0;

        public event EventHandler<ScanResult>? ScanResultReceived;
        public event EventHandler<ScanProgressEventArgs>? ScanProgressChanged;
        public event EventHandler<bool>? ScanStatusChanged;

        public bool IsScanning => _isScanning;
        public int TotalScans => _totalScans;
        public int CompletedScans => _completedScans;
        public int SuccessfulScans => _successfulScans;
        public double ProgressPercentage => _progressPercentage;

        public NetworkScannerService(ILogger<NetworkScannerService> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public async Task<bool> StartScanAsync(ScanConfiguration configuration, CancellationToken cancellationToken = default)
        {
            if (_isScanning)
            {
                _logger.LogWarning("Scan already in progress");
                return false;
            }

            _isScanning = true;
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _progressCounter = 0;
            _totalScans = 0;
            _completedScans = 0;
            _successfulScans = 0;
            _progressPercentage = 0;

            ScanStatusChanged?.Invoke(this, true);

            try
            {
                _logger.LogInformation("Starting network scan...");
                Debug.WriteLine("🔍 NetworkScannerService.StartScanAsync called - About to load IP list");
                
                var ips = await LoadIPListAsync(configuration.IplistPath);
                if (ips.Count == 0)
                {
                    _logger.LogWarning("No IPs found to scan");
                    return false;
                }

                _totalScans = ips.Count;
                _logger.LogInformation("Loaded {IPCount} IPs to scan", ips.Count);

                // Process IPs sequentially to prevent overwhelming
                foreach (var ip in ips)
                {
                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                        break;

                    try
                    {
                        await ScanSingleIPAsync(ip, configuration, _cancellationTokenSource.Token);
                        _successfulScans++;
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancelling
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error scanning IP: {IP}", ip);
                    }
                    finally
                    {
                        _completedScans++;
                        _progressPercentage = _totalScans > 0 ? (double)_completedScans / _totalScans * 100 : 0;
                        ReportProgress(ip, _completedScans, _totalScans, _successfulScans);
                        
                        // Small delay to prevent overwhelming
                        await Task.Delay(50, _cancellationTokenSource.Token);
                    }
                }

                _logger.LogInformation("Scan completed. Total: {Total}, Successful: {Successful}", _completedScans, _successfulScans);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Scan was cancelled");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during scan");
                return false;
            }
            finally
            {
                _isScanning = false;
                ScanStatusChanged?.Invoke(this, false);
            }
        }

        public async Task StopScanAsync()
        {
            _logger.LogInformation("Stopping scan...");
            _cancellationTokenSource?.Cancel();
            _isScanning = false;
            ScanStatusChanged?.Invoke(this, false);
        }

        public async Task<List<string>> LoadIPListAsync(string filePath)
        {
            Debug.WriteLine($"🔍 LoadIPListAsync called with filePath: {filePath}");
            try
            {
                // Try multiple possible paths for the IP list file
                var possiblePaths = new List<string>
                {
                    filePath, // Original path
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ipv4.txt"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "ipv4.txt"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "ipv4.txt"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "ipv4.txt"),
                    "ipv4.txt", // Current directory
                    Path.Combine(Directory.GetCurrentDirectory(), "ipv4.txt")
                };

                string? actualPath = null;
                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        actualPath = path;
                        _logger.LogInformation("Found IP list file at: {Path}", actualPath);
                        break;
                    }
                }

                if (actualPath == null)
                {
                    _logger.LogWarning("IP list file not found in any of the expected locations. Searched paths:");
                    foreach (var path in possiblePaths)
                    {
                        _logger.LogWarning("  - {Path}", path);
                    }
                    return new List<string>();
                }

                var lines = await System.IO.File.ReadAllLinesAsync(actualPath);
                var validIPs = lines.Where(line => !string.IsNullOrWhiteSpace(line))
                                   .Select(line => line.Trim())
                                   .Where(line => IsValidIP(line))
                                   .ToList();

                _logger.LogInformation("Loaded {Count} valid IPs from: {Path}", validIPs.Count, actualPath);
                
                // Remove the limit for production use, but keep it for testing
                // return validIPs.Take(100).ToList(); // Limit to first 100 IPs for testing
                return validIPs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading IP list from: {FilePath}", filePath);
                return new List<string>();
            }
        }

        private bool IsValidIP(string ip)
        {
            return !string.IsNullOrWhiteSpace(ip) && 
                   (ip.Contains('.') || ip.Contains(':'));
        }

        private async Task ScanSingleIPAsync(string ip, ScanConfiguration config, CancellationToken cancellationToken)
        {
            var scanStartTime = DateTime.Now;
            var result = new ScanResult
            {
                IpAddress = ip,
                Port = config.Ports?.FirstOrDefault() ?? 80,
                ScanTime = scanStartTime,
                Status = ScanStatus.Scanning
            };

            try
            {
                // Ping test
                if (config.Ping)
                {
                    var pingTime = await PingHostAsync(ip, config.MaxPing, cancellationToken);
                    result.PingTime = pingTime;
                    
                    if (pingTime.TotalMilliseconds > config.MaxPing)
                    {
                        result.Status = ScanStatus.Timeout;
                        result.ErrorMessage = "Ping timeout";
                        OnScanResultReceived(result);
                        return;
                    }
                }

                // HTTP/HTTPS scan
                var ports = config.Ports?.Any() == true ? config.Ports : new List<int> { 80, 443 };
                foreach (var port in ports)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    result.Port = port;

                    try
                    {
                        var url = $"http://{ip}:{port}{config.Path}";
                        
                        var stopwatch = Stopwatch.StartNew();
                        var response = await _httpClient.GetAsync(url, cancellationToken);
                        stopwatch.Stop();

                        result.Latency = stopwatch.Elapsed;
                        result.Status = ScanStatus.Success;

                        OnScanResultReceived(result);
                        break; // Success, no need to try other ports
                    }
                    catch (HttpRequestException ex)
                    {
                        result.Status = ScanStatus.Failed;
                        result.ErrorMessage = ex.Message;
                        OnScanResultReceived(result);
                    }
                    catch (TaskCanceledException)
                    {
                        result.Status = ScanStatus.Timeout;
                        result.ErrorMessage = "Request timeout";
                        OnScanResultReceived(result);
                    }
                    catch (Exception ex)
                    {
                        result.Status = ScanStatus.Failed;
                        result.ErrorMessage = ex.Message;
                        OnScanResultReceived(result);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning IP: {IP}", ip);
                result.Status = ScanStatus.Failed;
                result.ErrorMessage = ex.Message;
                OnScanResultReceived(result);
            }
        }

        private async Task<TimeSpan> PingHostAsync(string host, int maxPing, CancellationToken cancellationToken)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, maxPing);
                
                if (reply.Status == IPStatus.Success)
                {
                    return TimeSpan.FromMilliseconds(reply.RoundtripTime);
                }
                else
                {
                    return TimeSpan.FromMilliseconds(maxPing + 1);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Ping failed for {Host}: {Error}", host, ex.Message);
                return TimeSpan.FromMilliseconds(maxPing + 1);
            }
        }

        private void OnScanResultReceived(ScanResult result)
        {
            ScanResultReceived?.Invoke(this, result);
        }

        private void OnScanProgressChanged(ScanProgressEventArgs args)
        {
            ScanProgressChanged?.Invoke(this, args);
        }

        private void ReportProgress(string currentTarget, int completed, int total, int successful)
        {
            lock (_progressLock)
            {
                _progressCounter++;
                // Report progress every 5 scans
                if (_progressCounter % 5 == 0)
                {
                    OnScanProgressChanged(new ScanProgressEventArgs
                    {
                        CurrentTarget = currentTarget,
                        CompletedScans = completed,
                        TotalScans = total,
                        SuccessfulScans = successful,
                        ProgressPercentage = _progressPercentage
                    });
                }
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }
} 