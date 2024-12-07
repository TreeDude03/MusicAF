// Track.cs
using System;
using Google.Cloud.Firestore;

namespace MusicAF.Models
{
    [FirestoreData]
    public class Track
    {
        [FirestoreProperty]
        public string SongId { get; set; }

        [FirestoreProperty]
        public string Title { get; set; }

        [FirestoreProperty]
        public string Artist { get; set; }

        [FirestoreProperty]
        public string Album { get; set; }

        [FirestoreProperty]
        public string Genre { get; set; }

        [FirestoreProperty]
        public long LengthInSeconds { get; set; }

        [FirestoreProperty]
        public string Uploader { get; set; }

        [FirestoreProperty]
        public int Streams { get; set; }

        [FirestoreProperty]
        public int Likes { get; set; }

        [FirestoreProperty]
        public int Saves { get; set; }

        [FirestoreProperty]
        public bool IsPrivate { get; set; }

        // Change from decimal to double
        [FirestoreProperty]
        public double DownloadPrice { get; set; }

        [FirestoreProperty]
        public bool AllowDownload { get; set; }

        [FirestoreProperty]
        public DateTime UploadDate { get; set; }

        [FirestoreProperty]
        public FileDetails FileDetails { get; set; }

        [FirestoreProperty]
        public string AudioFileUrl { get; set; }

        [FirestoreProperty]
        public string DriveFileId { get; set; }

        [FirestoreProperty]
        public string DriveWebViewLink { get; set; }

        public bool CanDownload => AllowDownload && DownloadPrice == 0;
        public override bool Equals(object obj)
        {
            if (obj is not Track other) return false;
            return this.SongId == other.SongId; // Compare by unique identifier
        }

        public override int GetHashCode()
        {
            return SongId?.GetHashCode() ?? 0; // Use SongId for hashing
        }

    }

    [FirestoreData]
    public class FileDetails
    {
        [FirestoreProperty]
        public string FileName { get; set; }

        [FirestoreProperty]
        public string FileType { get; set; }

        [FirestoreProperty]
        public ulong FileSize { get; set; }

        [FirestoreProperty]
        public string StoragePath { get; set; }

        [FirestoreProperty]
        public string MimeType { get; set; }


    }
}