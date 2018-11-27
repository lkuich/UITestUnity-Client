using NUnit.Framework;
using Xamarin.UITest;
using System.Threading;
using System;

namespace UnityTest.Tests.iOS
{
    [TestFixture]
    class IosTests : TestEnv
    {
        UnityApp App;
        [SetUp]
        public void Setup()
        {
            App = new UnityApp(
                ConfigureApp.iOS.AppBundle(APP_PATH).StartApp(),
                PHONE_IP
            );
        }
    }
}
