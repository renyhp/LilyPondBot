using System;
using System.Linq;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using File = System.IO.File;
using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;

namespace LilypondBot
{
	public static class Handler
	{
		public static void HandleUpdate (Update u)
		{
			var msg = u.Message;
			if (msg == null)
				return;
			var chatid = msg.Chat.Id;

			var cmd = msg.Text.Replace ("@" + Program.Me.Username, "").TrimStart ('/', '!');
			if (cmd == "help" || cmd == "start") {
				Api.Send (chatid, "Hello! Send me some lilypond code, I will compile it for you and send you a picture with the sheet music.");
				return;
			}

			//first of all, where do we store the file	
			string filename = GenerateFilename (msg.From.Username);
			string path = Path.Combine (Directory.GetCurrentDirectory (), filename);
			string srcfile = filename + ".ly";
			string srcpath = path + ".ly";
			string pngpath = path + ".png";
			string text = msg.Text;

			//set paper settings
			text = @"" + text;

			//get rid of missing version warning
			if (!text.Contains (@"\version "))
				text = @"\version """ + GetLilyVersion () + "\"" + Environment.NewLine + text;
			
			File.WriteAllText (srcpath, text);

			//ok, compile
			var process = Lilypond ("-dbackend=eps -dresolution=600 --png --loglevel=WARN " + srcpath);

			string error = "";
			string output = Run (process, out error) ?? "";

			NormalizeOutput (error, srcpath, srcfile);

			Message dummy; //I'm noob and I'll use this to wait for Api to do its work

			if (error != "")
				Api.Send (chatid, error);
			if (output != "") { //gonna want to know
				Api.Send (Settings.renyhp, "OUTPUT\n\n" + output);
				Api.Send (Settings.renyhp, "ERROR\n\n" + error);
				dummy = Api.SendFile (Settings.renyhp, srcpath).Result;
				if (File.Exists (pngpath))
					dummy = Api.SendFile (Settings.renyhp, pngpath).Result;
			}

			if (File.Exists (pngpath))
				dummy = Api.SendPhoto (chatid, pngpath).Result;

			File.Delete (pngpath);
			File.Delete (srcpath);

			return;
		}

		public static void HandleCallback (CallbackQuery q)
		{
			//Not used
			return;
		}

		private static string GenerateFilename (string username)
		{
			int counter = 0;
			string filename;
			var exists = false;
			do {
				filename = "lala" + counter;
				//DateTime.UtcNow.ToString ("yyMMddHHmmssff-") + (username ?? counter.ToString ());
				var files = Directory.EnumerateFiles (Directory.GetCurrentDirectory ()).Select (x => Path.GetFileName (x));
				exists = files.Any (x => x.StartsWith (filename));
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
			if (p.StartInfo.RedirectStandardOutput == false)
				output = null;
			else {
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
			if (p.StartInfo.RedirectStandardError == false)
				error = null;
			else {
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

