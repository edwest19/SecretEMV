
// Copyright (c) 2026 edwest19
// All rights reserved.
// This file was generated using Microsoft Copilot.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace SecretEmv.GenAC
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // Load the AC Generation page as the main content.
            var page = new AcGenPage();
            Content = page;

            // Set window size for better initial display
            // This must be done AFTER setting content
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

            if (appWindow != null)
            {
                // Set a larger width to accommodate all content
                appWindow.Resize(new Windows.Graphics.SizeInt32(800, 900));
            }
        }
    }
}

