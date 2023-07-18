using backend;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;

namespace uwpGUI
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private SolidColorBrush readerBrush;
        private SolidColorBrush serialBrush;
        private SolidColorBrush tagBrush;
        private Reader reader;
        private sensor sensor;
        private string readerStateText;
        public string ReaderStateText
        {
            get { return readerStateText; }
            set { SetProperty(ref readerStateText, value); }
        }
        private string speedText;
        public string SpeedText
        {
            get { return speedText; }
            set { SetProperty(ref speedText, value); }
        }

        private string serialStateText;
        public string SerialStateText
        {
            get { return serialStateText; }
            set { SetProperty(ref serialStateText, value); }
        }

        private string tagStateText;
        public string TagStateText
        {
            get { return tagStateText; }
            set { SetProperty(ref tagStateText, value); }
        }

        public SolidColorBrush ReaderBrush
        {
            get { return readerBrush; }
            set { SetProperty(ref readerBrush, value); }
        }

        public SolidColorBrush SerialBrush
        {
            get { return serialBrush; }
            set { SetProperty(ref serialBrush, value); }
        }

        public SolidColorBrush TagBrush
        {
            get { return tagBrush; }
            set { SetProperty(ref tagBrush, value); }
        }

        //private ObservableCollection<Tags> tagsList;

        public ObservableCollection<Tags> TagsList
        {
            get { return Tags.tagsList; }
            set { SetProperty(ref Tags.tagsList, value); }
        }

        public MainPage()
        {
            this.InitializeComponent();
            this.DataContext = this; // Set the DataContext to the current instance of the page
            sensor = new sensor(this, reader);
            reader = new Reader(this, sensor);

            // Initialize the collection and populate it with sample data
            ReaderStateText = "Reader state";
            SerialStateText = "Serial state";
            TagStateText = "Tag State";

            TagsList = new ObservableCollection<Tags>();
            Application.Current.Suspending += App_Suspending;
            //{
            //    new Tags
            //    {
            //        EPC = "EPC 1",
            //        FirstSeen = DateTime.Now,
            //        LastSeen = DateTime.Now,
            //        Accuracy = 0.9,
            //        SeenTimes = 10,
            //        RealSpeed = 45.6,
            //        RealAccuracy = 1.2,
            //        MinDelay = TimeSpan.FromSeconds(1),
            //        MaxDelay = TimeSpan.FromSeconds(2),
            //        DiffT4LastSeen = TimeSpan.FromMilliseconds(300),
            //        FirstAssetExactDetection = TimeSpan.FromMinutes(5),
            //        MinusAccuracy = 0.1,
            //        PlusAccuracy = 0.2,
            //        MaxAccuracyError = 0.3
            //    },
            //    new Tags
            //    {
            //        EPC = "EPC 2",
            //        FirstSeen = DateTime.Now,
            //        LastSeen = DateTime.Now,
            //        Accuracy = 0.8,
            //        SeenTimes = 8,
            //        RealSpeed = 50.2,
            //        RealAccuracy = 1.5,
            //        MinDelay = TimeSpan.FromSeconds(1.5),
            //        MaxDelay = TimeSpan.FromSeconds(3),
            //        DiffT4LastSeen = TimeSpan.FromMilliseconds(500),
            //        FirstAssetExactDetection = TimeSpan.FromMinutes(7),
            //        MinusAccuracy = 0.2,
            //        PlusAccuracy = 0.3,
            //        MaxAccuracyError = 0.4
            //    }
            //};
        }

        private void App_Suspending(object sender, SuspendingEventArgs e)
        {
            reader.Delete_RoSpec();
        }

        public async void DisplayDialog(string content, string title)
        {
            ContentDialog Dialog = new ContentDialog()
            {
                Title = title,
                Content = content,
                CloseButtonText = "Ok"
            };

            await Dialog.ShowAsync();
        }
        public void setTags(Tags tags)
        {
            TagsList.Add(tags);
        }

        private void test_Click(object sender, RoutedEventArgs e)
        {
            // Update the ListView with new sample data
            setTags(new Tags
            {
                EPC = "Test",
                Date_Time = DateTime.Now,
                Speed = 0.7,
                Power = 6,
                Peak_RSSI = 55.1,
                Time_PC = DateTime.Now
            });
        }

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler PropertyChanged;

        private void SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    

    public class BorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is SolidColorBrush backgroundBrush)
            {
                Color borderColor = GetInverseColor(backgroundBrush.Color);
                return new SolidColorBrush(borderColor);
            }

            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }

        private Color GetInverseColor(Color color)
        {
            byte inverseRed = (byte)(255 - color.R);
            byte inverseGreen = (byte)(255 - color.G);
            byte inverseBlue = (byte)(255 - color.B);
            return Color.FromArgb(255, inverseRed, inverseGreen, inverseBlue);
        }
    }
}
