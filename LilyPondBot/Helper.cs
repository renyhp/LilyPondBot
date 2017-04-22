using LiteDB;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using FileMode = System.IO.FileMode;

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
		public LilyUser ()
		{
		}

		public int Id { get; set; }

		public long TelegramId { get; set; }

		/// <summary>
		/// Gets or sets the default format to compile in.
		/// </summary>
		/// <value>"png" or "pdf"</value>
		public string Format { get; set; }

		/// <summary>
		/// Gets or sets the default paper size (used if the format is PDF).
		/// </summary>
		/// <value>a4, letter, etc.</value>
		public string Paper { get; set; }

		/// <summary>
		/// Gets or sets the padding to apply to PNG.
		/// </summary>
		/// <value>A string in the format "leftxrightxupperxlower" (eg 30x30x15x15)</value>
		public string Padding { get; set; }
	}


	public static class Api //mostly some aliases
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

		public static Task<Message> AnswerQuery (CallbackQuery query, string text, string popuptext = null, bool edit = true, IReplyMarkup replyMarkup = null, bool showalert = false)
		{
			Program.Bot.AnswerCallbackQueryAsync (query.Id, popuptext, showalert);
			if (edit)
				return Program.Bot.EditMessageTextAsync (query.Message.Chat.Id, query.Message.MessageId, text, ParseMode.Html, true, replyMarkup);
			else
				return Program.Bot.SendTextMessageAsync (query.Message.Chat.Id, text, true, false, 0, replyMarkup, ParseMode.Html);
		}

		public static Task AnswerQuery (string popuptext, CallbackQuery query, bool showalert = false)
		{
			return Program.Bot.AnswerCallbackQueryAsync (query.Id, popuptext, showalert);
		}
			
	}

	public static class Helpers
	{
		public static string MakeReadable (this string output, string path, string filename)
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
			File.WriteAllText (path, text);
			return Api.SendFile (chatid, path);

		}

		public static LilyUser GetUser (this LiteDatabase db, long telegramid)
		{
			var users = db.GetCollection<LilyUser> ("Users");
			var user = users.FindOne (x => x.TelegramId == telegramid);
			if (user == null) {
				user = new LilyUser { TelegramId = telegramid };
				user.Id = users.Insert (user);
			}
			return user;
		}

		public static bool Update (this LiteDatabase db, LilyUser user)
		{
			//don't wanna type GetCollection every time :P
			return db.GetCollection<LilyUser> ("Users").Update (user);
		}

		public static string FormatHTML (this string str)
		{
			return str.Replace ("&", "&amp;").Replace (">", "&gt;").Replace ("<", "&lt;");
		}

		public static bool IsValidPaperSize (this string str)
		{
			str = str.ToLower ();

			if (str.EndsWith ("landscape"))
				str = str.Remove (str.Length - 9);
			
			var regexs = new[] { 
				"([abc]|pa)(\\d|10)", 
				"ansi [a-e]", 
				"arch ([a-e]|e1)", 
				"(4|2)a0", 
				"((junior|government|philippine)-)?legal",
				"(double |quad )?demy",
				"(government-|half )?letter",
				"(large )?post"
			};
			var strings = new[] {
				"ledger",
				"tabloid",
				"11x17",
				"17x11",
				"engineering f",
				"statement",
				"quarto",
				"octavo",
				"executive",
				"monarch",
				"foolscap",
				"folio",
				"super-b",
				"crown",
				"medium",
				"broadsheet",
				"royal",
				"elephant",
				"atlas",
				"imperial",
				"antiquarian",
				"f4"
			};

			if (strings.Contains (str))
				return true;

			foreach (var r in regexs) {
				var regex = new Regex ($"^{r}$");
				if (regex.IsMatch (str)) {    
					return true;
				}
			}
			
			return false;
		}

		public static string DescribePadding (string str)
		{
			if (!new Regex ("^(\\d{1,3}x){3}\\d{1,3}$").IsMatch (str))
				throw new InvalidDataException ();
			var directions = new [] { "Left", "Right", "Upper", "Lower" };
			var paddings = str.Split ('x');
			string padding = "";
			for (var i = 0; i < 4; i++) {
				padding += "<i>" + directions [i] + " padding:</i> " + paddings [i] + (i == 3 ? "" : "\n");
			}
			return padding;
		}
	}
}

