using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using System;
using UndertaleModLib;
using UndertaleModToolAvalonia.Utilities;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels;
using UndertaleModToolAvalonia.ViewModels.EditorViewModels.EditorComponents;

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
            
            MainTree.AddHandler(PointerPressedEvent, MainTree_PointerPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            MainTree.AddHandler(PointerMovedEvent, MainTree_PointerMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            MainTree.AddHandler(PointerReleasedEvent, (s, e) => dragPress = null, RoutingStrategies.Bubble, handledEventsToo: true);
        }
        
        private const double DragStartDistance = 6;

        private PointerPressedEventArgs? dragPress;
        private Point dragPressPoint;
        private UndertaleObject? dragResource;
        
        private void MainTree_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            dragPress = null;
            if (!e.GetCurrentPoint(MainTree).Properties.IsLeftButtonPressed)
                return;

            if ((e.Source as StyledElement)?.DataContext is ResourceNodeViewModel { Model: UndertaleObject resource })
            {
                dragPress = e;
                dragPressPoint = e.GetPosition(MainTree);
                dragResource = resource;
            }
        }
        
        private async void MainTree_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (dragPress is null || dragResource is null)
                return;

            if (!e.GetCurrentPoint(MainTree).Properties.IsLeftButtonPressed)
            {
                dragPress = null;
                return;
            }

            Point moved = e.GetPosition(MainTree) - dragPressPoint;
            if (Math.Abs(moved.X) < DragStartDistance && Math.Abs(moved.Y) < DragStartDistance)
                return;

            PointerPressedEventArgs press = dragPress;
            dragPress = null;
            await DragDrop.DoDragDropAsync(press, ResourceDragDrop.CreateTransfer(dragResource), DragDropEffects.Link);
        }

        private void TabController_TabCloseRequested(FluentAvalonia.UI.Controls.FATabView sender, FluentAvalonia.UI.Controls.FATabViewTabCloseRequestedEventArgs args)
        {
            if (DataContext is EditorViewModel editorViewModel)
            {
                editorViewModel.CloseAssetTabCommand.Execute(args.Tab.DataContext);
            }
        }
    }
}
