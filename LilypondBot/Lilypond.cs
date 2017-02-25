using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
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
	public static class Lilypond
	{
		public static void CompileAndSend (string text, string username, long chatid)
		{
			//first of all, where do we store the file	
			string filename = Helpers.GenerateFilename (username);
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
			var process = LilypondProcess ($"-dbackend=eps -dresolution=600 --png --loglevel=WARN {srcpath}");

			string error = "";
			string output = Run (process, out error);
			error = error.NormalizeOutput (srcpath, srcfile);

			if (error != "")
				Api.Send (chatid, error);

			if (output != "") { //gonna want to know
				Api.Send (Settings.renyhp, "OUTPUT\n\n" + output);
				Api.Send (Settings.renyhp, "ERROR\n\n" + error);
				Api.SendFile (Settings.renyhp, srcpath);
			}


			//send pngs
			var imgresult = Directory.GetFiles (path).Where (x => x.Contains (filename) && x.EndsWith (".png"));
			if (imgresult.Any ())  //yay successful compilation
				foreach (var file in imgresult) {
					file.AddPadding (30, 30, 30, 30);
					Api.SendPhoto (chatid, file);
				}

			//send midis
			var midiresult = Directory.GetFiles (path).Where (x => x.Contains (filename) && x.EndsWith (".midi"));
			if (midiresult.Any ())
				foreach (var file in midiresult)
					Api.SendFile (chatid, file);

			//clean up
			foreach (var f in Directory.GetFiles(path).Where(x => x.Contains(filename)))
				File.Delete (f);
		}


		private static Process LilypondProcess (string args)
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
			var p = LilypondProcess ("-v");
			var output = Run (p);
			return new Regex (@"GNU LilyPond (\d+\.\d+\.\d+)\n").Match (output).Groups [1].Captures [0].Value;
		}

		private static void AddPadding (this string path, int left, int right, int top, int bottom)
		{
			var img = Image.FromFile (path);
			var dest = new Bitmap (img.Width + left + right, img.Height + top + bottom);
			using (var g = Graphics.FromImage (dest)) {
				g.DrawImage (img, left, top);
			}
			string filename = Path.GetFileNameWithoutExtension (path);
			string newfile = Path.Combine (Directory.GetCurrentDirectory (), filename + "-new.png");
			dest.Save (newfile);
			File.Delete (path);
			File.Move (newfile, path);
			return;
		}
	}
}

