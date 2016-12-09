using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CrossCardReader.Abstractions;
using Foundation;
using UIKit;

namespace CrossCardReader
{

    /// <summary>
    /// Read a card without card number setoff from a cross platform API.
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ReaderImplementation : ICardReader
    {
        private string api;
        private ReaderController readerController;
        private int requestId;
        private TaskCompletionSource<Card> completionSource;
        private UINavigationController navigationController; 
        /// <summary>
        /// Property to check if camera is available
        /// </summary>
        public bool IsCameraAvailable { get; }

        /// <summary>
        /// Implementation of the card reader
        /// </summary>
        public ReaderImplementation()
        {
            navigationController = FindNavigationController();
            IsCameraAvailable = UIImagePickerController.IsSourceTypeAvailable(UIImagePickerControllerSourceType.Camera);
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
            if (!IsCameraAvailable)
                throw new NotSupportedException("This device does not has a camera available");

            if (supportedProducts == null)
                throw new NotSupportedException("Product list cannot be null or empty");

            if (supportedProducts.Count == 0)
                throw new NotSupportedException("Product list cannot be null or empty");

            CheckCameraUsageDescription();

            return await ReadAsync(supportedProducts, api);
        }
        

        private Task<Card> ReadAsync(Products supportedProducts, string apiKey)
        {
            var window = UIApplication.SharedApplication.KeyWindow;

            if (window == null)
            {
                throw new InvalidOperationException("There's no current active window");
            }

            if (navigationController == null)
            {
                throw new InvalidOperationException("There's no current navigation controller");
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
                {
                    navigationController.DismissModalViewController(true);
                    tcs.SetResult(null);
                }
                else if (e.Error != null)
                {
                    navigationController.DismissModalViewController(true);
                    tcs.SetException(e.Error);
                }
                else
                {
                    navigationController.DismissModalViewController(true);
                    tcs.SetResult(e.Card);
                }
            };

            readerController.CardRead += handler;

            return completionSource.Task;
        }

        private int GetRequestId()
        {
            var id = requestId;
            if (requestId == int.MaxValue)
                requestId = 0;
            else
                requestId++;

            return id;
        }

        private static void CheckCameraUsageDescription()
        {
            var info = NSBundle.MainBundle.InfoDictionary;

            if (!UIDevice.CurrentDevice.CheckSystemVersion(10, 0)) return;

            if (!info.ContainsKey(new NSString("NSCameraUsageDescription")))
                throw new UnauthorizedAccessException("On iOS 10 and higher you must set NSCameraUsageDescription in your Info.plist file to enable Authorization Requests for Camera access!");
        }

        /// <summary>
        /// Find NavigationController in navigation stack.
        /// </summary>
        /// <returns>UINavigationController</returns>
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

        /// <summary>
        /// Find navigation controller in childs controllers.
        /// </summary>
        /// <param name="controllers">Controllers in the hierarchy of the app</param>
        /// <returns>Controller who have the NavigationController</returns>
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
