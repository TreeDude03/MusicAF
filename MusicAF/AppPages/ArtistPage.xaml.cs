using Google.Cloud.Firestore;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MusicAF.Models;
using MusicAF.ThirdPartyServices;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicAF.AppPages
{
    public sealed partial class ArtistPage : Page
    {
        private string currentUserEmail;
        private readonly FirestoreService _firestoreService;
        public ObservableCollection<Track> Tracks { get; private set; }
        private bool isLoading;
        private bool isFollowed;

        public ArtistPage()
        {
            this.InitializeComponent();
            _firestoreService = FirestoreService.Instance;
            Tracks = new ObservableCollection<Track>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ValueTuple<string, string> parameters)
            {
                currentUserEmail = parameters.Item1;
                ArtistNameTextBlock.Text = parameters.Item2;
                await LoadTracksAsync(ArtistNameTextBlock.Text);
            }
        }

        // Method to check if the artist is followed
        private async Task CheckFollowStatusAsync(string artistName)
        {
            var userRef = _firestoreService.FirestoreDb.Collection("users").Document(currentUserEmail);
            var userDoc = await userRef.GetSnapshotAsync();

            if (userDoc.Exists)
            {
                var followedArtists = userDoc.GetValue<List<string>>("followedArtists") ?? new List<string>();
                isFollowed = followedArtists.Contains(artistName);
            }

            FollowButton.Content = isFollowed ? "Unfollow" : "Follow";
        }

        // Method to toggle follow/unfollow
        private async void FollowButton_Click(object sender, RoutedEventArgs e)
        {
            var artistName = ArtistNameTextBlock.Text;
            var userRef = _firestoreService.FirestoreDb.Collection("users").Document(currentUserEmail);
            var userDoc = await userRef.GetSnapshotAsync();

            if (userDoc.Exists)
            {
                List<string> followedArtists;

                try
                {
                    // Try to get the followedArtists field
                    followedArtists = userDoc.GetValue<List<string>>("followedArtists");
                }
                catch (Exception ex)
                {
                    // Log the exception and initialize followedArtists as an empty list if there's an error
                    Console.WriteLine($"Error retrieving followedArtists: {ex.Message}");
                    followedArtists = new List<string>();
                }

                if (isFollowed)
                {
                    // Unfollow the artist
                    followedArtists.Remove(artistName);
                    await userRef.SetAsync(new { followedArtists }, SetOptions.MergeAll);
                    isFollowed = false;
                    FollowButton.Content = "Follow"; // Update button text
                }
                else
                {
                    // Follow the artist
                    followedArtists.Add(artistName);
                    await userRef.SetAsync(new { followedArtists }, SetOptions.MergeAll);
                    isFollowed = true;
                    FollowButton.Content = "Unfollow"; // Update button text
                }
            }
            else
            {
                Console.WriteLine("User document does not exist."); // Log if the user document is not found
            }
        }

        private async Task LoadTracksAsync(string artistName)
        {
            var tracksRef = _firestoreService.FirestoreDb.Collection("tracks");
            var query = tracksRef.WhereEqualTo("Artist", artistName)
                                 .WhereEqualTo("IsPrivate", false);

            var snapshot = await query.GetSnapshotAsync();
            foreach (var doc in snapshot.Documents)
            {
                var track = doc.ConvertTo<Track>();
                Tracks.Add(track);
            }
        }

        private async void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is Track track)
                {
                    // Create instance of NowPlayingPage
                    var nowPlayingPage = new NowPlayingPage();

                    // Navigate to NowPlayingPage
                    Frame.Navigate(typeof(NowPlayingPage), track);

                    App.PlaybackService.SetTrackList(Tracks.ToList(), track);
                    App.PlaybackService.PlayTrack(track);

                    Debug.WriteLine($"Play button clicked for: {track.Title}");

                    // Update button visual state
                    UpdatePlayButtonState(button, true);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PlayButton_Click: {ex}");
                await ShowErrorDialog("Failed to play track");
            }
        }

        private void UpdatePlayButtonState(Button button, bool isPlaying)
        {
            if (button.Content is FontIcon icon)
            {
                icon.Glyph = isPlaying ? "\uE769" : "\uE768"; // Pause : Play
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
                        LoadingProgressRing.IsActive = true;
                        LoadingProgressRing.Visibility = Visibility.Visible;

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
                        LoadingProgressRing.IsActive = false;
                        LoadingProgressRing.Visibility = Visibility.Collapsed;
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

        private Task DownloadTrackAsync(Track track)
        {
            // Implement the download logic
            Debug.WriteLine($"Downloading track: {track.Title}");
            return Task.CompletedTask;
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}