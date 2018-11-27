using System;
using Xamarin.UITest;
using Xamarin.GameTestServer;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Threading;
using System.Linq;
using System.Reflection;

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

        HttpClient client;

        private bool IsConnected { get; set; }

        public readonly IApp App;
        public UnityApp(IApp app, string deviceIp)
        {
            this.App = app;

            var isTestCloud = Environment.GetEnvironmentVariable("XAMARIN_TEST_CLOUD") == "1";
            var url = isTestCloud ? Environment.GetEnvironmentVariable("XTC_APP_ENDPOINT") : "http://" + deviceIp + ":8081";
            Console.WriteLine("Connecting to: {0}", url);

            client = new HttpClient
            {
                BaseAddress = new Uri(url),
            };

            Console.WriteLine("Initializing Game Driver");

            WaitForOnline();
            app.WaitFor(() => IsConnected, timeoutMessage: "Timeout while connecting to Game Server", timeout: new TimeSpan(0, 5, 0));
        }

        public void ScrollDown()
        {
            App.ScrollDown();
        }

        async Task<bool> WaitForOnline()
        {
            Console.WriteLine("Connecting to game server");
            client.Timeout = new TimeSpan(0, 2, 0);

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
                        DeviceInfo = result.Result;
                    IsConnected = success;
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

        public void SetLocation(double lat, double lng)
        {
            // The application does not have access mock location permission. Add the permission 'android.permission.ACCESS_MOCK_LOCATION' to your manifest
            // App.Device.SetLocation(lat, lng);
        }

        public void WaitForElement(string name, int timeout = defaultTimeout, string screenshot = "")
        {
            App.WaitFor(() => {
                var elems = GetAllObjects();
                return elems.Any(elem => elem.Name == name);
            }, retryFrequency: new TimeSpan(0, 0, 0, 0, 800), timeout: new TimeSpan(0, 0, 0, 0, timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));

            if (!string.IsNullOrEmpty(screenshot))
                App.Screenshot(screenshot);
        }

        public void WaitForText(string name, string text, int timeout = defaultTimeout, string screenshot = "")
        {
            WaitForText(
                GetGameInputField(name),
                text,
                timeout,
                screenshot);
        }

        public void WaitForText(GameInputField input, string text, int timeout = defaultTimeout, string screenshot = "")
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

        private async Task<NetworkResponse<DeviceInfo>> GetDeviceInfo()
        {
            return await GetAsync<DeviceInfo>("DeviceInfo");
        }

        public GameButton GetGameButton(string name = "")
        {
            return GetGameElem<GameButton>(name);
        }
        public GameButton[] GetGameButtons(string name = "")
        {
            return GetGameElems<GameButton>(name);
        }

        public GameInputField GetGameInputField(string name = "")
        {
            return GetGameElem<GameInputField>(name);
        }
        public GameInputField[] GetGameInputFields(string name = "")
        {
            return GetGameElems<GameInputField>(name);
        }

        public GameText GetGameText(string name = "")
        {
            return GetGameElem<GameText>(name);
        }
        public GameText[] GetGameTexts(string name = "")
        {
            return GetGameElems<GameText>(name);
        }

        public GameImage GetGameImage(string name = "")
        {
            return GetGameElem<GameImage>(name);
        }
        public GameImage[] GetGameImages(string name = "")
        {
            return GetGameElems<GameImage>(name);
        }

        private GameElement[] GetAllObjects(string name = "")
        {
            return GetGameElems<GameElement>();
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
                elements = Post<T[]>(gameObjectFindRoute, content);
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
            return Get<string>(currentScreenRoute);
        }

        public Task<NetworkResponse<string>> GetCurrentScreenAsync()
        {
            return GetAsync<string>(currentScreenRoute);
        }

        public GameButton InvokeButton(string name, int timeout = defaultTimeout)
        {
            GameButton button = null;
            App.WaitFor(() => {
                button = Post<GameButton>(invokeButtonRoute, string.Format("?name={0}", name));
                return button != null;
            }, retryFrequency: TimeSpan.FromMilliseconds(800), timeout: TimeSpan.FromMilliseconds(timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));

            return button;
        }

        public GameInputField InvokeText(string name, string text, int timeout = defaultTimeout)
        {
            GameInputField input = null;
            App.WaitFor(() => {
                input = Post<GameInputField>(invokeInputRoute, string.Format("?name={0}&text={1}", name, text));
                return input != null;
            }, retryFrequency: TimeSpan.FromMilliseconds(800), timeout: TimeSpan.FromMilliseconds(timeout), timeoutMessage: string.Format("Timed out waiting for: {0}", name));

            return input;
        }

        public void TapBack()
        {
            TapRelative(0.083f, 0.052f);
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

        public void Tap(string name)
        {
            Tap<GameButton>(name);
        }

        public void Tap<T>(string name, bool doubleTap = false) where T : GameElement
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

        public void Tap(GameElement element, bool doubleTap = false)
        {
            if (element == null)
                throw new Exception("Element not found");

            if (element.IsOnScreen.HasValue)
                if (!element.IsOnScreen.Value)
                    throw new Exception("Element is off screen");

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
        
        public void TapRelative(float xpercent, float ypercent)
        {
            if (DeviceInfo == null)
                throw new Exception("DeviceInfo is null");

            var xpos = xpercent * DeviceInfo.Width;
            var ypos = ypercent * DeviceInfo.Height;

            App.TapCoordinates(xpos, ypos);
        }
        
        public void PhysicalEnterTextAndDismiss(string name, string text)
        {
            var elm = GetGameInputField(name);
            PhysicalEnterTextAndDismiss(elm, text);
        }

        public void PhysicalEnterTextAndDismiss(string text)
        {
            TapRelative(0.46f, 0.89f); // For Android P
            App.EnterText(text);
            App.TapCoordinates(0, 0);
        }

        public void PhysicalEnterTextAndDismiss(GameElement element, string text)
        {
            Tap(element);
            TapRelative(0.46f, 0.89f); // For Android P

            App.EnterText(text);

            App.TapCoordinates(0, 0);
        }

        public T Get<T>(string path, string content = "")
        {
            var result = client.GetAsync(path + content).Result;
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
            var result = client.PostAsync(path, new StringContent(content)).Result;
            var success = result.IsSuccessStatusCode;
            if (!success)
                return default(T);
            var json = result.Content.ReadAsStringAsync().Result;

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<T> PostAsync<T>(string path, string content)
        {
            var result = await client.PostAsync(path, new StringContent(content));
            var success = result.IsSuccessStatusCode;
            if (!success)
                return default(T);
            var json = await result.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<T>(json);
        }

        public async Task<NetworkResponse<T>> GetAsync<T>(string path, string content = "")
        {
            var result = await client.GetAsync(path + content);
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

        public void ScreenShot(string step)
        {
            Console.WriteLine("Screen shotting {0}", step);
            App.Screenshot(step);
        }
    }

    public class NetworkResponse<T>
    {
        public bool Succeeded { get; set; }
        public T Result { get; set; }
    }
}