using System.ComponentModel;
using System.Windows.Input;
using survival_list_overlay.Commands;
using survival_list_overlay.Models;

namespace survival_list_overlay.ViewModels;

public sealed class TrackedItemViewModel : INotifyPropertyChanged
{
    public TrackedItemViewModel(TrackedItem item)
    {
        Item = item;
        Item.PropertyChanged += OnItemPropertyChanged;
        IncrementCommand = new RelayCommand(_ => Progress++, _ => Progress < Total);
        DecrementCommand = new RelayCommand(_ => Progress--, _ => Progress > 0);
    }

    public TrackedItem Item { get; }

    public string Name
    {
        get => Item.Name;
        set => Item.Name = value;
    }

    public int Total
    {
        get => Item.Total;
        set => Item.Total = value;
    }

    public int Progress
    {
        get => Item.Progress;
        set => Item.Progress = value;
    }

    public string? Category
    {
        get => Item.Category;
        set => Item.Category = value;
    }

    public TrackingProgressStatus ProgressStatus => Item.ProgressStatus;

    public ICommand IncrementCommand { get; }
    public ICommand DecrementCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Dispose()
    {
        Item.PropertyChanged -= OnItemPropertyChanged;
    }

    private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is null)
        {
            OnPropertyChanged(string.Empty);
            RelayCommand.RefreshCanExecute();
            return;
        }

        OnPropertyChanged(e.PropertyName);
        RelayCommand.RefreshCanExecute();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
