﻿using Miki.Cache;
using Miki.Cache.InMemory;
using Miki.Discord.Caching.Stages;
using Miki.Discord.Common;
using Miki.Discord.Gateway;
using Miki.Discord.Rest;
using Miki.Logging;
using Miki.Net.WebSockets;
using Miki.Serialization.Protobuf;
using System;
using System.Threading.Tasks;

namespace Miki.Discord.Example
{
    // This is a test app for Miki.Discord's current interfacing.
    // The goal of this app is to explain how the system works and why these steps are neccesary.
    // If you're new to bot programming, this might not be the most suitable API for you, but it does promise
    // Scaling and control over certain aspects whenever needed.
    // For questions join the Miki Stack discord (link in README.md) or tweet me @velddev
    class Program
	{
		// Your discord token should be placed here if you want to run the application.
		const string Token = "token-here";

        // Log level required to filter out logging. Useful for when you do not want all verbose information.
        const LogLevel logLevel = LogLevel.Debug;
		
        // Miki.Discord is pure-async, therefore; we would like to start in an async Task.
		static void Main(string[] args)
			=> MainAsync(args).GetAwaiter().GetResult();
		static async Task MainAsync(string[] args)
		{
            // Enables library wide logging for all Miki libraries. Consider using this logging for debugging or general information.
            new LogBuilder().SetLogHeader(x => $"[{DateTime.UtcNow.ToLongTimeString()}]")
                .AddLogEvent((msg, level) =>
            {
                if (level >= logLevel)
                {
                    Console.WriteLine($"{msg}");
                }
            }).Apply();

			// A cache client is needed to store ratelimits and entities.
			// This is an in-memory cache, and will be used as a local storage repository.
			IExtendedCacheClient cache = new InMemoryCacheClient(
				new ProtobufSerializer()
			);

			// Discord REST API implementation gets set up.
			IApiClient api = new DiscordApiClient(Token, cache);

			// Discord direct gateway implementation. 
			IGateway gateway = new GatewayCluster(new GatewayProperties
			{
				ShardCount = 1,
				ShardId = 0,
				Token = Token,
                Compressed = true,
				WebSocketClientFactory = () => new BasicWebSocketClient()
			});

			// This function adds additional utility, caching systems and more.
			DiscordClient bot = new DiscordClient(new DiscordClientConfigurations
			{
				ApiClient = api,
				Gateway = gateway,
				CacheClient = cache
			});

			// Add caching events for the gateway.
			new BasicCacheStage().Initialize(gateway, cache);

			// Hook up on the MessageCreate event. This will send every message through this flow.
			bot.MessageCreate += async (msg) => {

				if (msg.Content == "ping")
				{
					IDiscordTextChannel channel = await msg.GetChannelAsync();
					await channel.SendMessageAsync("pong!");
				}
			};

			// Start the connection to the gateway.
			await gateway.StartAsync();

			// Wait, else the application will close.
			await Task.Delay(-1);
		}
	}
}