using System;
using Xamarin.UITest;
using Xamarin.GameTestServer;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

namespace UnityTest
{
    public class UnityApp
    {
        public DeviceInfo DeviceInfo { get; set; }

        const int defaultTimeout = 15000;
        const string currentScreenRoute = "CurrentScreen";
        const string gameObjectFindRoute = "GameObjectFind";
        const string invokeButtonRoute = "InvokeButton";
        const string invokeInputRoute = "InvokeInput";
        
        private bool IsConnected { get; set; }
        private UITestClient Client { get; set; }

        public readonly IApp App;
        public UnityApp(IApp app, string deviceIp)
        {
            this.App = app;

            var isTestCloud = Environment.GetEnvironmentVariable("XAMARIN_TEST_CLOUD") == "1";
            var url = isTestCloud ? Environment.GetEnvironmentVariable("XTC_APP_ENDPOINT") : "http://" + deviceIp + ":8081";
            Console.WriteLine("Connecting to: {0}", url);

            Client = new UITestClient(url);

            Console.WriteLine("Initializing Game Driver");

            WaitForOnline();
            app.WaitFor(() => IsConnected, timeoutMessage: "Timeout while connecting to Game Server", timeout: new TimeSpan(0, 5, 0));
        }

        async Task<bool> WaitForOnline()
        {
            Console.WriteLine("Connecting to game server");

            int count = 0;
            bool success = false;
            while (!success && count < 20)
            {
                try
                {
                    await Task.Delay(2000);
                    var result = await GetDeviceInfo();
                    success = result.Succeeded;

                    if (success)
                    {
                        DeviceInfo = result.Result;
                        IsConnected = success;

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    // Console.WriteLine(ex);
                    Console.WriteLine("Fail to connect to game server try {0}", count + 1);
                }
                finally
                {
                    count++;
                }
            }

            return false;
        }

        public void WaitForElement(string name, int timeout = defaultTimeout, string screenshot = "")
        {
            App.WaitFor(() => {
                return GetGameElements(name).Count() > 0;
            }, retryFrequency: new TimeSpan(0, 0, 0, 0, 800), timeout: new TimeSpan(0, 0, 0, 0, timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));

            if (!string.IsNullOrEmpty(screenshot))
                App.Screenshot(screenshot);
        }

        public void WaitForInput(string name, string text, int timeout = defaultTimeout, string screenshot = "")
        {
            WaitForInput(
                GetGameInputField(name),
                text,
                timeout,
                screenshot);
        }

        public void WaitForInput(GameInputField input, string text, int timeout = defaultTimeout, string screenshot = "")
        {
            App.WaitFor(() => {
                return input.Text == text;
            }, retryFrequency: new TimeSpan(0, 0, 0, 0, 800), timeout: new TimeSpan(0, 0, 0, 0, timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", input.Name));
            if (!string.IsNullOrEmpty(screenshot))
                App.Screenshot(screenshot);
        }

        public void WaitForScreen(string name, int timeout = defaultTimeout, string screenshot = "")
        {
            App.WaitFor(() => {
                var curScreen = GetCurrentScreen();
                return curScreen == name;
            }, retryFrequency: new TimeSpan(0, 0, 0, 0, 800), timeout: new TimeSpan(0, 0, 0, 0, timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));
            if (!string.IsNullOrEmpty(screenshot))
                App.Screenshot(screenshot);
        }

        private async Task<UITestClient.NetworkResponse<DeviceInfo>> GetDeviceInfo()
        {
            return await Client.GetAsync<DeviceInfo>("DeviceInfo");
        }

        public GameButton GetGameButton(string name)
        {
            return GetGameElem<GameButton>(name);
        }
        public GameButton[] GetGameButtons(string name)
        {
            return GetGameElems<GameButton>(name);
        }

        public GameInputField GetGameInputField(string name)
        {
            return GetGameElem<GameInputField>(name);
        }
        public GameInputField[] GetGameInputFields(string name)
        {
            return GetGameElems<GameInputField>(name);
        }

        public GameText GetGameText(string name)
        {
            return GetGameElem<GameText>(name);
        }
        public GameText[] GetGameTexts(string name)
        {
            return GetGameElems<GameText>(name);
        }

        public GameImage GetGameImage(string name)
        {
            return GetGameElem<GameImage>(name);
        }

        public GameImage[] GetGameImages(string name)
        {
            return GetGameElems<GameImage>(name);
        }

        public GameElement[] GetGameElements(string name)
        {
            return GetGameElems<GameElement>(name);
        }

        private T[] GetGameElems<T>(string name = "", int timeout = defaultTimeout) where T : GameElement
        {
            var attr = typeof(T).GetCustomAttribute(typeof(GameObjectTypeAttribute), false) as GameObjectTypeAttribute;
            var attrType = attr.Type;

            var content = string.Format("?type={0}", attrType);
            if (!string.IsNullOrEmpty(name))
                content += string.Format("&name={0}", name);

            T[] elements = null;
            App.WaitFor(() => {
                elements = Client.Post<T[]>(gameObjectFindRoute, content);
                return elements != null;
            }, retryFrequency: TimeSpan.FromMilliseconds(800), timeout: TimeSpan.FromMilliseconds(timeout), timeoutMessage: string.Format("Timed out waiting for: {0} : {1}", name, attrType));

            return elements;
        }
        private T GetGameElem<T>(string name) where T : GameElement
        {
            return GetGameElems<T>(name).First();
        }

        public string GetCurrentScreen()
        {
            return Client.Get<string>(currentScreenRoute);
        }

        public Task<UITestClient.NetworkResponse<string>> GetCurrentScreenAsync()
        {
            return Client.GetAsync<string>(currentScreenRoute);
        }

#region Interaction
        public GameButton InvokeButton(string name, int timeout = defaultTimeout)
        {
            GameButton button = null;
            App.WaitFor(() => {
                button = Client.Post<GameButton>(invokeButtonRoute, string.Format("?name={0}", name));
                return button != null;
            }, retryFrequency: TimeSpan.FromMilliseconds(800), timeout: TimeSpan.FromMilliseconds(timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));

            return button;
        }

        public GameInputField InvokeInput(string name, string text, int timeout = defaultTimeout)
        {
            GameInputField input = null;
            App.WaitFor(() => {
                input = Client.Post<GameInputField>(invokeInputRoute, string.Format("?name={0}&text={1}", name, text));
                return input != null;
            }, retryFrequency: TimeSpan.FromMilliseconds(800), timeout: TimeSpan.FromMilliseconds(timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));

            return input;
        }

        public void Tap(string name)
        {
            Tap<GameButton>(name);
        }

        public void Tap<T>(string name) where T : GameElement
        {
            Tap<T>(name);
        }

        private void Tap<T>(string name, bool doubleTap = false) where T : GameElement
        {
            var t = typeof(T);
            GameElement btn = null;

            if (t == typeof(GameButton))
            {
                btn = GetGameButton(name);
            } else if (t == typeof(GameInputField))
            {
                btn = GetGameInputField(name);
            } else
            {
                btn = GetGameElem<GameElement>(name);
            }

            Tap(btn, doubleTap);
        }

        public void Tap(GameElement element)
        {
            Tap(element);
        }

        private void Tap(GameElement element, bool doubleTap = false)
        {
            if (element == null)
                throw new Exception("Element not found");

            if (!element.IsActive)
                throw new Exception("Element is inactive");

            // TODO: Look this up, 0, 0 is 0, 1920 for some reason
            // float y = DeviceInfo.Height - element.Location.Y;

            float y = (DeviceInfo.Height - element.Location.Y) + element.Rectangle.Height / 3;
            float x = element.Location.X + (element.Rectangle.Width / 3);

            if (element.GetType() == typeof(GameButton))
            {
                y = DeviceInfo.Height - element.Location.Y;
                x = element.Location.X;
            }

            if (doubleTap)
                App.DoubleTapCoordinates(x, y);
            else
                App.TapCoordinates(x, y);
        }

        public void DelayedTap(string name)
        {
            // Let animations finish
            Thread.Sleep(1000);
            Tap(name);
            Thread.Sleep(1000);
        }

        public void DoubleTap(string name)
        {
            Tap<GameButton>(name, true);
        }

        public void DoubleTap(GameElement element)
        {
            Tap(element, true);
        }

        public void TapRelative(float xpercent, float ypercent)
        {
            if (DeviceInfo == null)
                throw new Exception("DeviceInfo is null");

            var xpos = xpercent * DeviceInfo.Width;
            var ypos = ypercent * DeviceInfo.Height;

            App.TapCoordinates(xpos, ypos);
        }

        public void TapAbsolute(float x, float y)
        {
            if (DeviceInfo == null)
                throw new Exception("DeviceInfo is null");
            
            App.TapCoordinates(x, y);
        }

        public void PhysicalEnterTextAndDismiss(string name, string text)
        {
            var elm = GetGameInputField(name);
            App.EnterText(text);
        }

        public void ScrollUp()
        {
            App.ScrollUp();
        }

        public void ScrollDown()
        {
            App.ScrollDown();
        }

        public void Screenshot(string step)
        {
            Console.WriteLine("Screen shotting {0}", step);
            App.Screenshot(step);
        }
#endregion
    }

    public class UITestClient
    {
        private HttpClient Client { get; set; }
        public UITestClient(string url)
        {
            Client = new HttpClient
            {
                BaseAddress = new Uri(url),
                Timeout = new TimeSpan(0, 2, 0)
            };
        }

        public T Get<T>(string path, string content = "")
        {
            var result = Client.GetAsync(path + content).Result;
            var success = result.IsSuccessStatusCode;
            if (!success)
                return default(T);
            var json = result.Content.ReadAsStringAsync().Result;
            if (json is T)
                return (T)(object)json;
            Console.WriteLine(json);
            return JsonConvert.DeserializeObject<T>(json);
        }

        public T Post<T>(string path, string content)
        {
            var result = Client.PostAsync(path, new StringContent(content)).Result;
            var success = result.IsSuccessStatusCode;
            if (!success)
                return default(T);
            var json = result.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<T> PostAsync<T>(string path, string content)
        {
            var result = await Client.PostAsync(path, new StringContent(content));
            var success = result.IsSuccessStatusCode;
            if (!success)
                return default(T);
            var json = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<NetworkResponse<T>> GetAsync<T>(string path, string content = "")
        {
            var result = await Client.GetAsync(path + content);
            var success = result.IsSuccessStatusCode;
            if (!success)
                return new NetworkResponse<T>()
                {
                    Succeeded = success,
                    Result = default(T)
                };
            var json = await result.Content.ReadAsStringAsync();
            if (json is T)
                return new NetworkResponse<T>()
                {
                    Succeeded = success,
                    Result = (T)(object)json
                };

            Console.WriteLine(json);
            return new NetworkResponse<T>()
            {
                Succeeded = success,
                Result = JsonConvert.DeserializeObject<T>(json)
            };
        }

        public class NetworkResponse<T>
        {
            public bool Succeeded { get; set; }
            public T Result { get; set; }
        }
    }
}