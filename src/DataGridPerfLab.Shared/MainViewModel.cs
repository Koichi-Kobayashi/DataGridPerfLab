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
    /// - buildMode                     : how to generate items (sequential / parallel strategies).
    /// </summary>
    public void Rebuild(int count, bool batchReplaceItemsSource, BuildMode buildMode)
    {
        var sw = Stopwatch.StartNew();

        // Build data first (CPU work). Then apply to ObservableCollection (UI-friendly).
        List<Item> built = buildMode switch
        {
            BuildMode.ParallelConcurrentBag => BuildItemsParallelConcurrentBag(count),
            BuildMode.ParallelArray => BuildItemsParallelArray(count),
            BuildMode.ParallelLockedList => BuildItemsParallelLockedList(count),
            _ => BuildItemsSequential(count),
        };

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
    /// Parallel generation demo using System.Collections.Concurrent.ConcurrentBag.
    /// NOTE: This is NOT always the fastest approach (unordered + ToList + Sort).
    /// It's included because it's a canonical example of "concurrent collection" usage.
    /// </summary>
    private static List<Item> BuildItemsParallelConcurrentBag(int count)
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

        // ConcurrentBag is unordered; sort by Id for stable UI behavior
        var list = bag.ToList();
        list.Sort(static (a, b) => a.Id.CompareTo(b.Id));
        return list;
    }

    /// <summary>
    /// Parallel generation using an indexed array (often faster than concurrent collections).
    /// This avoids shared-collection contention and keeps the result ordered by Id.
    /// </summary>
    private static List<Item> BuildItemsParallelArray(int count)
    {
        var arr = new Item[count];

        var rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            var r = rng.Value!;
            arr[i] = new Item
            {
                Id = i,
                Name = "Item " + i,
                Score = r.Next(0, 100)
            };
        });

        rng.Dispose();

        // Already ordered by i
        return new List<Item>(arr);
    }

    /// <summary>
    /// Parallel generation into a shared List&lt;T&gt; guarded by a lock.
    /// On .NET 9+ we demonstrate System.Threading.Lock (EnterScope).
    /// On older TFMs we fall back to lock(object).
    /// </summary>
    private static List<Item> BuildItemsParallelLockedList(int count)
    {
        var list = new List<Item>(capacity: count);

        var rng = new ThreadLocal<Random>(() => new Random(Guid.NewGuid().GetHashCode()));

#if NET9_0_OR_GREATER
        var gate = new System.Threading.Lock();
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            var r = rng.Value!;
            var item = new Item
            {
                Id = i,
                Name = "Item " + i,
                Score = r.Next(0, 100)
            };

            using (gate.EnterScope())
            {
                list.Add(item);
            }
        });
#else
        object gate = new();
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            var r = rng.Value!;
            var item = new Item
            {
                Id = i,
                Name = "Item " + i,
                Score = r.Next(0, 100)
            };

            lock (gate)
            {
                list.Add(item);
            }
        });
#endif

        rng.Dispose();

        // Stable UI order
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
