using System;
using System.Linq;
using System.Threading.Tasks;
using AVFoundation;
using CoreGraphics;
using CrossCardReader.Abstractions;
using Foundation;
using UIKit;

namespace CrossCardReader
{
    /// <summary>
    /// Controller for card reader view
    /// </summary>
    public class ReaderController : UIViewController
    {
        private AVCaptureSession captureSession;
        private AVCaptureDeviceInput captureDeviceInput;
        private UIView liveCameraStream;
        private AVCaptureStillImageOutput stillImageOutput;
        private UIButton takePhotoButton;
        private readonly string apiKey;
        private readonly string[] supportedProducts;
        internal event EventHandler<CardReadEventArgs> CardRead;
        private readonly int requestCode;
        private UIView helperView;

        /// <summary>
        /// Initializes Controller
        /// </summary>
        /// <param name="products">Supported Products</param>
        /// <param name="api">Cognitive Api Key</param>
        /// <param name="request">Id of the read request</param>
        public ReaderController(string[] products, string api, int request)
        {
            apiKey = api;
            supportedProducts = products;
            requestCode = request;
        }

        /// <summary>Finalizer for the NSObject object</summary>
        ~ReaderController()
        {
            takePhotoButton.TouchUpInside -= CapturePhoto;
        }

        /// <summary>Called after the controller’s <see cref="P:UIKit.UIViewController.View" /> is loaded into memory.</summary>
        /// <remarks>
        ///   <para>This method is called after <c>this</c> <see cref="T:UIKit.UIViewController" />'s <see cref="P:UIKit.UIViewController.View" /> and its entire view hierarchy have been loaded into memory. This method is called whether the <see cref="T:UIKit.UIView" /> was loaded from a .xib file or programmatically.</para>
        /// </remarks>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            SetupUserInterface();
            SetupEventHandlers();

            AuthorizeCameraUse();
            SetupLiveCameraStream();
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

        private async void CapturePhoto(object sender, EventArgs eventArgs)
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
        

        private void SetupUserInterface()
        {
            var centerButtonX = View.Bounds.GetMidX() - 35f;
            var bottomButtonY = View.Bounds.Bottom - 150;
            var buttonWidth = 70;
            var buttonHeight = 70;

            liveCameraStream = new UIView
            {
                Frame = new CGRect(0f, 0f, View.Bounds.Width, View.Bounds.Height)
            };

            takePhotoButton = new UIButton
            {
                Frame = new CGRect(centerButtonX, bottomButtonY, buttonWidth, buttonHeight)
            };

            takePhotoButton.SetBackgroundImage(UIImage.FromFile("TakePhotoButton.png"), UIControlState.Normal);

            helperView = new UIView
            {
                Frame = new CGRect(View.Bounds.GetMidX() - 150f, View.Bounds.GetMidY() - 150f, 300f, 200f)
            };

            var red = new UIColor((nfloat)(100.0 / 255.0), (nfloat)(130.0 / 255.0), (nfloat)(230.0 / 255.0), 1);
            helperView.Layer.BorderColor = red.CGColor;
            helperView.Layer.BorderWidth = 5.8f;

            Add(liveCameraStream);
            Add(helperView);
            Add(takePhotoButton);
        }

        private void SetupEventHandlers()
        {
            takePhotoButton.TouchUpInside += CapturePhoto;
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
                throw new ArgumentNullException(nameof(error));

            RequestId = id;
            Error = error;
        }

        public CardReadEventArgs(int id, bool isCanceled, Card card = null)
        {
            RequestId = id;
            IsCanceled = isCanceled;
            if (!IsCanceled && card == null)
                throw new ArgumentNullException(nameof(card));

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
        }

        public Exception Error
        {
            get;
        }

        public Card Card
        {
            get;
        }
        
    }
}
