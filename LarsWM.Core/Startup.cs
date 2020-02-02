using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static LarsWM.Core.WindowsApi.WindowsApiService;
using static LarsWM.Core.WindowsApi.WindowsApiFacade;
using LarsWM.Core.UserConfigs.Commands;
using LarsWM.Core.Monitors.Commands;
using LarsWM.Core.Common.Models;
using LarsWM.Core.Common.Services;
using LarsWM.Core.Monitors;
using LarsWM.Core.Windows;
using LarsWM.Core.Workspaces;

namespace LarsWM.Core
{
    class Startup
    {
        private IBus _bus;
        private List<Monitor> _monitors = new List<Monitor>();

        public Startup(IBus bus)
        {
            _bus = bus;
        }

        public void Init()
        {
            _bus.Invoke(new GetUserConfigCommand());

            // Create a Monitor instance for each Screen
            foreach (var screen in Screen.AllScreens)
            {
                _bus.Invoke(new AddMonitorCommand(screen));
            }


            var windows = GetOpenWindows();

            foreach (var window in windows)
            {
                // Debug log
                DumpManagedWindows(window);

                // Add window to its nearest workspace
                var targetMonitor = GetMonitorFromWindowHandle(window);
                targetMonitor.DisplayedWorkspace.WindowsInWorkspace.Add(window);
            }

            foreach (var monitor in _monitors)
            {
                // Force initial layout
                var windowsInMonitor = monitor.DisplayedWorkspace.WindowsInWorkspace;
                var moveableWindows = windowsInMonitor.Where(w => w.CanLayout).ToList();

                var windowLocations = LayoutService.CalculateInitialLayout(monitor, moveableWindows);

                var handle = BeginDeferWindowPos(moveableWindows.Count());

                for (var i = 0; i < windowLocations.Count() - 1; i++)
                {
                    var window = moveableWindows[i];
                    var loc = windowLocations[i];

                    var adjustedLoc = new WindowLocation(loc.X + monitor.X, loc.Y + monitor.Y, 
                        loc.Width, loc.Height);

                    var flags = SWP.SWP_FRAMECHANGED | SWP.SWP_NOACTIVATE | SWP.SWP_NOCOPYBITS |
                        SWP.SWP_NOZORDER | SWP.SWP_NOOWNERZORDER;

                    DeferWindowPos(handle, window.Hwnd, IntPtr.Zero, adjustedLoc.X, adjustedLoc.Y, adjustedLoc.Width, adjustedLoc.Height, flags);
                }

                EndDeferWindowPos(handle);
            }

            Debug.WriteLine(_monitors);
        }

        private static void DumpManagedWindows(Window window)
        {
            StringBuilder sb = new StringBuilder(GetWindowTextLength(window.Hwnd) + 1);
            GetWindowText(window.Hwnd, sb, sb.Capacity);
            Debug.WriteLine(sb.ToString());

            uint processId;
            GetWindowThreadProcessId(window.Hwnd, out processId);
            var _processId = (int)processId;

            var process = Process.GetProcesses().FirstOrDefault(p => p.Id == _processId);
            var _processName = process.ProcessName;
            Debug.WriteLine(_processName);
            Debug.WriteLine(window.CanLayout);
        }

        public Monitor GetMonitorFromWindowHandle(Window window)
        {
            var screen = Screen.FromHandle(window.Hwnd);
            return _monitors.FirstOrDefault(m => m.Screen.DeviceName == screen.DeviceName) ?? _monitors[0];
        }

        public void ShiftFocusInDirection(MovementDirection direction)
        { }

        public void ShiftFocusToWorkspace(Workspace workspace)
        { }

        public void MoveFocusedWindowToWorkspace(Window window, Workspace workspace)
        { } 

        public void MoveFocusedWindowInDirection(Window window, MovementDirection direction)
        { }

    }
}

