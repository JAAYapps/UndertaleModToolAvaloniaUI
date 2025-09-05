using Avalonia.Interactivity;
using FluentAvalonia.UI.Windowing;
using System;
using System.Runtime.InteropServices;

namespace UndertaleModToolAvalonia.Views
{
    public class UndertaleWindow : AppWindow
    {
        // Delegate for the new window procedure
        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Keep a reference to the delegate to prevent it from being garbage collected
        private WndProcDelegate? _wndProcDelegate;
        private IntPtr _originalWndProc = IntPtr.Zero;
        private bool wasMaximizedBeforeMove = false;

        protected override void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);

            // Subclass the window procedure only on Windows
            if (OperatingSystem.IsWindows())
            {
                try
                {
                    IntPtr hWnd = TryGetPlatformHandle().Handle;

                    // Pin the delegate to prevent garbage collection
                    _wndProcDelegate = new WndProcDelegate(CustomWndProc);

                    // Get the original window procedure
                    _originalWndProc = GetWindowLongPtr(hWnd, GWLP_WNDPROC);

                    // Set the new window procedure
                    SetWindowLongPtr(hWnd, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
                }
                catch { }
            }
        }

        private IntPtr CustomWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                // --- State-Based Drag Detection & Correction ---
                case WM_ENTERSIZEMOVE: // Fired when a move/resize operation begins.
                    wasMaximizedBeforeMove = IsMaximized(hWnd);
                    break;

                case WM_EXITSIZEMOVE: // Fired when the user releases the mouse after a resize/move.
                    if (wasMaximizedBeforeMove && !IsMaximized(hWnd))
                    {
                        // This condition is only true after a drag-to-restore operation.
                        int borderThickness = GetSystemMetrics(SM_CXFRAME) + GetSystemMetrics(SM_CXPADDEDBORDER);

                        // Add the border size back to the window's dimensions to fix clipping.
                        this.Width += borderThickness;
                        this.Height += borderThickness;
                    }
                    wasMaximizedBeforeMove = false;
                    break;

                case WM_NCCALCSIZE:
                    {
                        // This logic is only for solving the maximization gap.
                        if (wParam != IntPtr.Zero && IsMaximized(hWnd))
                        {
                            var screen = this.Screens.ScreenFromVisual(this);
                            if (screen != null)
                            {
                                var ncCalcSizeParams = Marshal.PtrToStructure<NCCALCSIZE_PARAMS>(lParam);
                                ncCalcSizeParams.rgrc[0].left = screen.WorkingArea.X;
                                ncCalcSizeParams.rgrc[0].top = screen.WorkingArea.Y;
                                ncCalcSizeParams.rgrc[0].right = screen.WorkingArea.X + screen.WorkingArea.Width;
                                ncCalcSizeParams.rgrc[0].bottom = screen.WorkingArea.Y + screen.WorkingArea.Height;
                                Marshal.StructureToPtr(ncCalcSizeParams, lParam, true);
                                return IntPtr.Zero;
                            }
                        }
                        break;
                    }

                case WM_GETMINMAXINFO:
                    {
                        var screen = this.Screens.ScreenFromVisual(this);
                        if (screen != null)
                        {
                            var minMaxInfo = Marshal.PtrToStructure<MINMAXINFO>(lParam);
                            minMaxInfo.ptMaxPosition.X = screen.WorkingArea.X;
                            minMaxInfo.ptMaxPosition.Y = screen.WorkingArea.Y;
                            minMaxInfo.ptMaxSize.X = screen.WorkingArea.Width;
                            minMaxInfo.ptMaxSize.Y = screen.WorkingArea.Height;
                            Marshal.StructureToPtr(minMaxInfo, lParam, true);
                        }
                        break;
                    }
            }

            // Call the original window procedure for all other messages
            return CallWindowProc(_originalWndProc, hWnd, msg, wParam, lParam);
        }

        private static bool IsMaximized(IntPtr hWnd)
        {
            var style = GetWindowLongPtr(hWnd, GWL_STYLE);
            return (style & WS_MAXIMIZE) != 0;
        }

        #region Win32 Definitions

        private const int GWLP_WNDPROC = -4;
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZE = 0x01000000;
        private const int WM_GETMINMAXINFO = 0x0024;
        private const int WM_NCCALCSIZE = 0x0083;
        private const int SM_CXFRAME = 32;
        private const int SM_CXPADDEDBORDER = 92;
        private const int WM_EXITSIZEMOVE = 0x0232;
        private const int WM_ENTERSIZEMOVE = 0x0231;

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int left; public int top; public int right; public int bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NCCALCSIZE_PARAMS
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public RECT[] rgrc;
            public IntPtr lppos;
        }

        #endregion
    }
}
