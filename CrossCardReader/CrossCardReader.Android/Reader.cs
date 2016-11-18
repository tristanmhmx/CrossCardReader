using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using CrossCardReader.Abstractions;

namespace CrossCardReader.Android
{
    [Preserve(AllMembers = true)]
    public class ReaderImplementation : ICardReader
    {
        private Context context;
        private int requestId;
        private TaskCompletionSource<Card> completionSource;
        private string api;
        public bool IsCameraAvailable { get; }
        public ReaderImplementation()
        {
            context = Application.Context as Activity;
            IsCameraAvailable = context.PackageManager.HasSystemFeature(PackageManager.FeatureCamera);
        }

        public Task<bool> Initialize(string apiKey)
        {
            api = apiKey;
            return Task.FromResult(true);
        }

        public async Task<Card> RecognizeCardAsync(Products supportedProducts)
        {
            if(!IsCameraAvailable)
                throw new NotSupportedException();

            if(supportedProducts == null)
                throw new NotSupportedException();

            if(supportedProducts.Count == 0)
                throw new NotSupportedException();

            return await ReadAsync(supportedProducts, api);
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