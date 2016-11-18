using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrossCardReader.Abstractions;
using Foundation;
using UIKit;

namespace CrossCardReader.iOS
{
    public class ReaderImplementation : ICardReader
    {
        private string api;
        private UINavigationController navigationController;
        private ReaderController readerController;
        private int requestId;
        private TaskCompletionSource<Card> completionSource;
        public bool IsCameraAvailable { get; }

        public bool IsTakePhotoSupported { get; }
        public const string TypeImage = "public.image";
        public bool IsPickPhotoSupported { get; }
        public ReaderImplementation()
        {
            IsCameraAvailable = UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);
            var availableCameraMedia = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.Camera) ?? new string[0];
            var avaialbleLibraryMedia = UIImagePickerController.AvailableMediaTypes(UIImagePickerControllerSourceType.PhotoLibrary) ?? new string[0];

            foreach (var type in availableCameraMedia.Concat(avaialbleLibraryMedia))
            {
                if (type == TypeImage)
                    IsTakePhotoSupported = IsPickPhotoSupported = true;
            }
        }

        public Task<bool> Initialize(string apiKey)
        {
            api = apiKey;
            return Task.FromResult(true);
        }

        public Task<Card> RecognizeCardAsync(Products supportedProducts)
        {
            if(!IsTakePhotoSupported)
                throw new NotSupportedException();
            if(!IsCameraAvailable)
                throw new NotSupportedException();

            CheckCameraUsageDescription();

            return ReadAsync(supportedProducts, api);

        }

        private Task<Card> ReadAsync(Products supportedProducts, string apiKey)
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            if (window == null)
            {
                throw new InvalidOperationException("There's no current active window");
            }

            navigationController = FindNavigationController();

            if (navigationController == null)
            {
                throw new InvalidOperationException("Could not find current Navigation Controller");
            }

            var id = GetRequestId();
            var ntcs = new TaskCompletionSource<Card>(id);
            if (Interlocked.CompareExchange(ref completionSource, ntcs, null) != null)
                throw new InvalidOperationException("Only one operation can be active at a time");

            readerController = new ReaderController(supportedProducts.ToArray(), apiKey, id);
            navigationController.PresentModalViewController(readerController, true);

            EventHandler<CardReadEventArgs> handler = null;
            handler = (s, e) =>
            {
                var tcs = Interlocked.Exchange(ref completionSource, null);
                readerController.CardRead -= handler;
                if (e.RequestId != id)
                {
                    navigationController.DismissModalViewController(true);
                    return;
                }
                if (e.IsCanceled)
                    tcs.SetResult(null);
                else if (e.Error != null)
                    tcs.SetException(e.Error);
                else
                    tcs.SetResult(e.Card);
            };

            readerController.CardRead += handler;

            var result = completionSource.Task;
            navigationController.DismissModalViewController(true);
            return result;
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

        void CheckCameraUsageDescription()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (UIDevice.CurrentDevice.CheckSystemVersion(10, 0))
            {
                if (!info.ContainsKey(new NSString("NSCameraUsageDescription")))
                    throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSCameraUsageDescription in your Info.plist file to enable Authorization Requests for Camera access!");
            }
        }
        

        private UINavigationController FindNavigationController()
        {
            //Check to see if the roomviewcontroller is the navigationcontroller.
            foreach (var window in UIApplication.SharedApplication.Windows)
            {
                if (window.RootViewController.NavigationController != null)
                    return window.RootViewController.NavigationController;
                var val = CheckSubs(window.RootViewController.ChildViewControllers);
                if (val != null)
                    return val;
            }

            return null;
        }
        private UINavigationController CheckSubs(UIViewController[] controllers)
        {
            foreach (var controller in controllers)
            {
                //Check to see if the one of the childs is the navigationcontroller.
                if (controller.NavigationController != null)
                    return controller.NavigationController;
                var val = CheckSubs(controller.ChildViewControllers);
                if (val != null)
                    return val;
            }
            return null;
        }
    }
}
