using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Storage;
using Windows.Web.Http;
using CK.HomeAutomation.Actuators;
using CK.HomeAutomation.Actuators.Contracts;
using CK.HomeAutomation.Core.Timer;
using CK.HomeAutomation.Networking;
using CK.HomeAutomation.Notifications;
using HttpMethod = CK.HomeAutomation.Networking.HttpMethod;

namespace CK.HomeAutomation.Hardware.OpenWeatherMapWeatherStation
{
    public class OWMWeatherStation : IWeatherStation
    {
        private readonly INotificationHandler _notificationHandler;
        private readonly Uri _weatherDataSourceUrl;
        private readonly WeatherStationTemperatureSensor _temperature = new WeatherStationTemperatureSensor();
        private readonly WeatherStationHumiditySensor _humidity = new WeatherStationHumiditySensor();

        private DateTime? _lastFetched;
        private TimeSpan _sunrise;
        private TimeSpan _sunset;
        
        public OWMWeatherStation(double lat, double lon, string appId, IHomeAutomationTimer timer, IHttpRequestController httpApiController, INotificationHandler notificationHandler)
        {
            if (timer == null) throw new ArgumentNullException(nameof(timer));
            if (httpApiController == null) throw new ArgumentNullException(nameof(httpApiController));
            if (notificationHandler == null) throw new ArgumentNullException(nameof(notificationHandler));

            Temperature = _temperature;
            Humidity = _humidity;

            _notificationHandler = notificationHandler;
            _weatherDataSourceUrl = new Uri(string.Format("http://api.openweathermap.org/data/2.5/weather?lat={0}&lon={1}&APPID={2}&units=metric", lat, lon, appId));

            httpApiController.Handle(HttpMethod.Get, "weatherStation").Using(c => c.Response.Body = new JsonBody(ApiGet()));
            httpApiController.Handle(HttpMethod.Post, "weatherStation").WithRequiredJsonBody().Using(c => ApiPost(c.Request));

            LoadPersistedValues();
            timer.Every(TimeSpan.FromMinutes(2.5)).Do(Update);
        }

        public Daylight Daylight => new Daylight(_sunrise, _sunset);
        public ITemperatureSensor Temperature { get; }
        public IHumiditySensor Humidity { get; }

        public JsonObject ApiGet()
        {
            var result = new JsonObject();
            result.SetNamedValue("temperature", JsonValue.CreateNumberValue(Temperature.Value));
            result.SetNamedValue("humidity", JsonValue.CreateNumberValue(Humidity.Value));

            result.SetNamedValue("lastFetched",
                _lastFetched.HasValue ? JsonValue.CreateStringValue(_lastFetched.Value.ToString("O")) : JsonValue.CreateNullValue());

            result.SetNamedValue("sunrise", JsonValue.CreateStringValue(_sunrise.ToString()));
            result.SetNamedValue("sunset", JsonValue.CreateStringValue(_sunset.ToString()));

            return result;
        }

        private async void Update()
        {
            try
            {
                var json = await FetchWeatherData();
                string filename = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WeatherStationValues.json");
                File.WriteAllText(filename, json.Stringify());

                Update(json);
                _lastFetched = DateTime.Now;
            }
            catch (Exception exception)
            {
                _notificationHandler.PublishFrom(this, NotificationType.Warning, "Could not fetch weather information. " + exception.Message);
            }
        }

        private void Update(JsonObject data)
        {
            var sys = data["sys"].GetObject();
            var sunriseValue = sys["sunrise"].GetNumber();
            var sunsetValue = sys["sunset"].GetNumber();

            _sunrise = UnixTimeStampToDateTime(sunriseValue).TimeOfDay;
            _sunset = UnixTimeStampToDateTime(sunsetValue).TimeOfDay;

            var main = data["main"].GetObject();
            _temperature.UpdateValue((float) main["temp"].GetNumber());
            _humidity.UpdateValue((float) main["humidity"].GetNumber());
        }

        private async Task<JsonObject> FetchWeatherData()
        {
            using (var httpClient = new HttpClient())
            using (var result = await httpClient.GetAsync(_weatherDataSourceUrl))
            {
                var jsonContent = await result.Content.ReadAsStringAsync();
                return JsonObject.Parse(jsonContent);
            }
        }

        private DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            var buffer = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            return buffer.AddSeconds(unixTimeStamp).ToLocalTime();
        }

        private void ApiPost(HttpRequest request)
        {
            _temperature.UpdateValue((float) request.JsonBody.GetNamedNumber("temperature"));
            _humidity.UpdateValue((float) request.JsonBody.GetNamedNumber("humidity"));
            _sunrise = TimeSpan.Parse(request.JsonBody.GetNamedString("sunrise"));
            _sunset = TimeSpan.Parse(request.JsonBody.GetNamedString("sunset"));

            _lastFetched = DateTime.Now;
        }

        private void LoadPersistedValues()
        {
            string filename = Path.Combine(ApplicationData.Current.LocalFolder.Path, "WeatherStationValues.json");
            if (!File.Exists(filename))
            {
                return;
            }

            var values = JsonObject.Parse(File.ReadAllText(filename));
            Update(values);
        }
    }
}