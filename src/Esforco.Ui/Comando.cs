using System.Windows.Input;

namespace Esforco.Ui;

public sealed class Comando : ICommand
{
    private readonly Action _executar;
    private readonly Func<bool> _pode;

    public Comando(Action executar, Func<bool> pode)
    {
        _executar = executar;
        _pode = pode;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _pode();

    public void Execute(object? parameter) => _executar();
}
