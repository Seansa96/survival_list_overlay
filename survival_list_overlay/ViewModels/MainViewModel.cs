using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Input;
using survival_list_overlay.Commands;
using survival_list_overlay.Models;
using survival_list_overlay.Services;
using survival_list_overlay.Settings;

namespace survival_list_overlay.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IAudioCueService audioCueService;
    private readonly IUserNotificationService notificationService;
    private readonly OverlaySettings settings;
    private TrackedItemViewModel? lastDeletedItem;
    private int? lastDeletedItemIndex;
    private string newItemName = string.Empty;
    private string newItemTotal = string.Empty;

    public MainViewModel(
        IUserNotificationService notificationService,
        IAudioCueService audioCueService,
        OverlaySettings? settings = null)
    {
        this.notificationService = notificationService;
        this.audioCueService = audioCueService;
        this.settings = settings ?? new OverlaySettings();

        AddNewItemCommand = new RelayCommand(_ => AddNewItem(), _ => CanAddNewItem());
        RemoveItemCommand = new RelayCommand(RemoveItem, param => param is TrackedItemViewModel);
        UndoLastRemovalCommand = new RelayCommand(_ => UndoLastRemoval(), _ => lastDeletedItem is not null);
    }

    public ObservableCollection<TrackedItemViewModel> Items { get; } = new();

    public string Title => settings.Title;

    public string NewItemName
    {
        get => newItemName;
        set
        {
            if (newItemName == value)
            {
                return;
            }

            newItemName = value;
            OnPropertyChanged(nameof(NewItemName));
            RelayCommand.RefreshCanExecute();
        }
    }

    public string NewItemTotal
    {
        get => newItemTotal;
        set
        {
            if (newItemTotal == value)
            {
                return;
            }

            newItemTotal = value;
            OnPropertyChanged(nameof(NewItemTotal));
            RelayCommand.RefreshCanExecute();
        }
    }

    public ICommand AddNewItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand UndoLastRemovalCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void AddNewItem()
    {
        if (!TryGetNewItemTotal(out var total))
        {
            return;
        }

        var item = new TrackedItem
        {
            Name = NewItemName.Trim(),
            Total = total,
            Progress = 0
        };

        AddItem(new TrackedItemViewModel(item));
        NewItemName = string.Empty;
        NewItemTotal = string.Empty;
    }

    private bool CanAddNewItem()
    {
        return Items.Count < settings.MaxVisibleItems
            && !string.IsNullOrWhiteSpace(NewItemName)
            && TryGetNewItemTotal(out _);
    }

    private void AddItem(TrackedItemViewModel item)
    {
        if (Items.Count >= settings.MaxVisibleItems)
        {
            notificationService.ShowError(
                $"Cannot add more items. The list limit is {settings.MaxVisibleItems} items.",
                "List limit reached");
            return;
        }

        item.Item.Completed += OnItemCompleted;
        Items.Add(item);
        RelayCommand.RefreshCanExecute();
    }

    private void RemoveItem(object? parameter)
    {
        if (parameter is not TrackedItemViewModel item)
        {
            return;
        }

        var itemIndex = Items.IndexOf(item);
        if (itemIndex < 0)
        {
            return;
        }

        item.Item.Completed -= OnItemCompleted;
        Items.RemoveAt(itemIndex);

        lastDeletedItem?.Dispose();
        lastDeletedItem = item;
        lastDeletedItemIndex = itemIndex;
        RelayCommand.RefreshCanExecute();
    }

    private void UndoLastRemoval()
    {
        if (lastDeletedItem is null || lastDeletedItemIndex is null)
        {
            return;
        }

        var insertIndex = Math.Min(lastDeletedItemIndex.Value, Items.Count);
        lastDeletedItem.Item.Completed += OnItemCompleted;
        Items.Insert(insertIndex, lastDeletedItem);

        lastDeletedItem = null;
        lastDeletedItemIndex = null;
        RelayCommand.RefreshCanExecute();
    }

    private bool TryGetNewItemTotal(out int total)
    {
        return int.TryParse(NewItemTotal, out total) && total > 0;
    }

    private void OnItemCompleted(TrackedItem item)
    {
        audioCueService.PlayItemCompleted();
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
