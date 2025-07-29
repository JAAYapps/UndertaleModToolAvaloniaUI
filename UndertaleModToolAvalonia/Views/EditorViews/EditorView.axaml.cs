using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;

namespace UndertaleModToolAvalonia.Views.EditorViews
{
    public partial class EditorView : UserControl
    {
        public EditorView()
        {
            InitializeComponent();
            TabController.TabCloseRequested += TabController_TabCloseRequested;
            MainTree.DoubleTapped += (s, e) =>
            {
                if (DataContext is EditorViewModel vm && vm.SelectedNode?.Model != null)
                {
                    vm.OpenAssetInTabCommand.Execute(vm.SelectedNode.Model);
                }
            };

        }

        private void TabController_TabCloseRequested(FluentAvalonia.UI.Controls.TabView sender, FluentAvalonia.UI.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            if (DataContext is EditorViewModel editorViewModel)
            {
                editorViewModel.CloseAssetTabCommand.Execute(args.Tab.DataContext);
            }
        }
    }
}
