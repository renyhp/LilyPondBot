using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace LilyPondBot
{
	public static class Settings
	{
		#if !DEBUG
		public static readonly string TokenPath = Path.Combine(Directory.GetCurrentDirectory(), @"token.txt");
		#endif
		#if DEBUG
		public static readonly string TokenPath = Path.Combine (Directory.GetCurrentDirectory (), @"../../debugtoken.txt");
		#endif
		public static readonly long renyhp = 133748469;
		public static readonly string LogPath = Path.Combine (Directory.GetCurrentDirectory (), "logs.txt");
		public static readonly string LilyPondPath = @"/usr/bin/lilypond";
		public static readonly int DailyLogUtcHour = 3;
		#if !DEBUG
		//can be relative or absolute
		public static readonly string LilySettingsPath = @"lilysettings.ly";
		#endif
		#if DEBUG
		public static readonly string LilySettingsPath = @"../../lilysettings.ly";
		#endif
		public static readonly long MaxFileSizeMB = 3;
	}

	public class LilyUser
	{
		public LilyUser (long id)
		{
			TelegramId = id;
		}

		public long Id { get; }


		public long TelegramId { get; }

		/// <summary>
		/// Gets or sets the default format to compile in.
		/// </summary>
		/// <value>"png" or "pdf"</value>
		public string Format { get; set; }

		/// <summary>
		/// Gets or sets the default page (used if the format is PDF).
		/// </summary>
		/// <value>a4, letter, etc.</value>
		public string Page { get; set; }

		public Nullable<int> LeftPadding { get; set; }

		public Nullable<int> RightPadding { get; set; }

		public Nullable<int> UpperPadding { get; set; }

		public Nullable<int> LowerPadding { get; set; }
	}


	public static class Api
	{
		public static Task<Message> Send (long chatid, string text, IReplyMarkup replyMarkup = null, int replyid = 0)
		{
			return Program.Bot.SendTextMessageAsync (chatid, text, true, false, replyid, replyMarkup, ParseMode.Html);
		}

		public static Task<Message> SendFile (long chatid, string path)
		{
			string filename = Path.GetFileName (path);
			return Program.Bot.SendDocumentAsync (chatid, new FileToSend (filename, new FileStream (path, FileMode.Open)));
		}

		public static Task<Message> SendPhoto (long chatid, string path)
		{
			string filename = Path.GetFileName (path);
			return Program.Bot.SendPhotoAsync (chatid, new FileToSend (filename, new FileStream (path, FileMode.Open)));
		}

		public static Task<Message> Edit (long chatid, int msgId, string text, IReplyMarkup replyMarkup = null)
		{
			return Program.Bot.EditMessageTextAsync (chatid, msgId, text, ParseMode.Html, true, replyMarkup);
		}

		public static Task SendAction (long chatid, ChatAction action)
		{
			return Program.Bot.SendChatActionAsync (chatid, action);
		}

		public static string FormatHTML (this string str)
		{
			return str.Replace ("&", "&amp;").Replace (">", "&gt;").Replace ("<", "&lt;");
		}
	}

	public static class Helpers
	{
		public static string GenerateFilename (string username)
		{
			int counter = 0;
			string filename;
			var exists = false;
			do {
				filename = DateTime.UtcNow.ToString ("yyMMddHHmmssff-") + (username == "" ? counter.ToString () : username);
				exists = Directory.GetFiles (Directory.GetCurrentDirectory ()).Where (x => x.Contains (filename)).Any ();
				counter++;
			} while (exists);
			return filename;
		}

		public static string NormalizeOutput (this string output, string path, string filename)
		{
			return output
				.Replace (path + ":", "")
				.Replace (path, filename)
				.Replace (Environment.NewLine + Environment.NewLine, Environment.NewLine)
				.Replace (string.Format (@"\include ""{0}"" ", Settings.LilySettingsPath), "");
		}

		public static void LogError (Exception e)
		{
			var msg = "";
			do {
				msg = DateTime.Now.ToString ("yyyy/MM/dd HH:mm:ss") + " - " + e.GetType ().ToString () + " " + e.Source +
				Environment.NewLine + e.Message +
				Environment.NewLine + e.StackTrace + Environment.NewLine + Environment.NewLine;
				File.AppendAllText (Settings.LogPath, msg);
				try {
					Api.Send (Settings.renyhp, msg);
				} catch {
					//ignored
				}
				e = e.InnerException;
			} while (e != null);
			return;
		}

		public static Task<Message> SecureSend (this string text, long chatid, string path)
		{
			if (text.Length < 4096)
				return Api.Send (chatid, text.FormatHTML ());
			else {
				File.WriteAllText (path, text);
				return Api.SendFile (chatid, path);
			}

		}
	}
}

