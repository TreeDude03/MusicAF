using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using System.Diagnostics;
using MusicAF.Models;
using System.Linq;

namespace MusicAF.ThirdPartyServices
{
    public class FirestoreService : GoogleService
    {
        // Change to public property with private setter
        public FirestoreDb FirestoreDb { get; private set; }

        // Singleton instance
        private static FirestoreService _instance;
        public static FirestoreService Instance => _instance ??= new FirestoreService();

        private FirestoreService()
        {
            InitializeFirestore();
        }

        private async void InitializeFirestore()
        {
            try
            {
                // Obtain Google Credential
                GoogleCredential credential = await GetGoogleCredentialAsync();

                // Use the credential to initialize the Firestore client
                FirestoreClientBuilder clientBuilder = new FirestoreClientBuilder
                {
                    Credential = credential
                };

                FirestoreClient firestoreClient = clientBuilder.Build();
                string projectId = FirebaseConfig.ProjectId;
                FirestoreDb = FirestoreDb.Create(projectId, firestoreClient);

                Debug.WriteLine("Firestore initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error initializing Firestore: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        //Add document to Firestore
        public async Task AddDocumentAsync(string collection, string documentId, object data)
        {
            try
            {
                Console.WriteLine($"Starting document upload to {collection}/{documentId}");

                if (FirestoreDb == null)
                {
                    Console.WriteLine("Error: FirestoreDb is null");
                    throw new InvalidOperationException("Firestore database is not initialized");
                }

                DocumentReference docRef = FirestoreDb.Collection(collection).Document(documentId);
                await docRef.SetAsync(data);
                Console.WriteLine("Document added successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding document: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }

        public async Task IncrementFieldAsync(DocumentReference documentRef, string fieldName, int incrementBy)
        {
            try
            {
                var updates = new Dictionary<string, object>
            {
                { fieldName, FieldValue.Increment(incrementBy) }
            };
                await documentRef.UpdateAsync(updates);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to increment field '{fieldName}': {ex.Message}", ex);
            }
        }

        //Retrieve document
        public async Task<DocumentSnapshot> GetDocumentAsync<T>(string collection, string documentId)
        {
            try
            {
                DocumentReference docRef = FirestoreDb.Collection(collection).Document(documentId);
                DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
                if (snapshot.Exists)
                {
                    return snapshot;
                }
                else
                {
                    Console.WriteLine("Document not found.");
                    return default;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching document: {ex.Message}");
                return default;
            }
        }

        public async Task<QuerySnapshot> GetCollectionAsync(string collectionName, string fieldPath = null, object value = null)
        {
            try
            {
                CollectionReference collectionRef = FirestoreDb.Collection(collectionName);
                Query query = collectionRef;

                if (fieldPath != null && value != null)
                {
                    query = query.WhereEqualTo(fieldPath, value);
                }

                return await query.GetSnapshotAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error getting collection: {ex.Message}");
                throw;
            }
        }

        public async Task<T> GetFieldFromDocumentAsync<T>(string collectionName, string documentId, string fieldName)
        {
            DocumentSnapshot snapshot = await GetDocumentAsync<DocumentSnapshot>(collectionName, documentId);

            if (snapshot == null)
            {
                return default;
            }

            if (snapshot.Exists)
            {
                if (snapshot.TryGetValue(fieldName, out T fieldValue))
                {
                    return fieldValue;
                }
            }
            return default;
        }

        // Example method: Delete document from Firestore
        public async Task DeleteDocumentAsync(string collection, string documentId)
        {
            try
            {
                DocumentReference docRef = FirestoreDb.Collection(collection).Document(documentId);
                await docRef.DeleteAsync();
                Console.WriteLine("Document deleted successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting document: {ex.Message}");
                throw;
            }
        }

        //
        public async Task RecordKeywordsAsync(string email, Track track)
        {
            try
            {
                if (track == null || string.IsNullOrEmpty(email)) return;
                var keywords = new List<string> { track.Genre ?? "Unknown Genre", track.Artist ?? "Unknown Artist" };

                var statDocRef = FirestoreDb.Collection("StatRecords").Document(email);
                var statSnapshot = await statDocRef.GetSnapshotAsync();

                if (statSnapshot.Exists)
                {
                    // Get existing list of keywords
                    var data = statSnapshot.ToDictionary();
                    var keywordList = data.ContainsKey("List") ? data["List"] as List<object> : new List<object>();

                    // Convert to list of strings and trim to max 50 keywords
                    var currentKeywords = keywordList.Select(k => k.ToString()).ToList();

                    foreach (var keyword in keywords)
                    {
                        currentKeywords.Insert(0, keyword);
                    }
                    if (currentKeywords.Count > 50)
                        currentKeywords = currentKeywords.Take(50).ToList();
                    await statDocRef.UpdateAsync("List", currentKeywords);
                }
                else
                {
                    await statDocRef.SetAsync(new { List = keywords });
                }
                Console.WriteLine("Keywords recorded successfully.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error recording keywords: {ex.Message}");
            }
        }
    }

    public static class FirebaseConfig
    {
        public const string ApiKey = "AIzaSyDX9VYUOnkEI62tTdvbgw7TZijK806yGEg";
        public const string ProjectId = "musicaf-f6fb8";
    }
}


