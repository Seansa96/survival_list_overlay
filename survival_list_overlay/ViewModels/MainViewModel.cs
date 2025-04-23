using survival_list_overlay.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace survival_list_overlay.ViewModels

{
    public class MainViewModel
    { 
    public ObservableCollection<TrackedItem> Items { get; } = new();

    public MainViewModel()
    {
        Items.Add(new TrackedItem { Name = "Wooden Sword", Total = 5, Progress = 0 });
        Items.Add(new TrackedItem { Name = "Stone Axe", Total = 3, Progress = 2 });

        foreach (var item in Items)
        {
            item.CompletionCallback += OnItemCompleted;
        }
    }

    private void OnItemCompleted(TrackedItem item)
    {
        SystemSounds.Exclamation.Play(); // Or use your own wav with SoundPlayer
    }
}
}