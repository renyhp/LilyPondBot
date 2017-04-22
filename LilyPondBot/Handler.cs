using LiteDB;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

namespace LilyPondBot
{
	public static class Handler
	{

		public static void HandleMessage (Message msg)
		{
			var chatid = msg.Chat.Id;
			if (msg.Document != null) {
				//TODO: send the same menu as /compile!
				return;
			}
			if (msg.Text == null)
				return;
			
			if ((msg.ReplyToMessage?.From.Id ?? 0) == Program.Me.Id) {
				var text = msg.ReplyToMessage.Text;
				var reply = msg.Text.ToLower ();

				if (text.Contains ("Send me your default paper size")) {
					if (!reply.IsValidPaperSize ()) {
						Api.Send (chatid, "Invalid paper size.\nSend me your default paper size again.", new ForceReply () { Force = true });
						return;
					}
					using (var db = new LiteDatabase ("lilypondbot.db")) {
						var user = db.GetUser (chatid);
						user.Paper = reply;
						db.Update (user);
						Api.Send (chatid, "Default paper size set to " + user.Paper);
					}
					return;
				}

				if (text.Contains ("You can also set the padding by replying to this message") && new Regex ("^(\\d{1,3}x){3}\\d{1,3}$").IsMatch (reply)) {
					using (var db = new LiteDatabase ("lilypondbot.db")) {
						var user = db.GetUser (chatid);
						user.Padding = reply
							.Split ('x').ToList ()
							.Select (x => int.Parse (x))
							.Select (x => x > 200 ? 200 : x).ToList ()
							.Select (x => x.ToString ())
							.Aggregate ((a, b) => a + "x" + b);
						db.Update (user);
						Api.Send (chatid, "Default padding set.\n\n" + Helpers.DescribePadding (user.Padding));
					}

					return;
				}
			}

			if (msg.Text.StartsWith ("/") || msg.Text.StartsWith ("!")) {
				if (msg.From.Id != Settings.renyhp)
					Program.CommandsProcessed++;
				
				var text = msg.Text.Replace ("@" + Program.Me.Username, "").TrimStart ('/', '!');
				var cmd = text.Contains (' ') ? text.Substring (0, text.IndexOf (' ')) : text;
				text = text.Replace (cmd, "").Trim ();

				string reply = "";
				string file = "";
				switch (cmd) {
				case "start":
					reply = string.Format ("Hello! Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
					Api.Send (chatid, reply);
					break;
				case "help":
					reply = string.Format ("Send me some LilyPond code{0}, I will compile it for you and send you a picture with the sheet music.", msg.Chat.Type != ChatType.Private ? " in PM" : "");
					reply += "\nFor now I can compile only little pieces of music, so the output of a big sheet music could be bad.\n<i>Note: Telegram Desktop substitutes &lt;&lt; with «. To avoid it, surround your code with triple backticks ```</i>";
					reply += "\n\n<b>What is LilyPond?</b>\n<i>LilyPond is a very powerful open-source music engraving program, which compiles text code to produce sheet music output. Full information:</i> lilypond.org";
					reply += "\n\n<b>Other commands:</b>\n/ping - Check response time\n/version - Get the running version\n/contact - Feedback & dev support info";
					//TODO: add howto command with LilyPond help (overwriting \paper, explaining /append etc.)
					Api.Send (chatid, reply);
					break;
				case "ping":
					var ping = DateTime.Now - msg.Date;
					var sendtime = DateTime.Now;
					if (msg.From.Id == Settings.renyhp) //send the monitor to renyhp
							reply = Program.Monitor + Environment.NewLine + Environment.NewLine;
					reply += "Time to receive your message: " + ping.ToString (@"mm\:ss\.fff");
					var result = Api.Send (chatid, reply).Result;
					ping = DateTime.Now - sendtime;
					reply += Environment.NewLine + "Time to send this message: " + ping.ToString (@"mm\:ss\.fff");
					Api.Edit (chatid, result.MessageId, reply);
					break;
				case "version":
					Api.Send (chatid, Program.BotVersion + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion);
					break;
				case "contact":
					reply = "If you like how I work, or if you want to make suggestions, or even criticisms, please <a href=\"https://t.me/storebot?start=lilypondbot\">rate me</a>, and leave some feedback.\n\n";
					reply += "If you want to donate, or give some feedback in private, please PM my developer at @renyhp.";
					Api.Send (chatid, reply);
					break;
				case "append":
					new Task (() => CheckOldFiles ()).Start ();
					file = Path.Combine (Directory.GetCurrentDirectory (), chatid.ToString () + ".ly");
					if (File.Exists (file) && new FileInfo (file).Length > Settings.MaxFileSizeMB * 1048576) {
						Api.Send (chatid, "Maximum file size exceeded.");
						return;
					}
					File.AppendAllText (file, Environment.NewLine + text);
					if (new FileInfo (file).Length > Settings.MaxFileSizeMB * 1048576) {
						Api.Send (chatid, "<b>Warning: Maximum file size exceeded.</b>\nYou can't append any more text. If you need to append some more text, please use /show to download your file and edit it on your device, then send it to me, I'll try to compile it.");
					} else {
						Api.Send (chatid, string.Format ("Appended{0} to your code. Use /show to download your file.", string.IsNullOrWhiteSpace (text) ? " a blank line" : ""));
					}
					break;
				case "show":
					new Task (() => CheckOldFiles ()).Start ();
					file = Path.Combine (Directory.GetCurrentDirectory (), chatid.ToString () + ".ly");
					if (File.Exists (file)) {
						Api.SendFile (chatid, file);
					} else {
						Api.Send (chatid, "File not found. Use <code>/append &lt;some code&gt;</code> to create it.");
					}
					break;
				case "clear":
					new Task (() => CheckOldFiles ()).Start ();
					file = Path.Combine (Directory.GetCurrentDirectory (), chatid.ToString () + ".ly");
					if (File.Exists (file)) {
						File.Delete (file);
						Api.Send (chatid, "File cleared.");
					}
					break;
				case "compile":
					//TODO: send a menu: Set file format. Set paper size / adjust padding. Compile. 
					break;
				case "settings":
					Api.Send (chatid, "Manage your settings:", MakeSettingsMenu (chatid));
					break;
				default:
					Program.CommandsProcessed--;
					break;
				}
			} else if (msg.Chat.Type == ChatType.Private) {
				LilyPond.FastCompile (msg.Text, chatid, msg.From.Id != Settings.renyhp);
			}

			return;
		}

		public static void HandleCallback (CallbackQuery q)
		{
			var chatid = q.Message.Chat.Id;
			var args = q.Data.Split ('|');
			switch (args [0]) {
			case "user":
				using (var db = new LiteDatabase ("lilypondbot.db")) {
					var user = db.GetUser (chatid);

					InlineKeyboardMarkup menu;
					switch (args [1]) {
					case "format":
						if (args.Length >= 4) {
							user.Format = args [3];
							db.Update (user);
						}
						menu = new InlineKeyboardMarkup (new [] {
							new[] {
								new InlineKeyboardButton ("PDF", $"user|format|{chatid}|PDF"),
								new InlineKeyboardButton ("PNG", $"user|format|{chatid}|PNG")
							}, 
							new [] {
								new InlineKeyboardButton ("Back to settings", $"settings")
							}
						});
						Api.AnswerQuery (q, "Set your default format.\nCurrent: <b>" + (String.IsNullOrWhiteSpace (user.Format) ? "PDF" : user.Format) + "</b>", args.Length >= 4 ? args [3] : "File format", replyMarkup: menu);
						break;
					case "paper":
						Api.AnswerQuery (q, 
							string.Format (
								"Send me your default paper size.\n" +
								"Current: <b>{0}</b>\n\n" +
								"<i>You can find a list of available paper sizes</i> " +
								"<a href=\"http://lilypond.org/doc/v2.18/Documentation/notation/predefined-paper-sizes\">here</a>", 
								String.IsNullOrWhiteSpace (user.Paper) ? "a4" : user.Paper
							), popuptext: "Paper size", edit: false, replyMarkup: new ForceReply () { Force = true });
						break;
					case "padding":
						var directions = new [] { "Left", "Right", "Upper", "Lower" };

						//get the padding setting
						var paddings = (String.IsNullOrWhiteSpace (user.Padding) ? "30x30x30x30" : user.Padding).Split ('x').ToList ().Select (x => int.Parse (x)).ToList ();

						//process the request
						var popuptext = "";
						int i;
						switch (args.Length) {
						case 3:
							popuptext = "PNG padding";
							break;
						case 4:
							i = int.Parse (args [3]);
							popuptext = directions [i] + " padding: " + paddings [i];
							break;
						case 5:
							i = int.Parse (args [3]);
							paddings [i] += int.Parse (args [4]);
							if (paddings [i] < 0)
								paddings [i] = 0;
							else if (paddings [i] > 200)
								paddings [i] = 200;
							user.Padding = paddings.Select (x => x.ToString ()).Aggregate ((a, b) => a + "x" + b);
							db.Update (user);
							popuptext = directions [i] + " padding: " + args [4];
							break;
						default:
							throw new IndexOutOfRangeException ();
						}

						//ok, now for the view.
						//make the menu
						var rows = new List<InlineKeyboardButton[]> (); 
						for (i = 0; i < 4; i++) {
							string qdata = $"user|padding|{chatid}|{i}";
							rows.Add (new [] {
								new InlineKeyboardButton ("-10", qdata + "|-10"),
								new InlineKeyboardButton ("-5", qdata + "|-5"),
								new InlineKeyboardButton (directions [i], qdata),
								new InlineKeyboardButton ("+5", qdata + "|+5"),
								new InlineKeyboardButton ("+10", qdata + "|+10")
							});
						}
						rows.Add (new[] { new InlineKeyboardButton ("Back to settings", $"settings")}); 
						menu = new InlineKeyboardMarkup (rows.ToArray ());

						//finally answer
						Api.AnswerQuery (q, 
							"Set your default PNG padding. This is the amount of blank pixels that will be added to your score when compiling.\n" +
							"<i>You can also set the padding by replying to this message with the four numbers separated by x.\n" +
							"For example, to set the left, right, upper, lower paddings respectively to 15, 20, 30, 40, send 15x20x30x40.</i>\n\nCurrent:\n" +
							Helpers.DescribePadding (String.IsNullOrWhiteSpace (user.Padding) ? "30x30x30x30" : user.Padding), 
							popuptext, replyMarkup: menu);
						break;
					}
				}
				break;
			case "quit":
				Api.AnswerQuery (q, "Settings updated.");
				break;
			case "settings":
				Api.AnswerQuery (q, "Manage your settings:", replyMarkup: MakeSettingsMenu (chatid));
				break;
			}
			return;
		}

		private static void CheckOldFiles ()
		{
			foreach (var file in Directory.GetFiles(Directory.GetCurrentDirectory()).Where(x => x.EndsWith(".ly") && File.GetLastWriteTime(x).CompareTo(DateTime.Now.AddDays(-1)) < 0)) {
				try {
					File.Delete (file);
				} catch (Exception e) {
					Helpers.LogError (e);
				}
			}
		}

		private static InlineKeyboardMarkup MakeSettingsMenu (long chatid)
		{
			return new InlineKeyboardMarkup (new[] {
				new [] {
					new InlineKeyboardButton ("File format", $"user|format|{chatid}"),
					new InlineKeyboardButton ("Paper size", $"user|paper|{chatid}")
				},
				new [] {
					new InlineKeyboardButton ("PNG padding", $"user|padding|{chatid}"),
					new InlineKeyboardButton ("Close menu", $"quit")
				}
			});
		}
	}
}

