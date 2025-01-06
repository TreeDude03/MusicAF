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
                    content.Add(new StringContent("apple_music,spotify"), "return");

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
            // Example: Deserialize and check the response
            dynamic result = Newtonsoft.Json.JsonConvert.DeserializeObject(jsonResponse);

            if (result.status == "success" && result.result != null)
            {
                string detectedPlatforms = result.result.apple_music != null ? "Apple Music" : "";
                detectedPlatforms += result.result.spotify != null ? " and Spotify" : "";

                return !string.IsNullOrEmpty(detectedPlatforms)
                    ? true
                    : false;
            }
            else
            {
                return true;
            }
        }
    }

    public class DetectionResult
    {

    }
}
