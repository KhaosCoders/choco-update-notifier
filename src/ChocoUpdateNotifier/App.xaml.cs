using CommandLine;
using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Linq;
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

        protected override void OnStartup(StartupEventArgs e)
        {
            // Register AUMID and COM server (for MSIX/sparse package apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<CunNotificationActivator>(AppId);
            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<CunNotificationActivator>();

            // Parse parameters
            Parser.Default.ParseArguments<CheckOptions>(e.Args)
                .WithParsed(Run);
        }

        private void Run(CheckOptions o)
        {
            // Check for outdated packages
            var pcks = Choco.OutdatedPackages();

            if (pcks.Count > 0)
            {
                ShowOutdatedNotification(pcks.Count, string.Join(", ", pcks.Select(p => p.Name)));
            }
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
                .AddToastActivationInfo("", ToastActivationType.Foreground)
                .AddHeader(MsgId, $"Chocolatey Packages outdated: {num}", "")
                .AddText(pcks, AdaptiveTextStyle.Body)
                .AddButton(new ToastButton("Update all", CunNotificationActivator.UpdateAllAction))
                .AddButton(new ToastButton("Update", CunNotificationActivator.UpdateAction))
                .GetToastContent();

            // Create the notification
            var notif = new ToastNotification(content.GetXml());
            notif.Dismissed += Notif_Dismissed;

            // And show it!
            var oNotifier = DesktopNotificationManagerCompat.CreateToastNotifier();
            oNotifier.Show(notif);
        }

        private static void Notif_Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            Environment.Exit(0);
        }
    }
}
