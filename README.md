# DataGridPerfLab

A WPF performance comparison lab for **DataGrid** across **.NET 6 – .NET 10**.

This project demonstrates how different features affect performance and allocations:

- ItemsSource replacement vs Clear/Add
- Filter / Sort / Group
- DeferRefresh (batching view changes)
- LiveShaping (live filter/sort/group)
- Row/Column virtualization
- Allocation measurement

---

## 📊 Results Summary

### 1. Feature Impact Overview

| Feature | Configuration | Performance | Allocations | Notes |
|---|---|---|---|---|
| ItemsSource update | Clear + Add | Slow | High | Many CollectionChanged events |
|  | Replace ItemsSource | Fast | Low | **Most important optimization** |
| Virtualization | OFF | Very Slow | Very High | UI freezes with 100k rows |
|  | ON (Recycling) | Fast | Low | Essential for DataGrid |
| Filter / Sort / Group | DeferRefresh OFF | Sluggish | High | Re-evaluated per change |
|  | DeferRefresh ON | Fast | Low | Correct way to update views |
| LiveShaping | OFF | Stable | Low | Manual refresh |
|  | ON | Variable | Higher | Good for demos |
| Group + LiveShaping | Both ON | Heavy | Very High | Demonstration purpose |
| .NET runtime | 6 → 10 | Improved | Slightly Lower | Incremental internal optimizations |

---

### 2. Runtime Comparison Template

| .NET | Rebuild 100k (ms) | ApplyView (ms) | Mutate 5k (ms) | Notes |
|---|---:|---:|---:|---|
| net6 | | | | |
| net7 | | | | |
| net8 | | | | |
| net9 | | | | |
| net10 | | | | |

---

### 3. Recommended Configurations

| Configuration | Practical Use | Demo Use | Comment |
|---|---|---|---|
| Virtualization ON + Batch ON | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | Baseline best practice |
| Virtualization OFF | ⭐ | ⭐⭐⭐⭐⭐ | Worst-case comparison |
| DeferRefresh ON | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | Correct view update |
| LiveShaping ON | ⭐⭐ | ⭐⭐⭐⭐⭐ | Visual clarity |
| Group + LiveShaping | ⭐ | ⭐⭐⭐⭐⭐ | Shows performance limits |

---

## Conclusion

This lab confirms that **DataGrid performance is dominated by correct usage patterns**, not just runtime version.
Virtualization, ItemsSource replacement, and DeferRefresh provide far greater gains than upgrading .NET alone.

---

## Build Mode

For comparing data generation strategies (CPU work), you can switch between:

- **Sequential**: single-thread generation
- **Parallel (ConcurrentBag)**: uses `System.Collections.Concurrent.ConcurrentBag` (unordered, then sorted by Id)
- **Parallel (Array)**: writes into an indexed `Item[]` (often faster than concurrent collections)

Note: DataGrid rendering is dominated by UI-thread work; this primarily compares generation cost.
