using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Data;
using System.Text.RegularExpressions;
using System.Threading;

using PRoCon.Core;
using PRoCon.Core.Plugin;
using PRoCon.Core.Plugin.Commands;
using PRoCon.Core.Players;

namespace PRoConEvents
{
    public class SeedTrackerPlus : PRoConPluginAPI, IPRoConPluginInterface
    {


        private DateTime LogStartDateTime;
        private CServerInfo ServerData;
        private List<GameClient> Seeders = new List<GameClient>();
        private String FilePath;
        private String logText;
        private bool ContinueLogging = true;

        public enum MessageType { Warning, Error, Exception, Normal, Debug };

        private bool DebugEnabled;
        private bool AddNewClient;
        private bool ListNeedsUpdate = true;
        private String SoldierToAdd;

        private uint LowerPopThreshold = 16;
        private uint UpperPopThreshold = 64;
        //private String Suffix = "Pure";

        private String SeederPeriodStart;
        private String SeederPeriodEnd;


        private string debugLevelString = "1";
        private int debugLevel = 1;
        private string logName = "";

        public SeedTrackerPlus()
        {
            
        }

        public class GameClient
        {

            private CPlayerInfo playerData = null;


            public String Name { get { return playerData.SoldierName; } }
            public CPlayerInfo PlayerData { get { return playerData; } set { if (value != null) playerData = value; } }


            public GameClient(CPlayerInfo playerInfo)
            {
                playerData = playerInfo;
            }
        }

        public void ConsoleWrite(String msg, MessageType type)
        {
            toConsole(1, msg);
        }

        public void ConsoleWrite(String msg)
        {
            ConsoleWrite(msg, MessageType.Normal);
        }

        public void ConsoleWarn(String msg)
        {
            ConsoleWrite(msg, MessageType.Warning);
        }

        public void ConsoleError(String msg)
        {
            ConsoleWrite(msg, MessageType.Error);
        }

        public void ConsoleException(String msg)
        {
            ConsoleWrite(msg, MessageType.Exception);
        }

        public void DebugWrite(String msg, int level)
        {
            toConsole(2, msg);
            /*if (DebugEnabled)
            {
                ConsoleWrite(msg, MessageType.Debug);
            }*/
        }

        //Jeff Things

        public void toConsole(int msgLevel, String message)
        {
            //a message with msgLevel 1 is more important than 2
            if (debugLevel >= msgLevel)
            {
                this.ExecuteCommand("procon.protected.pluginconsole.write", "SeedTrackerPlus: " + message);
            }
        }

