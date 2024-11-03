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

namespace MusicAF
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
            _driveService = GoogleDriveService.Instance;
            httpClient = new System.Net.Http.HttpClient();
            InitializeMediaPlayer();
            InitializeProgressTimer();
        }

        private void InitializeMediaPlayer()
        {
            try
            {
                mediaPlayer = new MediaPlayer();
                mediaPlayer.AudioCategory = MediaPlayerAudioCategory.Media;
                mediaPlayer.CommandManager.IsEnabled = true;

                // Add media events
                mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;
                mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
                mediaPlayer.MediaOpened += (sender, args) =>
                {
                    Debug.WriteLine("Media opened successfully");
                };

                // Set initial volume
                if (VolumeSlider != null)
                {
                    mediaPlayer.Volume = VolumeSlider.Value / 100.0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing media player: {ex.Message}");
            }
        }

        protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
        {
            try
            {
                base.OnNavigatingFrom(e);

                _progressTimer?.Stop();
                if (mediaPlayer != null)
                {
                    mediaPlayer.Pause();
                    mediaPlayer.Source = null;
                    mediaPlayer.Dispose();
                    mediaPlayer = null;
                }

                // Dispose of HttpClient
                httpClient?.Dispose();

                // Clean up temp files
                _ = CleanupTempFilesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up resources: {ex.Message}");
            }
        }

        private async Task CleanupTempFilesAsync()
        {
            try
            {
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var files = await tempFolder.GetFilesAsync();
                foreach (var file in files)
                {
                    try
                    {
                        await file.DeleteAsync(StorageDeleteOption.PermanentDelete);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error deleting temp file: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cleaning up temp files: {ex.Message}");
            }
        }

        private void MediaPlayer_MediaEnded(MediaPlayer sender, object args)
        {
            Debug.WriteLine("Media playback ended");
            DispatcherQueue.TryEnqueue(() =>
            {
                _progressTimer.Stop();
                UpdatePlayPauseButton(false);
                if (ProgressSlider != null)
                    ProgressSlider.Value = 0;
                if (CurrentTimeText != null)
                    CurrentTimeText.Text = "00:00";
            });
        }

        private void InitializeProgressTimer()
        {
            _progressTimer = new DispatcherTimer();
            _progressTimer.Interval = TimeSpan.FromMilliseconds(500);
            _progressTimer.Tick += ProgressTimer_Tick;
        }

        private void ProgressTimer_Tick(object sender, object e)
        {
            if (mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds > 0)
            {
                var progress = (mediaPlayer.PlaybackSession.Position.TotalSeconds /
                              mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds) * 100;

                if (ProgressSlider != null)
                {
                    ProgressSlider.Value = progress;
                }

                if (CurrentTimeText != null)
                {
                    CurrentTimeText.Text = string.Format("{0:mm\\:ss}",
                        mediaPlayer.PlaybackSession.Position);
                }
            }
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
                _ = PlayTrack();
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

        private async Task PlayTrack()
        {
            try
            {
                if (LoadingProgressRing != null)
                    LoadingProgressRing.IsActive = true;

                if (PlayPauseButton != null)
                    PlayPauseButton.IsEnabled = false;

                // Get the direct download URL and access token
                var accessToken = await _driveService.GetAccessTokenAsync();
                var downloadUrl = $"https://www.googleapis.com/drive/v3/files/{currentTrack.DriveFileId}?alt=media";

                // Set up HTTP client with authentication
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                // Download the audio file to local storage first
                var localFile = await DownloadFileAsync(downloadUrl);

                // Create media source from local file
                var streamRef = RandomAccessStreamReference.CreateFromFile(localFile);
                var stream = await streamRef.OpenReadAsync();

                // Create and set up media source
                var mediaSource = MediaSource.CreateFromStream(stream, "audio/mpeg");

                // Handle media source events
                mediaSource.OpenOperationCompleted += (sender, args) =>
                {
                    Debug.WriteLine($"Media source open completed with status");
                };

                // Set the source and play
                mediaPlayer.Source = mediaSource;
                await Task.Delay(500); // Small delay to ensure source is set

                Debug.WriteLine("Starting playback");
                mediaPlayer.Play();

                _progressTimer.Start();
                UpdatePlayPauseButton(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error playing track: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await ShowErrorDialogAsync($"Failed to play track: {ex.Message}");
            }
            finally
            {
                if (LoadingProgressRing != null)
                    LoadingProgressRing.IsActive = false;
                if (PlayPauseButton != null)
                    PlayPauseButton.IsEnabled = true;
            }
        }

        private async Task<StorageFile> DownloadFileAsync(string url)
        {
            try
            {
                // Get temp folder
                var tempFolder = ApplicationData.Current.TemporaryFolder;
                var fileName = $"{Guid.NewGuid()}.mp3";
                var tempFile = await tempFolder.CreateFileAsync(fileName, CreationCollisionOption.ReplaceExisting);

                // Download the file
                var response = await httpClient.GetAsync(url);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsByteArrayAsync();

                // Write to temp file
                await FileIO.WriteBytesAsync(tempFile, content);
                Debug.WriteLine($"File downloaded to: {tempFile.Path}");

                return tempFile;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading file: {ex.Message}");
                throw;
            }
        }


        private async Task<string> GetMediaUrlWithAccessTokenAsync(string downloadUrl)
        {
            try
            {
                // Get fresh access token
                var accessToken = await _driveService.GetAccessTokenAsync();

                // Add access token to URL
                var uriBuilder = new UriBuilder(downloadUrl);
                var query = System.Web.HttpUtility.ParseQueryString(uriBuilder.Query);
                query["access_token"] = accessToken;
                uriBuilder.Query = query.ToString();

                return uriBuilder.ToString();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting media URL: {ex.Message}");
                throw;
            }
        }

        private void MediaPlayer_MediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
        {
            Debug.WriteLine($"Media playback failed: {args.Error.ToString()}");
            Debug.WriteLine($"Extended error code: {args.ExtendedErrorCode}");
            Debug.WriteLine($"HRESULT: 0x{args.ExtendedErrorCode:X8}");

            DispatcherQueue.TryEnqueue(async () =>
            {
                _progressTimer.Stop();
                UpdatePlayPauseButton(false);
                if (LoadingProgressRing != null)
                    LoadingProgressRing.IsActive = false;
                if (PlayPauseButton != null)
                    PlayPauseButton.IsEnabled = true;

                var errorMessage = $"Media playback failed: {args.Error.ToString()}\n" +
                                  $"Error Code: 0x{args.ExtendedErrorCode:X8}";
                await ShowErrorDialogAsync(errorMessage);
            });
        }
        // Add these helper methods for better diagnostics
        private void LogMediaPlaybackState()
        {
            if (mediaPlayer?.PlaybackSession != null)
            {
                Debug.WriteLine($"Playback State: {mediaPlayer.PlaybackSession.PlaybackState}");
                Debug.WriteLine($"Playback Rate: {mediaPlayer.PlaybackSession.PlaybackRate}");
                Debug.WriteLine($"Position: {mediaPlayer.PlaybackSession.Position}");
                Debug.WriteLine($"Natural Duration: {mediaPlayer.PlaybackSession.NaturalDuration}");
            }
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (mediaPlayer?.PlaybackSession == null)
                {
                    Debug.WriteLine("PlaybackSession is null");
                    return;
                }

                if (mediaPlayer.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
                {
                    Debug.WriteLine("Pausing playback");
                    mediaPlayer.Pause();
                    _progressTimer.Stop();
                    UpdatePlayPauseButton(false);
                }
                else
                {
                    Debug.WriteLine("Starting playback");
                    mediaPlayer.Play();
                    _progressTimer.Start();
                    UpdatePlayPauseButton(true);
                }

                LogMediaPlaybackState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in PlayPauseButton_Click: {ex.Message}");
            }
        }

        private void UpdatePlayPauseButton(bool isPlaying)
        {
            if (PlayPauseButton?.Content is FontIcon playPauseIcon)
            {
                playPauseIcon.Glyph = isPlaying ? "\uE769" : "\uE768"; // Pause : Play
            }
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


        private void ProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (mediaPlayer?.PlaybackSession != null &&
                mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds > 0)
            {
                var position = TimeSpan.FromSeconds(
                    (e.NewValue / 100.0) * mediaPlayer.PlaybackSession.NaturalDuration.TotalSeconds);
                mediaPlayer.PlaybackSession.Position = position;
            }
        }

        private void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            if (mediaPlayer != null)
            {
                mediaPlayer.Volume = e.NewValue / 100.0;
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
    }
}

