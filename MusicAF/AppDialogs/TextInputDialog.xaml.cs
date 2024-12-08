using Microsoft.UI.Xaml.Controls;

namespace MusicAF.AppDialogs
{
    public sealed partial class TextInputDialog : ContentDialog
    {
        public TextInputDialog()
        {
            this.InitializeComponent();
        }

        public string PlaylistName => PlaylistNameTextBox.Text;
    }
}
