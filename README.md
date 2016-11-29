##Media Plugin for Xamarin and Windows
Simple cross platform plugin read a non-setoff card from shared code.
Ported from Xamarin.Mobile to a cross platform API.

###Setup
*Install into your PCL project and Client projects.
*Please see the additional setup for each platforms permissions.

**Platform Support**

|Platform|Supported|Version|
| ------------------- | :-----------: | :------------------: |
|Xamarin.iOS|Yes|iOS 8+|
|Xamarin.iOS Unified|Yes|iOS 8+|
|Xamarin.Android|Yes|API 22+|

###API Usage
Call CrossCardReader.Current from any project or PCL to gain access to APIs.
Before taking photos or videos you should check to see if a camera exists and if photos and videos are supported on the device. 

###Usage
Via a Xamarin.Forms project with a Button and Image to take a photo:


```csharp
takePhoto.Clicked += async (sender, args) =>
{
    await CrossCardReader.Current.Initialize(“API_KEY”);

    var supportedProducts = new Products();
    supportedProducts.Add("55123804");
    supportedProducts.Add("40276602");
    
    var card = await CrossCardReader.CrossCardReader.Current.RecognizeCardAsync(supportedProducts);
};
```

###Important Permission Information
Please read these as they must be implemented for all platforms.

###Android
The CAMERA permission is required.

By adding these permissions Google Play will automatically filter out devices without specific hardward. You can get around this by adding the following to your AssemblyInfo.cs file in your Android project:
[assembly: UsesFeature("android.hardware.camera", Required = false)]
[assembly: UsesFeature("android.hardware.camera.autofocus", Required = false)]

###iOS
Your app is required to have NSPhotoLibraryUsageDescription  key in your Info.plist in order to access the device's camera. The string that you provide for each of these keys will be displayed to the user when they are prompted to provide permission to access these device features. You can read me here: https://blog.xamarin.com/new-ios-10-privacy-permission-settings/
Such as:
<key>NSCameraUsageDescription</key>
<string>This app needs access to the camera to take photos.</string>

License
Licensed under MIT, see license file.  // you may not use this file except in compliance with the License. // Unless required by applicable law or agreed to in writing, software // distributed under the License is distributed on an "AS IS" BASIS, // WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. // See the License for the specific language governing permissions and // limitations under the License. //

