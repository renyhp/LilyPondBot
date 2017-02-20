using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace LilypondBot
{
	public static class Settings
	{
		public static readonly string TokenPath = Path.Combine (Directory.GetCurrentDirectory (), @"../../token.txt");
		public static readonly long renyhp = 133748469;
	}

	public static class Api
	{
		public static Task<Message> Send (long chatId, string text, int replyid = 0, IReplyMarkup replyMarkup = null)
		{
			return Program.Bot.SendTextMessageAsync (chatId, text.FormatHTML (), true, false, replyid, replyMarkup, ParseMode.Html);
		}

		public static string FormatHTML (this string str)
		{
			return str.Replace (">", "&gt;").Replace ("<", "&lt;").Replace ("&", "&amp;");
		}
	}
}

