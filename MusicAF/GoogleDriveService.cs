using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MusicAF
{
    public class GoogleDriveService
    {
        private DriveService _driveService;
        private static GoogleDriveService _instance;
        public static GoogleDriveService Instance => _instance ??= new GoogleDriveService();

        private GoogleDriveService()
        {
            Initialize();
        }

        private void Initialize()
        {
            try
            {
                var credential = GoogleCredential.FromFile("C:\\Users\\ACER\\source\\repos\\MusicAF\\MusicAF\\Assets\\config.json")
                    .CreateScoped(new[]
                    {
                DriveService.ScopeConstants.DriveFile,
                DriveService.ScopeConstants.DriveReadonly
                    });

                _driveService = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
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
                request.ProgressChanged += (IUploadProgress progress) =>
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
                var request = _driveService.Files.Get(fileId);
                var file = await request.ExecuteAsync();

                // Create a downloadable URL
                return $"https://www.googleapis.com/drive/v3/files/{fileId}?alt=media";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting download URL: {ex.Message}");
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