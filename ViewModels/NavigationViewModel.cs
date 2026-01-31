using System;
using System.Windows.Input;

namespace RimWorldTranslationTool.ViewModels
{
    public class NavigationViewModel : BaseViewModel
    {
        private string _currentPage = "Settings";
        private bool _isSidebarExpanded = true;

        public NavigationViewModel()
        {
            NavigateCommand = new RelayCommand<string>(Navigate);
            ToggleSidebarCommand = new RelayCommandEmpty(ToggleSidebar);
        }

        public string CurrentPage
        {
            get => _currentPage;
            set => SetProperty(ref _currentPage, value);
        }

        public bool IsSidebarExpanded
        {
            get => _isSidebarExpanded;
            set => SetProperty(ref _isSidebarExpanded, value);
        }

        public ICommand NavigateCommand { get; }
        public ICommand ToggleSidebarCommand { get; }

        private void Navigate(string? page)
        {
            if (!string.IsNullOrEmpty(page))
            {
                CurrentPage = page;
            }
        }

        private void ToggleSidebar()
        {
            IsSidebarExpanded = !IsSidebarExpanded;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Predicate<T?>? _canExecute;

        public RelayCommand(Action<T?> execute, Predicate<T?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
