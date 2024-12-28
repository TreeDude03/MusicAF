using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MusicAF.Models;
using MusicAF.ThirdPartyServices;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Diagnostics;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicAF.AppPages
{
    public sealed partial class LikedPage : Page
    {
        private string currentUserEmail;

        private bool isLoading;
        private Track _currentlyPlayingTrack;

        private readonly FirestoreService _firestoreService;
        public ObservableCollection<Track> Tracks { get; private set; }

        public LikedPage()
        {
            this.InitializeComponent();
            _firestoreService = FirestoreService.Instance;
            Tracks = new ObservableCollection<Track>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string userEmail)
            {
                currentUserEmail = userEmail;
                await LoadFavoritesAsync(userEmail);
            }
        }

        private async Task LoadFavoritesAsync(string userEmail)
        {
            var favoritesRef = _firestoreService.FirestoreDb.Collection("favorites").Document(userEmail);
            var favoritesDoc = await favoritesRef.GetSnapshotAsync();

            if (favoritesDoc.Exists)
            {
                var favoriteTrackIds = favoritesDoc.GetValue<List<string>>("TrackIds");

                foreach (var trackId in favoriteTrackIds)
                {
                    var trackRef = _firestoreService.FirestoreDb.Collection("tracks").Document(trackId);
                    var trackDoc = await trackRef.GetSnapshotAsync();

                    if (trackDoc.Exists)
                    {
                        var track = trackDoc.ConvertTo<Track>();
                        Tracks.Add(track);
                    }
                }
            }
        }
        private async Task ShowLoadingStateAsync(bool isLoading)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    LoadingProgressRing.IsActive = isLoading;
                    LoadingProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error updating loading state: {ex}");
                }
            });
        }
        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Track track)
                {
                    // Record keywords when a track is played
                    await _firestoreService.RecordKeywordsAsync(currentUserEmail, track);
                    // Create instance of NowPlayingPage
                    var nowPlayingPage = new NowPlayingPage();

                    // Navigate to NowPlayingPage
                    Frame.Navigate(typeof(NowPlayingPage), (_track: track, _email: currentUserEmail));

                    App.PlaybackService.SetTrackList(Tracks.ToList(), track);
                    App.PlaybackService.PlayTrack(track);

                    Debug.WriteLine($"Play button clicked for: {track.Title}");

                    // Update button visual state
                    UpdatePlayButtonState(button, true, track);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PlayButton_Click: {ex}");
                await ShowErrorDialog("Failed to play track");
            }
        }

        private void UpdatePlayButtonState(Button button, bool isPlaying, Track selectedTrack)
        {
            if (_currentlyPlayingTrack != null && _currentlyPlayingTrack != selectedTrack)
            {
                _currentlyPlayingTrack = selectedTrack;
                ResetAllPlayButtons();
            }
            _currentlyPlayingTrack = selectedTrack;
            if (button.Content is FontIcon icon)
            {
                icon.Glyph = isPlaying ? "\uE769" : "\uE768"; // Pause : Play
            }

        }

        private void ResetAllPlayButtons()
        {
            // Iterate through all items in the ListView
            foreach (var item in FavoritesListView.Items)
            {
                // Get the container for the current item
                var container = FavoritesListView.ContainerFromItem(item) as ListViewItem;

                if (container != null)
                {
                    // Assuming the Button is directly part of the container's visual structure
                    var button = container.ContentTemplateRoot as Button;

                    if (button != null && button.Content is FontIcon icon)
                    {
                        // Reset the glyph to "Play" (&#xE768;)
                        icon.Glyph = "\uE768";
                    }
                }
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing dialog: {ex}");
            }
        }
        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track)
            {
                try
                {
                    Console.WriteLine("Here.");
                    var window = new Microsoft.UI.Xaml.Window();
                    // ...
                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var folderPicker = new Windows.Storage.Pickers.FolderPicker();
                    folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.Desktop;
                    folderPicker.FileTypeFilter.Add("*");
                    WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);
                    Windows.Storage.StorageFolder folder = await folderPicker.PickSingleFolderAsync();
                    if (folder != null)
                    {
                        // Application now has read/write access to all contents in the picked folder
                        // (including other sub-folder contents)
                        Windows.Storage.AccessCache.StorageApplicationPermissions.
                        FutureAccessList.AddOrReplace("PickedFolderToken", folder);

                        // Show the loading state
                        DownloadProgressRing.IsActive = true;
                        DownloadProgressRing.Visibility = Visibility.Visible;

                        // Retrieve the audio file using DriveFileId
                        var fileStream = await RetrieveAudioFileAsync(track.DriveFileId);

                        // Create a file in the selected folder
                        var newFile = await folder.CreateFileAsync(track.FileDetails.FileName, CreationCollisionOption.ReplaceExisting);

                        // Save the file
                        using (var fileOutputStream = await newFile.OpenStreamForWriteAsync())
                        {
                            await fileStream.CopyToAsync(fileOutputStream);
                        }

                        Console.WriteLine($"File downloaded successfully: {newFile.Path}");
                        DownloadProgressRing.IsActive = false;
                        DownloadProgressRing.Visibility = Visibility.Collapsed;
                        await ShowDownloadSuccessMessage(newFile.Path);

                    }
                    else
                    {
                        Console.WriteLine("No folder selected.");
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                }
            }
        }

        private async Task ShowDownloadSuccessMessage(string filePath)
        {
            var dialog = new ContentDialog
            {
                Title = "Download Complete",
                Content = $"Your file has been successfully downloaded to:\n{filePath}",
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot // Ensure XamlRoot is set for the current page
            };

            await dialog.ShowAsync();
        }


        private async Task<Stream> RetrieveAudioFileAsync(string driveFileId)
        {
            try
            {
                // Assume you have a GoogleDriveService to handle file downloads
                var driveService = GoogleDriveService.Instance;
                var fileStream = await driveService.DownloadFileAsync(driveFileId);
                return fileStream;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error retrieving file from Drive: {ex.Message}");
                throw;
            }
        }

        private void ArtistButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Content is TextBlock textBlock)
            {
                string artistName = textBlock.Text;
                Frame.Navigate(typeof(ArtistPage), artistName);
            }
        }

        private void BackClick(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}
