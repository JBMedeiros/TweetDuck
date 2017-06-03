﻿using System;
using System.Runtime.InteropServices;
using TweetLib.Audio.Utils;
using WMPLib;

namespace TweetLib.Audio.Impl{
    sealed class SoundPlayerImplWMP : AudioPlayer{
        public override string SupportedFormats => "*.wav;*.mp3;*.mp2;*.m4a;*.mid;*.midi;*.rmi;*.wma;*.aif;*.aifc;*.aiff;*.snd;*.au";

        public override event EventHandler<PlaybackErrorEventArgs> PlaybackError;

        private readonly WindowsMediaPlayer player;
        private bool wasTryingToPlay;
        private bool ignorePlaybackError;

        // changing the player volume also affects the value in the Windows mixer
        // however, the initial value is always 50 or some other stupid shit
        // so we have to tell the player to set its volume to whatever the mixer is set to
        // using the most code required for the least functionality with a sorry excuse for an API
        // thanks, Microsoft

        public SoundPlayerImplWMP(){
            player = new WindowsMediaPlayer();
            player.settings.autoStart = false;
            player.settings.enableErrorDialogs = false;
            player.settings.invokeURLs = false;
            player.settings.volume = (int)Math.Floor(100.0*NativeCoreAudio.GetMixerVolumeForCurrentProcess());
            player.MediaChange += player_MediaChange;
            player.MediaError += player_MediaError;
        }

        public override void Play(string file){
            wasTryingToPlay = true;

            try{
                if (player.URL != file){
                    player.close();
                    player.URL = file;
                    ignorePlaybackError = false;
                }
                else{
                    player.controls.stop();
                }
            
                player.controls.play();
            }catch(Exception e){
                OnNotificationSoundError("An error occurred in Windows Media Player: "+e.Message);
            }
        }

        public override void Stop(){
            try{
                player.controls.stop();
            }catch{
                // ignore
            }
        }

        public override void Dispose(){
            player.close();
            Marshal.ReleaseComObject(player);
        }

        private void player_MediaChange(object item){
            IWMPMedia2 media = item as IWMPMedia2;

            if (media == null){
                OnNotificationSoundError("Unknown error.");
                return;
            }
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            else if (media.Error == null && media.duration == 0.0){
                OnNotificationSoundError("File does not contain an audio track.");
            }
            else if (media.Error != null){
                OnNotificationSoundError(media.Error);
            }

            Marshal.ReleaseComObject(media);
        }

        private void player_MediaError(object pMediaObject){
            IWMPMedia2 media = pMediaObject as IWMPMedia2;

            if (media == null){
                OnNotificationSoundError("Unknown error.");
                return;
            }
            else if (media.Error != null){
                OnNotificationSoundError(media.Error);
            }

            Marshal.ReleaseComObject(media);
        }

        private void OnNotificationSoundError(IWMPErrorItem error){
            OnNotificationSoundError(error.errorCode == -1072885353 ? "Invalid media file." : error.errorDescription);
            Marshal.ReleaseComObject(error);
        }

        private void OnNotificationSoundError(string message){
            if (wasTryingToPlay){
                wasTryingToPlay = false;

                if (!ignorePlaybackError && PlaybackError != null){
                    PlaybackErrorEventArgs args = new PlaybackErrorEventArgs(message);
                    PlaybackError(this, args);
                    ignorePlaybackError = args.Ignore;
                }
            }
        }
    }
}
