using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Reflection;
using File = System.IO.File;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;

namespace LilyPondBot
{
	public static class Handler
	{

		public static void HandleUpdate(Update u)
		{
			var msg = u.Message;
			if (msg == null || msg.Date < Program.StartTime.AddSeconds(-5))
				return;
			var chatid = msg.Chat.Id;
			
			if (msg.Text.StartsWith("/") || msg.Text.StartsWith("!")) {
				Program.CommandsProcessed++;
				var text = msg.Text.Replace("@" + Program.Me.Username, "").TrimStart('/', '!');
				var cmd = text.Contains(' ') ? text.Substring(0, text.IndexOf(' ')) : text;
				string message;
				switch (cmd) {
					case "start":
						message = string.Format("Hello! Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
						Api.Send(chatid, message);
						break;
					case "help":
						message = string.Format("Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
						message += "\nFor now I can compile only little pieces of music, so the output of a big sheet music could be bad.\n<i>Note: Telegram Desktop substitutes &lt;&lt; with «. To avoid it, surround your code with triple backticks ```</i>";
						message += "\n\nOther commands:\n/ping - Check response time\n/version - Get the running version\n/support - Support the developer";
						Api.Send(chatid, message);
						break;
					case "ping":
						var ping = DateTime.Now - msg.Date;
						var sendtime = DateTime.Now;
						message = "Time to receive your message: " + ping.ToString(@"mm\:ss\.fff");
						var result = Api.Send(chatid, message).Result;
						ping = DateTime.Now - sendtime;
						message += Environment.NewLine + "Time to send this message: " + ping.ToString(@"mm\:ss\.fff");
						Api.Edit(chatid, result.MessageId, message);
						break;
					case "version":
						Api.Send(chatid, 
							"LilyPondBot v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion +
							Environment.NewLine + "GNU LilyPond " + LilyPond.GetLilyVersion()
						);
						break;
					case "support":
						message = "If you like how I work, or if you want to make suggestions, or even criticisms, please <a href=\"https://t.me/storebot?start=lilypondbot\">rate me</a>, and leave some feedback.\n\n";
						message += "If you want to donate, or give some feedback in private, please PM my developer at @renyhp.";
						Api.Send(chatid, message);
						break;
					case "append":
					//start a new Task to monitor old files
					//if the file is too big, give error
					//append the text to a (new) file, named after the chatid.
						break;
					case "show":
					//start the Task
					//send the file with this chatid
						break;
					case "delete":
					//start the Task
					//delete the file with this chatid
						break;
					case "compile":
					//send a menu: Set file format. Set page size / adjust padding. Compile. 
						break;
					default:
						Program.CommandsProcessed--;
						break;
				}
			} else if (msg.Chat.Type == ChatType.Private) {
				LilyPond.FastCompile(msg.Text, msg.From.Username, chatid, msg.From.Id != Settings.renyhp);
			}

			return;
		}

		public static void HandleCallback(CallbackQuery q)
		{
			//Not used
			return;
		}


	}
}

