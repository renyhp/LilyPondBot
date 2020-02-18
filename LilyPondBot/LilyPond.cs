using System;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using File = System.IO.File;
using Telegram.Bot.Types.Enums;
using System.Text;
using System.Threading;

namespace LilyPondBot
{
    public static class LilyPond
    {
        public static void FastCompile(string text, string username, long chatid, bool logonsuccess)
        {
            var tokensource = new CancellationTokenSource();
            var token = tokensource.Token;

            //first of all, where do we store the file	
            string filename = Helpers.GenerateFilename(username);
            string path = Directory.GetCurrentDirectory();
            string srcfile = filename + ".ly";
            string srcpath = Path.Combine(path, srcfile);

            //set paper settings & get rid of missing version warning
            text =
                (text.Contains(@"\version ") ? "" : @"\version """ + Program.LilyVersion + "\" ")
            + string.Format(@"\include ""{0}"" ", Settings.LilySettingsPath) + text;

            //save it
            File.WriteAllLines(srcpath, text.Split('\n'));

            //ok, compile
            var chataction = Api.SendAction(chatid, ChatAction.Typing, token);
            var process = LilyPondProcess($"-dbackend=eps -dresolution=300 --png --loglevel=WARN {srcpath}");

            string output = Run(process, out string error);
            error = error.NormalizeOutput(srcpath, srcfile);

            if (!string.IsNullOrWhiteSpace(error))
                error.SecureSend(chatid, Path.Combine(path, filename + ".log"));

            if (!string.IsNullOrWhiteSpace(output))
            { //gonna want to know
                error = "ERROR\n\n" + error;
                error.SecureSend(Settings.renyhp, Path.Combine(path, filename + ".error"));
                output = "OUTPUT\n\n" + output;
                output.SecureSend(Settings.renyhp, Path.Combine(path, filename + ".output"));
                Api.SendFile(Settings.renyhp, srcpath).Wait();
            }

            //send pngs
            var imgresult = Directory.GetFiles(path).Where(x => x.Contains(filename) && x.EndsWith(".png"));
            if (imgresult.Any())  //yay successful compilation
                foreach (var file in imgresult)
                {
                    WaitUntilFree(file);
                    if (!chataction.IsCompleted)
                        tokensource.Cancel();
                    chataction = Api.SendAction(chatid, ChatAction.UploadPhoto, token);
                    file.AddPadding(30, 30, 30, 30);
                    if (!chataction.IsCompleted)
                        tokensource.Cancel();
                    chataction = Api.SendAction(chatid, ChatAction.UploadPhoto, token);
                    try
                    {
                        Api.SendPhoto(chatid, file).Wait();
                    }
                    catch
                    {
                        Api.SendFile(chatid, file).Wait();
                    }
                }

            //send midis
            var midiresult = Directory.GetFiles(path).Where(x => x.Contains(filename) && x.EndsWith(".mid"));
            if (midiresult.Any())
                foreach (var file in midiresult)
                {
                    WaitUntilFree(file);
                    if (!chataction.IsCompleted)
                        tokensource.Cancel();
                    chataction = Api.SendAction(chatid, ChatAction.UploadAudio, token);
                    Api.SendFile(chatid, file).Wait();
                }

            if (imgresult.Union(midiresult).Any() && logonsuccess)
            {
                Program.SuccesfulCompilations++;
                Program.UpdateMonitor = true;
            }


            //clean up
            foreach (var f in Directory.GetFiles(path).Where(x => x.Contains(filename)))
            {
                WaitUntilFree(f);
                File.Delete(f);
            }
        }


        private static Process LilyPondProcess(string args)
        {
            return new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Settings.LilyPondPath,
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
        private static string Run(Process p)
        {
            var outputBuilder = new StringBuilder();
            p.OutputDataReceived += (sender, args) => outputBuilder.AppendLine(args.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            p.CancelOutputRead();
            return outputBuilder.ToString();
        }

        /// <summary>
        /// Run the process and store stderr. Returns stdout
        /// </summary>
        private static string Run(Process p, out string error)
        {
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();
            p.OutputDataReceived += (sender, args) => outputBuilder.AppendLine(args.Data);
            p.ErrorDataReceived += (sender, args) => errorBuilder.AppendLine(args.Data);
            p.Start();
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();
            p.CancelOutputRead();
            p.CancelErrorRead();
            error = errorBuilder.ToString();
            return outputBuilder.ToString();
        }

        public static string GetLilyVersion()
        {
            var p = LilyPondProcess("-v");
            var output = Run(p);
            return output.Substring(13, output.IndexOf(Environment.NewLine) - 13);
        }

        private static void AddPadding(this string path, int left, int right, int top, int bottom)
        {
            var img = Image.FromFile(path);
            var dest = new Bitmap(img.Width + left + right, img.Height + top + bottom);
            using (var g = Graphics.FromImage(dest))
            {
                g.DrawImage(img, left, top, img.Width, img.Height);
            }
            string filename = Path.GetFileNameWithoutExtension(path);
            string newfile = Path.Combine(Directory.GetCurrentDirectory(), filename + "-new.png");
            dest.Save(newfile);
            img.Dispose();
            WaitUntilFree(path);
            File.Delete(path);
            WaitUntilFree(newfile);
            File.Move(newfile, path);
            return;
        }

        public static void WaitUntilFree(string file)
        {
            bool locked = true;
            var start = DateTime.Now;
            while (locked && DateTime.Now - start < TimeSpan.FromSeconds(3))
            {
                try
                {
                    FileStream fs = File.Open(file, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    fs.Close();
                    locked = false;
                }
                catch (IOException)
                {
                    locked = true;
                }
            }
            return;
        }
    }
}

