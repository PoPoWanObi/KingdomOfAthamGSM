using System;
using System.IO;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
    public class KingdomOfAtham : SteamCMDAgent
    {
        public Plugin Plugin = new Plugin
        {
            name = "WindowsGSM.Kingdom of Atham",
            author = "PoPoWanObi",
            description = "🧩 WindowsGSM plugin for supporting Kingdom of Atham Server",
            version = "1.0.0",
            url = "https://github.com/PoPoWanObi/KingdomOfAthamGSM",
            color = "#ff0000"
        };

        public KingdomOfAtham(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
        private readonly ServerConfig _serverData;
        public string Error, Notice;

        public override bool loginAnonymous => true; // true if allows login anonymous on steamcmd, else false
        public override string AppId => "1736750"; // Value of app_update <AppId> 

        public override string StartPath => @"KoA\Binaries\Win64\KoAServer-Win64-Shipping.exe"; // Game server start path
        public string FullName = "Kingdom of Atham Dedicated Server"; // Game server FullName
        public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
        public int PortIncrements = 1; // This tells WindowsGSM how many ports should skip after installation
        public object QueryMethod = new A2S(); // Query method. Accepted value: null or new A2S() or new FIVEM() or new UT3()

        public string ServerName = "KoA Server";
        public string Port = "27001"; // Default port
        public string QueryPort = "27014"; // Default query port
        public string Defaultmap = ""; // Default map name
        public string Maxplayers = "100"; // Default maxplayers
        public string Additional = "-log -Multihome=192.168.0.69 -ip=192.168.0.69 -port=27001 -queryport=27014"; // Additional server start parameter

        private Dictionary<string, string> configData = new Dictionary<string, string>();

        public async void CreateServerCFG()
        {

        }

        public async Task<Process> Start()
        {
            string shipExePath = Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath);
            if (!File.Exists(shipExePath))
            {
                Error = $"{Path.GetFileName(shipExePath)} not found ({shipExePath})";
                return null;
            }

            string param = $" {_serverData.ServerParam} ";
            param += $"-publicip=\"{_serverData.ServerIP}\" ";
            param += $"-port={_serverData.ServerPort} ";
            param += $"-queryport={_serverData.ServerQueryPort} ";
            param += $"-publicqueryport={_serverData.ServerQueryPort} ";
            //param += $"-players={_serverData.ServerMaxPlayer} ";
            //param += $"-servername=\"\"\"{_serverData.ServerName}\"\"\"";

            // Prepare Process
            var p = new Process
            {
                StartInfo =
                {
                    WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
                    FileName = shipExePath,
                    Arguments = param.ToString(),
                    WindowStyle = ProcessWindowStyle.Hidden,
                    UseShellExecute = false

                },
                EnableRaisingEvents = true
            };

            // Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
            if (AllowsEmbedConsole)
            {
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                var serverConsole = new ServerConsole(_serverData.ServerID);
                p.OutputDataReceived += serverConsole.AddOutput;
                p.ErrorDataReceived += serverConsole.AddOutput;
            }

            // Start Process
            try
            {
                p.Start();
                if (AllowsEmbedConsole)
                {
                    p.BeginOutputReadLine();
                    p.BeginErrorReadLine();
                }

                return p;
            }
            catch (Exception e)
            {
                Error = e.Message;
                return null; // return null if fail to start
            }
        }

        public async Task Stop(Process p)
        {
            await Task.Run(() =>
            {
                Functions.ServerConsole.SetMainWindow(p.MainWindowHandle);
                Functions.ServerConsole.SendWaitToMainWindow("^c");
            });
            await Task.Delay(2000);
        }

        public async Task<Process> Update(bool validate = false, string custom = null)
        {
            var (p, error) = await Installer.SteamCMD.UpdateEx(serverData.ServerID, AppId, validate, custom: custom, loginAnonymous: loginAnonymous);
            Error = error;
            await Task.Run(() => { p.WaitForExit(); });
            return p;
        }

        public bool IsInstallValid()
        {
            return File.Exists(Functions.ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath));
        }

        public string GetLocalBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return steamCMD.GetLocalBuild(_serverData.ServerID, AppId);
        }

        public async Task<string> GetRemoteBuild()
        {
            var steamCMD = new Installer.SteamCMD();
            return await steamCMD.GetRemoteBuild(AppId);
        }
    }
}