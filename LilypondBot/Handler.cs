using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using File = System.IO.File;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;

namespace LilypondBot
{
	public static class Handler
	{

		public static void HandleUpdate (Update u)
		{
			var msg = u.Message;
			if (msg == null || msg.Date < Program.StartTime.AddSeconds (-5)
			    || msg.Chat.Type != ChatType.Private)
				return;
			
			var chatid = msg.Chat.Id;

			var cmd = msg.Text.Replace ("@" + Program.Me.Username, "").TrimStart ('/', '!');
			if (cmd == "start") {
				Api.Send (chatid, "Hello! Send me some lilypond code, I will compile it for you and send you a picture with the sheet music.");
				return;
			}
			if (cmd == "help") {
				Api.Send (chatid, "Hello! Send me some lilypond code, I will compile it for you and send you a picture with the sheet music.\nFor now I can compile only little pieces of music, so the output of a big sheet music could be bad.");
			}

			Lilypond.CompileAndSend (msg.Text, msg.From.Username, chatid);

			return;
		}

		public static void HandleCallback (CallbackQuery q)
		{
			//Not used
			return;
		}


	}
}

