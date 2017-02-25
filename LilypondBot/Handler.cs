using System;
using System.Linq;
using System.Diagnostics;
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
			    || msg.Chat.Type != ChatType.Private) //TODO: Make a command to enable it also outside private chat
				return;
			
			var chatid = msg.Chat.Id;

			var cmd = msg.Text.Replace ("@" + Program.Me.Username, "").TrimStart ('/', '!');
			if (cmd == "help" || cmd == "start") {
				Api.Send (chatid, "Hello! Send me some lilypond code, I will compile it for you and send you a picture with the sheet music.");
				return;
			}

			//TODO: Figure out how to make multiple message input possible
			CompileAndSend (msg.Text, msg.From.Username, chatid);

			return;
		}

		public static void HandleCallback (CallbackQuery q)
		{
			//Not used
			return;
		}

		public static void CompileAndSend (string text, string username, long chatid)
		{
			//first of all, where do we store the file	
			string filename = GenerateFilename (username);
			string path = Directory.GetCurrentDirectory ();
			string srcfile = filename + ".ly";
			string srcpath = Path.Combine (path, srcfile);

			//set paper settings
			text = Settings.PaperSettings + text;

			//get rid of missing version warning
			if (!text.Contains (@"\version "))
				text = @"\version """ + GetLilyVersion () + "\"" + Environment.NewLine + text;

			File.WriteAllText (srcpath, text);

			//ok, compile
			var process = Lilypond ($"-dbackend=eps -dresolution=600 --png --loglevel=WARN {srcpath}");

			string error = "";
			string output = Run (process, out error);
			NormalizeOutput (error, srcpath, srcfile);

			if (error != "")
				Api.Send (chatid, error);

			if (output != "") { //gonna want to know
				Api.Send (Settings.renyhp, "OUTPUT\n\n" + output);
				Api.Send (Settings.renyhp, "ERROR\n\n" + error);
				Api.SendFile (Settings.renyhp, srcpath);
			}
						
			//TODO: send midi too
			var result = Directory.GetFiles (path).Where (x => x.Contains (filename) && x.EndsWith (".png"));
			if (result.Any ())  //yay successful compilation
				foreach (var file in result)
					Api.SendPhoto (chatid, file);

			//clean up
			foreach (var f in Directory.GetFiles(path).Where(x => x.Contains(filename)))
				File.Delete (f);
		}

		private static string GenerateFilename (string username)
		{
			int counter = 0;
			string filename;
			var exists = false;
			do {
				filename = DateTime.UtcNow.ToString ("yyMMddHHmmssff-") + (username ?? counter.ToString ());
				exists = Directory.GetFiles (Directory.GetCurrentDirectory ()).Where (x => x.Contains (filename)).Any ();
				counter++;
			} while (exists);
			return filename;
		}

		private static string NormalizeOutput (string output, string path, string filename)
		{
			return output.Replace ("«" + path + "»", filename).Replace (path + ":", "").Replace (Environment.NewLine + Environment.NewLine, Environment.NewLine);
		}

		private static Process Lilypond (string args)
		{
			return new Process () { 
				StartInfo = new ProcessStartInfo () {
					FileName = "/usr/bin/lilypond",
					Arguments = args,
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true
				} 
			};
		}

		/// <summary>
		/// Run the process. Returns stdout
		/// </summary>
		private static string Run (Process p)
		{
			p.Start ();
			string output = "";
			if (p.StartInfo.RedirectStandardOutput == true) {
				while (!p.StandardOutput.EndOfStream) {
					output += p.StandardOutput.ReadLine () + Environment.NewLine;
				}
			}
			return output;
		}

		/// <summary>
		/// Run the process and store stderr. Returns stdout
		/// </summary>
		private static string Run (Process p, out string error)
		{
			string output = Run (p);
			error = "";
			if (p.StartInfo.RedirectStandardError == true) {
				while (!p.StandardError.EndOfStream) {
					error += p.StandardError.ReadLine () + Environment.NewLine;
				}
			}
			return output;
		}

		private static string GetLilyVersion ()
		{
			var p = Lilypond ("-v");
			var output = Run (p);
			return new Regex (@"GNU LilyPond (\d+\.\d+\.\d+)\n").Match (output).Groups [1].Captures [0].Value;
		}
	}
}

