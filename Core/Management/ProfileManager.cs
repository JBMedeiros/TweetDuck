﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TweetDuck.Core.Other;
using TweetDuck.Data;
using TweetDuck.Plugins;
using TweetDuck.Plugins.Enums;

namespace TweetDuck.Core.Management{
    sealed class ProfileManager{
        private static readonly string CookiesPath = Path.Combine(Program.StoragePath, "Cookies");
        private static readonly string TempCookiesPath = Path.Combine(Program.StoragePath, "CookiesTmp");

        [Flags]
        public enum Items{
            None = 0,
            UserConfig = 1,
            SystemConfig = 2, // TODO implement later
            Session = 4,
            PluginData = 8,
            All = UserConfig|SystemConfig|Session|PluginData
        }

        public bool IsRestarting { get; private set; }
        public Exception LastException { get; private set; }

        private readonly string file;
        private readonly PluginManager plugins;

        public ProfileManager(string file, PluginManager plugins){
            this.file = file;
            this.plugins = plugins;
        }

        public bool Export(Items items){
            try{
                using(CombinedFileStream stream = new CombinedFileStream(new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.None))){
                    if (items.HasFlag(Items.UserConfig)){
                        stream.WriteFile("config", Program.UserConfigFilePath);
                    }

                    if (items.HasFlag(Items.SystemConfig)){
                        stream.WriteFile("system", Program.SystemConfigFilePath);
                    }

                    if (items.HasFlag(Items.PluginData)){
                        stream.WriteFile("plugin.config", Program.PluginConfigFilePath);

                        foreach(Plugin plugin in plugins.Plugins){
                            foreach(PathInfo path in EnumerateFilesRelative(plugin.GetPluginFolder(PluginFolder.Data))){
                                try{
                                    stream.WriteFile(new string[]{ "plugin.data", plugin.Identifier, path.Relative }, path.Full);
                                }catch(ArgumentOutOfRangeException e){
                                    FormMessage.Warning("Export Profile", "Could not include a plugin file in the export. "+e.Message, FormMessage.OK);
                                }
                            }
                        }
                    }

                    if (items.HasFlag(Items.Session)){
                        stream.WriteFile("cookies", CookiesPath);
                    }

                    stream.Flush();
                }

                return true;
            }catch(Exception e){
                LastException = e;
                return false;
            }
        }

        public Items FindImportItems(){
            Items items = Items.None;

            try{
                using(CombinedFileStream stream = new CombinedFileStream(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))){
                    string key;

                    while((key = stream.SkipFile()) != null){
                        switch(key){
                            case "config":
                                items |= Items.UserConfig;
                                break;

                            case "system":
                                items |= Items.SystemConfig;
                                break;

                            case "plugin.config":
                            case "plugin.data":
                                items |= Items.PluginData;
                                break;

                            case "cookies":
                                items |= Items.Session;
                                break;
                        }
                    }
                }
            }catch(Exception e){
                LastException = e;
                items = Items.None;
            }

            return items;
        }

        public bool Import(Items items){
            try{
                HashSet<string> missingPlugins = new HashSet<string>();

                using(CombinedFileStream stream = new CombinedFileStream(new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))){
                    CombinedFileStream.Entry entry;

                    while((entry = stream.ReadFile()) != null){
                        switch(entry.KeyName){
                            case "config":
                                if (items.HasFlag(Items.UserConfig)){
                                    entry.WriteToFile(Program.UserConfigFilePath);
                                }

                                break;

                            case "system":
                                if (items.HasFlag(Items.SystemConfig)){
                                    entry.WriteToFile(Program.SystemConfigFilePath);
                                    IsRestarting = true;
                                }

                                break;

                            case "plugin.config":
                                if (items.HasFlag(Items.PluginData)){
                                    entry.WriteToFile(Program.PluginConfigFilePath);
                                }

                                break;

                            case "plugin.data":
                                if (items.HasFlag(Items.PluginData)){
                                    string[] value = entry.KeyValue;

                                    entry.WriteToFile(Path.Combine(Program.PluginDataPath, value[0], value[1]), true);

                                    if (!plugins.IsPluginInstalled(value[0])){
                                        missingPlugins.Add(value[0]);
                                    }
                                }

                                break;

                            case "cookies":
                                if (items.HasFlag(Items.Session)){
                                    entry.WriteToFile(Path.Combine(Program.StoragePath, TempCookiesPath));
                                    IsRestarting = true;
                                }

                                break;
                        }
                    }
                }

                if (missingPlugins.Count > 0){
                    FormMessage.Information("Importing TweetDuck Profile", "Detected missing plugins when importing plugin data:\n"+string.Join("\n", missingPlugins), FormMessage.OK);
                }

                return true;
            }catch(Exception e){
                LastException = e;
                return false;
            }
        }

        public static void ImportCookies(){
            if (File.Exists(TempCookiesPath)){
                try{
                    if (File.Exists(CookiesPath)){
                        File.Delete(CookiesPath);
                    }

                    File.Move(TempCookiesPath, CookiesPath);
                }catch(Exception e){
                    Program.Reporter.HandleException("Profile Import Error", "Could not import the cookie file to restore login session.", true, e);
                }
            }
        }

        public static void DeleteCookies(){
            try{
                if (File.Exists(CookiesPath)){
                    File.Delete(CookiesPath);
                }
            }catch(Exception e){
                Program.Reporter.HandleException("Session Reset Error", "Could not remove the cookie file to reset the login session.", true, e);
            }
        }

        private static IEnumerable<PathInfo> EnumerateFilesRelative(string root){
            return Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories).Select(fullPath => new PathInfo{
                Full = fullPath,
                Relative = fullPath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) // strip leading separator character
            }) : Enumerable.Empty<PathInfo>();
        }

        private sealed class PathInfo{
            public string Full { get; set; }
            public string Relative { get; set; }
        }
    }
}