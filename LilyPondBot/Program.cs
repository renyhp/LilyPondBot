using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using File = System.IO.File;
using System.Collections.Generic;
using Telegram.Bot.Exceptions;

namespace LilyPondBot
{

    static class Program
    {
        //TELEGRAM
        public static TelegramBotClient Bot;
        public static User Me;
        //MONITOR
        public static DateTime StartTime = DateTime.UtcNow;
        public static int MessagesReceived = 0;
        public static int CommandsProcessed = 0;
        public static int SuccesfulCompilations = 0;
        public static string Monitor = "";
        public static DateTime LatestMessageTime = DateTime.UtcNow;
        public static DateTime PreviousMessageTime = DateTime.UtcNow;
        public static readonly string BotVersion = "LilyPondBot v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
        public static readonly string LilyVersion = LilyPond.GetLilyVersion();
        public static bool UpdateMonitor = true;

        public static void Main(string[] args)
        {
            Console.Title = "LilyPondBot";
            Console.WriteLine(Program.BotVersion + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion);
            Bot = new TelegramBotClient(File.ReadAllText(Settings.TokenPath)) { Timeout = TimeSpan.FromSeconds(20) };
            Me = Bot.GetMeAsync().Result;
            Task.Run(() => ProgramMonitor());

            Bot.OnUpdate += async (sender, e) => await Task.Run(() => Bot_OnUpdate(sender, e));
            Bot.OnReceiveError += async (sender, e) => await Task.Run(() => Bot_OnReceiveError(sender, e));
            Bot.OnReceiveGeneralError += async (sender, e) => await Task.Run(() => Bot_OnReceiveGeneralError(sender, e));

            Bot.StartReceiving();

            new ManualResetEvent(false).WaitOne();
        }

        static void Bot_OnUpdate(object sender, Telegram.Bot.Args.UpdateEventArgs e)
        {
            bool log = false;

            try
            {
                if (e.Update.Message != null)
                {
                    if (e.Update.Message?.Date == null || e.Update.Message.Date < Program.StartTime.AddSeconds(-5))
                        return;
                    Handler.HandleMessage(e.Update.Message);
                    if (e.Update.Message.From.Id != Settings.renyhp)
                        log = true;
                }
                if (e.Update.CallbackQuery != null)
                {
                    if (e.Update.CallbackQuery?.Message?.Date == null || e.Update.CallbackQuery.Message.Date < Program.StartTime.AddSeconds(-5))
                        return;
                    Handler.HandleCallback(e.Update.CallbackQuery);
                    if (e.Update.CallbackQuery.From.Id != Settings.renyhp)
                        log = true;
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            if (log)
            {
                MessagesReceived++;
                LatestMessageTime = DateTime.UtcNow;
                UpdateMonitor = true;
            }
            return;
        }

        static void Bot_OnReceiveError(object sender, Telegram.Bot.Args.ReceiveErrorEventArgs e)
        {
            if (!Bot.IsReceiving)
                Bot.StartReceiving();
            LogError(e.ApiRequestException);
            return;
        }

        static void Bot_OnReceiveGeneralError(object sender, Telegram.Bot.Args.ReceiveGeneralErrorEventArgs e)
        {
            if (!Bot.IsReceiving)
                Bot.StartReceiving();
            LogError(e.Exception);
            return;
        }

        static void LogError(object o)
        {
            if ((o is ApiRequestException apiex && apiex.Message == "Request timed out") || !(o is Exception e))
                return;

            var msg = "";
            var counter = 0;
            do
            {
                var indents = new String('>', counter++);
                msg += indents + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + " - " + e.GetType().ToString() + " " + e.Source +
                    Environment.NewLine + indents + e.Message +
                    Environment.NewLine + indents + e.StackTrace +
                    Environment.NewLine + Environment.NewLine;
                e = e.InnerException;
            } while (e != null);

            //send to renyhp
            foreach (var text in ChunksUpto(msg, 4000))
            {
                try
                {
                    Bot.SendTextMessageAsync(Settings.renyhp, text);
                }
                catch
                {
                    // ignored
                }
            }

            //log
            msg += Environment.NewLine +
                "------------------------------------------------------------------------------------" +
                Environment.NewLine + Environment.NewLine;
            File.AppendAllText(Settings.LogPath, msg);

            return;
        }

        static void ProgramMonitor()
        {
            var version = Program.BotVersion + " @" + Me.Username + Environment.NewLine + "GNU LilyPond " + Program.LilyVersion + Environment.NewLine;
            while (true)
            {
                if (UpdateMonitor)
                {
                    //update the monitor
                    Monitor = "Start time: " + StartTime.ToString("dd/MM/yyyy HH:mm:ss") + " UTC" + Environment.NewLine +
                    "Last message received: " + LatestMessageTime.ToString("dd/MM/yyyy HH:mm:ss") + " UTC" + Environment.NewLine +
                    "Messages received: " + MessagesReceived.ToString() + Environment.NewLine +
                    "Commands processed: " + CommandsProcessed.ToString() + Environment.NewLine +
                    "Successful compilations: " + SuccesfulCompilations.ToString();
                    Console.SetCursorPosition(0, 0);
                    Console.Clear();
                    Console.WriteLine(version + Environment.NewLine + Monitor);

                    //daily log
                    if (PreviousMessageTime.CompareTo(DateTime.UtcNow.Date.AddHours(Settings.DailyLogUtcHour)) < 0 && LatestMessageTime.Hour >= Settings.DailyLogUtcHour)
                    {
                        File.AppendAllText(
                            Settings.LogPath, DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + " DAILY LOG ----- " +
                        Environment.NewLine + Monitor + Environment.NewLine + "-----" + Environment.NewLine + Environment.NewLine
                        );
                        //reset
                        MessagesReceived = 0;
                        CommandsProcessed = 0;
                        SuccesfulCompilations = 0;
                    }

                    PreviousMessageTime = LatestMessageTime;
                    UpdateMonitor = false;
                }
                //wait before redoing this
                Task.Delay(60000).Wait();
            }
        }

        static IEnumerable<string> ChunksUpto(string str, int maxChunkSize)
        {
            for (int i = 0; i < str.Length; i += maxChunkSize)
                yield return str.Substring(i, Math.Min(maxChunkSize, str.Length - i));
        }
    }
}
