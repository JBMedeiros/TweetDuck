﻿using System;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using TweetDck.Configuration;
using TweetDck.Core.Handling;
using TweetDck.Core.Other;
using TweetDck.Resources;
using TweetDck.Core.Controls;
using System.Drawing;
using TweetDck.Core.Utils;
using TweetDck.Updates;
using TweetDck.Plugins;
using TweetDck.Plugins.Enums;
using TweetDck.Plugins.Events;
using System.Media;
using TweetDck.Core.Bridge;
using TweetDck.Core.Notification;
using TweetDck.Core.Notification.Screenshot;

namespace TweetDck.Core{
    sealed partial class FormBrowser : Form{
        private static UserConfig Config{
            get{
                return Program.UserConfig;
            }
        }

        public string UpdateInstallerPath { get; private set; }

        private readonly ChromiumWebBrowser browser;
        private readonly PluginManager plugins;
        private readonly UpdateHandler updates;
        private readonly FormNotification notification;

        private FormSettings currentFormSettings;
        private FormAbout currentFormAbout;
        private FormPlugins currentFormPlugins;
        private bool isLoaded;

        private FormWindowState prevState;

        private TweetScreenshotManager notificationScreenshotManager;
        private SoundPlayer notificationSound;

        public FormBrowser(PluginManager pluginManager, UpdaterSettings updaterSettings){
            InitializeComponent();

            Text = Program.BrandName;

            this.plugins = pluginManager;
            this.plugins.Reloaded += plugins_Reloaded;
            this.plugins.PluginChangedState += plugins_PluginChangedState;

            this.notification = CreateNotificationForm(NotificationFlags.AutoHide | NotificationFlags.TopMost);
            this.notification.CanMoveWindow = () => false;
            this.notification.Show();

            this.browser = new ChromiumWebBrowser("https://tweetdeck.twitter.com/"){
                MenuHandler = new ContextMenuBrowser(this),
                DialogHandler = new FileDialogHandler(this),
                JsDialogHandler = new JavaScriptDialogHandler(),
                LifeSpanHandler = new LifeSpanHandler()
            };

            #if DEBUG
            this.browser.ConsoleMessage += BrowserUtils.HandleConsoleMessage;
            #endif

            this.browser.LoadingStateChanged += Browser_LoadingStateChanged;
            this.browser.FrameLoadEnd += Browser_FrameLoadEnd;
            this.browser.RegisterJsObject("$TD", new TweetDeckBridge(this, notification));
            this.browser.RegisterAsyncJsObject("$TDP", plugins.Bridge);

            Controls.Add(browser);

            Disposed += (sender, args) => {
                browser.Dispose();

                if (notificationScreenshotManager != null){
                    notificationScreenshotManager.Dispose();
                }

                if (notificationSound != null){
                    notificationSound.Dispose();
                }
            };

            this.trayIcon.ClickRestore += trayIcon_ClickRestore;
            this.trayIcon.ClickClose += trayIcon_ClickClose;
            Config.TrayBehaviorChanged += Config_TrayBehaviorChanged;

            UpdateTrayIcon();

            this.updates = new UpdateHandler(browser, this, updaterSettings);
            this.updates.UpdateAccepted += updates_UpdateAccepted;
        }

        private void ShowChildForm(Form form){
            form.Show(this);
            form.Shown += (sender, args) => form.MoveToCenter(this);
        }

        public void ForceClose(){
            trayIcon.Visible = false; // checked in FormClosing event
            Close();
        }

        // window setup

        private void SetupWindow(){
            Config.BrowserWindow.Restore(this, true);
            prevState = WindowState;
            isLoaded = true;
        }

        private void UpdateTrayIcon(){
            trayIcon.Visible = Config.TrayBehavior.ShouldDisplayIcon();
        }

        // active event handlers

