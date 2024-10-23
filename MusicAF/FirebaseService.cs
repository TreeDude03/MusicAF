using System;
using System.Collections.Generic;
/*using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;

public class FirebaseService
{
    private FirestoreDb _firestoreDb;
    public FirebaseService()
    {
        var options = new AppOptions
        {
            ApiKey = "AIzaSyDX9VYUOnkEI62tTdvbgw7TZijK806yGEg",
            AuthDomain = "musicaf-f6fb8.firebaseapp.com",
            ProjectId = "musicaf-f6fb8",
            StorageBucket = "musicaf-f6fb8.appspot.com",
            MessageingSenderId = "874282381323",
            AppId = "1:874282381323:web:d16e88625672b29b88570e",
            MeasurementId = "G-Z16CRWNETV"
        };

        FirebaseApp.Create(options);
        _firestoreDb = FirestoreDb.Create("musicaf-f6fb8");
    }

    public async Task AddDataAsync(string collectionName, object data)
    {
        CollectionReference collection = _firestoreDb.Collection(collectionName);
        await collection.AddAsync(data);
    }
}*/