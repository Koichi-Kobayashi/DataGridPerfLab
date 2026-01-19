[English](README.md) | [日本語](README.ja.md)

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

## Test Environment

- OS: Windows 11 Pro 26H1
- CPU: AMD Ryzen 7 5700U with Radeon Graphics (1.80 GHz)
- RAM: 32 GB
- UI Framework: WPF / DataGrid
- Target Runtimes: .NET 6 / 7 / 8 / 9 / 10
- Dataset Size: 100,000 rows
- Build Configuration: Release

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

---

## .NET 6–10 version-specific comparison points

This lab includes a few intentionally **version-specific** switches you can compare across TFMs:

- **.NET 9+ (WPF): ThemeMode / Fluent Theme**
  - `Window.ThemeMode` / `Application.ThemeMode` lets you enable the Fluent theme without manually merging resource dictionaries.
- **.NET 9+ (Runtime): System.Threading.Lock**
  - .NET 9 introduces `System.Threading.Lock` as a modern synchronization primitive. The lab’s “Parallel (List+Lock)” build mode uses `Lock.EnterScope()` on .NET 9+ and falls back to `lock(object)` on older TFMs.
- **.NET 7 (WPF): ongoing perf work**
  - WPF in .NET 7 shipped with a broad set of performance improvements (allocations/boxing reductions, etc.).
- **.NET 10 (WPF): internal perf improvements**
  - WPF in .NET 10 mentions internal optimizations (UI automation, dialogs, pixel conversions, caches, etc.).

Note: DataGrid responsiveness is still dominated by **virtualization, ItemsSource replacement, and DeferRefresh**. Version upgrades tend to deliver incremental gains on top of correct usage patterns.

---

### Preview API: ThemeMode (WPF0001)

On `.NET 9+`, **`Window.ThemeMode`** triggers analyzer warning **WPF0001**.
This indicates it is an *evaluation/preview* API that may change or be removed in a future update.

This lab uses it intentionally as a **.NET 9+ specific comparison point**,
so the call site explicitly suppresses the warning via **`#pragma warning disable WPF0001`**.
