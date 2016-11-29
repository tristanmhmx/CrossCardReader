using System;
using System.IO;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using CrossCardReader.Abstractions;
#pragma warning disable 618
using Camera = Android.Hardware.Camera;
#pragma warning restore 618

namespace CrossCardReader
{
    /// <summary>
    /// View for the card reader
    /// </summary>
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    [Preserve(AllMembers = true)]
    public class CardReaderView : Activity, TextureView.ISurfaceTextureListener
    {
#pragma warning disable 618
        private Camera camera;
#pragma warning restore 618
        private Button takePhotoButton;
        private CameraFacing cameraType;
        private TextureView textureView;
        internal static event EventHandler<CardReadEventArgs> CardRead;

        // ReSharper disable InconsistentNaming
        internal const string ExtraId = "id";
        internal const string ExtraFront = "android.intent.extras.CAMERA_FACING";
        internal const string ExtraProducts = "supportedProducts";
        internal const string ExtraApiKey = "cognitiveKey";
        // ReSharper enable InconsistentNaming

        private int id;
        private int front;
        private string[] supportedProducts;
        private string apikey;

        private byte[] imageBytes;

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutStringArray(ExtraProducts, supportedProducts);
            outState.PutInt(ExtraId, id);
            outState.PutInt(ExtraFront, front);
            outState.PutString(ExtraApiKey, apikey);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var b = savedInstanceState ?? Intent.Extras;

            id = b.GetInt(ExtraId);
            supportedProducts = b.GetStringArray(ExtraProducts);
            front = b.GetInt(ExtraFront);
            apikey = b.GetString(ExtraApiKey);

            SetContentView(Resource.Layout.CameraLayout);

            cameraType = (CameraFacing)front;

            takePhotoButton = FindViewById<Button>(Resource.Id.takePhotoButton);
            takePhotoButton.Click += TakePhotoButtonTapped;

            textureView = FindViewById<TextureView>(Resource.Id.textureView);
            textureView.SurfaceTextureListener = this;

        }

        /// <summary>
        /// Events when surfacetexture is available, sets camera parameters
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void OnSurfaceTextureAvailable(SurfaceTexture surface, int width, int height)
        {
#pragma warning disable 618
            camera = Camera.Open((int)cameraType);
            var parameters = camera.GetParameters();
            if (parameters.SupportedFocusModes.Contains(Camera.Parameters.FocusModeContinuousPicture))
            {
                parameters.FocusMode = Camera.Parameters.FocusModeContinuousPicture;
            }
            camera.SetParameters(parameters);
#pragma warning restore 618
            textureView.LayoutParameters = new FrameLayout.LayoutParams(width, height);
            camera.SetPreviewTexture(surface);
            PrepareAndStartCamera();
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <returns></returns>
        public bool OnSurfaceTextureDestroyed(SurfaceTexture surface)
        {
            return true;
        }

        /// <summary>
        /// Resets camera
        /// </summary>
        /// <param name="surface">Surface</param>
        /// <param name="width">Width</param>
        /// <param name="height">Height</param>
        public void OnSurfaceTextureSizeChanged(SurfaceTexture surface, int width, int height)
        {
            PrepareAndStartCamera();
        }

        /// <summary>
        /// Does nothing
        /// </summary>
        /// <param name="surface"></param>
        public void OnSurfaceTextureUpdated(SurfaceTexture surface)
        {

        }

        private void PrepareAndStartCamera()
        {
            camera.StopPreview();

            var display = WindowManager.DefaultDisplay;
            if (display.Rotation == SurfaceOrientation.Rotation0)
            {
                camera.SetDisplayOrientation(90);
            }

            if (display.Rotation == SurfaceOrientation.Rotation270)
            {
                camera.SetDisplayOrientation(180);
            }

            camera.StartPreview();
        }

        private async void TakePhotoButtonTapped(object sender, EventArgs e)
        {
            try
            {
                camera.StopPreview();
                camera.Release();

                var image = textureView.Bitmap;
                using (var imageStream = new MemoryStream())
                {
                    await image.CompressAsync(Bitmap.CompressFormat.Jpeg, 50, imageStream);
                    image.Recycle();
                    imageBytes = imageStream.ToArray();
                }
                var cea = await GetCardAsync(id);
                OnCardRead(cea);
                Finish();

            }
            catch (Exception ex)
            {
                OnCardRead(new CardReadEventArgs(id, ex));
                Finish();
            }
        }

        private async Task<CardReadEventArgs> GetCardAsync(int requestCode)
        {
            try
            {
                var service = new CardRecognitionService(supportedProducts, apikey);

                var card = await service.RecognizeCard(imageBytes);

                imageBytes = null;

                return new CardReadEventArgs(requestCode, false, card);
            }
            catch (Exception ex)
            {
                return new CardReadEventArgs(requestCode, ex);
            }
        }

        private static void OnCardRead(CardReadEventArgs e)
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