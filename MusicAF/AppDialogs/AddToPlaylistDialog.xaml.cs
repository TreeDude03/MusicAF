using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MusicAF.Models;
using MusicAF.ThirdPartyServices;

namespace MusicAF.AppDialogs
{
    public sealed partial class AddToPlaylistDialog : ContentDialog
    {
        private readonly string _playlistId;
        private readonly FirestoreService _firestoreService;
        private ObservableCollection<Track> _allTracks;
        public ObservableCollection<Track> FilteredTracks { get; private set; }

        public AddToPlaylistDialog(string playlistId)
        {
            this.InitializeComponent();
            _playlistId = playlistId;
            _firestoreService = FirestoreService.Instance;
            _allTracks = new ObservableCollection<Track>();
            FilteredTracks = new ObservableCollection<Track>();
        }

        private async Task LoadAllTracksAsync(string query)
        {
            if (string.IsNullOrEmpty(query))
            {
                FilteredTracks.Clear();
                SearchResultsListView.ItemsSource = FilteredTracks;
                return;
            }

            try
            {
                var tracksRef = _firestoreService.FirestoreDb.Collection("tracks");
                var querySnapshot = await tracksRef.GetSnapshotAsync();

                _allTracks.Clear();

                foreach (var doc in querySnapshot.Documents)
                {
                    if (doc.Exists)
                    {
                        var track = doc.ConvertTo<Track>();
                        if (track != null && !track.IsPrivate)
                        {
                            _allTracks.Add(track);
                        }
                    }
                }

                UpdateFilteredTracks(query);
            }
            catch (Exception ex)
            {
                ShowErrorMessage("Failed to load tracks: " + ex.Message);
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchTextBox.Text.Trim();
            _ = LoadAllTracksAsync(query);
        }

        private void UpdateFilteredTracks(string query)
        {
            FilteredTracks.Clear();

            var filtered = _allTracks.Where(track =>
                (track.Title?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (track.Artist?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0) ||
                (track.Genre?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0));

            foreach (var track in filtered)
            {
                FilteredTracks.Add(track);
            }

            SearchResultsListView.ItemsSource = FilteredTracks;
        }

        private async void AddTrackButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Track track)
            {
                try
                {
                    var playlistRef = _firestoreService.FirestoreDb.Collection("playlists").Document(_playlistId);
                    var playlistSnapshot = await playlistRef.GetSnapshotAsync();

                    if (!playlistSnapshot.Exists)
                    {
                        ShowErrorMessage("Playlist not found.");
                        return;
                    }

                    var trackIds = playlistSnapshot.ContainsField("TrackIds")
                        ? playlistSnapshot.GetValue<System.Collections.Generic.List<string>>("TrackIds")
                        : new System.Collections.Generic.List<string>();

                    if (trackIds.Contains(track.SongId))
                    {
                        ShowErrorMessage("Track is already in the playlist.");
                        return;
                    }

                    trackIds.Add(track.SongId);
                    await playlistRef.UpdateAsync("TrackIds", trackIds);

                    ShowErrorMessage("Track added successfully!", isError: false);
                }
                catch (Exception ex)
                {
                    ShowErrorMessage("Failed to add track: " + ex.Message);
                }
            }
        }

        private void ShowErrorMessage(string message, bool isError = true)
        {
            ErrorMessageTextBlock.Text = message;
            ErrorMessageTextBlock.Visibility = Visibility.Visible;
            //ErrorMessageTextBlock.Foreground = isError ? new Windows.UI.Xaml.Media.Brush(Windows.UI.Colors.Red) : new Windows.UI.Xaml.Media.Brush(Windows.UI.Colors.Green);
        }
    }
}
