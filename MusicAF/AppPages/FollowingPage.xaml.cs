using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Dispatching;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using System.Linq;
using System.Diagnostics;
using MusicAF.ThirdPartyServices;
using MusicAF.Models;
using Windows.Storage;
using System.IO;
using System.Collections.Generic;

namespace MusicAF.AppPages
{
    public sealed partial class FollowingPage : Page
    {
        private string currentUserEmail;
        private readonly FirestoreService _firestoreService;
        private bool isLoading;
        private ObservableCollection<Track> _tracks;
        private Track _currentlyPlayingTrack;

        public ObservableCollection<Track> Tracks
        {
            get => _tracks;
            private set
            {
                _tracks = value;
                UpdateTracksListView();
            }
        }

        public FollowingPage()
        {
            this.InitializeComponent();
            _firestoreService = FirestoreService.Instance;
            Tracks = new ObservableCollection<Track>();
        }

        private void UpdateTracksListView()
        {
            if (TracksListView != null)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    TracksListView.ItemsSource = null;
                    TracksListView.ItemsSource = _tracks;
                });
            }
        }

        private async Task LoadTracksAsync()
        {
            if (isLoading) return;

            isLoading = true;
            try
            {
                await ShowLoadingStateAsync(true);
                DispatcherQueue.TryEnqueue(() => { Tracks.Clear(); });

                // Fetch followed artists' tracks
                var followedTracks = await GetFollowedArtistsTracksAsync(currentUserEmail);

                // Update UI
                DispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var track in followedTracks)
                    {
                        Tracks.Add(track);
                    }

                    NoTracksMessage.Visibility = Tracks.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    TracksListView.Visibility = Tracks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in LoadTracksAsync: {ex.Message}");
                await ShowErrorDialog("Failed to load tracks: " + ex.Message);
            }
            finally
            {
                isLoading = false;
                await ShowLoadingStateAsync(false);
            }
        }

        private async Task ShowLoadingStateAsync(bool isLoading)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                LoadingProgressRing.IsActive = isLoading;
                LoadingProgressRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            });
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string userEmail)
            {
                currentUserEmail = userEmail;
                _ = LoadTracksAsync();
            }
            else
            {
                ShowNoTracksMessage("Invalid user session. Please log in again.");
            }
        }

        private void ResetAllPlayButtons()
        {
            // Iterate through all items in the ListView
            foreach (var item in TracksListView.Items)
            {
                // Get the container for the current item
                var container = TracksListView.ContainerFromItem(item) as ListViewItem;

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

        private void ShowNoTracksMessage(string message = null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NoTracksMessage.Text = message ?? "No tracks found. Click the 'Upload music' button to add some tracks.";
                NoTracksMessage.Visibility = Visibility.Visible;
                TracksListView.Visibility = Visibility.Collapsed;
            });
        }

        private async Task<List<Track>> GetFollowedArtistsTracksAsync(string email)
        {
            var followedTracks = new List<Track>();
            try
            {
                // Fetch the user document
                var userRef = _firestoreService.FirestoreDb.Collection("users").Document(email);
                var userDoc = await userRef.GetSnapshotAsync();

                if (!userDoc.Exists)
                {
                    Console.WriteLine("User document not found.");
                    return followedTracks;
                }

                List<string> followedArtists;
                try
                {
                    // Attempt to get the followedArtists field
                    followedArtists = userDoc.GetValue<List<string>>("followedArtists") ?? new List<string>();
                }
                catch (Exception ex)
                {
                    // Log the error and initialize followedArtists as an empty list if an error occurs
                    Console.WriteLine($"Error retrieving followedArtists: {ex.Message}");
                    followedArtists = new List<string>();
                }

                Console.WriteLine($"Followed Artists: {string.Join(", ", followedArtists)}");

                // Fetch tracks from followed artists
                var tracksRef = _firestoreService.FirestoreDb.Collection("tracks");
                foreach (var artist in followedArtists)
                {
                    try
                    {
                        var querySnapshot = await tracksRef
                            .WhereEqualTo("Artist", artist)
                            .OrderByDescending("UploadDate")
                            .Limit(50)
                            .GetSnapshotAsync();


                        foreach (var doc in querySnapshot.Documents)
                        {
                            var track = doc.ConvertTo<Track>();
                            Console.WriteLine($"Checking track: {track?.Title}, Artist: {track?.Artist}");
                            if (track != null && !track.IsPrivate && !followedTracks.Contains(track))
                            {
                                followedTracks.Add(track);
                            }

                            // Break if we already have 50 tracks
                            if (followedTracks.Count >= 50) break;
                        }

                        // Break from the outer loop if we already have 50 tracks
                        if (followedTracks.Count >= 50) break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching tracks for artist '{artist}': {ex.Message}");
                    }
                }

                Console.WriteLine($"Followed tracks loaded: {followedTracks.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading followed artists' tracks: {ex.Message}");
            }

            return followedTracks;
        }

        private async Task ShowErrorDialog(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Error",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
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
                Frame.Navigate(typeof(ArtistPage), (_currentUser: currentUserEmail, _artist: artistName));
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