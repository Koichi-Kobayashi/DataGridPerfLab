using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DataGridPerfLab.Shared;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<Item> _items = new();
    public ObservableCollection<Item> Items
    {
        get => _items;
        private set
        {
            if (ReferenceEquals(_items, value)) return;
            _items = value;
            OnPropertyChanged();
        }
    }

    public long LastLoadMs { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    /// <summary>
    /// Rebuilds the data. When <paramref name="batchReplaceItemsSource"/> is true, it creates a new collection
    /// and replaces Items (ItemsSource replacement). This is the safest way to batch-update WPF DataGrid.
    /// When false, it clears and adds into the existing ObservableCollection (many CollectionChanged events).
    /// </summary>
    public void Rebuild(int count, bool batchReplaceItemsSource)
    {
        var sw = Stopwatch.StartNew();
        var rnd = new Random(0);

        if (batchReplaceItemsSource)
        {
            var newItems = new ObservableCollection<Item>();
            for (int i = 0; i < count; i++)
            {
                newItems.Add(new Item
                {
                    Id = i,
                    Name = "Item " + i,
                    Score = rnd.Next(0, 100)
                });
            }
            Items = newItems;
        }
        else
        {
            Items.Clear();
            for (int i = 0; i < count; i++)
            {
                Items.Add(new Item
                {
                    Id = i,
                    Name = "Item " + i,
                    Score = rnd.Next(0, 100)
                });
            }
        }

        sw.Stop();
        LastLoadMs = sw.ElapsedMilliseconds;
        OnPropertyChanged(nameof(LastLoadMs));
    }

    /// <summary>
    /// Mutates <paramref name="count"/> items' Score values to demonstrate live shaping.
    /// </summary>
    public void MutateScores(int count, int seed = 1)
    {
        var rnd = new Random(seed);
        var n = Math.Min(count, Items.Count);
        for (int i = 0; i < n; i++)
        {
            var it = Items[i];
            it.Score = (it.Score + rnd.Next(1, 100)) % 100;
        }
    }
}
