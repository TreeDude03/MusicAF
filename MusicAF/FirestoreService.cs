using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Firestore.V1;
using System.Diagnostics;

namespace MusicAF
{
    public class FirestoreService
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

        private void InitializeFirestore()
        {
            GoogleCredential credential = GoogleCredential.FromFile("C:\\Users\\ACER\\source\\repos\\MusicAF\\MusicAF\\Assets\\config.json");
            FirestoreClientBuilder clientBuilder = new FirestoreClientBuilder
            {
                Credential = credential
            };

            FirestoreClient firestoreClient = clientBuilder.Build();
            string projectId = FirebaseConfig.ProjectId;
            FirestoreDb = FirestoreDb.Create(projectId, firestoreClient);
        }

        //Add document to Firestore
        public async Task AddDocumentAsync(string collection, string documentId, object data)
        {
            try
            {
                Debug.WriteLine($"Starting document upload to {collection}/{documentId}");

                if (FirestoreDb == null)
                {
                    Debug.WriteLine("Error: FirestoreDb is null");
                    throw new InvalidOperationException("Firestore database is not initialized");
                }

                DocumentReference docRef = FirestoreDb.Collection(collection).Document(documentId);
                await docRef.SetAsync(data);
                Debug.WriteLine("Document added successfully");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error adding document: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
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

            if (snapshot.Exists)
            {
                if (snapshot.TryGetValue(fieldName, out T fieldValue))
                {
                    return fieldValue;
                }
            }
            return default(T);
        }

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
    }

    public static class FirebaseConfig
    {
        public const string ApiKey = "AIzaSyDX9VYUOnkEI62tTdvbgw7TZijK806yGEg";
        public const string ProjectId = "musicaf-f6fb8";
    }
}