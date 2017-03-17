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
		public static int CommandsProcessed = 0;
		public static int SuccesfulCompilations = 0;
		public static DateTime LastMessageTime = DateTime.UtcNow;
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
			var date = e.Update.Message?.Date ?? e.Update.CallbackQuery?.Message?.Date ?? DateTime.MaxValue;
			if (date < Program.StartTime.AddSeconds(-5))
				return;
			
			new Task(() => {
				try {
					switch (e.Update.Type) {
						case UpdateType.MessageUpdate:
							Handler.HandleMessage(e.Update.Message);
							break;
						case UpdateType.CallbackQueryUpdate:
							Handler.HandleCallback(e.Update.CallbackQuery);
							break;
					}
				} catch (Exception ex) {
					LogError(ex);
				}
			}
			).Start();

			//log other people's activity
			var id = e.Update.Message?.From.Id ?? e.Update.CallbackQuery?.From.Id ?? Settings.renyhp;
			if (id != Settings.renyhp) {
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

		static void ProgramMonitor()
		{
			var version = Program.BotVersion + " @" + Me.Username + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion + Environment.NewLine;
			while (true) {
				if (UpdateMonitor) {
					//update the monitor
					Console.SetCursorPosition(0, 0);
					Console.WriteLine(
						version + Environment.NewLine +
						"Last message received: " + DateTime.UtcNow.ToString("G") + " UTC" + Environment.NewLine +
						"Messages received: " + MessagesReceived.ToString() + Environment.NewLine +
						"Commands processed: " + CommandsProcessed.ToString() + Environment.NewLine +
						"Successful compilations: " + SuccesfulCompilations.ToString()
					);

					//daily log
					if (LastMessageTime.CompareTo(DateTime.UtcNow.Date.AddHours(Settings.DailyLogUtcHour)) < 0 && DateTime.UtcNow.Hour >= Settings.DailyLogUtcHour) {
						File.AppendAllText(
							Settings.LogPath, DateTime.UtcNow.ToString("s") + " DAILY LOG ----- " +
						Environment.NewLine + "    Messages received: " + MessagesReceived.ToString() +
						Environment.NewLine + "    Commands processed: " + CommandsProcessed.ToString() +
						Environment.NewLine + "    Successful compilations: " + SuccesfulCompilations.ToString() +
						Environment.NewLine + "-----" + Environment.NewLine + Environment.NewLine
						);
						//reset
						MessagesReceived = 0;
						CommandsProcessed = 0;
						SuccesfulCompilations = 0;
					}

					LastMessageTime = DateTime.UtcNow;
					UpdateMonitor = false;
				}
			}
		}
	}
}
