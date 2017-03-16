using System;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

using TelegramFile = Telegram.Bot.Types.File;
using File = System.IO.File;

namespace LilyPondBot
{
	
	static class Program
	{
		public static TelegramBotClient Bot;
		public static User Me;
		public static DateTime StartTime = DateTime.UtcNow;
		public static int MessagesReceived = 0;
		public static DateTime LastMessageTime = DateTime.UtcNow;

		public static void Main(string[] args)
		{
			Console.Title = "LilyPondBot " + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
			var token = File.ReadAllText(Settings.TokenPath);
			Bot = new TelegramBotClient(token);
			Me = Bot.GetMeAsync().Result;
			Console.WriteLine("Successfully connected to @" + Me.Username);

			Bot.OnUpdate += Bot_OnUpdate;
			Bot.OnCallbackQuery += Bot_OnCallbackQuery;
			Bot.OnReceiveError += Bot_OnReceiveError;
			Bot.OnReceiveGeneralError += Bot_OnReceiveGeneralError;

			Bot.StartReceiving();
			Thread.Sleep(-1);
		}


		static void Bot_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
		{
			try {
				new Task(() => Handler.HandleUpdate(e.Update)).Start();
			} catch (Exception ex) {
				LogError(ex);
			}
			if (e.Update.Type == UpdateType.MessageUpdate && (e.Update.Message?.From.Id ?? Settings.renyhp) != Settings.renyhp) {
				MessagesReceived++;
				if (LastMessageTime.CompareTo(DateTime.UtcNow.Date.AddHours(8)) < 0 && DateTime.UtcNow.Hour >= 8) {
					File.AppendAllText(Settings.LogPath, DateTime.UtcNow.ToString("s") + " --- Messages received: " + MessagesReceived.ToString() + Environment.NewLine);
					MessagesReceived = 0;
				}
				LastMessageTime = DateTime.UtcNow;
			}
			return;
		}

		static void Bot_OnCallbackQuery(object sender, Telegram.Bot.Args.CallbackQueryEventArgs e)
		{
			try {
				new Task(() => Handler.HandleCallback(e.CallbackQuery)).Start();
			} catch (Exception ex) {
				LogError(ex);
			}
			return;
		}

		static void Bot_OnReceiveError(object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
		{
			if (!Bot.IsReceiving)
				Bot.StartReceiving();
			LogError(e.ApiRequestException);
			return;
		}

		static void Bot_OnReceiveGeneralError(object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
		{
			if (!Bot.IsReceiving)
				Bot.StartReceiving();
			LogError(e.Exception);
			return;
		}

		static void LogError(Exception e)
		{
			var msg = "";
			do {
				msg = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + " - " + e.GetType().ToString() + " " + e.Source +
				Environment.NewLine + e.Message +
				Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine;
				File.AppendAllText(Settings.LogPath, msg);
				try {
					Bot.SendTextMessageAsync(Settings.renyhp, msg);
				} catch {
					//ignored
				}
				e = e.InnerException;
			} while (e == null);
			return;
		}
	}
}