        private void Browser_LoadingStateChanged(object sender, LoadingStateChangedEventArgs e){
            if (!e.IsLoading){
                browser.AddWordToDictionary("tweetdeck");
                browser.AddWordToDictionary("TweetDeck");
                browser.AddWordToDictionary("tweetduck");
                browser.AddWordToDictionary("TweetDuck");
                browser.AddWordToDictionary("TD");

                Invoke(new Action(SetupWindow));
                browser.LoadingStateChanged -= Browser_LoadingStateChanged;
            }
        }

        private void Browser_FrameLoadEnd(object sender, FrameLoadEndEventArgs e){
            if (e.Frame.IsMain && BrowserUtils.IsTweetDeckWebsite(e.Frame)){
                ScriptLoader.ExecuteFile(e.Frame, "code.js");

                #if DEBUG
                ScriptLoader.ExecuteFile(e.Frame, "debug.js");
                #endif

                if (plugins.HasAnyPlugin(PluginEnvironment.Browser)){
                    ScriptLoader.ExecuteFile(e.Frame, PluginManager.PluginBrowserScriptFile);
                    ScriptLoader.ExecuteFile(e.Frame, PluginManager.PluginGlobalScriptFile);
                    plugins.ExecutePlugins(e.Frame, PluginEnvironment.Browser, true);
                }

                TweetDeckBridge.ResetStaticProperties();
            }
        }

        private void FormBrowser_Activated(object sender, EventArgs e){
            if (!isLoaded)return;

            trayIcon.HasNotifications = false;
        }

        private void FormBrowser_Resize(object sender, EventArgs e){
            if (!isLoaded)return;

            if (WindowState != prevState){
                prevState = WindowState;

                if (WindowState == FormWindowState.Minimized){
                    if (Config.TrayBehavior.ShouldHideOnMinimize()){
                        Hide(); // hides taskbar too?! welp that works I guess
                    }
                }
                else{
                    FormBrowser_ResizeEnd(sender, e);
                }
            }
        }

        private void FormBrowser_ResizeEnd(object sender, EventArgs e){ // also triggers when the window moves
            if (!isLoaded)return;

            if (Location != ControlExtensions.InvisibleLocation){
                Config.BrowserWindow.Save(this);
                Config.Save();
            }
        }

        private void FormBrowser_FormClosing(object sender, FormClosingEventArgs e){
            if (!isLoaded)return;

            if (Config.TrayBehavior.ShouldHideOnClose() && trayIcon.Visible && e.CloseReason == CloseReason.UserClosing){
                Hide(); // hides taskbar too?! welp that works I guess
                e.Cancel = true;
            }
        }

        private void Config_TrayBehaviorChanged(object sender, EventArgs e){
            if (!isLoaded)return;
            
            UpdateTrayIcon();
        }

        private void trayIcon_ClickRestore(object sender, EventArgs e){
            if (!isLoaded)return;

            isLoaded = false;
            Show();
            SetupWindow();
            Activate();
            UpdateTrayIcon();
        }

        private void trayIcon_ClickClose(object sender, EventArgs e){
            if (!isLoaded)return;

            ForceClose();
        }
        
        private void plugins_Reloaded(object sender, PluginLoadEventArgs e){
            ReloadBrowser();
        }

        private void plugins_PluginChangedState(object sender, PluginChangedStateEventArgs e){
            browser.ExecuteScriptAsync("window.TDPF_setPluginState", e.Plugin, e.IsEnabled ? 1 : 0); // ExecuteScriptAsync cannot handle boolean values as of yet
        }

        private void updates_UpdateAccepted(object sender, UpdateAcceptedEventArgs e){
            Hide();

            FormUpdateDownload downloadForm = new FormUpdateDownload(e.UpdateInfo);
            downloadForm.MoveToCenter(this);
            downloadForm.ShowDialog();

            if (downloadForm.UpdateStatus == FormUpdateDownload.Status.Succeeded){
                UpdateInstallerPath = downloadForm.InstallerPath;
                ForceClose();
            }
            else if (downloadForm.UpdateStatus == FormUpdateDownload.Status.Manual){
                ForceClose();
            }
            else{
                Show();
            }
        }

