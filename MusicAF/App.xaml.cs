using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicAF
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        public static Window MainWindow { get; private set; }
        public static string CurrentUserEmail { get; private set; }

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        static extern bool AllocConsole();
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {

            AllocConsole();
            MainWindow = new LogInWindow();
            MainWindow.Activate();
        }

        public static void NavigateToMainWindow(string userEmail)
        {
            try
            {
                CurrentUserEmail = userEmail;
                // Create and show the new window first
                var newWindow = new MainWindow(userEmail);
                newWindow.Activate();

                // Store the reference to the old window
                var oldWindow = MainWindow;

                // Update the MainWindow reference
                MainWindow = newWindow;

                // Close the old window after the new one is shown
                oldWindow?.Close();
            }
            catch (Exception ex)
            {
                // In case of any errors, show a dialog
                ShowErrorDialog($"Error navigating to main window: {ex.Message}");
            }
        }

        private static async void ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK"
            };

            // Use current window's XamlRoot if available
            if (MainWindow != null)
            {
                errorDialog.XamlRoot = MainWindow.Content.XamlRoot;
                await errorDialog.ShowAsync();
            }
        }
    }
}
