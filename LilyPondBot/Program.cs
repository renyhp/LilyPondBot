using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
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
		public static int SuccessfulCompilations = 0;
		public static string Monitor = "";
		public static DateTime LatestMessageTime = DateTime.UtcNow;
		public static DateTime PreviousMessageTime = DateTime.UtcNow;
		public static readonly string BotVersion = "LilyPondBot v" + FileVersionInfo.GetVersionInfo (Assembly.GetExecutingAssembly ().Location).FileVersion;
		public static readonly string LilyVersion = LilyPond.GetLilyVersion ();
		public static bool UpdateMonitor = true;

		public static void Main (string[] args)
		{
			Console.Title = "LilyPondBot";
			Console.WriteLine (Program.BotVersion + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion);
			var token = File.ReadAllText (Settings.TokenPath);
			Bot = new TelegramBotClient (token);
			Me = Bot.GetMeAsync ().Result;
			new Task (() => ProgramMonitor ()).Start ();

			Bot.OnUpdate += Bot_OnUpdate;
			Bot.OnReceiveError += Bot_OnReceiveError;
			Bot.OnReceiveGeneralError += Bot_OnReceiveGeneralError;
			 
			Bot.StartReceiving ();

			Thread.Sleep (-1);
		}


		static void Bot_OnUpdate (object sender, Telegram.Bot.Args.UpdateEventArgs e)
		{
			bool log = false;
			if (e.Update.Message != null) {
				if (e.Update.Message?.Date == null || e.Update.Message.Date < Program.StartTime.AddSeconds (-5))
					return;
				new Task (() => {
					try {
						Handler.HandleMessage (e.Update.Message);
					} catch (Exception ex) {
						Helpers.LogError (ex);
					}
				}).Start ();
				if (e.Update.Message.From.Id != Settings.renyhp)
					log = true;
			}
			if (e.Update.CallbackQuery != null) {
				if (e.Update.CallbackQuery?.Message?.Date == null || e.Update.CallbackQuery.Message.Date < Program.StartTime.AddSeconds (-5))
					return;
				new Task (() => {
					try {
						Handler.HandleCallback (e.Update.CallbackQuery);
					} catch (Exception ex) {
						Helpers.LogError (ex);
					}
				}).Start ();
				if (e.Update.CallbackQuery.From.Id != Settings.renyhp)
					log = true;
			}

			if (log) {
				MessagesReceived++;
				LatestMessageTime = DateTime.UtcNow;
				UpdateMonitor = true;
			}
			return;
		}

		static void Bot_OnReceiveError (object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
		{
			if (!Bot.IsReceiving)
				Bot.StartReceiving ();
			Helpers.LogError (e.ApiRequestException);
			return;
		}

		static void Bot_OnReceiveGeneralError (object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
		{
			if (!Bot.IsReceiving)
				Bot.StartReceiving ();
			Helpers.LogError (e.Exception);
			return;
		}



		static void ProgramMonitor ()
		{
			var version = Program.BotVersion + " @" + Me.Username + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion + Environment.NewLine;
			while (true) {
				if (UpdateMonitor) {
					//update the monitor
					Monitor = "Start time: " + StartTime.ToString ("dd/MM/yyyy HH:mm:ss") + " UTC" + Environment.NewLine +
					"Latest message received: " + LatestMessageTime.ToString ("dd/MM/yyyy HH:mm:ss") + " UTC" + Environment.NewLine +
					"Messages received: " + MessagesReceived.ToString () + Environment.NewLine +
					"Commands processed: " + CommandsProcessed.ToString () + Environment.NewLine +
					"Successful compilations: " + SuccessfulCompilations.ToString ();
					Console.SetCursorPosition (0, 0);
					Console.Clear ();
					Console.WriteLine (version + Environment.NewLine + Monitor);

					//daily log
					if (PreviousMessageTime.CompareTo (DateTime.UtcNow.Date.AddHours (Settings.DailyLogUtcHour)) < 0 && LatestMessageTime.Hour >= Settings.DailyLogUtcHour) {
						File.AppendAllText (
							Settings.LogPath, DateTime.UtcNow.ToString ("yyyy/MM/dd HH:mm:ss") + " DAILY LOG ----- " +
						Environment.NewLine + Monitor + Environment.NewLine + "-----" + Environment.NewLine + Environment.NewLine
						);
						//reset
						MessagesReceived = 0;
						CommandsProcessed = 0;
						SuccessfulCompilations = 0;
					}

					PreviousMessageTime = LatestMessageTime;
					UpdateMonitor = false;
				}
				//wait before redoing this
				Task.Delay (60000).Wait ();
			}
		}
	}
}
