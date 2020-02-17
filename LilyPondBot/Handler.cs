using System;
using System.Linq;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace LilyPondBot
{
    public static class Handler
	{

		public static void HandleMessage(Message msg)
		{
			var chatid = msg.Chat.Id;

			if (msg.Text == null)
				//for v2.0: if it's a document, try to compile it!
				return;
			
			if (msg.Text.StartsWith("/") || msg.Text.StartsWith("!")) {
				if (msg.From.Id != Settings.renyhp) {
					Program.CommandsProcessed++;
					Program.UpdateMonitor = true;
				}

				var text = msg.Text.Replace("@" + Program.Me.Username, "").TrimStart('/', '!');
				var cmd = text.Contains(' ') ? text.Substring(0, text.IndexOf(' ')) : text;
				string reply = "";

				switch (cmd) {
					case "start":
						reply = string.Format("Hello! Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
						Api.Send(chatid, reply).Wait();
						break;
					case "help":
						reply = string.Format("Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
						reply += "\nFor now I can compile only little pieces of music, so the output of a big sheet music could be bad.\n<i>Note: Telegram Desktop substitutes &lt;&lt; with «. To avoid it, surround your code with triple backticks ```</i>";
						reply += "\n\n<b>What is LilyPond?</b>\n<i>LilyPond is a very powerful open-source music engraving program, which compiles text code to produce sheet music output. Full information:</i> lilypond.org";
						reply += "\n\n<b>Other commands:</b>\n/ping - Check response time\n/version - Get the running version\n/contact - Feedback & dev support info";
						Api.Send(chatid, reply).Wait();
						break;
					case "ping":
						var ping = DateTime.Now - msg.Date;
						var sendtime = DateTime.Now;
						if (msg.From.Id == Settings.renyhp) //send the monitor to renyhp
							reply = Program.Monitor + Environment.NewLine + Environment.NewLine;
						reply += "Time to receive your message: " + ping.ToString(@"mm\:ss\.fff");
						var result = Api.Send(chatid, reply).Result;
						ping = DateTime.Now - sendtime;
						reply += Environment.NewLine + "Time to send this message: " + ping.ToString(@"mm\:ss\.fff");
						Api.Edit(chatid, result.MessageId, reply).Wait();
						break;
					case "version":
						Api.Send(chatid, Program.BotVersion + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion).Wait();
						break;
					case "contact":
						reply = "If you like how I work, or if you want to make suggestions, or even criticisms, please <a href=\"https://t.me/storebot?start=lilypondbot\">rate me</a>, and leave some feedback.\n\n";
						reply += "If you want to donate, or give some feedback in private, please PM my developer at @renyhp.";
						Api.Send(chatid, reply).Wait();
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
				LilyPond.FastCompile(msg.Text, msg.From.Username ?? "", chatid, msg.From.Id != Settings.renyhp);
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

