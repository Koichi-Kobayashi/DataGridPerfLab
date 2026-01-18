using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DataGridPerfLab.Shared;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();
    private readonly string _tfm;

    private long _lastApplyViewMs;
    private long _lastMutateMs;

    private long _lastRebuildAllocBytes;
    private long _lastApplyViewAllocBytes;
    private long _lastMutateAllocBytes;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;

        _tfm = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<TargetFrameworkAttribute>()?
            .FrameworkName ?? "Unknown TFM";

        Title = $"DataGridPerfLab - {_tfm}";

        // Ensure DataGrid virtualization matches initial checkbox state
        ApplyVirtualizationSettings();

        UpdateStatus();
    }

    private void OnVirtualizationChanged(object sender, RoutedEventArgs e)
    {
        ApplyVirtualizationSettings();
        UpdateStatus();
    }

    
private void ApplyVirtualizationSettings()
{
    var on = VirtualizationToggle?.IsChecked == true;
    if (Grid == null) return;

    // DataGrid-level toggles (safe to change at runtime)
    Grid.EnableRowVirtualization = on;
    Grid.EnableColumnVirtualization = on;

    // Panel-level virtualization toggle (safe). IMPORTANT:
    // Do NOT change VirtualizationMode (Recycling/Standard) after the panel has been measured,
    // otherwise WPF throws InvalidOperationException.
    VirtualizingPanel.SetIsVirtualizing(Grid, on);

    // Keep VirtualizationMode fixed (set in XAML). We only toggle virtualization on/off.
    // Keep logical scrolling for consistency.
    Grid.SetValue(ScrollViewer.CanContentScrollProperty, true);

    // Optional: force re-measure after toggle (helps UI reflect change quickly)
    Grid.InvalidateMeasure();
    Grid.InvalidateArrange();
}

    private void OnRebuild(object sender, RoutedEventArgs e)
    {
        var batch = BatchToggle.IsChecked == true;

        var buildMode = (BuildMode)(BuildModeCombo?.SelectedIndex ?? 0);

        var allocBefore = GetAllocatedBytes();

        _vm.Rebuild(100_000, batch, buildMode);

        var allocAfter = GetAllocatedBytes();
        _lastRebuildAllocBytes = allocAfter - allocBefore;

        // After rebuilding (especially when ItemsSource is replaced), the view instance changes.
        // Re-apply the current view settings so comparisons are consistent.
        ApplyViewSettings();

        UpdateStatus();
    }

    private void OnApplyView(object sender, RoutedEventArgs e)
    {
        ApplyViewSettings();
        UpdateStatus();
    }

    private void OnMutateScores(object sender, RoutedEventArgs e)
    {
        var allocBefore = GetAllocatedBytes();
        var sw = Stopwatch.StartNew();

        _vm.MutateScores(5_000, seed: Environment.TickCount);

        sw.Stop();
        var allocAfter = GetAllocatedBytes();

        _lastMutateMs = sw.ElapsedMilliseconds;
        _lastMutateAllocBytes = allocAfter - allocBefore;

        UpdateStatus();
    }

    private void ApplyViewSettings()
    {
        var view = CollectionViewSource.GetDefaultView(_vm.Items);
        if (view == null) return;

        var useDefer = UseDeferRefreshToggle.IsChecked == true;
        var live = LiveShapingToggle.IsChecked == true;

        var filterEven = FilterEvenToggle.IsChecked == true;
        var filterScore = FilterScoreToggle.IsChecked == true;

        // Sort toggles: if both checked, Score sort wins (clearer for live shaping demo).
        var sortScoreDesc = SortScoreDescToggle.IsChecked == true;
        var sortIdDesc = SortIdDescToggle.IsChecked == true && !sortScoreDesc;

        var groupByCategory = GroupByCategoryToggle.IsChecked == true;

        IDisposable? defer = null;

        var allocBefore = GetAllocatedBytes();
        var sw = Stopwatch.StartNew();

        try
        {
            if (useDefer)
                defer = view.DeferRefresh();

            // Filter (composable)
            if (filterEven || filterScore)
            {
                view.Filter = o =>
                {
                    if (o is not Item it) return false;
                    if (filterEven && (it.Id % 2 != 0)) return false;
                    if (filterScore && (it.Score < 50)) return false;
                    return true;
                };
            }
            else
            {
                view.Filter = null;
            }

            // Sort
            view.SortDescriptions.Clear();
            if (sortScoreDesc)
            {
                view.SortDescriptions.Add(new SortDescription(nameof(Item.Score), ListSortDirection.Descending));
            }
            else if (sortIdDesc)
            {
                view.SortDescriptions.Add(new SortDescription(nameof(Item.Id), ListSortDirection.Descending));
            }

            // Group
            view.GroupDescriptions.Clear();
            if (groupByCategory)
            {
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Item.Category)));
            }

            // Live Shaping (only works on ListCollectionView / BindingListCollectionView)
            if (view is ICollectionViewLiveShaping liveView)
            {
                liveView.IsLiveFiltering = live && (filterScore); // even-id filter won't change dynamically
                liveView.IsLiveSorting = live && (sortScoreDesc);
                liveView.IsLiveGrouping = live && (groupByCategory);

                // Properties that should trigger live updates
                liveView.LiveFilteringProperties.Clear();
                liveView.LiveSortingProperties.Clear();
                liveView.LiveGroupingProperties.Clear();

                if (liveView.IsLiveFiltering == true)
                    liveView.LiveFilteringProperties.Add(nameof(Item.Score));

                if (liveView.IsLiveSorting == true)
                    liveView.LiveSortingProperties.Add(nameof(Item.Score));

                if (liveView.IsLiveGrouping == true)
                    liveView.LiveGroupingProperties.Add(nameof(Item.Category));
            }
        }
        finally
        {
            defer?.Dispose();
            sw.Stop();
            var allocAfter = GetAllocatedBytes();

            _lastApplyViewMs = sw.ElapsedMilliseconds;
            _lastApplyViewAllocBytes = allocAfter - allocBefore;
        }
    }

    private void UpdateStatus()
    {
        var batch = BatchToggle?.IsChecked == true;
        var filterEven = FilterEvenToggle?.IsChecked == true;
        var filterScore = FilterScoreToggle?.IsChecked == true;
        var sortIdDesc = SortIdDescToggle?.IsChecked == true;
        var sortScoreDesc = SortScoreDescToggle?.IsChecked == true;
        var group = GroupByCategoryToggle?.IsChecked == true;
        var defer = UseDeferRefreshToggle?.IsChecked == true;
        var live = LiveShapingToggle?.IsChecked == true;
        var virt = VirtualizationToggle?.IsChecked == true;
        var buildMode = (BuildMode)(BuildModeCombo?.SelectedIndex ?? 0);

        var mode =
            $"Virt={(virt ? "ON" : "OFF")}, " +
            $"BuildMode={buildMode}, " +
                        $"Batch={(batch ? "ON" : "OFF")}, " +
            $"FilterEven={(filterEven ? "ON" : "OFF")}, " +
            $"FilterScore={(filterScore ? "ON" : "OFF")}, " +
            $"SortIdDesc={(sortIdDesc ? "ON" : "OFF")}, " +
            $"SortScoreDesc={(sortScoreDesc ? "ON" : "OFF")}, " +
            $"Group={(group ? "ON" : "OFF")}, " +
            $"DeferRefresh={(defer ? "ON" : "OFF")}, " +
            $"LiveShaping={(live ? "ON" : "OFF")}";

        var text =
            $"Rebuild(100k): {_vm.LastLoadMs} ms (alloc {_lastRebuildAllocBytes:N0} B) | " +
            $"ApplyView: {_lastApplyViewMs} ms (alloc {_lastApplyViewAllocBytes:N0} B) | " +
            $"Mutate: {_lastMutateMs} ms (alloc {_lastMutateAllocBytes:N0} B) | " +
            mode;

        if (StatusText != null) StatusText.Text = text;

        Title = $"DataGridPerfLab - {_tfm} | {_vm.LastLoadMs} ms | View {_lastApplyViewMs} ms | Mut {_lastMutateMs} ms";
    }

    private static long GetAllocatedBytes()
    {
        return GC.GetTotalAllocatedBytes(precise: false);
    }
}
