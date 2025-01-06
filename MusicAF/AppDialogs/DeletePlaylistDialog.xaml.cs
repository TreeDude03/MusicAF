using Microsoft.UI.Xaml.Controls;

namespace MusicAF.AppDialogs
{
    public sealed partial class DeletePlaylistDialog : ContentDialog
    {
        public string PlaylistName { get; set; }

        public DeletePlaylistDialog(string playlistName)
        {
            this.InitializeComponent();
            PlaylistName = playlistName;
            PlaylistNameText.Text = playlistName;
        }
    }
}
