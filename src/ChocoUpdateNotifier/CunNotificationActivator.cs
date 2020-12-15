using Microsoft.Toolkit.Uwp.Notifications;
using System;
using System.Runtime.InteropServices;
using System.Windows;

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
            switch (invokedArgs)
            {
                case UpdateAllAction:
                    Choco.UpdateAllPackages();
                    App.WaitLock.Set();
                    break;

                case UpdateAction:
                default:
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        var oWind = new UpdateWindow();
                        oWind.Closed += (sender, e) => App.WaitLock.Set();
                        oWind.Show();
                    }));
                    break;
            }
        }
    }
}
