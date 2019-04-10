using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.Bot.Builder;

namespace IgniteWeatherBot.Helpers
{
    public static class LuisParser
    {
        public static string GetEntityValue(RecognizerResult result)
        {
            foreach (var entity in result.Entities)
            {
                var location = JObject.Parse(entity.Value.ToString())[Constants.LocationLabel];
                var locationPattern = JObject.Parse(entity.Value.ToString())[Constants.LocationPatternLabel];

                if (location != null || locationPattern != null)
                {
                    dynamic value = JsonConvert.DeserializeObject<dynamic>(entity.Value.ToString());

                    if (location != null)
                        return value.Location[0].text;

                    if (locationPattern != null)
                        return value.Location_PatternAny[0].text;
                }
            }

            return string.Empty;
        }
    }
}
