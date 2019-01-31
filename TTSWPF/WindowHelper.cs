using System;
using System.Runtime.InteropServices;

namespace TTSWPF
{
    public static class WindowHelper
    {
        public static void BringProcessToFront()
        {
            IntPtr handle = csgo;
            if (csgo == IntPtr.Zero)
            {
                return;
            }
            if (IsIconic(handle))
            {
                ShowWindow(handle, SW_RESTORE);
            }

            SetForegroundWindow(handle);
        }

        const int SW_RESTORE = 9;

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr handle);
        [DllImport("User32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int nCmdShow);
        [DllImport("User32.dll")]
        private static extern bool IsIconic(IntPtr handle);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        private static IntPtr csgo = IntPtr.Zero;
        public static void SaveForeGround()
        {

            csgo = GetForegroundWindow();
        }
    }
}