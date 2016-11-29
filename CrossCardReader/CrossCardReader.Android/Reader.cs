using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using CrossCardReader.Abstractions;

namespace CrossCardReader
{
    /// <summary>
    /// Read a card without card number setoff from a cross platform API.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ReaderImplementation : ICardReader
    {
        private readonly Context context;
        private int requestId;
        private TaskCompletionSource<Card> completionSource;
        private string api;
        /// <summary>
        /// Property to check if camera is available
        /// </summary>
        public bool IsCameraAvailable { get; }
        /// <summary>
        /// Implementation of the card reader
        /// </summary>
        public ReaderImplementation()
        {
            context = Application.Context;
            IsCameraAvailable = context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);
        }
        /// <summary>
        /// Initializes control and sets Cognitive Api Key
        /// </summary>
        /// <param name="apiKey">Cognitvie Services Api Key</param>
        /// <returns>True if initialized</returns>
        public Task<bool> Initialize(string apiKey)
        {
            api = apiKey;
            return Task.FromResult(true);
        }

        /// <summary>
        /// Recognizes a card given a list of supportes bins (8 digits)
        /// </summary>
        /// <param name="supportedProducts">Hashset of supported bins</param>
        /// <returns>Card number and expiration</returns>
        /// <exception cref="NotSupportedException">Exception thrown if camera is not available, supported products is null or hashset count is 0</exception>
        public async Task<Card> RecognizeCardAsync(Products supportedProducts)
        {
            if(!IsCameraAvailable)
                throw new NotSupportedException("This device does not has a camera available");

            if(supportedProducts == null)
                throw new NotSupportedException("Product list cannot be null or empty");

            if(supportedProducts.Count == 0)
                throw new NotSupportedException("Product list cannot be null or empty");

            CheckCameraUsageDescription();

            return await ReadAsync(supportedProducts, api);
        }

        void CheckCameraUsageDescription()
        {
            const string permission = Manifest.Permission.Camera;
            
            if (context.CheckCallingOrSelfPermission(permission) != (int)Permission.Granted)
            {
                throw new UnauthorizedAccessException("You must enable CAMERA usage on your Manifest file");
            }
        }

        private Task<Card> ReadAsync(Products supportedProducts, string apiKey)
        {
            var id = GetRequestId();
            var ntcs = new TaskCompletionSource<Card>(id);
            if(Interlocked.CompareExchange(ref completionSource, ntcs, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");
            context.StartActivity(CreateReaderIntent(id, supportedProducts, apiKey));
            EventHandler<CardReadEventArgs> handler = null;
            handler = (s, e) =>
            {
                var tcs = Interlocked.Exchange(ref completionSource, null);
                CardReaderView.CardRead -= handler;
                if (e.RequestId != id)
                    return;
                if (e.IsCanceled)
                    tcs.SetResult(null);
                else if (e.Error != null)
                    tcs.SetException(e.Error);
                else
                    tcs.SetResult(e.Card);
            };

            CardReaderView.CardRead += handler;

            return completionSource.Task;
        }

        private Intent CreateReaderIntent(int id, Products supportedProducts, string apiKey)
        {
            var reader = new Intent(context, typeof(CardReaderView));
            reader.PutExtra(CardReaderView.ExtraId, id);
            reader.PutExtra(CardReaderView.ExtraProducts, supportedProducts.ToArray());
            reader.PutExtra(CardReaderView.ExtraFront, 0);
            reader.PutExtra(CardReaderView.ExtraApiKey, apiKey);
            reader.SetFlags(ActivityFlags.NewTask);
            return reader;
        }
        private int GetRequestId()
        {
            int id = requestId;
            if (requestId == Int32.MaxValue)
                requestId = 0;
            else
                requestId++;

            return id;
        }
    }
}