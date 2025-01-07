using Google.Cloud.Firestore;
using MusicAF.Models;
using MusicAF.ThirdPartyServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicAF.Helpers
{
    public class PlaybackService
    {
        private readonly FirestoreService _firestoreService = FirestoreService.Instance;
        public event Action<Track> TrackChanged;
        private Track _currentTrack;

        private List<Track> _currentTrackList;
        private int _currentTrackIndex;

        public Track CurrentTrack
        {
            get => _currentTrack;
            private set => _currentTrack = value;
        }

        public event Action TimerEnded;

        public void TriggerTimerEnded()
        {
            TimerEnded?.Invoke();
        }

        public void SetTrackList(List<Track> trackList, Track track)
        {
            Console.WriteLine(trackList.Count);
            _currentTrackList = trackList;
            // Find the index of the specified track in the list
            _currentTrackIndex = GetTrackIndexById(trackList, track.SongId);

            // If the track is not found, default to the first track
            if (_currentTrackIndex == -1)
            {
                Console.WriteLine("Specified track not found in the list. Starting with the first track.");
                _currentTrackIndex = 0;
            }
            else
            {
                Console.WriteLine($"Current track index set to: {_currentTrackIndex} ({track.Title})");
            }
        }

        public void PlayNextTrack()
        {
            if (_currentTrackList == null || _currentTrackList.Count == 0) return;

            _currentTrackIndex = (_currentTrackIndex + 1) % _currentTrackList.Count;
            Console.WriteLine(_currentTrackList[_currentTrackIndex].Title);
            PlayTrack(_currentTrackList[_currentTrackIndex]);
        }

        public void PlayPreviousTrack()
        {
            if (_currentTrackList == null || _currentTrackList.Count == 0) return;

            _currentTrackIndex = (_currentTrackIndex - 1 + _currentTrackList.Count) % _currentTrackList.Count;
            Console.WriteLine(_currentTrackList[_currentTrackIndex].Title);
            PlayTrack(_currentTrackList[_currentTrackIndex]);
        }

        public void PlayTrack(Track track)
        {
            _currentTrack = track;
            TrackChanged?.Invoke(track); // Notify listeners about the track change
        }

        
        public int GetTrackIndexById(List<Track> trackList, string songId)
        {
            if (trackList == null || string.IsNullOrWhiteSpace(songId))
            {
                Console.WriteLine("Invalid track list or songId.");
                return -1; // Indicate not found
            }

            // Search for the track by SongId
            int index = trackList.FindIndex(t => t.SongId == songId);

            if (index == -1)
            {
                Console.WriteLine($"Track with SongId '{songId}' not found in the list.");
            }
            else
            {
                Console.WriteLine($"Track found at index: {index} (SongId: {songId})");
            }

            return index;
        }


        public Track GetCurrentTrack() => _currentTrack;



        //update latest track for user
        private async Task UpdateUserLatestTrackAsync(string userEmail, string LatestTrackId)
        {
            try
            {
                var userRef = _firestoreService.FirestoreDb.Collection("users").Document(userEmail);

                // SetAsync to update LatestTrack
                await userRef.SetAsync(new { LatestTrackId }, SetOptions.MergeAll);

                Debug.WriteLine($"---Updated latest track for user {userEmail} to {LatestTrackId}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"---TracError updating latest track: {ex.Message}");
            }
        }
    }
}
