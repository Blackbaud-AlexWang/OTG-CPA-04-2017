// 
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license.
// 
// Microsoft Cognitive Services: http://www.microsoft.com/cognitive
// 
// Microsoft Cognitive Services Github:
// https://github.com/Microsoft/Cognitive
// 
// Copyright (c) Microsoft Corporation
// All rights reserved.
// 
// MIT License:
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED ""AS IS"", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Net.Http;
using System.Web;
using System.Windows.Forms;
using ConstituentAPIHandler.API;
using Newtonsoft.Json;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;
using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using Microsoft.ProjectOxford.Vision;
using VideoFrameAnalyzer;
using CSHttpClientSample;
using Database;
using ConstituentAPIHandler.Contracts;
using ComboBox = System.Windows.Controls.ComboBox;

namespace LiveCameraSample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        private FaceServiceClient _faceClient = null;
        private readonly FrameGrabber<LiveCameraResult> _grabber = null;
        private static readonly ImageEncodingParam[] s_jpegParams = {
            new ImageEncodingParam(ImwriteFlags.JpegQuality, 60)
        };
        private readonly CascadeClassifier _localFaceDetector = new CascadeClassifier();
        private bool _fuseClientRemoteResults;
        private LiveCameraResult _latestResultsToDisplay = null;
        private AppMode _mode;
        private DateTime _startTime;
        private ConstituentHandler _constituentHandler;
        private SqlHandler _sqlHandler;

        private bool _refreshLeft = true;
        public enum AppMode
        {
            Faces
        }

        private const string _connectionString = "Data Source=CHS6ALEXWAN01;Initial Catalog=OTG-CPA_04-2017;Integrated Security=SSPI";
        private const string _personGroup = "otg";

        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = new Constituent();

            // ButtonBar.Background = new SolidColorBrush(Color.FromRgb(0xf0, 0xad, 0x4e));
            // ConstituentInfoPanel.Background = new SolidColorBrush(Color.FromRgb(0x5c, 0xb8, 0x5c));
            //ShowHideBar.Background = new SolidColorBrush(Color.FromRgb(0x78, 0x90, 0x9c));

            // Create grabber. 
            _grabber = new FrameGrabber<LiveCameraResult>();

            // Set up a listener for when the client receives a new frame.
            _grabber.NewFrameProvided += (s, e) =>
            {
                // The callback may occur on a different thread, so we must use the
                // MainWindow.Dispatcher when manipulating the UI. 
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    // Display the image in the left pane.
                    // LeftImage.Source = e.Frame.Image.ToBitmapSource();
                    LeftImage.Source = LeftImageVisualResults(e.Frame);
                    
                    // Update data context
                    DataContext = GetBannerInfo();

                    // If we're fusing client-side face detection with remote analysis, show the
                    // new frame now with the most recent analysis available. 
                    if (_fuseClientRemoteResults)
                    {
                        RightImage.Source = VisualizeResult(e.Frame);
                    }
                }));

                // See if auto-stop should be triggered. 
                if (Properties.Settings.Default.AutoStopEnabled && (DateTime.Now - _startTime) > Properties.Settings.Default.AutoStopTime)
                {
                    _grabber.StopProcessingAsync();
                }

            };

            // Set up a listener for when the client receives a new result from an API call. 
            _grabber.NewResultAvailable += (s, e) =>
            {
                this.Dispatcher.BeginInvoke((Action)(() =>
                {
                    if (e.TimedOut)
                    {
                        MessageArea.Text = "API call timed out.";
                    }
                    else if (e.Exception != null)
                    {
                        string apiName = "";
                        string message = e.Exception.Message;
                        var faceEx = e.Exception as FaceAPIException;
                        var emotionEx = e.Exception as Microsoft.ProjectOxford.Common.ClientException;
                        var visionEx = e.Exception as Microsoft.ProjectOxford.Vision.ClientException;
                        if (faceEx != null)
                        {
                            apiName = "Face";
                            message = faceEx.ErrorMessage;
                        }
                        else if (emotionEx != null)
                        {
                            apiName = "Emotion";
                            message = emotionEx.Error.Message;
                        }
                        else if (visionEx != null)
                        {
                            apiName = "Computer Vision";
                            message = visionEx.Error.Message;
                        }
                        MessageArea.Text = string.Format("{0} API call failed on frame {1}. Exception: {2}", apiName, e.Frame.Metadata.Index, message);
                    }
                    else
                    {
                        _latestResultsToDisplay = e.Analysis;

                        // Display the image and visualization in the right pane. 
                        if (!_fuseClientRemoteResults)
                        {
                            RightImage.Source = VisualizeResult(e.Frame);
                        }
                    }
                }));
            };

            // Create local face detector. 
            _localFaceDetector.Load("Data/haarcascade_frontalface_alt2.xml");
        }

        private object GetBannerInfo()
        {
            var result = _latestResultsToDisplay;
            if (result != null
                && result.Faces != null
                && result.Faces.Count() > 0
                && result.Constituents.ContainsKey(result.Faces[0].FaceId))
            {
                return result.Constituents[result.Faces[0].FaceId];
            }

            return DataContext;
        }

        /// <summary> Function which submits a frame to the Face API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the faces returned by the API. </returns>
        private async Task<LiveCameraResult> FacesAnalysisFunction(VideoFrame frame)
        {
            // Encode image. 
            var jpg = frame.Image.ToMemoryStream(".jpg", s_jpegParams);
            // Submit image to API. 
            var attrs = new List<FaceAttributeType> { FaceAttributeType.Age,
                FaceAttributeType.Gender, FaceAttributeType.HeadPose };
            var faces = await _faceClient.DetectAsync(jpg, returnFaceAttributes: attrs);

            // Identify faces
            var constituents = await IdentifyConstituentFaces(faces);

            // Count the API call. 
            Properties.Settings.Default.FaceAPICallCount++;
            // Output. 
            return new LiveCameraResult { Faces = faces, Constituents = constituents };
        }

        private async Task<Dictionary<Guid, Constituent>> IdentifyConstituentFaces(Face[] faces)
        {
            var constituents = new Dictionary<Guid, Constituent>();
            if (faces.Count() == 0) return constituents;

            var faceIds = faces.Select(face => face.FaceId).ToArray();
            var results = await _faceClient.IdentifyAsync(_personGroup, faceIds);
            foreach (var identifyResult in results.Where(r => r.Candidates.Length > 0))
            {
                if (constituents.ContainsKey(identifyResult.FaceId))
                {
                    continue;
                }

                Console.WriteLine("Result of face: {0}", identifyResult.FaceId);
                // Get top 1 among all candidates returned
                var candidateId = identifyResult.Candidates[0].PersonId;
                
                //var person = await _faceClient.GetPersonAsync(_personGroup, candidateId);
                //Console.WriteLine("Identified as {0} {1}", person.Name, candidateId);

                var constituentId = await GetConstituentId(candidateId);
                Console.WriteLine("ConstituentId: {0}", constituentId);
                if (constituentId > 0)
                {
                    var constituent = await _constituentHandler.GetConstituent(constituentId);
                    // Only get donation history if info will be displayed on DataContext
                    if (identifyResult.FaceId == faces[0].FaceId)
                    {
                        constituent.GivingHistory = await GetGivingHistory(candidateId);
                        constituent.LastGift = await GetLastGift(candidateId);
                    }
                    constituents.Add(identifyResult.FaceId, constituent);
                }
            }

            return constituents;
        }

        private async Task<GivingHistory> GetGivingHistory(Guid personId)
        {
            var givingHistoryId = await GetGivingHistortyId(personId);
            if (givingHistoryId == 0)
            {
                return new GivingHistory();
            }

            return await _constituentHandler.GetGivingHistory(givingHistoryId);
        }

        private async Task<LastGift> GetLastGift(Guid personId)
        {
            var givingHistoryId = await GetGivingHistortyId(personId);
            if (givingHistoryId == 0)
            {
                return new LastGift();
            }

            return await _constituentHandler.GetLastGift(givingHistoryId);
        }

        private BitmapSource VisualizeResult(VideoFrame frame)
        {
            // Draw any results on top of the image. 
            BitmapSource visImage = frame.Image.ToBitmapSource();

            var result = _latestResultsToDisplay;

            if (result != null)
            {
                // See if we have local face detections for this image.
                var clientFaces = (OpenCvSharp.Rect[])frame.UserData;
                if (clientFaces != null && result.Faces != null)
                {
                    // If so, then the analysis results might be from an older frame. We need to match
                    // the client-side face detections (computed on this frame) with the analysis
                    // results (computed on the older frame) that we want to display. 
                    MatchAndReplaceFaceRectangles(result.Faces, clientFaces);
                }

                visImage = Visualization.DrawFaces(visImage, result.Faces, result.Constituents);
                visImage = Visualization.DrawTags(visImage, result.Tags);        
            }

            return visImage;
        }

        private BitmapSource LeftImageVisualResults(VideoFrame frame)
        {
            // Draw any results on top of the image. 
            BitmapSource visImage = frame.Image.ToBitmapSource();

            var result = _latestResultsToDisplay;

            if (result != null)
            {
                // See if we have local face detections for this image.
                var clientFaces = (OpenCvSharp.Rect[])frame.UserData;
                if (clientFaces != null && result.Faces != null)
                {
                    // If so, then the analysis results might be from an older frame. We need to match
                    // the client-side face detections (computed on this frame) with the analysis
                    // results (computed on the older frame) that we want to display. 
                    MatchAndReplaceFaceRectangles(result.Faces, clientFaces);
                }

                visImage = Visualization.DrawFaces(visImage, result.Faces, result.Constituents);
                visImage = Visualization.DrawTags(visImage, result.Tags);
            }

            return visImage;
        }

        /// <summary> Populate CameraList in the UI, once it is loaded. </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Routed event information. </param>
        private void CameraList_Loaded(object sender, RoutedEventArgs e)
        {
            int numCameras = _grabber.GetNumCameras();

            if (numCameras == 0)
            {
                MessageArea.Text = "No cameras found!";
            }

            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = Enumerable.Range(0, numCameras).Select(i => string.Format("Camera {0}", i + 1));
            comboBox.SelectedIndex = 0;
        }

        /// <summary> Populate ModeList in the UI, once it is loaded. </summary>
        /// <param name="sender"> Source of the event. </param>
        /// <param name="e">      Routed event information. </param>
        private void ModeList_Loaded(object sender, RoutedEventArgs e)
        {
            var modes = (AppMode[])Enum.GetValues(typeof(AppMode));

            var comboBox = sender as ComboBox;
            comboBox.ItemsSource = modes.Select(m => m.ToString());
            comboBox.SelectedIndex = 0;
        }

        private void ModeList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Disable "most-recent" results display. 
            _fuseClientRemoteResults = false;

            var comboBox = sender as ComboBox;
            var modes = (AppMode[])Enum.GetValues(typeof(AppMode));
            _mode = modes[comboBox.SelectedIndex];
            switch (_mode)
            {
                case AppMode.Faces:
                    _grabber.AnalysisFunction = FacesAnalysisFunction;
                    break;
                default:
                    _grabber.AnalysisFunction = null;
                    break;
            }
        }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            var applicationId = "9bc8f539-a340-41e7-9020-d781e2024fc7";
            var url =
                $"https://oauth2.sky.blackbaud.com/authorization?client_id={applicationId}&response_type=token&redirect_uri=https://www.example.com/oauth2/callback&state=fdf80155";
            Process.Start("IExplore.exe", url);
            OauthLogin();
        }

        private static void OauthLogin()
        {
            var shellWindows = new SHDocVw.ShellWindows();
            while (true)
            {
                foreach (SHDocVw.InternetExplorer ie in shellWindows)
                {
                    if (!ie.LocationURL.Contains("callback#")) continue;
                    Headers.AccessKey = GetToken(ie.LocationURL);
                    return;
                }
            }
        }

        private static string GetToken(string url)
        {
            var start = url.IndexOf("=");
            var length = url.IndexOf("&") - start;
            return url.Substring(start + 1, length - 1);
        }
        
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!CameraList.HasItems)
            {
                MessageArea.Text = "No cameras found; cannot start processing";
                return;
            }

            // Clean leading/trailing spaces in API keys. 
            Properties.Settings.Default.FaceAPIKey = Properties.Settings.Default.FaceAPIKey.Trim();
            Properties.Settings.Default.EmotionAPIKey = Properties.Settings.Default.EmotionAPIKey.Trim();
            Properties.Settings.Default.VisionAPIKey = Properties.Settings.Default.VisionAPIKey.Trim();

            // Create API clients. 
            _faceClient = new FaceServiceClient(Properties.Settings.Default.FaceAPIKey);
            _constituentHandler = new ConstituentHandler(new HttpClient());

            // Create sql client
            _sqlHandler = new SqlHandler(_connectionString);

            // How often to analyze. 
            _grabber.TriggerAnalysisOnInterval(Properties.Settings.Default.AnalysisInterval);

            // Reset message. 
            MessageArea.Text = "";

             // Record start time, for auto-stop
            _startTime = DateTime.Now;

            await _grabber.StartProcessingCameraAsync(CameraList.SelectedIndex);
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            await _grabber.StopProcessingAsync();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = 1 - SettingsPanel.Visibility;
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsPanel.Visibility = Visibility.Hidden;
            Properties.Settings.Default.Save();
        }

        private void ConstituentInfoButton_Click(object sender, RoutedEventArgs e)
        {
            ConstituentInfoPanel.Visibility = 1 - ConstituentInfoPanel.Visibility;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        private async Task<int> GetConstituentId(Guid personId)
        {
            var sql = "select top 1 ConstituentId from dbo.Constituents where PersonId = @personId";

            var results = await _sqlHandler.QueryAsync(
                sql,
                new Dictionary<string, dynamic> { { "personId", personId.ToString() } },
                (reader) => reader.GetInt32(0));

            return results.FirstOrDefault();
        }

        private async Task<int> GetGivingHistortyId(Guid personId)
        {
            var sql = "select top 1 GivingHistoryId from dbo.Constituents where PersonId = @personId";

            var results = await _sqlHandler.QueryAsync(
                sql,
                new Dictionary<string, dynamic> { { "personId", personId.ToString() } },
                (reader) => reader.GetInt32(0));

            return results.FirstOrDefault();
        }

        private void MatchAndReplaceFaceRectangles(Face[] faces, OpenCvSharp.Rect[] clientRects)
        {
            // Use a simple heuristic for matching the client-side faces to the faces in the
            // results. Just sort both lists left-to-right, and assume a 1:1 correspondence. 

            // Sort the faces left-to-right. 
            var sortedResultFaces = faces
                .OrderBy(f => f.FaceRectangle.Left + 0.5 * f.FaceRectangle.Width)
                .ToArray();

            // Sort the clientRects left-to-right.
            var sortedClientRects = clientRects
                .OrderBy(r => r.Left + 0.5 * r.Width)
                .ToArray();

            // Assume that the sorted lists now corrrespond directly. We can simply update the
            // FaceRectangles in sortedResultFaces, because they refer to the same underlying
            // objects as the input "faces" array. 
            for (int i = 0; i < Math.Min(faces.Length, clientRects.Length); i++)
            {
                // convert from OpenCvSharp rectangles
                OpenCvSharp.Rect r = sortedClientRects[i];
                sortedResultFaces[i].FaceRectangle = new FaceRectangle { Left = r.Left, Top = r.Top, Width = r.Width, Height = r.Height };
            }
        }
    }
}
