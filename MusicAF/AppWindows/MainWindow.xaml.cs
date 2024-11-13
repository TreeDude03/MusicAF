using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using Windows.Graphics;
using System.Threading.Tasks;
using Microsoft.UI;
using MusicAF.ThirdPartyServices;
using MusicAF.AppPages;
using MusicAF.AppDialogs;
using MusicAF.Models;

namespace MusicAF.AppWindows
{
    public sealed partial class MainWindow : Window
    {
        private Models.User currentUser;
        private FirestoreService _firestoreService;
        //
        private string currentUserEmail;

        public MainWindow(string userEmail)
        {
            try
            {
                currentUserEmail = userEmail;
                _firestoreService = FirestoreService.Instance;
                this.InitializeComponent();

                // Set a default window size
                this.SetWindowSize(1200, 800);

                // Navigate to the library page
                MainFrame.Navigate(typeof(MyLibraryPage), currentUserEmail);
            }
            catch (Exception ex)
            {
                // Handle any initialization errors
                ShowErrorDialog($"Error initializing main window: {ex.Message}");
            }
        }

        private void SetWindowSize(int width, int height)
        {
            var windowId = Win32Interop.GetWindowIdFromWindow(
                WinRT.Interop.WindowNative.GetWindowHandle(this));
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow is not null)
            {
                appWindow.Resize(new Windows.Graphics.SizeInt32 { Width = width, Height = height });
            }
        }

        private async void ShowErrorDialog(string message)
        {
            ContentDialog errorDialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await errorDialog.ShowAsync();
        }

        public async Task setUpUser(string userEmail)
        {
            string login_email = await _firestoreService.GetFieldFromDocumentAsync<string>("users", userEmail, "Email");
            string login_password = await _firestoreService.GetFieldFromDocumentAsync<string>("users", userEmail, "Password");
            string datecreated = await _firestoreService.GetFieldFromDocumentAsync<string>("users", userEmail, "CreatedAt");
            currentUser = new Models.User()
            {
                Email = login_email,
                Password = login_password,
                CreatedAt = DateTime.Now,
            };
        }

        private void ForYouButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(typeof(ForYouPage), currentUserEmail);
        }
        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(typeof(MyLibraryPage), currentUserEmail);
        }
    }
}
