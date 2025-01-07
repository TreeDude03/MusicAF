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
using MusicAF.AppDialogs;
using MusicAF.AppWindows;
using System.Collections.Generic;

namespace MusicAF.AppPages
{
    public sealed partial class SinglePlaylistPage : Page
    {
        private string currentUserEmail;
        private string playlistId;
        private string playlistName;
        private readonly Window _window;
        private readonly FirestoreService _firestoreService;
        private bool isLoading;

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

        public SinglePlaylistPage()
        {
            try
            {
                Console.WriteLine("Initializing SinglePlaylistPage");
                this.InitializeComponent();
                _firestoreService = FirestoreService.Instance;
                Tracks = new ObservableCollection<Track>();
                Console.WriteLine("SinglePlaylistPage initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing SinglePlaylistPage: {ex}");
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

        private async Task LoadTracksAsync()
        {
            if (isLoading) return;

            isLoading = true;
            try
            {
                await ShowLoadingStateAsync(true);

                // Clear existing tracks
                DispatcherQueue.TryEnqueue(() =>
                {
                    Tracks.Clear();
                });

                // Step 1: Fetch track IDs from the playlist document
                var playlistRef = _firestoreService.FirestoreDb.Collection("playlists").Document(playlistId);
                var playlistDoc = await playlistRef.GetSnapshotAsync();

                if (!playlistDoc.Exists)
                {
                    Console.WriteLine($"Playlist with ID {playlistId} not found.");
                    ShowNoTracksMessage("Playlist not found.");
                    return;
                }

                if (!playlistDoc.ContainsField("TrackIds"))
                {
                    Console.WriteLine("No TrackIds field in playlist document.");
                    ShowNoTracksMessage("No tracks found in this playlist.");
                    return;
                }

                var trackIds = playlistDoc.GetValue<List<string>>("TrackIds");

                foreach (var trackId in trackIds)
                {
                    try
                    {
                        var trackRef = _firestoreService.FirestoreDb.Collection("tracks").Document(trackId);
                        var trackDoc = await trackRef.GetSnapshotAsync();

                        if (trackDoc.Exists)
                        {
                            var track = trackDoc.ConvertTo<Track>();

                            DispatcherQueue.TryEnqueue(() =>
                            {
                                Tracks.Add(track);
                            });
                        }
                        else
                        {
                            Console.WriteLine($"Track with ID {trackId} does not exist.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error fetching track with ID {trackId}: {ex}");
                    }
                }

                // Update UI based on whether tracks were found
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (Tracks.Count > 0)
                    {
                        NoTracksMessage.Visibility = Visibility.Collapsed;
                        TracksListView.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        NoTracksMessage.Visibility = Visibility.Visible;
                        TracksListView.Visibility = Visibility.Collapsed;
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"LoadTracksAsync error: {ex}");
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
                    // Create instance of NowPlayingPage
                    var nowPlayingPage = new NowPlayingPage();

                    Frame.Navigate(typeof(NowPlayingPage), (_track: track, _email: currentUserEmail));

                    // Play the selected track
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

        private async void AddToPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new AddToPlaylistDialog(playlistId);
                dialog.XamlRoot = this.XamlRoot;
                await dialog.ShowAsync();
                await LoadTracksAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UploadMusicButton_Click error: {ex}");
                await ShowErrorDialog("Failed to open upload dialog");
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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            try
            {
                base.OnNavigatedTo(e);
                Console.WriteLine("Navigation to SinglePlaylistPage");

                if (e.Parameter is ValueTuple<string, string, string> parameters)
                {
                    playlistId = parameters.Item2;
                    currentUserEmail = parameters.Item1;
                    playlistName = parameters.Item3;
                    PageName.Text = playlistName;
                    _ = LoadTracksAsync();
                    
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

        private void ShowNoTracksMessage(string message = null)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                NoTracksMessage.Text = message ?? "No tracks found";
                NoTracksMessage.Visibility = Visibility.Visible;
                TracksListView.Visibility = Visibility.Collapsed;
            });
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