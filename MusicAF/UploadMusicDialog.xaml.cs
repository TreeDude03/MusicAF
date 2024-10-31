using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;

namespace MusicAF
{
    public sealed partial class UploadMusicDialog : ContentDialog
    {
        private readonly Window _window;
        private Grid _fileSelectionScreen;
        private Grid _uploadProgressScreen;
        private Grid _fileDetailsScreen;
        private StackPanel _dialogContent;

        public UploadMusicDialog(Window window)
        {
            _window = window;
            this.InitializeComponent();

            // Get references to named elements after initialization
            _fileSelectionScreen = (Grid)FindName("FileSelectionScreen");
            _uploadProgressScreen = (Grid)FindName("UploadProgressScreen");
            _fileDetailsScreen = (Grid)FindName("FileDetailsScreen");
            _dialogContent = (StackPanel)FindName("DialogContent");

            // Verify the controls were found
            if (_fileSelectionScreen == null || _uploadProgressScreen == null ||
                _fileDetailsScreen == null || _dialogContent == null)
            {
                throw new InvalidOperationException("Required controls not found in XAML.");
            }
        }

        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker();
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");
            picker.FileTypeFilter.Add(".m4a");

            // Initialize the picker with the window handle
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();

            if (files != null && files.Count > 0)
            {
                // Show upload progress screen
                _fileSelectionScreen.Visibility = Visibility.Collapsed;
                _uploadProgressScreen.Visibility = Visibility.Visible;
            }
        }

        private void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            // Show file details screen
            _uploadProgressScreen.Visibility = Visibility.Collapsed;
            _fileDetailsScreen.Visibility = Visibility.Visible;
        }

        private async void FinalizeUpload_Click(object sender, RoutedEventArgs e)
        {
            // Show loading indicator
            var loader = new ProgressRing()
            {
                IsActive = true,
                Margin = new Thickness(0, 20, 0, 0)
            };
            _dialogContent.Children.Add(loader);

            try
            {
                // Simulate upload - replace with actual upload logic
                await Task.Delay(2000);

                // Close dialog on success
                this.Hide();
            }
            catch (Exception ex)
            {
                // Handle upload error
                ContentDialog errorDialog = new ContentDialog
                {
                    Title = "Upload Error",
                    Content = $"Failed to upload files: {ex.Message}",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await errorDialog.ShowAsync();
            }
        }
    }
}