using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using System.Text;
using System.Diagnostics;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.IO;
using MusicAF.ThirdPartyServices;
using MusicAF.Models;

namespace MusicAF.AppDialogs
{
    public sealed partial class UploadMusicDialog : ContentDialog
    {
        private readonly Window _window;
        private readonly string _currentUserEmail;
        private FirestoreService _firestoreService;
        private StorageFile selectedFile;
        private bool isUploading = false;
        private TaskCompletionSource<bool> uploadCompletionSource;
        private ContentDialog _currentDialog = null;


        public UploadMusicDialog(Window window, string userEmail)
        {
            try
            {
                _window = window;
                _currentUserEmail = userEmail;
                _firestoreService = FirestoreService.Instance;
                this.InitializeComponent();
                this.Closing += UploadMusicDialog_Closing;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Constructor error: {ex.Message}");
                throw;
            }
        }

        private void UploadMusicDialog_Closing(ContentDialog sender, ContentDialogClosingEventArgs args)
        {
            if (isUploading)
            {
                args.Cancel = true;
            }
        }


        private void ShowMessage(string message, bool isError = false)
        {
            MessageTextBlock.Text = message;
            MessageTextBlock.Foreground = isError ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
        }

        private async void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".mp3");
                picker.FileTypeFilter.Add(".wav");
                picker.FileTypeFilter.Add(".m4a");

                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

                var file = await picker.PickSingleFileAsync();

                if (file != null)
                {
                    selectedFile = file;
                    if (FileSelectionScreen != null && UploadProgressScreen != null)
                    {
                        FileSelectionScreen.Visibility = Visibility.Collapsed;
                        UploadProgressScreen.Visibility = Visibility.Visible;

                        // Update UI to show selected file
                        if (FileProgressContainer != null)
                        {
                            FileProgressContainer.Children.Clear();
                            var fileNameBlock = new TextBlock
                            {
                                Text = file.Name,
                                Margin = new Thickness(10, 0, 0, 0)
                            };
                            FileProgressContainer.Children.Add(fileNameBlock);
                        }
                        ShowMessage($"Selected file: {file.Name}", false);
                    }
                }
                else
                {
                    ShowMessage("No file was selected", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"File selection error: {ex.Message}");
                ShowMessage($"Error selecting file: {ex.Message}", true);
            }
        }

        private async void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (selectedFile != null && UploadProgressScreen != null && FileDetailsScreen != null)
                {
                    UploadProgressScreen.Visibility = Visibility.Collapsed;
                    FileDetailsScreen.Visibility = Visibility.Visible;
                    ShowMessage($"Ready to upload: {selectedFile.Name}", false);
                }
                else
                {
                    ShowMessage("Please select a file first", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Navigation error: {ex.Message}");
                ShowMessage($"Error preparing upload: {ex.Message}", true);
            }
        }

        private async void FinalizeUpload_Click(object sender, RoutedEventArgs e)
        {
            Button uploadButton = sender as Button;
            if (uploadButton != null)
            {
                uploadButton.IsEnabled = false;
            }

            try
            {
                if (!ValidateInputs())
                {
                    if (uploadButton != null)
                    {
                        uploadButton.IsEnabled = true;
                    }
                    return;
                }

                isUploading = true;
                ShowMessage("Uploading file...", false);

                try
                {
                    // Generate unique filename
                    string fileName = $"{Guid.NewGuid()}{selectedFile.FileType}";

                    // Upload to Google Drive
                    var driveService = GoogleDriveService.Instance;
                    var (fileId, webViewLink) = await driveService.UploadFileAsync(selectedFile.Path, fileName);

                    // Get file properties
                    var fileProperties = await selectedFile.GetBasicPropertiesAsync();

                    // Create track object
                    var track = new Track
                    {
                        SongId = Guid.NewGuid().ToString(),
                        Title = TitleTextBox?.Text ?? "",
                        Artist = ArtistTextBox?.Text ?? "",
                        Album = AlbumTextBox?.Text ?? "",
                        Genre = GenreComboBox?.SelectedItem is ComboBoxItem genreItem ?
                                genreItem.Content?.ToString() : "Other",
                        LengthInSeconds = 0,
                        Uploader = _currentUserEmail,
                        Streams = 0,
                        Likes = 0,
                        Saves = 0,
                        IsPrivate = PrivacyCheckBox?.IsChecked ?? false,
                        DownloadPrice = PriceTextBox != null ?
                                       double.TryParse(PriceTextBox.Text, out double price) ?
                                       price : 0.0 : 0.0,
                        AllowDownload = DownloadCheckBox?.IsChecked ?? false,
                        UploadDate = DateTime.UtcNow,
                        DriveFileId = fileId,
                        DriveWebViewLink = webViewLink,
                        FileDetails = new FileDetails
                        {
                            FileName = selectedFile.Name,
                            FileType = selectedFile.FileType,
                            FileSize = fileProperties.Size,
                            MimeType = "audio/mpeg"
                        }
                    };

                    // Upload to Firestore
                    await _firestoreService.AddDocumentAsync("tracks", track.SongId, track);

                    ShowMessage("Track uploaded successfully!", false);
                    await Task.Delay(2000);
                    this.Hide();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Upload error: {ex.Message}\nStack trace: {ex.StackTrace}");
                    ShowMessage($"Failed to upload track: {ex.Message}", true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Outer error: {ex.Message}\nStack trace: {ex.StackTrace}");
                ShowMessage($"An unexpected error occurred: {ex.Message}", true);
            }
            finally
            {
                isUploading = false;
                if (uploadButton != null)
                {
                    uploadButton.IsEnabled = true;
                }
            }
        }



        private bool ValidateInputs()
        {
            var error = new StringBuilder();

            if (selectedFile == null)
            {
                error.AppendLine("No file selected");
            }

            if (string.IsNullOrWhiteSpace(TitleTextBox?.Text))
            {
                error.AppendLine("Title is required");
            }

            if (string.IsNullOrWhiteSpace(ArtistTextBox?.Text))
            {
                error.AppendLine("Artist is required");
            }

            if (GenreComboBox?.SelectedItem == null)
            {
                error.AppendLine("Genre is required");
            }

            if (RightsConfirmationCheckBox?.IsChecked != true)
            {
                error.AppendLine("Please confirm you have rights to upload this music");
            }

            var errorMessage = error.ToString().Trim();
            if (!string.IsNullOrEmpty(errorMessage))
            {
                ShowMessage(errorMessage, true);
                return false;
            }

            return true;
        }
    }
}