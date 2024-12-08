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
using MusicAF.AppDialogs;
using MusicAF.Models;
using Windows.Storage;
using System.IO;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace MusicAF.AppPages
{
    public sealed partial class SearchPage : Page
    {
        private string currentUserEmail;
        private string searchText;
        private readonly FirestoreService _firestoreService;
        private bool isLoading;
        private Track _currentlyPlayingTrack;


        private ObservableCollection<Track> _tracks;
        public ObservableCollection<Track> Tracks
        {
            get => _tracks;
            private set
            {
                _tracks = value;
                UpdateTracksListView();
            }
        }

        public SearchPage()
        {
            try
            {
                Console.WriteLine("Initializing SearchPage");
                this.InitializeComponent();
                _firestoreService = FirestoreService.Instance;
                Tracks = new ObservableCollection<Track>();
                Console.WriteLine("SearchPage initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing ForYouPage: {ex}");
                throw;
            }
        }

        private void UpdateTracksListView()
        {
            try
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
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating TracksListView: {ex}");
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);
                Console.WriteLine("Navigation to ForYouPage");

                if (e.Parameter is ValueTuple<string, string> parameters)
                {
                    currentUserEmail = parameters.Item1;
                    String keyword = parameters.Item2;
                    SearchFor.Text = $"Search Result For {keyword}";
                    await LoadSearchResultsAsync(keyword);
                }
                else
                {
                    Console.WriteLine($"Invalid navigation parameter");
                    ShowNoTracksMessage("Invalid user session. Please log in again.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in OnNavigatedTo: {ex}");
                ShowNoTracksMessage("An error occurred while loading the library.");
            }
        }

        private async Task<ObservableCollection<Track>> SearchTracksAsync(string keyword)
        {
            var searchedTracks = new ObservableCollection<Track>();
            try
            {
                if (string.IsNullOrEmpty(keyword)) return searchedTracks;
                Console.WriteLine($"Searching tracks for keyword: {keyword}");

                // Query tracks based on the keyword
                var tracksRef = _firestoreService.FirestoreDb.Collection("tracks");

                // Query for Title
                var titleQuery = await tracksRef.WhereGreaterThanOrEqualTo("Title", keyword)
                                                .WhereLessThan("Title", keyword + "\uf8ff")
                                                .GetSnapshotAsync();

                // Query for Genre
                var genreQuery = await tracksRef.WhereGreaterThanOrEqualTo("Genre", keyword)
                                                .WhereLessThan("Genre", keyword + "\uf8ff")
                                                .GetSnapshotAsync();

                // Query for Artist
                var artistQuery = await tracksRef.WhereGreaterThanOrEqualTo("Artist", keyword)
                                                 .WhereLessThan("Artist", keyword + "\uf8ff")
                                                 .GetSnapshotAsync();

                // Combine results
                var allResults = titleQuery.Documents.Concat(genreQuery.Documents).Concat(artistQuery.Documents);

                // Process results
                foreach (var doc in allResults)
                {
                    var track = doc.ConvertTo<Track>();
                    if (track != null && !searchedTracks.Contains(track) && !track.IsPrivate)
                    {
                        searchedTracks.Add(track);
                    }
                }

                Console.WriteLine($"Tracks found: {searchedTracks.Count}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchTracksAsync: {ex.Message}");
            }

            return searchedTracks;
        }

        private async Task LoadSearchResultsAsync(string keyword)
        {
            if (isLoading) return;

            isLoading = true;
            try
            {
                await ShowLoadingStateAsync(true);

                DispatcherQueue.TryEnqueue(() => { Tracks.Clear(); });

                // Get search results
                var searchResults = await SearchTracksAsync(keyword);

                // Update UI
                DispatcherQueue.TryEnqueue(() =>
                {
                    foreach (var track in searchResults)
                    {
                        Tracks.Add(track);
                    }

                    NoTracksMessage.Visibility = Tracks.Count > 0 ? Visibility.Collapsed : Visibility.Visible;
                    TracksListView.Visibility = Tracks.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LoadSearchResultsAsync: {ex.Message}");
                await ShowErrorDialog("Failed to load search results: " + ex.Message);
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

        private void ShowNoTracksMessage(string message = null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NoTracksMessage.Text = message ?? "No tracks found";
                NoTracksMessage.Visibility = Visibility.Visible;
                TracksListView.Visibility = Visibility.Collapsed;
            });
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

        private Task DownloadTrackAsync(Track track)
        {
            // Implement the download logic
            Debug.WriteLine($"Downloading track: {track.Title}");
            return Task.CompletedTask;
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