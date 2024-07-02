using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Diagnostics;


namespace SMB3TwitchBot
{
    public static class Program
    {
        static TcpClient tcpClient = new TcpClient();
        static StreamReader? streamReader;
        static StreamWriter? streamWriter;

        public static string ip = "irc.chat.twitch.tv";
        public static int port = 6667;
        public static string iniPath = ".twitch-config.ini";
        public static string password = "";
        public static string botUsername = "";
        public static string twitchChannelName = "";

        public static Dictionary<string, TwitchCommand> commands = new Dictionary<string, TwitchCommand>();

        public static string PROJECT_PATH = Directory.GetParent(Environment.CurrentDirectory).Parent.Parent.FullName;
        public static JSONFile iniConfig = new JSONFile($"{PROJECT_PATH}/{iniPath}");

        public static string EMULATOR_PATH = @".\emulator\Mesen.exe";
        public static string SMB3_PATH = @".\emulator\Super Mario Bros. 3.nes";
        public static string LUA_SCRIPT_PATH = "twitchbot.lua";
        public static string LUA_COMMANDS_PATH = "luaCMD.txt";
        public static string LUA_COMMANDS_FULLPATH = $"{PROJECT_PATH.Replace("\\", "/")}/{LUA_COMMANDS_PATH}";


        public static int errorCount = 0;
        public static bool botRunning = false;
        public static bool restartingBot = false;

        public static int MAX_HAIR_LENGTH = 10000;
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting Program.");
            LoadVariables();
            StartBot();
            StartMesen();
            ReadTwitchChat();
        }


        public static void LoadVariables()
        {
            password = iniConfig.Get("OAUTH");
            botUsername = iniConfig.Get("BOT_NAME");
            twitchChannelName = iniConfig.Get("CHANNEL_NAME");

            commands.Add("help", new HelpCommand("help"));
            commands.Add("say", new ShowLuaTextCommand("say"));

            string[] lines = File.ReadAllLines($"{PROJECT_PATH}\\{LUA_SCRIPT_PATH}");
            lines[0] = $"local cmdPath = \"{LUA_COMMANDS_FULLPATH}\";";

            File.WriteAllLines($"{PROJECT_PATH}/{LUA_SCRIPT_PATH}", lines);

        }

        public static void StartBot()
        {
            restartingBot = false;
            errorCount = 0;

            tcpClient = new TcpClient();
            tcpClient.Connect(ip, port);

            streamReader = new StreamReader(tcpClient.GetStream());
            streamWriter = new StreamWriter(tcpClient.GetStream()) { NewLine = "\r\n", AutoFlush = true };
            streamWriter.AutoFlush = true;


            streamWriter.WriteLine($"PASS {password}");
            streamWriter.WriteLine($"NICK {botUsername}");
            streamWriter.WriteLine($"JOIN #{twitchChannelName}");
            streamWriter.WriteLine($"CAP REQ :twitch.tv/commands twitch.tv/tags");
            // SendTwitchMessage("test message");
        }

        public static void SendTwitchMessage(string message)
        {
            streamWriter?.WriteLine($"PRIVMSG #{twitchChannelName} :{message}");
        }
        public static void SendTwitchMessage(string messageID, string message)
        {
            streamWriter?.WriteLine($"@reply-parent-msg-id={messageID} PRIVMSG #{twitchChannelName} :{message}");
        }

        static void SendLuaCommand(string line)
        {
            string[] lines = File.ReadAllLines($"{LUA_COMMANDS_FULLPATH}");
            lines.Append(line);
            File.WriteAllLines($"{LUA_COMMANDS_FULLPATH}", lines);
        }

        public static void SendLuaDisplayMessage(string userName, string line)
        {
            SendLuaCommand($"emu.displayMessage(\"{userName}\", \"{line}\");");
        }

        public static void ReadTwitchChat()
        {
            botRunning = true;
            while (botRunning)
            {
                try
                {
                    string line = streamReader.ReadLine();
                    Console.WriteLine(line);
                    if (line.Trim() != "")
                    {
                        if (line.StartsWith("PING"))
                        {
                            streamWriter.WriteLine("PONG :tmi.twitch.tv");
                            Console.WriteLine("Responded to PING with PONG");

                        }
                        else
                        {
                            string[] split = line.Split(':');
                            string messageID = "";
                            try
                            {
                                messageID = line.Split(";id=")[1].Split(";")[0];
                            }
                            catch { }


                            if (split.Length > 2)
                            {
                                Console.WriteLine(split[2]);
                                string[] command = split[2].Split(" ");

                                if (commands.ContainsKey(command[0]))
                                {
                                    commands[command[0]].ProcessOptions(messageID, command.Skip(1).ToArray());
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("ERROR:" + e);
                    errorCount++;
                    if (errorCount > 10)
                    {
                        botRunning = false;
                        restartingBot = true;
                    }
                }
            }
            if (restartingBot)
            {
                StartBot();
            }
        }

        public static void StartMesen()
        {
            string command = $"{PROJECT_PATH}\\{EMULATOR_PATH} \"{SMB3_PATH}\" \"{LUA_SCRIPT_PATH}\" --video.ntscScale=_4x";

            var p = new Process
            {
                StartInfo =
                {
                    FileName = $"{PROJECT_PATH}\\{EMULATOR_PATH}",
                    WorkingDirectory = PROJECT_PATH,
                    Arguments = $"\"{SMB3_PATH}\" \"{LUA_SCRIPT_PATH}\" --video.ntscScale=_4x"
                }
            };

            p.Start();
        }
    }





    public abstract class TwitchCommand
    {
        public string name;

        public TwitchCommand(string name)
        {
            this.name = name;
        }


        public virtual void ProcessOptions(string user, string[] args)
        {
            Console.WriteLine($"COMMAND {name} by {user}");
        }
    }

    public class HelpCommand : TwitchCommand
    {
        string text = "'length <number>' to change hair length.    'speed <number>' to change hair speed.    'color <color>' to change hair color";

        public HelpCommand(string name) : base(name) { }

        public override void ProcessOptions(string user, string[] args)
        {
            base.ProcessOptions(user, args);
            Program.SendTwitchMessage(user, text);
        }
    }

    public class ShowLuaTextCommand : TwitchCommand
    {
        public ShowLuaTextCommand(string name) : base(name) { }

        public override void ProcessOptions(string user, string[] args)
        {
            base.ProcessOptions(user, args);
            Program.SendLuaDisplayMessage(user, args[0]);
        }
    }
}