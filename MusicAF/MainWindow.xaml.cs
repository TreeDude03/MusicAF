using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;
using System.Threading.Tasks;

namespace MusicAF
{
    public sealed partial class MainWindow : Window
    {
        private User currentUser;
        private FirestoreService _firestoreService;
        //
        public MainWindow(string userEmail)
        {
            _firestoreService = FirestoreService.Instance;
            _ = setUpUser(userEmail);
            this.InitializeComponent();
        }

        public async Task setUpUser(string userEmail)
        {
            string login_email = await _firestoreService.GetFieldFromDocumentAsync<string>("users", userEmail, "Email");
            string login_password = await _firestoreService.GetFieldFromDocumentAsync<string>("users", userEmail, "Password");
            string datecreated = await _firestoreService.GetFieldFromDocumentAsync<string>("users", userEmail, "CreatedAt");
            currentUser = new User()
            {
                Email = login_email,
                Password = login_password,
                CreatedAt = DateTime.Now,
            };
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
