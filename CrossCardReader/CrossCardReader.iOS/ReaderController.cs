using System;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using CrossCardReader.Abstractions;
using Foundation;
using UIKit;

namespace CrossCardReader.iOS
{
    public class ReaderController : UIViewController
    {
        private AVCaptureSession captureSession;
        private AVCaptureDeviceInput captureDeviceInput;
        private UIButton toggleCameraButton;
        private UIButton toggleFlashButton;
        private UIView liveCameraStream;
        private AVCaptureStillImageOutput stillImageOutput;
        private UIButton takePhotoButton;
        private CardRecognitionService cardDetectorService;
        private readonly string apiKey;
        private readonly string[] supportedProducts;
        internal event EventHandler<CardReadEventArgs> CardRead;
        private int requestCode;

        public ReaderController(string[] products, string api, int request)
        {
            apiKey = api;
            supportedProducts = products;
            requestCode = request;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            SetupUserInterface();
            SetupEventHandlers();

            AuthorizeCameraUse();
            SetupLiveCameraStream();

            ToggleFrontBackCamera();
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);

            UIApplication.SharedApplication.ApplicationIconBadgeNumber = 0;

            cardDetectorService = new CardRecognitionService(supportedProducts, apiKey);
        }

        private async void AuthorizeCameraUse()
        {
            var authorizationStatus = AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video);

