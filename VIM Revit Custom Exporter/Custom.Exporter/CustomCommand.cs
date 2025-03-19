using System;
using System.Windows.Input;

namespace Custom.Exporter
{
    /// <summary>
    /// A simple ICommand implementation
    /// </summary>
    public class CustomCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        // Constructor that takes both execute action and canExecute predicate.
        public CustomCommand(Action<object> execute, Predicate<object> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public CustomCommand(Action<object> execute) : this(execute, null)
        { }

        public static CustomCommand Create(Action<object> execute)
            => new CustomCommand(execute);

        public static CustomCommand Create(Action execute)
            => new CustomCommand(_ => execute());

        // ICommand implementation.
        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        // Event that is raised when changes occur that affect whether or not the command should execute.
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Execute the command.
        public void Execute(object parameter)
        {
            _execute(parameter);
        }
    }
}
