﻿using System;
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
			//TODO: append ly and png extension to generatefilename
			string filename = GenerateFilename (msg.From.Username);
			string path = Path.Combine (Directory.GetCurrentDirectory (), filename);
			string text = msg.Text;

			//get rid of missing version warning
			if (!text.Contains (@"\version "))
				text = @"\version """ + GetLilyVersion () + "\"" + Environment.NewLine + text;
			
			File.WriteAllText (path, text);

			//ok, compile
			var process = Lilypond ("--png --loglevel=BASIC_PROGRESS " + path);

			string error = "";
			string output = Run (process, out error) ?? "";

			NormalizeOutput (error, path, filename);
			NormalizeOutput (output, path, filename);

			//TODO: Send the stderror if any warning/error.
			//TODO: Send png
			//TODO: delete the files!
			Api.Send (chatid, "OUTPUT\n\n" + output);
			Api.Send (chatid, "ERROR\n\n" + error);



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
			do {
				//TODO: leva l'estensione da lì
				filename = DateTime.UtcNow.ToString ("yyMMddHHmmssff-") + (username ?? counter.ToString ()) + ".ly";
				counter++;
				//TODO: check for ly, png, ps existing
			} while (File.Exists (Path.Combine (Directory.GetCurrentDirectory (), filename)));
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
