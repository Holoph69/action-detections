using Accord.Video.FFMPEG;
using AForge.Video;
using AForge.Video.DirectShow;
using AForge.Vision.Motion;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CameraApp
{

    public class MainWindowViewModel : ObservableObject, IDisposable
    {
        #region Private fields

        private FilterInfo _currentDevice;
        private string _textForDetect;
        private BitmapImage _image;
        private IVideoSource _videoSource;
        private VideoFileWriter _writer;
        private bool _recording;
        private DateTime? _firstFrameTime;

        #endregion

        #region Constructor
        public MainWindowViewModel()
        {
            VideoDevices = new ObservableCollection<FilterInfo>();
            GetVideoDevices();
            StartSourceCommand = new RelayCommand(StartCamera);
            StopSourceCommand = new RelayCommand(StopCamera);
            //StartRecordingCommand = new RelayCommand(StartRecording);
            //StopRecordingCommand = new RelayCommand(StopRecording);
            //SaveSnapshotCommand = new RelayCommand(SaveSnapshot);
            DetectString = "not/Detected";
        }
        #endregion

        #region Properties
        public ObservableCollection<FilterInfo> VideoDevices { get; set; }
        public string DetectString
        {
            get { return _textForDetect; }
            set
            {
                if (_textForDetect != value)
                {
                    _textForDetect = value;
                    RaisePropertyChanged(nameof(DetectString));
                }
            }
        }
        public SolidColorBrush TextColor { get; set; }

        public BitmapImage Image
        {
            get { return _image; }
            set { Set(ref _image, value); }
        }

        public FilterInfo CurrentDevice
        {
            get { return _currentDevice; }
            set { Set(ref _currentDevice, value); }
        }

        public ICommand StartRecordingCommand { get; private set; }

        public ICommand StopRecordingCommand { get; private set; }

        public ICommand StartSourceCommand { get; private set; }

        public ICommand StopSourceCommand { get; private set; }

        public ICommand SaveSnapshotCommand { get; private set; }
        public ICommand MotionDetectedCommand { get; private set; }

        public MotionDetector detector = new MotionDetector(new SimpleBackgroundModelingDetector(), new MotionAreaHighlighting());

        #endregion

        private void GetVideoDevices()
        {
            var devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo device in devices)
            {
                VideoDevices.Add(device);
            }
            if (VideoDevices.Any())
            {
                CurrentDevice = VideoDevices[0];
            }
            else
            {
                MessageBox.Show("No webcam found");
            }
        }

        private void StartCamera()
        {
            if (CurrentDevice != null)
            {
                _videoSource = new VideoCaptureDevice(CurrentDevice.MonikerString);
                _videoSource.NewFrame += video_NewFrame;
                _videoSource.Start();
            }
            else
            {
                MessageBox.Show("Current device can't be null");
            }
        }

        private void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                if (_recording)
                {
                    using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                    {
                        if (_firstFrameTime != null)
                        {
                            _writer.WriteVideoFrame(bitmap, DateTime.Now - _firstFrameTime.Value);
                        }
                        else
                        {
                            _writer.WriteVideoFrame(bitmap);
                            _firstFrameTime = DateTime.Now;
                        }

                    }
                }
                using (var bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    var bi = bitmap.ToBitmapImage();
                    bi.Freeze();
                    Dispatcher.CurrentDispatcher.Invoke(() => Image = bi);

                    if (detector.ProcessFrame(bitmap) > 0.02)
                    {
                        //TextColor = System.Windows.Media.Brushes.Red;
                        DetectString = detector.ProcessFrame(bitmap).ToString();
                        DetectString = "Movement detected";
                    }
                    else
                    {
                        //TextColor = System.Windows.Media.Brushes.Green;
                        DetectString = detector.ProcessFrame(bitmap).ToString();
                        DetectString = "nothing detected";
                    }
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("Error on _videoSource_NewFrame:\n" + exc.Message, "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                StopCamera();
            }

        }

        private void StopCamera()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
                _videoSource.NewFrame -= video_NewFrame;
            }
            Image = null;
        }

        //private void StopRecording()
        //{
        //    _recording = false;
        //    _writer.Close();
        //    _writer.Dispose();
        //}

        //private void StartRecording()
        //{
        //    var dialog = new SaveFileDialog();
        //    dialog.FileName = "Video1";
        //    dialog.DefaultExt = ".avi";
        //    dialog.AddExtension = true;
        //    var dialogresult = dialog.ShowDialog();
        //    if (dialogresult != true)
        //    {
        //        return;
        //    }
        //    _firstFrameTime = null;
        //    _writer = new VideoFileWriter();
        //    _writer.Open(dialog.FileName, (int)Math.Round(Image.Width, 0), (int)Math.Round(Image.Height, 0));
        //    _recording = true;
        //}

        //private void SaveSnapshot()
        //{
        //    var dialog = new SaveFileDialog();
        //    dialog.FileName = "Snapshot1";
        //    dialog.DefaultExt = ".png";
        //    var dialogresult = dialog.ShowDialog();
        //    if (dialogresult != true)
        //    {
        //        return;
        //    }
        //    var encoder = new PngBitmapEncoder();
        //    encoder.Frames.Add(BitmapFrame.Create(Image));
        //    using (var filestream = new FileStream(dialog.FileName, FileMode.Create))
        //    {
        //        encoder.Save(filestream);
        //    }
        //}

        public void Dispose()
        {
            if (_videoSource != null && _videoSource.IsRunning)
            {
                _videoSource.SignalToStop();
            }
            _writer?.Dispose();
        }
    }
}
