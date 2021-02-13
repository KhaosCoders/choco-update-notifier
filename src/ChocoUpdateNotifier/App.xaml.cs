using CommandLine;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Windows.UI.Notifications;

namespace ChocoUpdateNotifier
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const string AppId = "Khaos-Coders.ChocoUpdateNotifier";
        private const string MsgId = "KC.CUN.HEADER1";

        [Verb("check", HelpText = "Check for outdated Chocolatey packages")]
        private class CheckOptions
        {
        }

        [Verb("list", HelpText = "List available updates for outdated Chocolatey packages")]
        private class ListOptions
        {
        }

        public static ManualResetEvent WaitLock = new(false);

        protected override void OnStartup(StartupEventArgs e)
        {
            // Register AUMID and COM server (for MSIX/sparse package apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<CunNotificationActivator>(AppId);
            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<CunNotificationActivator>();

            // Parse parameters
            Parser.Default.ParseArguments<CheckOptions, ListOptions>(e.Args)
                .WithParsed<CheckOptions>(Run)
                .WithParsed<ListOptions>(Run);
        }

        private void Run(CheckOptions o)
        {
            // Check for outdated packages
            Task.Run(() =>
            {
                var pcks = Choco.OutdatedPackages();

                if (pcks.Count > 0)
                {
                    ShowOutdatedNotification(pcks.Count, string.Join(", ", pcks.Select(p => p.Name)));
                    Environment.Exit(0);
                }
            });
        }

        private void Run(ListOptions o) => ShowUpdateDialog();

        internal static void ShowUpdateDialog()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var oWind = new UpdateWindow();
                oWind.Closed += (sender, e) => App.WaitLock.Set();
                oWind.Show();
            }));
        }

        static void ShowOutdatedNotification(int num, string pcks)
        {
            // enforce max length
            if (pcks.Length > 103)
            {
                pcks = pcks.Substring(0, 100) + "...";
            }

            // Construct the content
            var content = new ToastContentBuilder()
                .AddToastActivationInfo("list", ToastActivationType.Background)
                .AddHeader(MsgId, $"Chocolatey Packages outdated: {num}", "")
                .AddText(pcks, AdaptiveTextStyle.Body)
                .AddButton(new ToastButton("Update all", CunNotificationActivator.UpdateAllAction))
                .AddButton(new ToastButton("Update", CunNotificationActivator.UpdateAction))
                .GetToastContent();

            // Create the notification
            var notif = new ToastNotification(content.GetXml())
            {
                ExpirationTime = DateTimeOffset.Now.AddMinutes(10)
            };
            notif.Dismissed += Notif_Dismissed;

            // And show it!
            var oNotifier = DesktopNotificationManagerCompat.CreateToastNotifier();
            oNotifier.Show(notif);

            WaitLock.WaitOne();

            oNotifier.Hide(notif);
        }

        private static void Notif_Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            if (args.Reason != ToastDismissalReason.TimedOut)
            {
                WaitLock.Set();
            }
        }
    }
}
