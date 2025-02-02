﻿using System;
using System.Collections.Generic;
using Microsoft.Bot.Builder.AI.Luis;
using Microsoft.Bot.Configuration;

namespace IgniteWeatherBot.Services
{
    public class BotService
    {
        public BotService(BotConfiguration configuration)
        {
            foreach (var s in configuration.Services)
            {
                switch (s.Type)
                {
                    case ServiceTypes.Luis:
                        {
                            var luis = (LuisService)s;

                            if (luis == null)
                                throw new InvalidOperationException("The LUIS service is not configured correctly in your '.bot' file.");

                            var app = new LuisApplication(luis.AppId, luis.AuthoringKey, luis.GetEndpoint());
                            var recognizer = new LuisRecognizer(app);
                            this.LuisServices.Add(luis.Name, recognizer);
                            break;
                        }
                }
            }
        }

        public Dictionary<string, LuisRecognizer> LuisServices { get; } = new Dictionary<string, LuisRecognizer>();
    }
}
