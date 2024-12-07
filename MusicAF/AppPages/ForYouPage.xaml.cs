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

namespace MusicAF.AppPages
{
    public sealed partial class ForYouPage : Page
    {
        private string currentUserEmail;
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

        public ForYouPage()
        {
            try
            {
                Console.WriteLine("Initializing ForYouPage");
                this.InitializeComponent();
                _firestoreService = FirestoreService.Instance;
                Tracks = new ObservableCollection<Track>();
                Console.WriteLine("ForYouPage initialized successfully");
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

        private async Task LoadTracksAsync()
        {
            if (isLoading) return;

            isLoading = true;
            try
            {
                Console.WriteLine($"Loading recommended tracks for user: {currentUserEmail}");
                await ShowLoadingStateAsync(true);

                DispatcherQueue.TryEnqueue(() =>
                {
                    Tracks.Clear();
                });

                var tracksRef = _firestoreService.FirestoreDb.Collection("tracks");
                var querySnapshot = await tracksRef.GetSnapshotAsync();

                Console.WriteLine($"Found {querySnapshot.Documents.Count} tracks");

                foreach (var doc in querySnapshot.Documents)
                {
                    try
                    {
                        var track = doc.ConvertTo<Track>();
                        if (track != null)
                        {
                            Console.WriteLine($"Processing track: {track.Title}");

                            // Ensure all required properties are populated
                            track.Title = track.Title ?? "Untitled";
                            track.Artist = track.Artist ?? "Unknown Artist";
                            track.Album = track.Album ?? "Unknown Album";
                            track.Genre = track.Genre ?? "Unknown Genre";

                            DispatcherQueue.TryEnqueue(() =>
                            {
                                try
                                {
                                    Tracks.Add(track);
                                    Console.WriteLine($"Added track to UI: {track.Title}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Error adding track to collection: {ex}");
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error converting document: {ex}");
                    }
                }

                DispatcherQueue.TryEnqueue(() =>
                {
                    try
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
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error updating UI visibility: {ex}");
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

                    // Navigate to NowPlayingPage
                    Frame.Navigate(typeof(NowPlayingPage), (_track: track, _email: currentUserEmail));

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

        private async void UploadMusicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dialog = new UploadMusicDialog(App.MainWindow, currentUserEmail);
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
                Console.WriteLine("Navigation to ForYouPage");

                if (e.Parameter is string userEmail)
                {
                    currentUserEmail = userEmail;
                    Console.WriteLine($"User email received: {userEmail}");
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
                NoTracksMessage.Text = message ?? "No tracks found. Click the 'Upload music' button to add some tracks.";
                NoTracksMessage.Visibility = Visibility.Visible;
                TracksListView.Visibility = Visibility.Collapsed;
            });
        }
    }
}