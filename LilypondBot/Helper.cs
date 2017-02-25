using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace LilypondBot
{
	public static class Settings
	{
		public static readonly string TokenPath = Path.Combine (Directory.GetCurrentDirectory (), @"../../token.txt");
		public static readonly long renyhp = 133748469;
		public static readonly string PaperSettings = 
			@"\paper{
    indent=0\mm
    line-width=120\mm
    oddHeaderMarkup = ##f
  	evenHeaderMarkup = ##f
  	oddFooterMarkup = ##f
  	evenFooterMarkup = ##f
}";
	}

	public static class Api
	{
		public static Task<Message> Send (long chatId, string text, int replyid = 0, IReplyMarkup replyMarkup = null)
		{
			return Program.Bot.SendTextMessageAsync (chatId, text.FormatHTML (), true, false, replyid, replyMarkup, ParseMode.Html);
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

		public static string FormatHTML (this string str)
		{
			return str.Replace (">", "&gt;").Replace ("<", "&lt;").Replace ("&", "&amp;");
		}
	}

	public static class Helpers
	{
		public static void DeleteDirectory (string target_dir)
		{
			string[] files = Directory.GetFiles (target_dir);
			string[] dirs = Directory.GetDirectories (target_dir);

			foreach (string file in files) {
				File.SetAttributes (file, FileAttributes.Normal);
				File.Delete (file);
			}

			foreach (string dir in dirs) {
				DeleteDirectory (dir);
			}

			Directory.Delete (target_dir, false);
		}
	}
}

