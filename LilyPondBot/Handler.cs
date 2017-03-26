using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using File = System.IO.File;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Text.RegularExpressions;

namespace LilyPondBot
{
	public static class Handler
	{

		public static void HandleMessage(Message msg)
		{
			var chatid = msg.Chat.Id;

			if (msg.Document != null) {
				//send the same menu as /compile!
				return;
			}
				

			if (msg.Text == null)
				return;
			
			if (msg.Text.StartsWith("/") || msg.Text.StartsWith("!")) {
				if (msg.From.Id != Settings.renyhp) {
					Program.CommandsProcessed++;
					Program.UpdateMonitor = true;
				}

				var text = msg.Text.Replace("@" + Program.Me.Username, "").TrimStart('/', '!');
				var cmd = text.Contains(' ') ? text.Substring(0, text.IndexOf(' ')) : text;
				text = text.Replace(cmd, "").Trim();

				string reply = "";
				string file = "";
				switch (cmd) {
					case "start":
						reply = string.Format("Hello! Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
						Api.Send(chatid, reply);
						break;
					case "help":
						reply = string.Format("Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
						reply += "\nFor now I can compile only little pieces of music, so the output of a big sheet music could be bad.\n<i>Note: Telegram Desktop substitutes &lt;&lt; with «. To avoid it, surround your code with triple backticks ```</i>";
						reply += "\n\n<b>What is LilyPond?</b>\n<i>LilyPond is a very powerful open-source music engraving program, which compiles text code to produce sheet music output. Full information:</i> lilypond.org";
						reply += "\n\n<b>Other commands:</b>\n/ping - Check response time\n/version - Get the running version\n/contact - Feedback & dev support info";
						//add howto command with LilyPond help (overwriting \paper, explaining /append etc.)
						Api.Send(chatid, reply);
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
						Api.Edit(chatid, result.MessageId, reply);
						break;
					case "version":
						Api.Send(chatid, Program.BotVersion + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion);
						break;
					case "contact":
						reply = "If you like how I work, or if you want to make suggestions, or even criticisms, please <a href=\"https://t.me/storebot?start=lilypondbot\">rate me</a>, and leave some feedback.\n\n";
						reply += "If you want to donate, or give some feedback in private, please PM my developer at @renyhp.";
						Api.Send(chatid, reply);
						break;
					case "append":
						new Task(() => CheckOldFiles()).Start();
						file = Path.Combine(Directory.GetCurrentDirectory(), chatid.ToString() + ".ly");
						if (File.Exists(file) && new FileInfo(file).Length > Settings.MaxFileSizeMB * 1048576) {
							Api.Send(chatid, "Maximum file size exceeded.");
							return;
						}
						File.AppendAllText(file, Environment.NewLine + text);
						if (new FileInfo(file).Length > Settings.MaxFileSizeMB * 1048576) {
							Api.Send(chatid, "<b>Warning: Maximum file size exceeded.</b>\nYou can't append any more text. If you need to append some more text, please use /show to download your file and edit it on your device, then send it to me, I'll try to compile it.");
						} else {
							Api.Send(chatid, string.Format("Appended{0} to your code. Use /show to download your file.", string.IsNullOrWhiteSpace(text) ? " a blank line" : ""));
						}
						break;
					case "show":
						new Task(() => CheckOldFiles()).Start();
						file = Path.Combine(Directory.GetCurrentDirectory(), chatid.ToString() + ".ly");
						if (File.Exists(file)) {
							Api.SendFile(chatid, file);
						} else {
							Api.Send(chatid, "File not found. Use <code>/append &lt;some code&gt;</code> to create it.");
						}
						break;
					case "delete":
						new Task(() => CheckOldFiles()).Start();
						file = Path.Combine(Directory.GetCurrentDirectory(), chatid.ToString() + ".ly");
						if (File.Exists(file)) {
							File.Delete(file);
							Api.Send(chatid, "File deleted.");
						} else {
							Api.Send(chatid, "File not found. Use <code>/append &lt;some code&gt;</code> to create it.");
						}
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

		private static void CheckOldFiles()
		{
			foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory()).Where(x => x.EndsWith(".ly") && File.GetLastWriteTime(x).CompareTo(DateTime.Now.AddDays(-1)) < 0)) {
				try {
					File.Delete(file);
				} catch (Exception e) {
					Helpers.LogError(e);
				}
			}
		}
	}
}

