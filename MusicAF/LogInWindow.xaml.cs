using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Google.Cloud.Firestore;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MusicAF
{
    public sealed partial class LogInWindow : Window
    {
        private FirestoreService _firestoreService;

        public LogInWindow()
        {
            InitializeComponent();
            _firestoreService = FirestoreService.Instance;
        }

        // Sign-Up Button Click
        private async void SignupButton_Click(object sender, RoutedEventArgs e)
        {
            //
            string email = EmailTextBox_Signup.Text;
            string password = PasswordBox_Signup.Password;
            string confirm = ConfirmPasswordBox_Signup.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageTextBlock.Text = "Please enter both email and password.";
                return;
            }
            if (string.IsNullOrWhiteSpace(confirm))
            {
                MessageTextBlock.Text = "Please confirm your password";
                return;
            }
            if (!confirm.Equals(password))
            {
                MessageTextBlock.Text = "Password mismatch";
                return;
            }
            string encryptedPassword = SecurityHelper.EncryptPassword(password);
            await SignUpUserAsync(email, encryptedPassword);
        }

        // Login Button Click
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            string email = EmailTextBox_Login.Text;
            string password = PasswordBox_Login.Password;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageTextBlock.Text = "Please enter both email and password.";
                return;
            }

            string encryptedPassword = SecurityHelper.EncryptPassword(password);
            await LoginUserAsync(email, encryptedPassword);
        }
        //tranferring
        private void SignupTextBlock_Click(object sender, RoutedEventArgs e)
        {
            LoginPanel.Visibility = Visibility.Collapsed;
            SignupPanel.Visibility = Visibility.Visible;
            Title = "Music AF - Signup";
        }

        private void LoginTextBlock_Click(object sender, RoutedEventArgs e)
        {
            SignupPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;
            Title = "Music AF - Login";
        }

        // Sign-Up Logic
        private async Task SignUpUserAsync(string email, string encryptedPassword)
        {
            // Check if the user already exists
            string signup_email = await _firestoreService.GetFieldFromDocumentAsync<string>("users", email, "Email");
            if (signup_email != null)
            {
                MessageTextBlock.Text = "User already exists.";
                return;
            }
            await _firestoreService.AddDocumentAsync("users", email, new { Email = email, Password = encryptedPassword, CreatedAt = DateTime.UtcNow });
            MessageTextBlock.Text = "User signed up successfully. Please login";
            //
            SignupPanel.Visibility = Visibility.Collapsed;
            LoginPanel.Visibility = Visibility.Visible;

        }

        // Login Logic
        private async Task LoginUserAsync(string email, string encryptedPassword)
        {
            string login_email = await _firestoreService.GetFieldFromDocumentAsync<string>("users", email, "Email");
            string login_password = await _firestoreService.GetFieldFromDocumentAsync<string>("users", email, "Password");
            if (login_email == null)
            {
                MessageTextBlock.Text = "User does not exist.";
                return;
            }
            if (login_password == encryptedPassword)
            {
                MessageTextBlock.Text = "Login successful!";
                Window window = new MainWindow(email);
                window.Activate();
                this.Close();
            }
            else
            {
                MessageTextBlock.Text = "Incorrect password.";
            }
        }
    }
}
