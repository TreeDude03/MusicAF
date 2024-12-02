using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;

namespace MusicAF.ThirdPartyServices
{
    public class GoogleDriveService : GoogleService
    {
        private DriveService _driveService;
        private static GoogleDriveService _instance;
        private GoogleCredential _credential;
        public static GoogleDriveService Instance => _instance ??= new GoogleDriveService();

        private GoogleDriveService()
        {
            _ = Initialize();
        }

        private async Task Initialize()
        {
            try { 
            //{
            //    _credential = await GetGoogleCredentialAsync();
            //    _credential
            //        .CreateScoped(new[]
            //        {
            //            DriveService.ScopeConstants.DriveFile,
            //            DriveService.ScopeConstants.DriveReadonly
            //        });

            //    _driveService = new DriveService(new BaseClientService.Initializer()
            //    {
            //        HttpClientInitializer = _credential,
            //        ApplicationName = "MusicAF"
            //    });

                _credential = GoogleCredential.FromFile("C:\\Users\\ACER\\source\\repos\\MusicAF\\MusicAF\\Assets\\config.json")
                   .CreateScoped(new[]
                   {
                        DriveService.ScopeConstants.DriveFile,
                        DriveService.ScopeConstants.DriveReadonly
                   });

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "MusicAF"
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Drive service: {ex.Message}");
                throw;
            }
        }

        public async Task<(string fileId, string webViewLink)> UploadFileAsync(string filePath, string fileName)
        {
            try
            {
                var fileMetadata = new Google.Apis.Drive.v3.Data.File()
                {
                    Name = fileName,
                    MimeType = "audio/mpeg"
                };

                using var stream = new FileStream(filePath, FileMode.Open);
                var request = _driveService.Files.Create(fileMetadata, stream, "audio/mpeg");
                request.Fields = "id, webViewLink";

                // Create upload progress event handler
                request.ProgressChanged += (progress) =>
                {
                    switch (progress.Status)
                    {
                        case UploadStatus.Uploading:
                            Debug.WriteLine($"Uploading: {progress.BytesSent} bytes sent");
                            break;
                        case UploadStatus.Failed:
                            Debug.WriteLine($"Upload failed: {progress.Exception?.Message}");
                            break;
                        case UploadStatus.Completed:
                            Debug.WriteLine("Upload completed!");
                            break;
                    }
                };

                // Upload the file
                var response = await request.UploadAsync(CancellationToken.None);

                if (response.Status == UploadStatus.Failed)
                {
                    throw new Exception($"Upload failed: {response.Exception.Message}");
                }

                var file = request.ResponseBody;

                // Make the file publicly accessible
                var permission = new Google.Apis.Drive.v3.Data.Permission
                {
                    Type = "anyone",
                    Role = "reader"
                };

                await _driveService.Permissions.Create(permission, file.Id).ExecuteAsync();

                // Get the updated file with sharing links
                var getRequest = _driveService.Files.Get(file.Id);
                getRequest.Fields = "id, webViewLink, webContentLink";
                var updatedFile = await getRequest.ExecuteAsync();

                Debug.WriteLine($"File uploaded successfully. ID: {updatedFile.Id}");
                Debug.WriteLine($"Web View Link: {updatedFile.WebViewLink}");
                Debug.WriteLine($"Download Link: {updatedFile.WebContentLink}");

                // Store both links
                return (updatedFile.Id, updatedFile.WebViewLink ?? updatedFile.WebViewLink);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error uploading file: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task<string> GetDownloadUrlAsync(string fileId)
        {
            try
            {
                Debug.WriteLine($"Getting download URL for file ID: {fileId}");

                // Get file metadata
                var getRequest = _driveService.Files.Get(fileId);
                getRequest.Fields = "id, name, mimeType, webContentLink";
                var file = await getRequest.ExecuteAsync();

                Debug.WriteLine($"File metadata retrieved. Name: {file.Name}, MimeType: {file.MimeType}");

                // Get fresh access token
                var accessToken = await GetAccessTokenAsync();
                Debug.WriteLine("Access token retrieved");

                // Create direct download URL
                var downloadUrl = $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";

                // Verify the URL is accessible
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

                    var response = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        Debug.WriteLine($"URL verification failed. Status: {response.StatusCode}");
                        throw new Exception($"Unable to access file. Status: {response.StatusCode}");
                    }
                    Debug.WriteLine("URL verified successfully");
                }

                Debug.WriteLine($"Final download URL: {downloadUrl}");
                return downloadUrl;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error in GetDownloadUrlAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<string> GetAccessTokenAsync()
        {
            try
            {
                if (_credential == null)
                {
                    throw new InvalidOperationException("Credential is not initialized");
                }

                // Get a fresh access token
                var token = await _credential.UnderlyingCredential.GetAccessTokenForRequestAsync();
                Debug.WriteLine("Successfully retrieved access token");
                return token;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting access token: {ex.Message}");
                throw;
            }
        }

        public async Task DeleteFileAsync(string fileId)
        {
            try
            {
                var request = _driveService.Files.Delete(fileId);
                await request.ExecuteAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error deleting file: {ex.Message}");
                throw;
            }
        }
    }
}