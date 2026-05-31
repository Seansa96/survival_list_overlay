using System.ComponentModel;

namespace survival_list_overlay.Models;

public enum TrackingProgressStatus
{
    Unfinished,
    Completed
}

public sealed class TrackedItem : INotifyPropertyChanged
{
    private string? category;
    private string name = string.Empty;
    private int progress;
    private TrackingProgressStatus progressStatus = TrackingProgressStatus.Unfinished;
    private int total;

    public string Name
    {
        get => name;
        set
        {
            if (name == value)
            {
                return;
            }

            name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    public int Total
    {
        get => total;
        set
        {
            var normalizedValue = Math.Max(0, value);
            if (total == normalizedValue)
            {
                return;
            }

            total = normalizedValue;
            OnPropertyChanged(nameof(Total));

            if (progress > total)
            {
                Progress = total;
            }
            else
            {
                UpdateProgressStatus();
            }
        }
    }

    public int Progress
    {
        get => progress;
        set
        {
            var normalizedValue = Math.Clamp(value, 0, Total);
            if (progress == normalizedValue)
            {
                return;
            }

            progress = normalizedValue;
            OnPropertyChanged(nameof(Progress));
            UpdateProgressStatus();
        }
    }

    public string? Category
    {
        get => category;
        set
        {
            if (category == value)
            {
                return;
            }

            category = value;
            OnPropertyChanged(nameof(Category));
        }
    }

    public TrackingProgressStatus ProgressStatus
    {
        get => progressStatus;
        private set
        {
            if (progressStatus == value)
            {
                return;
            }

            progressStatus = value;
            OnPropertyChanged(nameof(ProgressStatus));

            if (progressStatus == TrackingProgressStatus.Completed)
            {
                Completed?.Invoke(this);
            }
        }
    }

    public event Action<TrackedItem>? Completed;
    public event PropertyChangedEventHandler? PropertyChanged;

    private void UpdateProgressStatus()
    {
        ProgressStatus = Total > 0 && Progress >= Total
            ? TrackingProgressStatus.Completed
            : TrackingProgressStatus.Unfinished;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
