using CommandLine;
using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;
using Serilog.Events;
using Serilog.Exceptions;
using System;
using System.Linq;
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

        private bool _exit = true;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Setup logging
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.WithEnvironmentUserName()
                .Enrich.WithProcessId()
                .Enrich.WithExceptionDetails()
#if DEBUG
                .WriteTo.Debug()
                .WriteTo.File(@"logs\nuc.txt", restrictedToMinimumLevel: LogEventLevel.Verbose)
                .WriteTo.Seq("http://localhost:5341/")
#else
                .Filter.ByExcluding("Args[?] = '-ToastActivated'")
                .WriteTo.File(@"logs\nuc.txt", restrictedToMinimumLevel: LogEventLevel.Warning)
#endif
                .CreateLogger();
            Log.Information("Start ChocoUpdateNotifier with Args: {Args}", e.Args);

            // Log unhandled exceptions
            AppDomain.CurrentDomain.UnhandledException += (_, e) => Log.Fatal(e.ExceptionObject as Exception, "Unhandled Exception");
            Application.Current.DispatcherUnhandledException += (_, e) => Log.Fatal(e.Exception, "Unhandled Dispatcher Exception");
            TaskScheduler.UnobservedTaskException += (_, e) => Log.Fatal(e.Exception, "Unobserved Task Exception");
            // Ensure Logs are saved
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Log.CloseAndFlush();


            // Register AUMID and COM server (for MSIX/sparse package apps, this no-ops)
            DesktopNotificationManagerCompat.RegisterAumidAndComServer<CunNotificationActivator>(AppId);
            // Register COM server and activator type
            DesktopNotificationManagerCompat.RegisterActivator<CunNotificationActivator>();
            // Clear all toasts
            try
            {
                DesktopNotificationManagerCompat.History.Clear();
            }
            catch (Exception)
            {
                // Ignore errors (when notification service is not available)
            }

            // Parse parameters
            Parser.Default.ParseArguments<CheckOptions, ListOptions>(e.Args)
                .WithParsed<CheckOptions>(Run)
                .WithParsed<ListOptions>(Run);

            if (_exit)
            {
                Log.Warning("No valid verb found!");
                Environment.Exit(1);
            }
        }

        private void Run(CheckOptions o)
        {
            _exit = false;
            Log.Debug("Mode: check");
            // Check for outdated packages
            Task.Run(() =>
            {
                var pcks = Choco.OutdatedPackages();
                Log.Debug("Choco found {PackageCount} outdated packages", pcks.Count);

                if (pcks.Count > 0)
                {
                    try
                    {
                        ShowOutdatedNotification(pcks.Count, string.Join(", ", pcks.Select(p => p.Name)));
                    }
                    catch (Exception e)
                    {
                        Log.Error(e, "Error sending notification");
                    }
                }
            });
        }

        private void Run(ListOptions o)
        {
            _exit = false;
            Log.Debug("Mode: list");
            ShowUpdateDialog();
        }

        internal static void ShowUpdateDialog()
        {
            Log.Debug("Will open dialog");
            Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                Log.Information("Show dialog");
                var oWind = new UpdateWindow();
                oWind.Closed += (sender, e) =>
                {
                    Log.Debug("Dialog closed");
                    Environment.Exit(0);
                };
                oWind.Show();
            }));
        }

        static void ShowOutdatedNotification(int num, string pcks)
        {
            Log.Debug("Show outdated notification. Packages: {Packages}", pcks);
            // enforce max length
            if (pcks.Length > 103)
            {
                pcks = pcks.Substring(0, 100) + "...";
            }

            // Construct the content
            var updateAll = new ToastButton("Update all", CunNotificationActivator.UpdateAllAction);
            updateAll.ActivationType = ToastActivationType.Background;

            var update = new ToastButton("Update", CunNotificationActivator.UpdateAction);
            update.ActivationType = ToastActivationType.Background;

            var content = new ToastContentBuilder()
                .AddToastActivationInfo("list", ToastActivationType.Background)
                .AddHeader(MsgId, $"Chocolatey Packages outdated: {num}", "list")
                .AddText(pcks, AdaptiveTextStyle.Body)
                .AddButton(updateAll)
                .AddButton(update)
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
        }

        private static void Notif_Dismissed(ToastNotification sender, ToastDismissedEventArgs args)
        {
            Log.Information("Notification dismissed");
            Environment.Exit(0);
        }
    }
}
