##Media Plugin for Xamarin and Windows
Simple cross platform plugin read a non-setoff card from shared code.
Ported from Xamarin.Mobile to a cross platform API.

###Setup
* Available on NuGet: https://www.nuget.org/packages/CrossCardReader/ [![NuGet](https://img.shields.io/nuget/v/CrossCardReader.svg?label=NuGet)](https://www.nuget.org/packages/CrossCardReader/)
* Install into your PCL project and Client projects.
* Please see the additional setup for each platforms permissions.

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

The MIT License (MIT)

Copyright (c) 2016 Alset & Tristan Martinez

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

