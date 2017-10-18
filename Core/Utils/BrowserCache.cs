﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Linq;

namespace TweetDuck.Core.Utils{
    static class BrowserCache{
        private static bool ClearOnExit { get; set; }

        public static readonly string CacheFolder = Path.Combine(Program.StoragePath, "Cache");

        private static IEnumerable<string> CacheFiles{
            get{
                return Directory.EnumerateFiles(CacheFolder);
            }
        }

        public static void CalculateCacheSize(Action<long> callbackBytes){
            Task<long> task = new Task<long>(() => {
                return CacheFiles.Select(file => {
                    try{
                        return new FileInfo(file).Length;
                    }catch{
                        return 0L;
                    }
                }).Sum();
            });
            
            task.ContinueWith(originalTask => callbackBytes(originalTask.Exception == null ? originalTask.Result : -1L), TaskContinuationOptions.ExecuteSynchronously);
            task.Start();
        }

        public static void SetClearOnExit(){
            ClearOnExit = true;
        }

        public static void Exit(){
            if (ClearOnExit){
                foreach(string file in CacheFiles){
                    try{
                        File.Delete(file);
                    }catch{
                        // welp, too bad
                    }
                }
            }
        }
    }
}
