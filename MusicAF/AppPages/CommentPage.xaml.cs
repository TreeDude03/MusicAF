using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.ObjectModel;
using MusicAF.Models;
using MusicAF.ThirdPartyServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Linq;

namespace MusicAF.AppPages
{
    public sealed partial class CommentPage : Page
    {
        private Track currentTrack;
        private string commentEmail;
        private readonly FirestoreService firestoreService;
        public ObservableCollection<Comment> Comments { get; set; }

        public CommentPage()
        {
            this.InitializeComponent();
            firestoreService = FirestoreService.Instance;
            Comments = new ObservableCollection<Comment>();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is ValueTuple<Track, string> parameters)
            {
                currentTrack = parameters.Item1;
                commentEmail = parameters.Item2;

                Debug.WriteLine($"Current commenter: {commentEmail}");

                await LoadCommentsAsync();
            }
        }

        private async Task LoadCommentsAsync()
        {
            try
            {
                var trackId = currentTrack.SongId;

                // Get comments from the Firestore subcollection
                var commentsSnapshot = await firestoreService.GetCollectionAsync($"Comments/{trackId}/List");

                Comments.Clear();

                foreach (var document in commentsSnapshot.Documents)
                {
                    var commentData = document.ToDictionary();
                    var comment = new Comment
                    {
                        User = commentData.TryGetValue("User", out var user) ? user.ToString() : "Anonymous",
                        Content = commentData.TryGetValue("Content", out var content) ? content.ToString() : "",
                        DatePosted = commentData.TryGetValue("Timestamp", out var timestamp) && timestamp is DateTime time ? time : DateTime.UtcNow
                    };
                    Comments.Add(comment);
                }

                CommentsListView.ItemsSource = Comments;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading comments: {ex.Message}");
            }
        }


        private async void PostComment_Click(object sender, RoutedEventArgs e)
        {
            var newCommentText = CommentTextBox.Text?.Trim();

            if (string.IsNullOrEmpty(newCommentText))
            {
                return;
            }

            try
            {
                var newComment = new Comment
                {
                    User = commentEmail,
                    Content = newCommentText,
                    DatePosted = DateTime.UtcNow
                };

                // Add comment to Firestore
                var trackId = currentTrack.SongId;
                var commentData = new Dictionary<string, object>
                {
                    { "User", newComment.User },
                    { "Content", newComment.Content },
                    { "Timestamp", newComment.DatePosted }
                };

                await firestoreService.AddDocumentAsync($"Comments/{trackId}/List", Guid.NewGuid().ToString(), commentData);

                // Update the local collection
                Comments.Add(newComment);

                // Clear the input box
                CommentTextBox.Text = string.Empty;

                Debug.WriteLine("Comment added successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error posting comment: {ex.Message}");
            }
        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.GoBack();
        }
    }

    public class Comment
    {
        public string User { get; set; }
        public string Content { get; set; }
        public DateTime DatePosted { get; set; }
    }
}