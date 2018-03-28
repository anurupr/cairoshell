using System;
using System.Runtime.InteropServices;
using CairoDesktop.Interop;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using CairoDesktop.Configuration;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using CairoDesktop.WindowsTray;

namespace CairoDesktop.SupportingClasses
{
    public static class AppBarHelper
    {
        public enum ABEdge : int
        {
            ABE_LEFT = 0,
            ABE_TOP,
            ABE_RIGHT,
            ABE_BOTTOM
        }

        public enum WinTaskbarState : int
        {
            AutoHide = 1,
            OnTop = 0
        }

        public static int RegisterBar(Window abWindow, Screen screen, double width, double height, ABEdge edge = ABEdge.ABE_TOP)
        {
            NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            IntPtr handle = new WindowInteropHelper(abWindow).Handle;
            abd.hWnd = handle;

            if (!appBars.Contains(handle))
            {
                uCallBack = NativeMethods.RegisterWindowMessage("AppBarMessage");
                abd.uCallbackMessage = uCallBack;

                uint ret = NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_NEW, ref abd);
                appBars.Add(handle);
                Trace.WriteLine("Created AppBar for handle " + handle.ToString());

                ABSetPos(abWindow, screen, width, height, edge);
            }
            else
            {
                NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_REMOVE, ref abd);
                appBars.Remove(handle);
                Trace.WriteLine("Removed AppBar for handle " + handle.ToString());

                return 0;
            }
            
