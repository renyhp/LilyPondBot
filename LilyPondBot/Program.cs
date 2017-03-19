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
		//TELEGRAM
		public static TelegramBotClient Bot;
		public static User Me;
		//MONITOR
		public static DateTime StartTime = DateTime.UtcNow;
		public static int MessagesReceived = 0;
		public static int CommandsProcessed = 0;
		public static int SuccesfulCompilations = 0;
		public static string Monitor = "";
		public static DateTime LatestMessageTime = DateTime.UtcNow;
		public static readonly string BotVersion = "LilyPondBot v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
		public static readonly string LilyVersion = LilyPond.GetLilyVersion();
		public static bool UpdateMonitor = true;

		public static void Main(string[] args)
		{
			Console.Title = "LilyPondBot";
			Console.WriteLine(Program.BotVersion + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion);
			var token = File.ReadAllText(Settings.TokenPath);
			Bot = new TelegramBotClient(token);
			Me = Bot.GetMeAsync().Result;
			new Task(() => ProgramMonitor()).Start();

			Bot.OnUpdate += Bot_OnUpdate;
			Bot.OnReceiveError += Bot_OnReceiveError;
			Bot.OnReceiveGeneralError += Bot_OnReceiveGeneralError;
			 
			Bot.StartReceiving();

			Thread.Sleep(-1);
		}


		static void Bot_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
		{
			bool log = false;
			if (e.Update.Message != null) {
				if (e.Update.Message?.Date == null || e.Update.Message.Date < Program.StartTime.AddSeconds(-5))
					return;
				new Task(() => {
					try {
						Handler.HandleMessage(e.Update.Message);
					} catch (Exception ex) {
						LogError(ex);
					}
				}).Start();
				if (e.Update.Message.From.Id != Settings.renyhp)
					log = true;
			}
			if (e.Update.CallbackQuery != null) {
				if (e.Update.CallbackQuery?.Message?.Date == null || e.Update.CallbackQuery.Message.Date < Program.StartTime.AddSeconds(-5))
					return;
				new Task(() => {
					try {
						Handler.HandleCallback(e.Update.CallbackQuery);
					} catch (Exception ex) {
						LogError(ex);
					}
				}).Start();
				if (e.Update.CallbackQuery.From.Id != Settings.renyhp)
					log = true;
			}

			if (log) {
				MessagesReceived++;
				UpdateMonitor = true;
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
				msg = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " - " + e.GetType().ToString() + e.Source +
				Environment.NewLine + e.Message +
				Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine;
				File.AppendAllText(Settings.LogPath, msg);
				try {
					Bot.SendTextMessageAsync(Settings.renyhp, msg);
				} catch {
					//ignored
				}
				e = e.InnerException;
			} while (e != null);
			return;
		}

		static void ProgramMonitor()
		{
			var version = Program.BotVersion + " @" + Me.Username + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion + Environment.NewLine;
			while (true) {
				if (UpdateMonitor) {
					//update the monitor
					Monitor = "Start time: " + StartTime.ToString("dd/MM/yyyy HH:mm:ss") + " UTC" + Environment.NewLine +
					"Last message received: " + DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm:ss") + " UTC" + Environment.NewLine +
					"Messages received: " + MessagesReceived.ToString() + Environment.NewLine +
					"Commands processed: " + CommandsProcessed.ToString() + Environment.NewLine +
					"Successful compilations: " + SuccesfulCompilations.ToString();
					Console.SetCursorPosition(0, 0);
					Console.Clear();
					Console.WriteLine(version + Environment.NewLine + Monitor);

					//daily log
					if (LatestMessageTime.CompareTo(DateTime.UtcNow.Date.AddHours(Settings.DailyLogUtcHour)) < 0 && DateTime.UtcNow.Hour >= Settings.DailyLogUtcHour) {
						File.AppendAllText(
							Settings.LogPath, DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + " DAILY LOG ----- " +
						Environment.NewLine + Monitor + Environment.NewLine + "-----" + Environment.NewLine + Environment.NewLine
						);
						//reset
						MessagesReceived = 0;
						CommandsProcessed = 0;
						SuccesfulCompilations = 0;
					}

					LatestMessageTime = DateTime.UtcNow;
					UpdateMonitor = false;
				}
				//wait before redoing this
				Task.Delay(60000).Wait();
			}
		}
	}
}
