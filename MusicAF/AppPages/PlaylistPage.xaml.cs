using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using MusicAF.Models;
using MusicAF.AppDialogs;
using MusicAF.ThirdPartyServices;
using static System.Net.Mime.MediaTypeNames;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Xml.Linq;

namespace MusicAF.AppPages
{
    public sealed partial class PlaylistPage : Page
    {
        private readonly FirestoreService _firestoreService;
        private string currentUserEmail;

        public ObservableCollection<Playlist> Playlists { get; set; }

        public PlaylistPage()
        {
            this.InitializeComponent();
            _firestoreService = FirestoreService.Instance;
            Playlists = new ObservableCollection<Playlist>();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            if (e.Parameter is string userEmail)
            {
                currentUserEmail = userEmail;
                _ = LoadPlaylistsAsync();
            }
        }

        private async Task LoadPlaylistsAsync()
        {
            try
            {
                Playlists.Clear();
                var playlistsRef = _firestoreService.FirestoreDb.Collection("playlists");
                var query = playlistsRef.WhereEqualTo("Owner", currentUserEmail);
                var querySnapshot = await query.GetSnapshotAsync();

                foreach (var doc in querySnapshot.Documents)
                {
                    // Manually map fields to the Playlist object
                    var playlist = new Playlist
                    {
                        Id = doc.Id,
                        Name = doc.ContainsField("Name") ? doc.GetValue<string>("Name") : string.Empty,
                        Owner = doc.ContainsField("Owner") ? doc.GetValue<string>("Owner") : string.Empty,
                        CreatedAt = doc.ContainsField("CreatedAt") ? doc.GetValue<DateTime>("CreatedAt") : DateTime.MinValue
                    };
                    Playlists.Add(playlist);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading playlists: {ex}");
            }
        }


        private async void AddPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            // Create and configure the dialog
            var dialog = new TextInputDialog();
            dialog.XamlRoot = this.XamlRoot; // Set the correct XamlRoot

            // Show the dialog and check if the user confirmed
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                // Get the input text from the dialog
                string playlistName = dialog.PlaylistName.Trim();

                if (!string.IsNullOrEmpty(playlistName))
                {
                    var newPlaylist = new Playlist
                    {
                        Name = playlistName,
                        Owner = currentUserEmail,
                        CreatedAt = DateTime.UtcNow,
                    };

                    try
                    {
                        // Add the playlist to Firestore
                        var playlistsRef = _firestoreService.FirestoreDb.Collection("playlists");
                        var addedDoc = await playlistsRef.AddAsync(new { Name = playlistName, Owner = currentUserEmail, CreatedAt = DateTime.UtcNow });

                        // Update the playlist with its Firestore ID and add it to the UI
                        newPlaylist.Id = addedDoc.Id;
                        Playlists.Add(newPlaylist);
                        await LoadPlaylistsAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error adding playlist: {ex.Message}");
                    }
                }
            }
        }


        private void PlaylistGoTo_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Playlist playlist)
            {
                Frame.Navigate(typeof(SinglePlaylistPage), (currentUserEmail, playlist.Id, playlist.Name));
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

    public class Playlist
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