            return uCallBack;
        }

        public static List<IntPtr> appBars = new List<IntPtr>();

        private static int uCallBack = 0;

        public static void SetWinTaskbarPos(int swp)
        {
            IntPtr taskbarHwnd = NativeMethods.FindWindow("Shell_TrayWnd", "");

            if (NotificationArea.Instance.Handle != null && NotificationArea.Instance.Handle != IntPtr.Zero)
            {
                while (taskbarHwnd == NotificationArea.Instance.Handle)
                {
                    taskbarHwnd = NativeMethods.FindWindowEx(IntPtr.Zero, taskbarHwnd, "Shell_TrayWnd", "");
                }
            }

            IntPtr startButtonHwnd = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, (IntPtr)0xC017, null);
            NativeMethods.SetWindowPos(taskbarHwnd, IntPtr.Zero, 0, 0, 0, 0, swp);
            NativeMethods.SetWindowPos(startButtonHwnd, IntPtr.Zero, 0, 0, 0, 0, swp);

            // adjust secondary taskbars for multi-mon
            if (swp == (int)NativeMethods.SetWindowPosFlags.SWP_HIDEWINDOW)
                SetSecondaryTaskbarVisibility(NativeMethods.WindowShowStyle.Hide);
            else
                SetSecondaryTaskbarVisibility(NativeMethods.WindowShowStyle.ShowNoActivate);
        }

        public static void SetWinTaskbarState(WinTaskbarState state)
        {
            NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
            abd.cbSize = (int)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            abd.hWnd = NativeMethods.FindWindow("Shell_TrayWnd");

            if (NotificationArea.Instance.Handle != null && NotificationArea.Instance.Handle != IntPtr.Zero)
            {
                while (abd.hWnd == NotificationArea.Instance.Handle)
                {
                    abd.hWnd = NativeMethods.FindWindowEx(IntPtr.Zero, abd.hWnd, "Shell_TrayWnd", "");
                }
            }

            abd.lParam = (IntPtr)state;
            NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_SETSTATE, ref abd);
        }

        private static void SetSecondaryTaskbarVisibility(NativeMethods.WindowShowStyle shw)
        {
            bool complete = false;
            IntPtr secTaskbarHwnd = NativeMethods.FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Shell_SecondaryTrayWnd", null);

            // if we have 3+ monitors there may be multiple secondary taskbars
            while (!complete)
            {
                if (secTaskbarHwnd != IntPtr.Zero)
                {
                    NativeMethods.ShowWindowAsync(secTaskbarHwnd, shw);
                    secTaskbarHwnd = NativeMethods.FindWindowEx(IntPtr.Zero, secTaskbarHwnd, "Shell_SecondaryTrayWnd", null);
                }
                else
                    complete = true;
            }
        }

        public static void AppBarActivate(IntPtr hwnd)
        {
            NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
            abd.cbSize = (int)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            abd.hWnd = hwnd;
            abd.lParam = (IntPtr)Convert.ToInt32(true);
            NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_ACTIVATE, ref abd);

            // apparently the taskbars like to pop up when app bars change
            if (Settings.EnableTaskbar)
            {
                SetSecondaryTaskbarVisibility(NativeMethods.WindowShowStyle.Hide);
            }

            // if taskbar z-order changed, need to move up notification area
            if (Settings.EnableSysTray == true)
                NotificationArea.Instance.MakeActive();
        }

        public static void AppBarWindowPosChanged(IntPtr hwnd)
        {
            NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
            abd.cbSize = (int)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            abd.hWnd = hwnd;
            NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_WINDOWPOSCHANGED, ref abd);
        }

        public static void ABSetPos(Window abWindow, Screen screen, double width, double height, ABEdge edge)
        {
            NativeMethods.APPBARDATA abd = new NativeMethods.APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
            IntPtr handle = new WindowInteropHelper(abWindow).Handle;
            abd.hWnd = handle;
            abd.uEdge = (int)edge;
            int sWidth = (int)width;
            int sHeight = (int)height;

            int top = 0;
            int left = SystemInformation.WorkingArea.Left;
            int right = SystemInformation.WorkingArea.Right;
            int bottom = PrimaryMonitorDeviceSize.Height;

            if (screen != null)
            {
                top = screen.Bounds.Y;
                left = screen.WorkingArea.Left;
                right = screen.WorkingArea.Right;
                bottom = screen.Bounds.Bottom;
            }
            
            if (abd.uEdge == (int)ABEdge.ABE_LEFT || abd.uEdge == (int)ABEdge.ABE_RIGHT)
            {
                abd.rc.top = top;
                abd.rc.bottom = bottom;
                if (abd.uEdge == (int)ABEdge.ABE_LEFT)
                {
                    abd.rc.left = left;
                    abd.rc.right = abd.rc.left + sWidth;
                }
                else
                {
                    abd.rc.right = right;
                    abd.rc.left = abd.rc.right - sWidth;
                }

            }
            else
            {
                abd.rc.left = left;
                abd.rc.right = right;
                if (abd.uEdge == (int)ABEdge.ABE_TOP)
                {
                    if (abWindow is Taskbar)
                        abd.rc.top = top + Convert.ToInt32(Startup.MenuBarWindow.Height);
                    else
                        abd.rc.top = top;
                    abd.rc.bottom = abd.rc.top + sHeight;
                }
                else
                {
                    abd.rc.bottom = bottom;
                    abd.rc.top = abd.rc.bottom - sHeight;
                }
            }

            NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_QUERYPOS, ref abd);

            // system doesn't adjust all edges for us, do some adjustments
            switch (abd.uEdge)		
            {		
                case (int)ABEdge.ABE_LEFT:		
                    abd.rc.right = abd.rc.left + sWidth;		
                    break;		
                case (int)ABEdge.ABE_RIGHT:		
                    abd.rc.left = abd.rc.right - sWidth;		
                    break;		
                case (int)ABEdge.ABE_TOP:		
                    abd.rc.bottom = abd.rc.top + sHeight;		
                    break;		
                case (int)ABEdge.ABE_BOTTOM:		
                    abd.rc.top = abd.rc.bottom - sHeight;		
                    break;		
            }

            NativeMethods.SHAppBarMessage((int)NativeMethods.ABMsg.ABM_SETPOS, ref abd);

            // tracing
            int h = abd.rc.bottom - abd.rc.top;
            Trace.WriteLineIf(abd.uEdge == (int)ABEdge.ABE_TOP, "Top AppBar height is " + h.ToString());
            Trace.WriteLineIf(abd.uEdge == (int)ABEdge.ABE_BOTTOM, "Bottom AppBar height is " + h.ToString());

            abWindow.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle,
                new ResizeDelegate(DoResize), abd.hWnd, abd.rc.left, abd.rc.top, abd.rc.right - abd.rc.left, abd.rc.bottom - abd.rc.top);

            if (h < sHeight)
                ABSetPos(abWindow, screen, width, height, edge);
        }

        private delegate void ResizeDelegate(IntPtr hWnd, int x, int y, int cx, int cy);
        private static void DoResize(IntPtr hWnd, int x, int y, int cx, int cy)
        {
            NativeMethods.MoveWindow(hWnd, x, y, cx, cy, true);

            // apparently the taskbars like to pop up when app bars change
            if (Settings.EnableTaskbar)
            {
                SetWinTaskbarPos((int)NativeMethods.SetWindowPosFlags.SWP_HIDEWINDOW);
            }

            foreach (MenuBarShadow barShadow in Startup.MenuBarShadowWindows)
            {
                if (barShadow.MenuBar != null && barShadow.MenuBar.handle == hWnd)
                    barShadow.SetPosition();
            }

            if (Startup.DesktopWindow != null)
                Startup.DesktopWindow.ResetPosition();

            // if taskbar z-order changed, need to move up notification area
            if (Settings.EnableSysTray == true)
                NotificationArea.Instance.MakeActive();
        }

        public static System.Drawing.Size PrimaryMonitorSize
        {
            get
            {
                return new System.Drawing.Size(Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenWidth / Shell.DpiScaleAdjustment), Convert.ToInt32(System.Windows.SystemParameters.PrimaryScreenHeight / Shell.DpiScaleAdjustment));
            }
        }

        public static System.Drawing.Size PrimaryMonitorDeviceSize
        {
            get
            {
                return new System.Drawing.Size(NativeMethods.GetSystemMetrics(0), NativeMethods.GetSystemMetrics(1));
            }
        }

        public static System.Drawing.Size PrimaryMonitorWorkArea
        {
            get
            {
                return new System.Drawing.Size(SystemInformation.WorkingArea.Right - SystemInformation.WorkingArea.Left, SystemInformation.WorkingArea.Bottom - SystemInformation.WorkingArea.Top);
            }
        }
        
        public static void SetWorkArea()
        {
            // TODO investigate why this method isn't working correctly on multi-mon systems

            NativeMethods.RECT rc;
            rc.left = SystemInformation.VirtualScreen.Left;
            rc.right = SystemInformation.VirtualScreen.Right;

            // only allocate space for taskbar if enabled
            if (Settings.EnableTaskbar && Settings.TaskbarMode == 0)
            {
                if (Settings.TaskbarPosition == 1)
                {
                    rc.top = SystemInformation.VirtualScreen.Top + (int)(Startup.MenuBarWindow.ActualHeight * Shell.DpiScale) + (int)(Startup.TaskbarWindow.ActualHeight * Shell.DpiScale);
                    rc.bottom = SystemInformation.VirtualScreen.Bottom;
                }
                else
                {
                    rc.top = SystemInformation.VirtualScreen.Top + (int)(Startup.MenuBarWindow.ActualHeight * Shell.DpiScale);
                    rc.bottom = SystemInformation.VirtualScreen.Bottom - (int)(Startup.TaskbarWindow.ActualHeight * Shell.DpiScale);
                }
            }
            else
            {
                rc.top = SystemInformation.VirtualScreen.Top + (int)(Startup.MenuBarWindow.ActualHeight * Shell.DpiScale);
                rc.bottom = SystemInformation.VirtualScreen.Bottom;
            }

            NativeMethods.SystemParametersInfo((int)NativeMethods.SPI.SPI_SETWORKAREA, 0, ref rc, (1 | 2));

            if (Startup.DesktopWindow != null)
                Startup.DesktopWindow.ResetPosition();
        }
        
        public static void ResetWorkArea()
        {
            // set work area back to full screen size. we can't assume what pieces of the old workarea may or may not be still used
            NativeMethods.RECT oldWorkArea;
            oldWorkArea.left = SystemInformation.VirtualScreen.Left;
            oldWorkArea.top = SystemInformation.VirtualScreen.Top;
            oldWorkArea.right = SystemInformation.VirtualScreen.Right;
            oldWorkArea.bottom = SystemInformation.VirtualScreen.Bottom;

            NativeMethods.SystemParametersInfo((int)NativeMethods.SPI.SPI_SETWORKAREA, 0, ref oldWorkArea, (1 | 2));
        }
    }
}
