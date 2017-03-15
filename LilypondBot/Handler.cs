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
			
			if (msg.Text.StartsWith ("/") || msg.Text.StartsWith ("!")) {
				var cmd = msg.Text.Replace ("@" + Program.Me.Username, "").TrimStart ('/', '!');
				switch (cmd) {
				case "start":
					Api.Send (chatid, "Hello! Send me some lilypond code, I will compile it for you and send you a picture with the sheet music.");
					break;
				case "help":
					Api.Send (chatid, "Hello! Send me some lilypond code, I will compile it for you and send you a picture with the sheet music.\nFor now I can compile only little pieces of music, so the output of a big sheet music could be bad.\nNote that Telegram sometimes substitutes &lt;&lt; with «, so you may want to enclose your code in backticks `");
					break;
				case "ping":
					var ping = DateTime.Now - msg.Date;
					var sendtime = DateTime.Now;
					var message = "Time to receive your message: " + ping.ToString (@"mm\:ss\.fff");
					var result = Api.Send (chatid, message).Result;
					ping = DateTime.Now - sendtime;
					message += Environment.NewLine + "Time to send this message: " + ping.ToString (@"mm\:ss\.fff");
					Api.Edit (chatid, result.MessageId, message);
					break;
				}
			} else {
				Lilypond.CompileAndSend (msg.Text, msg.From.Username, chatid);
			}

			return;
		}

		public static void HandleCallback (CallbackQuery q)
		{
			//Not used
			return;
		}


	}
}