        public void toLog(String logText)
        {
            if (!String.IsNullOrEmpty(logName))
            {
                DateTime sunday = DateTime.Today.AddDays(-(int)DateTime.Today.DayOfWeek);
                //DateTime saturday = sunday.AddDays(-1);

                //if (DateTime.Today.DayOfWeek == DayOfWeek.Saturday)
                //{
                //    saturday = DateTime.Today;
                //}
                DateTime monday = sunday.AddDays(-6);

                if (DateTime.Today.DayOfWeek == DayOfWeek.Monday)
                    monday = DateTime.Today;

                String logNameTimestamped = logName.Replace("[date]", monday.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
                using (StreamWriter writeFile = new StreamWriter(logNameTimestamped, true))
                {
                    writeFile.WriteLine(logText);
                }
                this.toConsole(2, "An event has been logged to " + logNameTimestamped + " with contents " + logText);
            }
            else
            {
                toConsole(1, "Log name is empty... Fix it!");
            }
        }

        //End Jeff Things



        public string GetPluginName()
        {
            return "SeedTrackerPlus";
        }

        public string GetPluginVersion()
        {
            return "0.3.4";
        }

        public string GetPluginAuthor()
        {
            return "LilAznCutie69x0 + Analytalica";
        }

        public string GetPluginWebsite()
        {
            return "purebattlefield.org";
        }

        public string GetPluginDescription()
        {
            return @"
<p><b>SeedTrackerPlus is NOT a direct upgrade to SeedTracker.</b></p>
<p>New configuration settings:</p>
<ul>
  <li><b>Debug Level:</b> 0 suppresses all messages, 1 shows important
messages (recommended), 2 shows all messages</li>
  <li><b>Log Path:</b> Specify the path and filename of the
log.&nbsp;[date] will be replaced with the weekly timestamp. Hint:
pureWords uses a very similar format. <b>NOTE: </b> there is a bug in PRoCon that may strip slashes from the configuration settings after a restart. Try forward slashes if this happens.</li>
  <li>Lower and Upper Population Thresholds (defaults have been set)</li>
</ul>
<p>This plugin will log players that play on the server during
seeding periods. These periods are defined by an upper and lower
population threshold. The seeding periods begin when the lower
population threshold is met. Anytime the population reaches the upper
population threshold, the seeding period will end and the list of
seeders
will be added to the log. Each line in the log contains: The seeding
period start time and date, the seeding period end time
and date, the lower population threshold, the upper population
threshold followed by the list of players present
during that specific seeding period. Each week a new log file will be
generated.</p>

        
        ";
        }

        public List<CPluginVariable> GetDisplayPluginVariables()
        {
            List<CPluginVariable> lstReturn = new List<CPluginVariable>();
            lstReturn.Add(new CPluginVariable("New Settings|Debug Level", typeof(string), debugLevelString));
            lstReturn.Add(new CPluginVariable("New Settings|Log Path", typeof(string), logName));
            lstReturn.Add(new CPluginVariable("Lower Population Threshold", typeof (Int32), LowerPopThreshold));
            lstReturn.Add(new CPluginVariable("Upper Population Threshold", typeof(Int32), UpperPopThreshold));
            //lstReturn.Add(new CPluginVariable("Log Suffix", typeof(String), Suffix));
            //lstReturn.Add(new CPluginVariable("Debug Enabled?", typeof(bool), DebugEnabled));

            return lstReturn;
        }

        public List<CPluginVariable> GetPluginVariables()
        {
            return GetDisplayPluginVariables();
        }

        public void SetPluginVariable(String strVariable, String strValue)
        {
            uint tempOut = 0;
            bool tempBool = true;

            if (strVariable.Contains("Lower Population Threshold") && uint.TryParse(strValue, out tempOut))
            {
                LowerPopThreshold = tempOut;
            }
            else if (strVariable.Contains("Upper Population Threshold") && uint.TryParse(strValue, out tempOut))
            {
                UpperPopThreshold = tempOut;
            }

            /*else if (strVariable.Contains("Log Suffix"))
                Suffix = strValue;
            else if (strVariable.Contains("Debug Enabled?") && bool.TryParse(strValue, out tempBool))
                DebugEnabled = tempBool;*/

            if (Regex.Match(strVariable, @"Debug Level").Success)
            {
                debugLevelString = strValue;
                try
                {
                    debugLevel = Int32.Parse(debugLevelString);
                }
                catch (Exception z)
                {
                    toConsole(1, "Invalid debug level! Choose 0, 1, or 2 only.");
                    debugLevel = 1;
                    debugLevelString = "1";
                }
            }
            else if (Regex.Match(strVariable, @"Log Path").Success)
            {
                logName = strValue.Trim();
            }
        }

        public void OnPluginLoaded(string strHostName, string strPort, string strProconVersion)
        {
            this.RegisterEvents(this.GetType().Name, "OnPlayerJoin", "OnPlayerLeft", "OnListPlayers", "OnServerInfo");

        }

        public void OnPluginEnable()
        {
            toConsole(1, "Plugin enabled.");
            toConsole(2, "FilePath: " + logName);
            toLog("SeedTracker Enabled! Test logging:");
            LogToFile();
        }

        public void OnPluginDisable()
        {
            toConsole(1, "Plugin disabled.");
            toLog("SeedTracker Disabled!");
        }

        public override void OnListPlayers(List<CPlayerInfo> lstPlayers, CPlayerSubset cpsSubset)
        {
            // If player count is equal to lower threshold, start seeding period
            if (ServerData.PlayerCount == this.LowerPopThreshold)
            {
                Seeders.Clear();
                this.SeederPeriodStart = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");
                CreateSeederList(lstPlayers);
            }

            // If player count reaches upper threshold, log the seeders to file and end current seeding period
            if (Seeders.Count > 0 && this.SeederPeriodStart != null && ServerData.PlayerCount >= this.UpperPopThreshold)
            {
                LogToFile();
            }
            

        }

        public override void OnServerInfo(CServerInfo serverInfo)
        {
            this.ServerData = serverInfo;
        }

        public void CreateSeederList(List<CPlayerInfo> lstPlayers)
        {
            
            ConsoleWrite("Creating new seeder list. Period start: " + SeederPeriodStart.ToString(), MessageType.Debug);
            foreach (CPlayerInfo player in lstPlayers)
            {
                GameClient newClient = new GameClient(player);
                this.Seeders.Add(newClient);
            }

        }

        public void AddNewSeeder(List<CPlayerInfo> lstPlayers, String strSoldierName)
        {
            
            foreach (CPlayerInfo player in lstPlayers)
            {
                if (player.SoldierName.Equals(strSoldierName, StringComparison.OrdinalIgnoreCase))
                {
                    ConsoleWrite("Adding new player " + strSoldierName + " to seeder list. Players: " + ServerData.PlayerCount, MessageType.Debug);
                    Seeders.Add(new GameClient(player));
                    AddNewClient = false;
                }
            }
        }

        public override void OnPlayerJoin(String strSoldierName)
        {
            ConsoleWrite("New player " + strSoldierName + " joined.", MessageType.Debug);
            /*if (ServerData.PlayerCount >= this.LowerPopThreshold)
            {
                this.AddNewClient = true;
                this.SoldierToAdd = strSoldierName;
            }*/
            
            this.ExecuteCommand("procon.protected.send", "admin.listPlayers", "all");
        }

        public void LogToFile()
        {
            
            toConsole(1, "Begin logging to file.");
            this.SeederPeriodEnd = DateTime.Now.ToString("yyyyMMdd HH:mm:ss");

            String logData = SeederPeriodStart + ", " + SeederPeriodEnd + ", " + LowerPopThreshold + ", " + UpperPopThreshold;

            foreach (GameClient player in Seeders)
            {
                logData += "," + player.Name;
                toConsole(2, "Adding player " + player.Name + " to the log.");
            }

            toLog(logData);
            SeederPeriodStart = null;
            Seeders.Clear();
            toConsole(1, "Clearing seeder count.");
            

        }

    }
}
