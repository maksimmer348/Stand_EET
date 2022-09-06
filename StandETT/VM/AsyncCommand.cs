using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace StandETT;
public interface IAsyncCommand : ICommand
{
    Task ExecuteAsync();
    bool CanExecute();
}

