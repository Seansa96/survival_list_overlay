using survival_list_overlay.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace survival_list_overlay.ViewModels

{
    public class MainViewModel : INotifyPropertyChanged
    { 
    public ObservableCollection<TrackedItem> Items { get; } = new();

        private const int MaxVisibleItems = 12;
        private TrackedItem? lastDeletedItem;
        private int? lastDeletedItemIndex;

        private string newItemName = string.Empty;
        private int newItemTotal;

        public string NewItemName
        {
            get => newItemName;
            set
            {
                if (newItemName != value)
                {
                    newItemName = value;
                    OnPropertyChanged(nameof(NewItemName));
                }
            }
        }

        public int NewItemTotal
        {
            get => newItemTotal;
            set
            {
                if (newItemTotal != value)
                {
                    newItemTotal = value;
                    OnPropertyChanged(nameof(NewItemTotal));
                }
            }
        }

        public ICommand AddNewItemCommand { get; set; }
        public ICommand RemoveItemCommand { get; set; }
        public MainViewModel()
        {
            
            foreach (var item in Items)
            {
                item.CompletionCallback += OnItemCompleted;
               
            }
                AddNewItemCommand = new RelayCommand(_ => AddNewItem());
                RemoveItemCommand = new RelayCommand(param => RemoveItem((TrackedItem)param!));

        }

        public void RemoveItem(TrackedItem item)
        {
            if (Items.Contains(item))
            {
                lastDeletedItemIndex = Items.IndexOf(item);
                lastDeletedItem = item;

                item.CompletionCallback -= OnItemCompleted;
                Items.Remove(item);
            }
        }

        public void UndoLastRemoval()
        {
            if (lastDeletedItem != null && lastDeletedItemIndex.HasValue)
            {
                Items.Insert(lastDeletedItemIndex.Value, lastDeletedItem);
                lastDeletedItem.CompletionCallback += OnItemCompleted;

                lastDeletedItem = null;
                lastDeletedItemIndex = null;
            }
        }

        private void AddNewItem()
        {
            var newItem = new TrackedItem
            { 
                Name = NewItemName,
                Total = newItemTotal,
                Progress = 0
                };
            AddItem(newItem);

            NewItemName = string.Empty;
            NewItemTotal = 0;
        }

        private bool CanAddNewItem()
        {
            return !string.IsNullOrWhiteSpace(NewItemName) && NewItemTotal > 0;
        }

        private void AddItem(TrackedItem item)
        {
            if (Items.Count < MaxVisibleItems)
            {
                item.CompletionCallback += OnItemCompleted;
                Items.Add(item);
            }
            else
            {
                MessageBox.Show("Cannot add more items, list limit reached (12 Items)!", "Error1", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnItemCompleted(TrackedItem item)
    {
        SystemSounds.Exclamation.Play(); // Or use your own wav with SoundPlayer
    }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
    
}