# UITest Unity Client
This test suite abstracts a number of common [Xamarin.UITest](https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/) functions with an HttpClient in order to interact with the embedded [UITest Unity Server](https://bitbucket.org/agentsofdiscovery/uitestserver). This project targets .NET 4.X standard, and uses NUnit 2.7, because Xamarin.UITest is incompatible with NUnit 3+.

## Setup Android Tests
* Record your test device's local IP address (`DEVICE_IP`)
* Record the path to your APK that includes the embedded UITest Server (`APK_PATH`)

Tests are written with NUnit. The `SetUp` method should configure the `UnityApp` API like so:
```csharp
[TestFixture]
class AndroidTests
{
    UnityApp App; // This will be used a lot, make it global
    [SetUp] // This gets run before any of our tests
    void Setup()
    {
        App = new UnityApp(
            ConfigureApp.Android.ApkFile(APK_PATH).StartApp(),
            DEVICE_IP
        );
    }

    // My tests...
}
```

## Setup iOS Tests
* Record your test device's local IP address (`DEVICE_IP`)
* Record the path to your Bundle that includes the embedded UITest Server (`APP_PATH`)

iOS apps must be packaged into a bundle with XCode, and include an embedded [Calabash server](https://github.com/calabash/calabash-ios/wiki/Tutorial%3A-Link-Calabash-in-Debug-config).

```csharp
[TestFixture]
class IosTests : TestEnv
{
    UnityApp App;
    [SetUp]
    void Setup()
    {
        App = new UnityApp(
            ConfigureApp.iOS.AppBundle(APP_PATH).StartApp(),
            DEVICE_IP
        );
    }
}
```

## Writing tests
`UnityApp` contains a number of helpers for performing UI operations on the device and wraps some common Xamarin.UITest functions. API details can be found below.

An example test with `UnityApp`:
```csharp
[Test]
void GoToLogin()
{
    // Wait for Main menu to show up
    // Note the extra-long timeout since we're expecting a long load
    App.WaitForScreen("MainMenu", 60000, "Menu loaded");
    
    // Tap the login button
    App.Tap("Login");
    
    // Wait for login scene to load, and screenshot it when it does
    App.WaitForScreen("UserLogin", screenshot: "Login page");

    // Our wait did not timeout, so our UserLogin scene is visible, test succeeded!
}
```

## Building
Debug:
`dotnet msbuild`  
Release:
`dotnet msbuild -p:Configuration=Release`

## Submitting to App Center
* Build your APK
* Build your tests with the aforementioned build process
* Go to App Center
* Create a New Test Run, select your device set
* Submit with the CLI tool (see `uitest.bat` as example)
---
## UnityApp

This document wraps many of the basic functions of [Xamarin.UITest](https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/), and abstracts the networking layer. Ideally, this should be broken up by route, maybe in the future.   
It also enables access to `IApp`, which is the interface for [Xamarin.UITest](https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/), which enables the interaction with the device itself. So if you want to extend this in the future, it's worth reading up on what you can do with [Xamarin.UITest](https://docs.microsoft.com/en-us/appcenter/test-cloud/uitest/). An example would be setting the device lat/long.

On construction, it creates an HTTP Client and attempts connecting to the test server. Upon connection, it queries the `DeviceInfo`, which contains information about the devices display so that we can perform taps relative to its size.

### GameObjects
In our embedded UITest Server, we defined a number of POCO objects that were serialized and sent as responses. The ones in this client project mirror those, since they are deserialized by our UITest Client. So any changes to those objects must be consistent from the client and server.

### Public API

`void Screenshot()`  
Takes and saves a screenshot, disabled by default locally

`void ScrollDown()`  
Scrolls down

`void ScrollUp()`  
Scrolls up

`void WaitForElement(string name, int timeout = defaultTimeout, string screenshot = "")`  
Queries, and waits for GameElement to appear on screen, takes element name. Optional parameters allow you to specify a timeout, and a screenshot name.

`void WaitForInput(string name, string text, int timeout = defaultTimeout, string screenshot = "")`  
Queries, and waits for an `Input` to contain specific text. Useful for when you expect an input to eventually contain text, especially when using a physical input.

`void WaitForInput(GameInputField input, string text, int timeout = defaultTimeout, string screenshot = "")`  
Waits for an existing `Input` to contain specific text. Useful for when you expect an input to eventually contain text, especially when using a physical input.

`void WaitForScreen(string name, int timeout = defaultTimeout, string screenshot = "")`  
Queries and waits for a specific scene to open.

`async Task<NetworkResponse<DeviceInfo>> GetDeviceInfo()`  
Gets basic display info, including `Height`, `Width`, and `DPI`

`GameButton GetGameButton(string name)`  
Gets first matching `Button` by `name`, otherwise will throw an exception

`GameButton[] GetGameButtons(string name)`  
Gets all matching `Button`'s by `name`

`GameInputField GetGameInputField(string name)`  
Gets first matching `Input` by `name`, otherwise will throw an exception

`GameInputField[] GetGameInputFields(string name)`  
Gets all matching `Input`'s by `name`

`GameText GetGameText(string name)`  
Gets first matching `Text` by `name`, otherwise will throw an exception

`GameText[] GetGameTexts(string name)`  
Gets all matching `Text`'s by `name`

`GameImage GetGameImage(string name)`  
Gets first matching `Image` by `name`, otherwise will throw an exception

`GameImage[] GetGameImages(string name)`  
Gets all matching `Image`'s by `name`

`string GetCurrentScreen()`  
Gets the current open screen

`GameButton InvokeButton(string name, int timeout = defaultTimeout)`  
Queries for a `Button` by `name`, and manually invokes its click event

`GameInputField InvokeInput(string name, string text, int timeout = defaultTimeout)`  
Queries for an `Input` by name, and sets specified `text`

`void Tap(GameElement element)`  
"Physically" taps an existing `GameElement`

`void Tap(string name)`  
Queries and "physically" taps a `Button` by `name`

`void Tap<T>(string name) where T : GameElement`  
Queries and "physically" taps a
* `GameButton`
* `GameInputField`
* `GameElement`

This can be used to tap any element that is on-screen.

`void DoubleTap(string name)`  
Double taps a `Button` by `name`

`void DoubleTap(GameElement element)`  
Double taps a `GameObject` by `name`

`void DelayedTap(string name)`  
Waits 1000ms, taps a button, waits another 1000ms

`void TapRelative(float xpercent, float ypercent)`  
Taps the display relative to its size, accepts percentages as input.

`void TapAbsolute(float x, float y)`  
Taps the display with the specified absolute coordinates.

`void PhysicalEnterTextAndDismiss(string name, string text)`  
Physically enters text with the device keyboard.