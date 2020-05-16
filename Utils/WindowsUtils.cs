﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Microsoft.Win32;

namespace TweetDuck.Utils{
    static class WindowsUtils{
        private static readonly bool IsWindows8OrNewer = OSVersionEquals(major: 6, minor: 2); // windows 8/10

        public static bool ShouldAvoidToolWindow { get; } = IsWindows8OrNewer;
        public static bool IsAeroEnabled => IsWindows8OrNewer || (NativeMethods.DwmIsCompositionEnabled(out bool isCompositionEnabled) == 0 && isCompositionEnabled);

        private static bool OSVersionEquals(int major, int minor){
            System.Version ver = Environment.OSVersion.Version;
            return ver.Major == major && ver.Minor == minor;
        }

        public static bool OpenAssociatedProgram(string file, string arguments = "", bool runElevated = false){
            try{
                using(Process.Start(new ProcessStartInfo{
                    FileName = file,
                    Arguments = arguments,
                    Verb = runElevated ? "runas" : string.Empty,
                    ErrorDialog = true
                })){
                    return true;
                }
            }catch(Win32Exception e) when (e.NativeErrorCode == 0x000004C7){ // operation canceled by the user
                return false;
            }catch(Exception e){
                Program.Reporter.HandleException("Error Opening Program", "Could not open the associated program for " + file, true, e);
                return false;
            }
        }

        public static bool TrySleepUntil(Func<bool> test, int timeoutMillis, int timeStepMillis){
            for(int waited = 0; waited < timeoutMillis; waited += timeStepMillis){
                if (test()){
                    return true;
                }

                Thread.Sleep(timeStepMillis);
            }

            return false;
        }

        public static void TryDeleteFolderWhenAble(string path, int timeout){
            new Thread(() => {
                TrySleepUntil(() => {
                    try{
                        Directory.Delete(path, true);
                        return true;
                    }catch(DirectoryNotFoundException){
                        return true;
                    }catch{
                        return false;
                    }
                }, timeout, 500);
            }).Start();
        }

        public static IEnumerable<Browser> FindInstalledBrowsers(){
            static IEnumerable<Browser> ReadBrowsersFromKey(RegistryHive hive){
                using RegistryKey root = RegistryKey.OpenBaseKey(hive, RegistryView.Default);
                using RegistryKey browserList = root.OpenSubKey(@"SOFTWARE\Clients\StartMenuInternet", false);

                if (browserList == null){
                    yield break;
                }

                foreach(string sub in browserList.GetSubKeyNames()){
                    using RegistryKey browserKey = browserList.OpenSubKey(sub, false);
                    using RegistryKey shellKey = browserKey?.OpenSubKey(@"shell\open\command");

                    if (shellKey == null){
                        continue;
                    }

                    string browserName = browserKey.GetValue(null) as string;
                    string browserPath = shellKey.GetValue(null) as string;

                    if (string.IsNullOrEmpty(browserName) || string.IsNullOrEmpty(browserPath)){
                        continue;
                    }

                    if (browserPath[0] == '"' && browserPath[browserPath.Length - 1] == '"'){
                        browserPath = browserPath.Substring(1, browserPath.Length - 2);
                    }

                    yield return new Browser(browserName, browserPath);
                }
            }

            HashSet<Browser> browsers = new HashSet<Browser>();
            
            try{
                browsers.UnionWith(ReadBrowsersFromKey(RegistryHive.CurrentUser));
                browsers.UnionWith(ReadBrowsersFromKey(RegistryHive.LocalMachine));
            }catch{
                // oops I guess
            }

            return browsers;
        }

        public sealed class Browser{
            public string Name { get; }
            public string Path { get; }

            public Browser(string name, string path){
                this.Name = name;
                this.Path = path;
            }

            public override int GetHashCode() => Name.GetHashCode();
            public override bool Equals(object obj) => obj is Browser other && Name == other.Name;
            public override string ToString() => Name;
        }
    }
}