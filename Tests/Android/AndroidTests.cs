using NUnit.Framework;
using Xamarin.UITest;
using System.Linq;
using System.Threading;
using System;

namespace UnityTest.Tests.Android
{
    class AndroidTests : TestEnv
    {
        [TestFixture]
        public class TestDA
        {
            UnityApp App;
            [SetUp]
            public void Setup()
            {
                App = new UnityApp(
                    ConfigureApp.Android.ApkFile(APK_PATH).StartApp(),
                    PHONE_IP
                );
            }

            [Test]
            public void GoToDashboard()
            {
                
            }
        }
    }
}