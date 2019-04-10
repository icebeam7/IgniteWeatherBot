// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IgniteWeatherBot.Helpers;
using IgniteWeatherBot.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;

namespace IgniteWeatherBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class IgniteWeatherBotBot : IBot
    {
        private readonly IgniteWeatherBotAccessors _accessors;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the class.
        /// </summary>
        /// <param name="conversationState">The managed conversation state.</param>
        /// <param name="loggerFactory">A <see cref="ILoggerFactory"/> that is hooked to the Azure App Service provider.</param>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-2.1#windows-eventlog-provider"/>
        public IgniteWeatherBotBot(ConversationState conversationState, ILoggerFactory loggerFactory)
        {
            if (conversationState == null)
            {
                throw new System.ArgumentNullException(nameof(conversationState));
            }

            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _accessors = new IgniteWeatherBotAccessors(conversationState)
            {
                CounterState = conversationState.CreateProperty<CounterState>(IgniteWeatherBotAccessors.CounterStateName),
            };

            _logger = loggerFactory.CreateLogger<IgniteWeatherBotBot>();
            _logger.LogTrace("Turn start.");
        }

        /// <summary>
        /// Every conversation turn for our Echo Bot will call this method.
        /// There are no dialogs used, since it's "single turn" processing, meaning a single
        /// request and response.
        /// </summary>
        /// <param name="turnContext">A <see cref="ITurnContext"/> containing all the data needed
        /// for processing this conversation turn. </param>
        /// <param name="cancellationToken">(Optional) A <see cref="CancellationToken"/> that can be used by other objects
        /// or threads to receive notice of cancellation.</param>
        /// <returns>A <see cref="Task"/> that represents the work queued to execute.</returns>
        /// <seealso cref="BotStateSet"/>
        /// <seealso cref="ConversationState"/>
        /// <seealso cref="IMiddleware"/>
        //public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        //{
        //    // Handle Message activity type, which is the main activity type for shown within a conversational interface
        //    // Message activities may contain text, speech, interactive cards, and binary or unknown attachments.
        //    // see https://aka.ms/about-bot-activity-message to learn more about the message and other activity types
        //    if (turnContext.Activity.Type == ActivityTypes.Message)
        //    {
        //        // Get the conversation state from the turn context.
        //        var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

        //        // Bump the turn count for this conversation.
        //        state.TurnCount++;

        //        // Set the property using the accessor.
        //        await _accessors.CounterState.SetAsync(turnContext, state);

        //        // Save the new turn count into the conversation state.
        //        await _accessors.ConversationState.SaveChangesAsync(turnContext);

        //        // Echo back to the user whatever they typed.
        //        var responseMessage = $"Turn {state.TurnCount}: You sent '{turnContext.Activity.Text}'\n";
        //        await turnContext.SendActivityAsync(responseMessage);
        //    }
        //    else
        //    {
        //        await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
        //    }
        //}

        private readonly BotService _services;
        public static readonly string LuisKey = "IgniteWeatherBotBot";

        public IgniteWeatherBotBot(BotService services)
        {
            _services = services ?? throw new System.ArgumentNullException(nameof(services));

            if (!_services.LuisServices.ContainsKey(LuisKey))
                throw new System.ArgumentException($"Invalid configuration....");
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                var recognizer = await _services.LuisServices[LuisKey].RecognizeAsync(turnContext, cancellationToken);
                var topIntent = recognizer?.GetTopScoringIntent();

                if (topIntent != null && topIntent.HasValue && topIntent.Value.intent != "None")
                {
                    var location = LuisParser.GetEntityValue(recognizer);

                    if (location.ToString() != string.Empty)
                    {
                        var ro = await WeatherService.GetWeather(location);
                        var weather = $"{ro.weather.First().main} ({ro.main.temp.ToString("N2")} °C)";

                        var typing = Activity.CreateTypingActivity();
                        var delay = new Activity { Type = "delay", Value = 5000 };

                        var activities = new IActivity[] {
                            typing,
                            delay,
                            MessageFactory.Text($"Weather of {location} is: {weather}"),
                            MessageFactory.Text("Thanks for using our service!")
                        };

                        await turnContext.SendActivitiesAsync(activities);
                    }
                    else
                        await turnContext.SendActivityAsync($"==>Can't understand you, sorry!");
                }
                else
                {
                    var msg = @"No LUIS intents were found.
                    This sample is about identifying a city and an intent:
                    'Find the current weather in a city'
                    Try typing 'What's the weather in Prague'";

                    await turnContext.SendActivityAsync(msg);
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
                await SendWelcomeMessageAsync(turnContext, cancellationToken);
            else
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected", cancellationToken: cancellationToken);
        }

        private static async Task SendWelcomeMessageAsync(ITurnContext turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in turnContext.Activity.MembersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    await turnContext.SendActivityAsync(
                        $"Welcome to WeatherBotv4 {member.Name}!",
                        cancellationToken: cancellationToken);
                }
            }
        }
    }
}
