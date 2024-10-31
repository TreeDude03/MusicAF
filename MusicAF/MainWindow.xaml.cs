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
        private string currentUserEmail;
        private FirestoreService _firestoreService;
        //
        public MainWindow(string userEmail)
        {
            _firestoreService = FirestoreService.Instance;
            currentUserEmail = userEmail;
            this.InitializeComponent();
        }

        private void UploadButton_Click(object sender, RoutedEventArgs e)
        {
        }
    }
}
