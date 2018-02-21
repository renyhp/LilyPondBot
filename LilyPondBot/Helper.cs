using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;
using Telegram.Bot.Types.InputFiles;

namespace LilyPondBot
{
	public static class Settings
	{
		#if !DEBUG
		public static readonly string TokenPath = Path.Combine(Directory.GetCurrentDirectory(), @"token.txt");
		#endif
		#if DEBUG
		public static readonly string TokenPath = Path.Combine(Directory.GetCurrentDirectory(), @"../../debugtoken.txt");
		#endif
		public static readonly long renyhp = 133748469;
		public static readonly string LogPath = Path.Combine(Directory.GetCurrentDirectory(), "logs.txt");
		public static readonly string LilyPondPath = @"/usr/bin/lilypond";
		public static readonly int DailyLogUtcHour = 3;
		#if !DEBUG
		//can be relative or absolute
		public static readonly string LilySettingsPath = @"lilysettings.ly";
		#endif
		#if DEBUG
		public static readonly string LilySettingsPath = @"../../lilysettings.ly";
		#endif
	}

	public static class Api
	{
		public static Task<Message> Send(long chatId, string text, int replyid = 0, IReplyMarkup replyMarkup = null)
		{
            return Program.Bot.SendTextMessageAsync(chatId, text, ParseMode.Html, true, false, replyid, replyMarkup);
		}

		public static Task<Message> SendFile(long chatid, string path)
		{
			string filename = Path.GetFileName(path);
            return Program.Bot.SendDocumentAsync(chatid, new InputOnlineFile(new FileStream(path, FileMode.Open), filename));
		}

		public static Task<Message> SendPhoto(long chatid, string path)
		{
			string filename = Path.GetFileName(path);
            return Program.Bot.SendPhotoAsync(chatid, new InputOnlineFile(new FileStream(path, FileMode.Open), filename));
		}

		public static Task<Message> Edit(long chatId, int msgId, string text, InlineKeyboardMarkup replyMarkup = null)
		{
			return Program.Bot.EditMessageTextAsync(chatId, msgId, text, ParseMode.Html, true, replyMarkup);
		}

		public static Task SendAction(long chatid, ChatAction action)
		{
			return Program.Bot.SendChatActionAsync(chatid, action);
		}

		public static string FormatHTML(this string str)
		{
			return str.Replace("&", "&amp;").Replace(">", "&gt;").Replace("<", "&lt;");
		}
	}

	public static class Helpers
	{
		public static string GenerateFilename(string username)
		{
			int counter = 0;
			string filename;
			var exists = false;
			do {
				filename = DateTime.UtcNow.ToString("yyMMddHHmmssff-") + (username == "" ? counter.ToString() : username);
				exists = Directory.GetFiles(Directory.GetCurrentDirectory()).Where(x => x.Contains(filename)).Any();
				counter++;
			} while (exists);
			return filename;
		}

		public static string NormalizeOutput(this string output, string path, string filename)
		{
			return output
				.Replace(path + ":", "")
				.Replace(path, filename)
				.Replace(Environment.NewLine + Environment.NewLine, Environment.NewLine)
				.Replace(string.Format(@"\include ""{0}"" ", Settings.LilySettingsPath), "");
		}

		public static Task<Message> SecureSend(this string text, long chatid, string path)
		{
			if (text.Length < 4096)
				return Api.Send(chatid, text.FormatHTML());
			else {
				File.WriteAllText(path, text);
				return Api.SendFile(chatid, path);
			}
		}
	}
}

