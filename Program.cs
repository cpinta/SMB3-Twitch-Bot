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
            List<string> lines = File.ReadAllLines($"{LUA_COMMANDS_FULLPATH}").ToList();
            lines.Add(line);
            File.WriteAllLines($"{LUA_COMMANDS_FULLPATH}", lines.ToArray());
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
                            string[] split = line.Split(';');
                            TwitchMessage message = new TwitchMessage();
                            try
                            {
                                List<string> formattedSplit = new List<string>();
                                for (int i=0; i<split.Length; i++)
                                {
                                    formattedSplit.Add(split[i].Trim().Split('=')[1]);
                                }
                                string msgText = formattedSplit[16].TrimStart().Split(" :")[1];
                                formattedSplit[16] = formattedSplit[16].Split(":")[0];
                                message = new TwitchMessage(formattedSplit.ToArray(), msgText);
                            }
                            catch { }

                            if (split.Length > 2)
                            {
                                Console.WriteLine(split[2]);
                                string[] command = split[2].Split(" ");

                                if (commands.ContainsKey(command[0]))
                                {
                                    commands[command[0]].ProcessOptions(message);
                                }
                                else
                                {
                                    SendLuaDisplayMessage(message.displayName, message.msg);
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

    public class TwitchMessage
    {
        public string badgeInfo = "";
        public string badges = "";
        public string clientNonce = "";
        public string color = "";
        public string displayName = "";
        public string emotes = "";
        public bool firstMsg = false;
        public string flags = "";
        public string ID = "";
        public bool mod = false;
        public bool returningChatter = false;
        public string roomID = "";
        public bool subscriber = false;
        public string tmiSentTs = "";
        public bool turbo = false;
        public string userID = "";
        public string userType = "";

        public string msg = "";
        public string command = "";
        public string allArgs = "";
        public string[] args;

        public TwitchMessage() { }
        public TwitchMessage(string[] strings, string msgText)
        {
            this.badgeInfo = strings[0];
            this.badges = strings[1];
            this.clientNonce = strings[2];
            this.color = strings[3];
            this.displayName = strings[4];
            this.emotes = strings[5];
            this.firstMsg = strings[6] == "0"? false : true;
            this.flags = strings[7];
            this.ID = strings[8];
            this.mod = strings[9] == "0" ? false : true;
            this.returningChatter = strings[10] == "0" ? false : true;
            this.roomID = strings[11];
            this.subscriber = strings[12] == "0" ? false : true;
            this.tmiSentTs = strings[13];
            this.turbo = strings[14] == "0" ? false : true;
            this.userID = strings[15];
            this.userType = strings[16];

            this.msg = msgText;

            string[] split = msgText.Split(' ');
            this.command = split[0];
            this.args = split.Skip(1).ToArray();
            this.allArgs = msgText.Split(' ', 2)[1];
        }
    }

    public abstract class TwitchCommand
    {
        public string name;
        public TwitchCommand(string name)
        {
            this.name = name;
        }
        public virtual void ProcessOptions(TwitchMessage message)
        {
            Console.WriteLine($"COMMAND {name} by {message}");
        }
    }

    public class HelpCommand : TwitchCommand
    {
        string text = "";

        public HelpCommand(string name) : base(name) { }

        public override void ProcessOptions(TwitchMessage message)
        {
            base.ProcessOptions(message);
            Program.SendTwitchMessage(message.ID, text);
        }
    }

    public class ShowLuaTextCommand : TwitchCommand
    {
        public ShowLuaTextCommand(string name) : base(name) { }

        public override void ProcessOptions(TwitchMessage message)
        {
            base.ProcessOptions(message);
            Program.SendLuaDisplayMessage(message.ID, message.args[0]);
        }
    }
}