        protected override void WndProc(ref Message m){
            if (isLoaded && m.Msg == Program.WindowRestoreMessage){
                trayIcon_ClickRestore(trayIcon, new EventArgs());
                return;
            }

            if (isLoaded && m.Msg == 0x210 && (m.WParam.ToInt32() & 0xFFFF) == 0x020B){ // WM_PARENTNOTIFY, WM_XBUTTONDOWN
                browser.ExecuteScriptAsync("TDGF_onMouseClickExtra", (m.WParam.ToInt32() >> 16) & 0xFFFF);
                return;
            }

            base.WndProc(ref m);
        }

        // notification helpers

        public FormNotification CreateNotificationForm(NotificationFlags flags){
            return new FormNotification(this, plugins, flags);
        }

        public void PauseNotification(){
            notification.PauseNotification();
        }

        public void ResumeNotification(){
            notification.ResumeNotification();
        }

        // callback handlers

        public void OpenSettings(){
            if (currentFormSettings != null){
                currentFormSettings.BringToFront();
            }
            else{
                bool prevEnableUpdateCheck = Config.EnableUpdateCheck;

                currentFormSettings = new FormSettings(this, plugins, updates);

                currentFormSettings.FormClosed += (sender, args) => {
                    currentFormSettings = null;

                    if (!prevEnableUpdateCheck && Config.EnableUpdateCheck){
                        Config.DismissedUpdate = string.Empty;
                        Config.Save();
                        updates.Check(false);
                    }

                    if (!Config.EnableTrayHighlight){
                        trayIcon.HasNotifications = false;
                    }
                };

                ShowChildForm(currentFormSettings);
            }
        }

        public void OpenAbout(){
            if (currentFormAbout != null){
                currentFormAbout.BringToFront();
            }
            else{
                currentFormAbout = new FormAbout();
                currentFormAbout.FormClosed += (sender, args) => currentFormAbout = null;
                ShowChildForm(currentFormAbout);
            }
        }

        public void OpenPlugins(){
            if (currentFormPlugins != null){
                currentFormPlugins.BringToFront();
            }
            else{
                currentFormPlugins = new FormPlugins(plugins);
                currentFormPlugins.FormClosed += (sender, args) => currentFormPlugins = null;
                ShowChildForm(currentFormPlugins);
            }
        }

        public void OnTweetNotification(){ // may be called multiple times, once for each type of notification
            if (Config.EnableTrayHighlight && !ContainsFocus){
                trayIcon.HasNotifications = true;
            }
        }

        public void PlayNotificationSound(){
            if (string.IsNullOrEmpty(Config.NotificationSoundPath)){
                return;
            }

            if (notificationSound == null){
                notificationSound = new SoundPlayer();
            }

            if (notificationSound.SoundLocation != Config.NotificationSoundPath){
                notificationSound.SoundLocation = Config.NotificationSoundPath;
            }

            notificationSound.Play();
        }

        public void OnTweetScreenshotReady(string html, int width, int height){
            if (notificationScreenshotManager == null){
                notificationScreenshotManager = new TweetScreenshotManager(this);
            }

            notificationScreenshotManager.Trigger(html, width, height);
        }

        public void DisplayTooltip(string text){
            if (string.IsNullOrEmpty(text)){
                toolTip.Hide(this);
            }
            else{
                Point position = PointToClient(Cursor.Position);
                position.Offset(20, 10);
                toolTip.Show(text, this, position);
            }
        }

        public void OnImagePasted(){
            browser.ExecuteScriptAsync("TDGF_tryPasteImage()");
        }

        public void OnImagePastedFinish(){
            browser.ExecuteScriptAsync("TDGF_tryPasteImageFinish()");
        }

        public void TriggerTweetScreenshot(){
            browser.ExecuteScriptAsync("TDGF_triggerScreenshot()");
        }

        public void ReloadBrowser(){
            browser.ExecuteScriptAsync("window.location.reload()");
        }
    }
}