            if (authorizationStatus != AVAuthorizationStatus.Authorized)
            {
                await AVCaptureDevice.RequestAccessForMediaTypeAsync(AVMediaType.Video);
            }
        }

        private void SetupLiveCameraStream()
        {
            captureSession = new AVCaptureSession();
            
            var videoPreviewLayer = new AVCaptureVideoPreviewLayer(captureSession)
            {
                Frame = liveCameraStream.Bounds
            };

            liveCameraStream.Layer.AddSublayer(videoPreviewLayer);

            var captureDevice = AVCaptureDevice.DefaultDeviceWithMediaType(AVMediaType.Video);
            ConfigureCameraForDevice(captureDevice);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(captureDevice);
            
            stillImageOutput = new AVCaptureStillImageOutput
            {
                OutputSettings = new NSDictionary()
            };

            captureSession.AddOutput(stillImageOutput);
            captureSession.AddInput(captureDeviceInput);
            captureSession.StartRunning();
        }

        private async void CapturePhoto()
        {

            var videoConnection = stillImageOutput.ConnectionFromMediaType(AVMediaType.Video);
            var sampleBuffer = await stillImageOutput.CaptureStillImageTaskAsync(videoConnection);
            
            var jpegImageAsNsData = AVCaptureStillImageOutput.JpegStillToNSData(sampleBuffer);

            var res = await GetCardAsync(requestCode, jpegImageAsNsData.ToArray());
            
            OnCardRead(res);

        }
        private async Task<CardReadEventArgs> GetCardAsync(int req, byte[] imageBytes)
        {
            try
            {
                var service = new CardRecognitionService(supportedProducts, apiKey);

                var card = await service.RecognizeCard(imageBytes);

                return new CardReadEventArgs(req, false, card);
            }
            catch (Exception ex)
            {
                return new CardReadEventArgs(req, ex);
            }
        }

        private void ToggleFrontBackCamera()
        {
            var devicePosition = captureDeviceInput.Device.Position;
            devicePosition = devicePosition == AVCaptureDevicePosition.Front ? AVCaptureDevicePosition.Back : AVCaptureDevicePosition.Front;

            var device = GetCameraForOrientation(devicePosition);
            ConfigureCameraForDevice(device);

            captureSession.BeginConfiguration();
            captureSession.RemoveInput(captureDeviceInput);
            captureDeviceInput = AVCaptureDeviceInput.FromDevice(device);
            captureSession.AddInput(captureDeviceInput);
            captureSession.CommitConfiguration();
        }

        private static void ConfigureCameraForDevice(AVCaptureDevice device)
        {
            NSError error;
            if (device.IsFocusModeSupported(AVCaptureFocusMode.ContinuousAutoFocus))
            {
                device.LockForConfiguration(out error);
                device.FocusMode = AVCaptureFocusMode.ContinuousAutoFocus;
                device.UnlockForConfiguration();
            }
            else if (device.IsExposureModeSupported(AVCaptureExposureMode.ContinuousAutoExposure))
            {
                device.LockForConfiguration(out error);
                device.ExposureMode = AVCaptureExposureMode.ContinuousAutoExposure;
                device.UnlockForConfiguration();
            }
            else if (device.IsWhiteBalanceModeSupported(AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance))
            {
                device.LockForConfiguration(out error);
                device.WhiteBalanceMode = AVCaptureWhiteBalanceMode.ContinuousAutoWhiteBalance;
                device.UnlockForConfiguration();
            }
        }

        private void ToggleFlash()
        {
            var device = captureDeviceInput.Device;

            if (device.HasFlash)
            {
                NSError error;
                if (device.FlashMode == AVCaptureFlashMode.On)
                {
                    device.LockForConfiguration(out error);
                    device.FlashMode = AVCaptureFlashMode.Off;
                    device.UnlockForConfiguration();

                    toggleFlashButton.SetBackgroundImage(UIImage.FromFile("NoFlashButton.png"), UIControlState.Normal);
                }
                else
                {
                    device.LockForConfiguration(out error);
                    device.FlashMode = AVCaptureFlashMode.On;
                    device.UnlockForConfiguration();

                    toggleFlashButton.SetBackgroundImage(UIImage.FromFile("FlashButton.png"), UIControlState.Normal);
                }
            }
        }

        private static AVCaptureDevice GetCameraForOrientation(AVCaptureDevicePosition orientation)
        {
            var devices = AVCaptureDevice.DevicesWithMediaType(AVMediaType.Video);

            return devices.FirstOrDefault(device => device.Position == orientation);
        }

        private void SetupUserInterface()
        {
            var centerButtonX = View.Bounds.GetMidX() - 35f;
            var topLeftX = View.Bounds.X + 25;
            var topRightX = View.Bounds.Right - 65;
            var bottomButtonY = View.Bounds.Bottom - 85;
            var topButtonY = View.Bounds.Top + 15;
            var buttonWidth = 70;
            var buttonHeight = 70;

            liveCameraStream = new UIView()
            {
                Frame = new CGRect(0f, 0f, 320f, View.Bounds.Height)
            };

            takePhotoButton = new UIButton()
            {
                Frame = new CGRect(centerButtonX, bottomButtonY, buttonWidth, buttonHeight)
            };
            takePhotoButton.SetBackgroundImage(UIImage.FromFile("TakePhotoButton.png"), UIControlState.Normal);

            toggleCameraButton = new UIButton()
            {
                Frame = new CGRect(topRightX, topButtonY + 5, 35, 26)
            };
            toggleCameraButton.SetBackgroundImage(UIImage.FromFile("ToggleCameraButton.png"), UIControlState.Normal);

            toggleFlashButton = new UIButton()
            {
                Frame = new CGRect(topLeftX, topButtonY, 37, 37)
            };
            toggleFlashButton.SetBackgroundImage(UIImage.FromFile("NoFlashButton.png"), UIControlState.Normal);

            View.Add(liveCameraStream);
            View.Add(takePhotoButton);
            View.Add(toggleCameraButton);
            View.Add(toggleFlashButton);
        }

        private void SetupEventHandlers()
        {
            takePhotoButton.TouchUpInside += (sender, e) => {
                CapturePhoto();
            };

            toggleCameraButton.TouchUpInside += (sender, e) => {
                ToggleFrontBackCamera();
            };

            toggleFlashButton.TouchUpInside += (sender, e) => {
                ToggleFlash();
            };
        }
        private void OnCardRead(CardReadEventArgs e)
        {
            var picked = CardRead;
            picked?.Invoke(null, e);
        }
    }
    internal class CardReadEventArgs : EventArgs
    {
        public CardReadEventArgs(int id, Exception error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            RequestId = id;
            Error = error;
        }

        public CardReadEventArgs(int id, bool isCanceled, Card card = null)
        {
            RequestId = id;
            IsCanceled = isCanceled;
            if (!IsCanceled && card == null)
                throw new ArgumentNullException("card");

            Card = card;
        }

        public int RequestId
        {
            get;
            private set;
        }

        public bool IsCanceled
        {
            get;
            private set;
        }

        public Exception Error
        {
            get;
            private set;
        }

        public Card Card
        {
            get;
            private set;
        }

        public Task<Card> ToTask()
        {
            var tcs = new TaskCompletionSource<Card>();

            if (IsCanceled)
                tcs.SetResult(null);
            else if (Error != null)
                tcs.SetException(Error);
            else
                tcs.SetResult(Card);

            return tcs.Task;
        }
    }
}
