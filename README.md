This project is a work in progress, docs and releases will follow shortly.
Property of [AoD](https://agentsofdiscovery.com)

#App Unit Tests
This Xamarin.UITest suite uses an embedded UITest server in the Unity project to invoke its tests.  
This project targets .NET 4.x, thanks to being forced to use an old version of NUnit (2.7) because Xamarin.UITest is incompatible with NUnit 3+.

##Setup your Android test enviormnent
* Record your device's local IP address and enter it in Tests > TestEnv.cs > PHONE_IP
* Paste the absolute path of your APK in: Tests > TestEnv.cs > APK_PATH

##Build
Debug:
`dotnet msbuild`  
Release:
`dotnet msbuild -p:Configuration=Release`
