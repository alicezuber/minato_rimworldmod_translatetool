using System.Windows.Controls;
using RimWorldTranslationTool.ViewModels;

namespace RimWorldTranslationTool.Views
{
    public partial class ModBrowserView : UserControl
    {
        public ModBrowserView(ModBrowserViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
