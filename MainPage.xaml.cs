using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

/// <summary>
/// The code is taken from https://github.com/Azure-Samples/cognitive-services-onnx-customvision-sample
/// and updated by https://blogs.msdn.microsoft.com/appconsult/2018/11/06/upgrade-your-winml-application-to-the-latest-bits/
/// I added real-time processing part only.
/// </summary>
namespace MyCatEvaluationApp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Stopwatch _stopwatch = new Stopwatch();
        private Model _model = null;
        /// <summary>
        /// Specify the model file name. Make it Content type and copy if newer.
        /// </summary>
        private const string _ourOnnxFileName = "CatModel.onnx";
        /// <summary>
        /// Labels of the model.
        /// </summary>
        private MediaCapture _mediaCapture = new MediaCapture();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async Task LoadModelAsync()
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"Loading {_ourOnnxFileName} ... patience ");

            try
            {
                _stopwatch = Stopwatch.StartNew();

                var modelFile = await StorageFile.GetFileFromApplicationUriAsync(new Uri($"ms-appx:///Assets/{_ourOnnxFileName}"));
                _model = await Model.CreateFromStreamAsync(modelFile);

                _stopwatch.Stop();
                Debug.WriteLine($"Loaded {_ourOnnxFileName}: Elapsed time: {_stopwatch.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: {ex.Message}");
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"error: {ex.Message}");
                _model = null;
            }
        }

        private async void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            // Start preview and take images.
            await _mediaCapture.InitializeAsync();
            captureElement.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();

            ButtonRun.IsEnabled = false;
            
            try
            {
                if (_model == null)
                {
                    // Load the model
                    await Task.Run(async () => await LoadModelAsync());
                }

                while (true)
                {
                    // Capture frame from video stream
                    var inputImage = await Capture();

                    if (inputImage == null)
                        continue;
                 
                    await Task.Run(async () =>
                    {
                        // Evaluate the image
                        await EvaluateVideoFrameAsync(inputImage);
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"error: {ex.Message}");
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"error: {ex.Message}");
            }
            finally
            {
                ButtonRun.IsEnabled = true;
            }
        }

        private async Task EvaluateVideoFrameAsync(VideoFrame frame)
        {
            if (frame != null)
            {
                try
                {
                    _stopwatch.Restart();
                    Input inputData = new Input()
                    {
                        data = ImageFeatureValue.CreateFromVideoFrame(frame)
                    };
                    var results = await _model.EvaluateAsync(inputData);
                    var loss = results.loss.ToList();
                    var labels = results.classLabel;
                    // Get the highest score result.
                    var possibleCat = loss.FirstOrDefault().ToList().OrderByDescending(x=>x.Value).First();

                    string message = $"Predictions: {possibleCat.Key} - {(possibleCat.Value * 100.0f).ToString("#0.00") + "%"}";
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = message);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"error: {ex.Message}");
                    await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => StatusBlock.Text = $"error: {ex.Message}");
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => ButtonRun.IsEnabled = true);
            }
        }

        /// <summary>
        /// Get preview frame
        /// https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/get-a-preview-frame
        /// </summary>
        /// <returns></returns>
        private async Task<VideoFrame> Capture()
        {            
            var previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview) as VideoEncodingProperties;
            VideoFrame videoFrame = new VideoFrame(BitmapPixelFormat.Bgra8, (int)previewProperties.Width, (int)previewProperties.Height);
            VideoFrame previewFrame = await _mediaCapture.GetPreviewFrameAsync(videoFrame);
            return previewFrame;
        }
    }
}
