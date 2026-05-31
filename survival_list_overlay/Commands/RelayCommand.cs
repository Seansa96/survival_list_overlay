using System.Windows.Input;

namespace survival_list_overlay.Commands;

public sealed class RelayCommand : ICommand
{
    private readonly Predicate<object?>? canExecute;
    private readonly Action<object?> execute;

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        this.canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => execute(parameter);

    public static void RefreshCanExecute() => CommandManager.InvalidateRequerySuggested();
}
