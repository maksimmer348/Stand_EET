using System;
using System.Windows.Input;

namespace StandETT;

public abstract class BaseCommand : ICommand
{
    //событие генериуется когда метод CanExecute возрващает другое значение те когда кмд преходит из одного
    //состояни в другое true->false или false->true, или можно выполнять это событие какимто своим способом
    public event EventHandler? CanExecuteChanged
    {
        //событие передает управленеие классом CommandManager когда чтот происходит
        add => CommandManager.RequerySuggested += value;
        //когда ко манда выполненя просиходит отписка от события команды
        remove => CommandManager.RequerySuggested -= value;
    }

    //если функция вернут false то команду выполнить нельзя то эл к которму привязана команда отключается
    public abstract bool CanExecute(object? parameter);

    //то что будет выполнено самой коммандой, те логика команды
    public abstract void Execute(object? parameter);

}