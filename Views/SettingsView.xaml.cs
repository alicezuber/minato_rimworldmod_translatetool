using System.Windows.Controls;
using RimWorldTranslationTool.ViewModels;

namespace RimWorldTranslationTool.Views
{
    public partial class SettingsView : UserControl
    {
        public SettingsView(SettingsViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
