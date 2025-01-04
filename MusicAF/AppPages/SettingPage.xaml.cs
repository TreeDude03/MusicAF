using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MusicAF.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MusicAF.AppPages
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingPage : Page
    {
        private bool _isTimerRunning = false;
        private DispatcherTimer _timer;
        private int _timerDuration;
        string selectedTime;

        public SettingPage()
        {
            this.InitializeComponent();
            _timer = new DispatcherTimer();
            _timer.Tick += Timer_Tick;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Restore toggle state
            bool isOn = AppSettings.GetSetting("MusicOffToggleState", false);
            MusicOffToggle.IsOn = isOn;
            TimerPickerSection.Visibility = isOn ? Visibility.Visible : Visibility.Collapsed;

            // Restore TimerPicker selection
            int selectedIndex = AppSettings.GetSetting("TimerPickerIndex", 0);
            TimerPicker.SelectedIndex = selectedIndex;

            // Restore RealTimePicker value
            string savedTime = AppSettings.GetSetting("RealTimePickerValue", TimeSpan.Zero.ToString());
            if (TimeSpan.TryParse(savedTime, out TimeSpan restoredTime))
            {
                RealTimePicker.Time = restoredTime;
            }
        }


        private void MusicOffToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (MusicOffToggle.IsOn)
            {
                TimerPickerSection.Visibility = Visibility.Visible;
            }
            else
            {
                _isTimerRunning = false; // Set flag
                AppSettings.SaveSetting("MusicOffToggleState", false);
                TimerPickerSection.Visibility = Visibility.Collapsed;
                StopTimer();
            }
        }
        private void TimerPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            // Check if the selected item is "Pick time"
            if (TimerPicker.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content.ToString() == "Pick time")
            {
                // Enable the TimePicker when "Pick time" is selected
                RealTimePicker.IsEnabled = true;
            }
            else
            {
                // Disable the TimePicker when any other item is selected
                RealTimePicker.IsEnabled = false;
            }
        }


        private void RealTimePicker_TimeChanged(object sender, TimePickerValueChangedEventArgs e)
        {
            
            selectedTime = e.NewTime.ToString();
            // Handle the selected time, e.g., calculate the duration to turn off the music
            Console.WriteLine($"Selected real-time: {selectedTime}");
        }

        private void StartTimer(object sender, RoutedEventArgs e)
        {
            AppSettings.SaveSetting("MusicOffToggleState", MusicOffToggle.IsOn);
            // Check if the user has selected a time from the ComboBox
            if (TimerPicker.SelectedItem is ComboBoxItem selectedItem)
            {
                AppSettings.SaveSetting("TimerPickerIndex", TimerPicker.SelectedIndex);
                string selectedContent = selectedItem.Content.ToString();

                // If the selected content is a preset time, parse it
                if (selectedContent != "Pick time" && int.TryParse(selectedContent.Split(' ')[0], out _timerDuration))
                {
                    _timer.Interval = TimeSpan.FromMinutes(_timerDuration);
                    _timer.Start();
                    _isTimerRunning = true; // Set flag
                }
                // If "Pick time" is selected, use the RealTimePicker's value
                else if (RealTimePicker.IsEnabled && RealTimePicker.Time != null)
                {
                    AppSettings.SaveSetting("RealTimePickerValue", RealTimePicker.Time.ToString());
                    // Use the RealTimePicker's selected time directly
                    var selectedTime = RealTimePicker.Time;

                    // Get the current time
                    var currentTime = DateTime.Now.TimeOfDay;

                    // Calculate the duration between the current time and the selected time
                    var timeDifference = selectedTime - currentTime;

                    // If the selected time is later than the current time, start the timer
                    if (timeDifference.TotalMinutes > 0)
                    {
                        _timerDuration = (int)timeDifference.TotalMinutes;
                        Console.WriteLine($"Timer duration: {_timerDuration} minutes");
                        _timer.Interval = timeDifference; // Set the timer interval to the difference
                        _timer.Start();
                        _isTimerRunning = true; // Set flag
                    }
                    else
                    {
                        ShowNotification("The selected time is in the past. Please select a future time.");
                    }
                }
                else
                {
                    ShowNotification("Please select a valid time.");
                }
            }
        }

        private void StopTimer()
        {
            AppSettings.SaveSetting("MusicOffToggleState", false);
            _timer.Stop();
            _isTimerRunning = false; // Set flag
        }

        private void Timer_Tick(object sender, object e)
        {
            bool isOn = AppSettings.GetSetting("MusicOffToggleState", false);
            if (!_isTimerRunning || !isOn)
            {
                // If the timer is not running, ignore this tick
                return;
            }
            StopTimer();
            //Assuming PlaybackService handles music playback
            //App.PlaybackService.StopPlayback();
            App.PlaybackService.TriggerTimerEnded();
            ShowNotification("Music playback has been turned off.");
        }

        private async void ShowNotification(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Timer Alert",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
