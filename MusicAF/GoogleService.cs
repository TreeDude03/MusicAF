using Google.Apis.Auth.OAuth2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace MusicAF
{
    public class GoogleService
    {
        public async Task<GoogleCredential> GetGoogleCredentialAsync()
        {
            // Access the file using ms-appx:
            StorageFile file = await StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///Assets/config.json"));

            // Read the file content
            using (Stream stream = await file.OpenStreamForReadAsync())
            {
                // Create credential from the stream
                return GoogleCredential.FromStream(stream);
            }
        }
    }
}
