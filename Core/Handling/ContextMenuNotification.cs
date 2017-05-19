﻿using CefSharp;
using TweetDuck.Core.Controls;
using TweetDuck.Core.Notification;

namespace TweetDuck.Core.Handling{
    class ContextMenuNotification : ContextMenuBase{
        private const int MenuSkipTweet = 26600;
        private const int MenuFreeze = 26601;
        private const int MenuCopyTweetUrl = 26602;
        private const int MenuCopyQuotedTweetUrl = 26603;

        private readonly FormNotificationBase form;
        private readonly bool enableCustomMenu;

        public ContextMenuNotification(FormNotificationBase form, bool enableCustomMenu) : base(form){
            this.form = form;
            this.enableCustomMenu = enableCustomMenu;
        }

        public override void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model){
            model.Clear();

            if (parameters.TypeFlags.HasFlag(ContextMenuType.Selection)){
                model.AddItem(CefMenuCommand.Copy, "Copy");
                model.AddSeparator();
            }

            base.OnBeforeContextMenu(browserControl, browser, frame, parameters, model);

            if (enableCustomMenu){
                model.AddItem((CefMenuCommand)MenuSkipTweet, "Skip tweet");
                model.AddCheckItem((CefMenuCommand)MenuFreeze, "Freeze");
                model.SetChecked((CefMenuCommand)MenuFreeze, form.FreezeTimer);
                model.AddSeparator();

                if (!string.IsNullOrEmpty(form.CurrentTweetUrl)){
                    model.AddItem((CefMenuCommand)MenuCopyTweetUrl, "Copy tweet address");

                    if (!string.IsNullOrEmpty(form.CurrentQuoteUrl)){
                        model.AddItem((CefMenuCommand)MenuCopyQuotedTweetUrl, "Copy quoted tweet address");
                    }

                    model.AddSeparator();
                }
            }
            
            if (HasDevTools){
                AddDebugMenuItems(model);
            }

            RemoveSeparatorIfLast(model);

            form.InvokeAsyncSafe(() => form.ContextMenuOpen = true);
        }

        public override bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags){
            if (base.OnContextMenuCommand(browserControl, browser, frame, parameters, commandId, eventFlags)){
                return true;
            }

            switch((int)commandId){
                case MenuSkipTweet:
                    form.InvokeAsyncSafe(form.FinishCurrentNotification);
                    return true;

                case MenuFreeze:
                    form.InvokeAsyncSafe(() => form.FreezeTimer = !form.FreezeTimer);
                    return true;

                case MenuCopyTweetUrl:
                    SetClipboardText(form.CurrentTweetUrl);
                    return true;

                case MenuCopyQuotedTweetUrl:
                    SetClipboardText(form.CurrentQuoteUrl);
                    return true;
            }

            return false;
        }

        public override void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame){
            base.OnContextMenuDismissed(browserControl, browser, frame);
            form.InvokeAsyncSafe(() => form.ContextMenuOpen = false);
        }
    }
}
