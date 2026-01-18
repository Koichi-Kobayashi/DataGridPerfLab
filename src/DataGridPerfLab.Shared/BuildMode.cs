namespace DataGridPerfLab.Shared;

public enum BuildMode
{
    Sequential = 0,
    ParallelConcurrentBag = 1,
    ParallelArray = 2,

    // Shared List<T> + lock for comparison.
    // On .NET 9+ this uses System.Threading.Lock (EnterScope) when available.
    ParallelLockedList = 3
}
