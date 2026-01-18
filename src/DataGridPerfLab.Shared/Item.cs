using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DataGridPerfLab.Shared;

public sealed class Item : INotifyPropertyChanged
{
    private int _id;
    private string _name = "";
    private int _score;

    public int Id
    {
        get => _id;
        set
        {
            if (_id == value) return;
            _id = value;
            OnPropertyChanged();
        }
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// A mutable value used to demonstrate live filtering/sorting/grouping.
    /// </summary>
    public int Score
    {
        get => _score;
        set
        {
            if (_score == value) return;
            _score = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Category));
        }
    }

    /// <summary>
    /// Group key derived from Score. Updates when Score changes.
    /// </summary>
    public int Category => Score / 10; // 0..9

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
