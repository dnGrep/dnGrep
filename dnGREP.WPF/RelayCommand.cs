using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace dnGREP.WPF
{
    public interface IUICommand : ICommand, INotifyPropertyChanged
    {
        string KeyGestureText { get; set; }
    }

    /// <summary>
    /// A command whose sole purpose is to 
    /// relay its functionality to other
    /// objects by invoking delegates. The
    /// default return value for the CanExecute
    /// method is 'true'.
    /// </summary>
    public class RelayCommand : IUICommand
    {
        private readonly Action<object> execute;
        private readonly Predicate<object>? canExecute;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Creates a new command that can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        public RelayCommand(Action<object> execute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        /// <summary>
        /// Creates a new command.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <param name="canExecute">The execution status logic.</param>
        public RelayCommand(Action<object> execute, Predicate<object> canExecute)
        {
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
            this.canExecute = canExecute;
        }

        private string _keyGestureText = string.Empty;
        public string KeyGestureText
        {
            get { return _keyGestureText; }
            set
            {
                if (value == _keyGestureText)
                    return;

                _keyGestureText = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(KeyGestureText)));
            }
        }

        [DebuggerStepThrough]
        public bool CanExecute(object? parameter)
        {
            return canExecute == null || canExecute(parameter ?? new());
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void Execute(object? parameter)
        {
            execute(parameter ?? new());
        }
    }
}
