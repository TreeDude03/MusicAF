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

        public NowPlayingPage()
        {
            this.InitializeComponent();
            App.PlaybackService.TrackChanged += OnTrackChanged;
            _driveService = GoogleDriveService.Instance;
            httpClient = new System.Net.Http.HttpClient();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Track track)
            {
                currentTrack = track;
                // Store the user email from the track
                currentUserEmail = track.Uploader;
                UpdateUI();
            }
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
        private void ForYouButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(ForYouPage), currentUserEmail);
        }
        private void LibraryButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(MyLibraryPage), currentUserEmail);
        }

    }
}

