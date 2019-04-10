using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using IgniteWeatherBot.Helpers;
using IgniteWeatherBot.Models;

namespace IgniteWeatherBot.Services
{
    public static class WeatherService
    {
        public static async Task<WeatherModel> GetWeather(string city)
        {
            var query = $"{Constants.OpenWeatherMapURL}?q={city}&appid={Constants.OpenWeatherMapKey}";

            using (var client = new HttpClient())
            {
                var getWeather = await client.GetAsync(query);

                if (getWeather.IsSuccessStatusCode)
                {
                    var json = await getWeather.Content.ReadAsStringAsync();
                    var weather = JsonConvert.DeserializeObject<WeatherModel>(json);

                    weather.main.temp = weather.main.temp - 273.15;
                    return weather;
                }
            }
            return default(WeatherModel);
        }
    }
}
