using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace WhiteScan.Models;

public class ScanResult : INotifyPropertyChanged
{
    private int _number;
    private string _ipAddress = string.Empty;
    private int _port;
    private TimeSpan _pingTime;
    private TimeSpan _latency;
    private double _jitter;
    private string _downloadSpeed = string.Empty;
    private ScanStatus _status;
    private string _errorMessage = string.Empty;
    private DateTime _scanTime;

    public string IpAddress
    {
        get => _ipAddress;
        set => SetField(ref _ipAddress, value);
    }

    public int Port
    {
        get => _port;
        set => SetField(ref _port, value);
    }

    public TimeSpan PingTime
    {
        get => _pingTime;
        set => SetField(ref _pingTime, value);
    }

    public TimeSpan Latency
    {
        get => _latency;
        set => SetField(ref _latency, value);
    }

    public double Jitter
    {
        get => _jitter;
        set => SetField(ref _jitter, value);
    }

    public string DownloadSpeed
    {
        get => _downloadSpeed;
        set => SetField(ref _downloadSpeed, value);
    }

    public ScanStatus Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }

    public DateTime ScanTime
    {
        get => _scanTime;
        set => SetField(ref _scanTime, value);
    }

    public string EndpointAddress => $"{IpAddress}:{Port}";

    public string StatusText => Status switch
    {
        ScanStatus.Success => "Success",
        ScanStatus.Failed => "Failed",
        ScanStatus.Timeout => "Timeout",
        ScanStatus.Scanning => "Scanning...",
        ScanStatus.Queued => "Queued",
        _ => "Unknown"
    };

    public int Number
    {
        get => _number;
        set => SetField(ref _number, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public enum ScanStatus
{
    Queued,
    Scanning,
    Success,
    Failed,
    Timeout
} 