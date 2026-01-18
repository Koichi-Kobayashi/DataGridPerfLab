using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

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
    /// Rebuilds the data.
    /// - batchReplaceItemsSource=true  : builds a new collection and replaces Items (recommended).
    /// - batchReplaceItemsSource=false : clears and adds into the existing collection (many CollectionChanged events).
    /// - parallelBuild=true            : generates items in parallel using System.Collections.Concurrent.
    /// </summary>
    public void Rebuild(int count, bool batchReplaceItemsSource, bool parallelBuild)
    {
        var sw = Stopwatch.StartNew();

        // Build data first (CPU work). Then apply to ObservableCollection (UI-friendly).
        List<Item> built = parallelBuild ? BuildItemsParallel(count) : BuildItemsSequential(count);

        if (batchReplaceItemsSource)
        {
            Items = new ObservableCollection<Item>(built);
        }
        else
        {
            Items.Clear();
            for (int i = 0; i < built.Count; i++)
                Items.Add(built[i]);
        }

        sw.Stop();
        LastLoadMs = sw.ElapsedMilliseconds;
        OnPropertyChanged(nameof(LastLoadMs));
    }

    private static List<Item> BuildItemsSequential(int count)
    {
        var rnd = new Random(0);
        var list = new List<Item>(capacity: count);
        for (int i = 0; i < count; i++)
        {
            list.Add(new Item
            {
                Id = i,
                Name = "Item " + i,
                Score = rnd.Next(0, 100)
            });
        }
        return list;
    }

    /// <summary>
    /// Parallel generation demo using System.Collections.Concurrent.
    /// NOTE: For maximum speed, an array indexed by i is usually faster than a concurrent collection.
    /// Here we intentionally use a concurrent collection because the lab is about comparing strategies.
    /// </summary>
    private static List<Item> BuildItemsParallel(int count)
    {
        var bag = new ConcurrentBag<Item>();

        // Each thread gets its own Random to avoid contention.
        var rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            var r = rng.Value!;
            bag.Add(new Item
            {
                Id = i,
                Name = "Item " + i,
                Score = r.Next(0, 100)
            });
        });

        rng.Dispose();

        // ConcurrentBag is unordered; sort by Id for stable behavior
        var list = bag.ToList();
        list.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        return list;
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
