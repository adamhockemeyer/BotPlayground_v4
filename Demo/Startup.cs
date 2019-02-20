// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Linq;
using Demo.Accessors.State;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Demo
{
    /// <summary>
    /// The Startup class configures services and the request pipeline.
    /// </summary>
    public class Startup
    {
        private ILoggerFactory _loggerFactory;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        /// <summary>
        /// Gets the configuration that represents a set of key/value application configuration properties.
        /// </summary>
        /// <value>
        /// The <see cref="IConfiguration"/> that represents a set of key/value application configuration properties.
        /// </value>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> specifies the contract for a collection of service descriptors.</param>
        /// <seealso cref="IStatePropertyAccessor{T}"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/dependency-injection"/>
        /// <seealso cref="https://docs.microsoft.com/en-us/azure/bot-service/bot-service-manage-channels?view=azure-bot-service-4.0"/>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddBot<DemoBot>(options =>
           {
               var secretKey = Configuration.GetSection("botFileSecret")?.Value;

               // Loads .bot configuration file and adds a singleton that your Bot can access through dependency injection.
               var botConfig = BotConfiguration.Load(@".\Demo.bot", secretKey);
               services.AddSingleton(sp => botConfig);

               // Retrieve current endpoint.
               var service = botConfig.Services.Where(s => s.Type == "endpoint" && s.Name == "development").FirstOrDefault();
               if (!(service is EndpointService endpointService))
               {
                   throw new InvalidOperationException($"The .bot file does not contain a development endpoint.");
               }

               options.CredentialProvider = new SimpleCredentialProvider(endpointService.AppId, endpointService.AppPassword);

               // Middleware

               // When added, this middleware will send typing activities back to the user when a Message activity is receieved to let them know that the bot has receieved the message and is working on the response.
               options.Middleware.Add(new ShowTypingMiddleware());

               // Creates a logger for the application to use.
               ILogger logger = _loggerFactory.CreateLogger<DemoBot>();

               // Catches any errors that occur during a conversation turn and logs them.
               options.OnTurnError = async (context, exception) =>
              {
                  await context.SendActivityAsync($"Sorry, it looks like something went wrong. Exception: {exception.Message}");
              };
           });

            // Create conversation and user state with in-memory storage provider.
            IStorage storage = new MemoryStorage();
            ConversationState conversationState = new ConversationState(storage);
            UserState userState = new UserState(storage);

            // Create and register state accessors.
            // Accessors created here are passed into the IBot-derived class on every turn.
            services.AddSingleton<StateBotAccessors>(sp =>
            {
                // Create the custom state accessor.
                return new StateBotAccessors(conversationState, userState)
                {
                    ConversationDataAccessor = conversationState.CreateProperty<ConversationData>(StateBotAccessors.ConversationDataName),
                    DialogStateAccessor = conversationState.CreateProperty<DialogState>(StateBotAccessors.DialogStateName),
                    UserInfoAccessor = conversationState.CreateProperty<UserInfo>(StateBotAccessors.UserInfoName)               
                };
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _loggerFactory = loggerFactory;

            app.UseDefaultFiles()
                .UseStaticFiles()
                .UseBotFramework();
        }
    }
}
