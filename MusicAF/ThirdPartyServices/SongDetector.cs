using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

//USE AUDD TO DETECT SONGS

namespace MusicAF.ThirdPartyServices
{
    public class SongDetector
    {
        private readonly string _auddApiKey = "45c1ffb2d9476a688f9e8d3233ad48bc";
        private readonly HttpClient _httpClient;

        public SongDetector()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> DetectPlagiarismAsync(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("The specified file does not exist.");

            // Validate file format
            var extension = Path.GetExtension(filePath).ToLower();
            if (extension != ".mp3" && extension != ".wav")
                throw new ArgumentException("Only MP3 and WAV files are supported.");

            Console.WriteLine($"Processing file: {filePath}");

            try
            {
                using (var fileStream = File.OpenRead(filePath))
                using (var content = new MultipartFormDataContent())
                {
                    // Add the API token and additional parameters
                    content.Add(new StringContent(_auddApiKey), "api_token");
                    content.Add(new StringContent("apple_music,spotify,deezer,napster,musicbrainz"), "return");

                    // Add the file to the request
                    var fileContent = new StreamContent(fileStream);
                    fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
                    content.Add(fileContent, "file", Path.GetFileName(filePath));

                    // Send request to Audd.io API
                    Console.WriteLine("Sending file to Audd.io API...");
                    var response = await _httpClient.PostAsync("https://api.audd.io/", content);

                    if (!response.IsSuccessStatusCode)
                    {
                        //fail to analyze -> not detected -> allow upload.
                        return false;
                    }

                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return ProcessResponse(jsonResponse);
                }
            }
            catch (Exception)
            {
               return false;
            }
        }

        private bool ProcessResponse(string jsonResponse)
        {
            dynamic response = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);

            if (response != null && response.status == "success" && response.result != null)
            {
                var data = response.result;
                List<string> platforms = new List<string>();

                if (data.apple_music != null) platforms.Add("Apple Music");
                if (data.spotify != null) platforms.Add("Spotify");
                if (data.deezer != null) platforms.Add("Deezer");
                if (data.napster != null) platforms.Add("Napster");
                if (data.musicbrainz != null) platforms.Add("MusicBrainz");
                string detectedPlatformStr = platforms.Count > 0 
                    ? string.Join(", ", platforms.Take(platforms.Count - 1)) + (platforms.Count > 1 ? " and " : "") + platforms.Last() 
                    : "";

                string title = data.title;
                string artist = data.artist;

                string notify = string.IsNullOrEmpty(detectedPlatformStr)
                    ? $"This song is not recognized in multi-platform database."
                    : $"This song is recognized on {detectedPlatformStr}. Similar song: '{title}' by {artist}.";
                return !string.IsNullOrEmpty(detectedPlatformStr);
            }
            return true;
        }
    }

    public class DetectionResult
    {

    }
}
