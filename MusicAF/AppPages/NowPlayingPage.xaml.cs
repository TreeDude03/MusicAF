using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Foundation;
using Microsoft.UI.Dispatching;
using System.Diagnostics;
using Windows.Web.Http;
using Windows.Web.Http.Headers;
using System.Net.Http; // We'll use System.Net.Http.HttpClient
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;
using Windows.Storage;
using Microsoft.UI.Xaml.Input;
using MusicAF.ThirdPartyServices;
using MusicAF.Models;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media;

namespace MusicAF.AppPages
{

    public sealed partial class NowPlayingPage : Page
    {
        private string currentUserEmail;
        private Track currentTrack;
        private MediaPlayer mediaPlayer;
        private GoogleDriveService _driveService;
        private DispatcherTimer _progressTimer;
        private string currentAccessToken;
        private System.Net.Http.HttpClient httpClient;
        private FirestoreService _firestoreService;

        //
        private string listenerEmail;

        public NowPlayingPage()
        {
            InitializeComponent();
            _firestoreService = FirestoreService.Instance;
            App.PlaybackService.TrackChanged += OnTrackChanged;
            _driveService = GoogleDriveService.Instance;
            httpClient = new System.Net.Http.HttpClient();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ValueTuple<Track, string> parameters)
            {
                currentTrack = parameters.Item1;
                // Store the user email from the track
                currentUserEmail = currentTrack.Uploader;
                //
                listenerEmail = parameters.Item2;

                var favoritesRef = _firestoreService.FirestoreDb.Collection("favorites").Document(listenerEmail);
                var favoritesDoc = await favoritesRef.GetSnapshotAsync();

                if (favoritesDoc.Exists)
                {
                    var favoriteTracks = favoritesDoc.GetValue<List<string>>("TrackIds");
                    if (favoriteTracks != null && favoriteTracks.Contains(currentTrack.SongId))
                    {
                        SetLikeButtonState(liked: true);
                    }
                }
                UpdateUI();
            }
        }
        private void SetLikeButtonState(bool liked)
        {
            var likeButton = LikeButton; // Assuming LikeButton is the name of your Button
            if (likeButton?.Content is FontIcon icon)
            {
                icon.Foreground = liked ? new SolidColorBrush(Microsoft.UI.Colors.Red) : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                likeButton.Background = liked ? new SolidColorBrush(Microsoft.UI.Colors.Red) : new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            }

            likeButton.IsEnabled = !liked; // Disable the button if already liked
        }
        private void UpdateUI()
        {
            if (TrackTitleText != null)
                TrackTitleText.Text = currentTrack.Title;

            if (ArtistText != null)
                ArtistText.Text = currentTrack.Artist;

            if (AlbumText != null)
                AlbumText.Text = currentTrack.Album ?? "No Album";
        }

        private void OnTrackChanged(Track track)
        {
            DispatcherQueue.TryEnqueue(() =>
            {

                currentTrack = track;
                UpdateUI();

                // Add your playback logic here
                Debug.WriteLine($"Now playing: {track.Title} by {track.Artist}");
            });
        }


        private async Task ShowErrorDialogAsync(string message)
        {
            try
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
            catch (Exception ex)
            {
                Debug.WriteLine($"Error showing dialog: {ex.Message}");
            }
        }


        private void Library_Tapped(object sender, TappedRoutedEventArgs e)
        {
            try
            {
                // Stop playback and clean up before navigating
                if (mediaPlayer != null)
                {
                    mediaPlayer.Pause();
                    mediaPlayer.Source = null;
                }

                _progressTimer?.Stop();

                // Navigate to MyLibraryPage
                Frame.Navigate(typeof(MyLibraryPage), currentUserEmail);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to library: {ex.Message}");
                ShowErrorDialogAsync($"Error navigating to library: {ex.Message}").ConfigureAwait(false);
            }
        }

        private void CommentIcon_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Navigate to CommentPage with the current track details
                Frame.Navigate(typeof(CommentPage), (_track: currentTrack, _commentUser: listenerEmail));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error navigating to comment page: {ex.Message}");
                ShowErrorDialogAsync($"Error navigating to comment page: {ex.Message}").ConfigureAwait(false);
            }
        }

        private void BackClick(object sender, RoutedEventArgs e)
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }

        private async void LikeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                try
                {
                    Console.WriteLine($"Starting document upload to {listenerEmail}");
                    string record = await _firestoreService.GetFieldFromDocumentAsync<string>("favorites", listenerEmail, "UserId");
                    if (record == null)
                    {
                        await _firestoreService.AddDocumentAsync("favorites", listenerEmail , new { UserId = listenerEmail, TrackIds = new List<string>() });

                    }

                    // Increment Likes for the track
                    var trackRef = _firestoreService.FirestoreDb.Collection("tracks").Document(currentTrack.SongId);
                    await _firestoreService.IncrementFieldAsync(trackRef, "Likes", 1);

                    // Add trackId to the user's Favorites
                    var userId = listenerEmail; // Assuming userId or email is stored globally
                    var favoritesRef = _firestoreService.FirestoreDb.Collection("favorites").Document(userId);
                    var favoritesDoc = await favoritesRef.GetSnapshotAsync();

                    if (favoritesDoc.Exists)
                    {
                        var favoriteTracks = favoritesDoc.GetValue<List<string>>("TrackIds");
                        if (!favoriteTracks.Contains(currentTrack.SongId))
                        {
                            favoriteTracks.Add(currentTrack.SongId);
                            await favoritesRef.UpdateAsync("TrackIds", favoriteTracks);

                            SetLikeButtonState(liked: true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle errors
                    Debug.WriteLine($"Error liking track: {ex.Message}");
                }
            }
        }

    }
}

