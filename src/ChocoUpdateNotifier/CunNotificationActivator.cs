using Microsoft.Toolkit.Uwp.Notifications;
using Serilog;
using System;
using System.Runtime.InteropServices;

namespace ChocoUpdateNotifier
{
    [ClassInterface(ClassInterfaceType.None)]
    [ComSourceInterfaces(typeof(INotificationActivationCallback))]
    [Guid("4725539A-DBCE-4F06-8711-AF493DCC6016"), ComVisible(true)]
    public class CunNotificationActivator : NotificationActivator
    {
        public const string UpdateAllAction = "updateAll";
        public const string UpdateAction = "update";

        public override void OnActivated(string invokedArgs, NotificationUserInput userInput, string appUserModelId)
        {
            Log.Information("Activator activated with {Args}", invokedArgs);
            // Clear all toasts
            try
            {
                DesktopNotificationManagerCompat.History.Clear();
            }
            catch (Exception)
            {
                // Ignore errors (when notification service is not available)
            }
            switch (invokedArgs)
            {
                case UpdateAllAction:
                    Choco.UpdateAllPackages();
                    Environment.Exit(0);
                    break;

                case UpdateAction:
                default:
                    App.ShowUpdateDialog();
                    break;
            }
        }
    }
}
