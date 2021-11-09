using AetherCompass.UI.SeFunctions;
using Dalamud.Game.Text.SeStringHandling;
using System;

namespace AetherCompass.UI
{
    public class Notifier
    {
        
        private static DateTime lastSeNotifiedTime = DateTime.MinValue;


        public void TryNotifyByChat(string compassName, SeString msg, bool playSe, int macroSeId = 0)
        {
            Chat.PrintChat(msg.PrependText($"{compassName}: "));
            if (playSe && CanNotifyBySe())
            {
                Sound.PlaySoundEffect(macroSeId);
                lastSeNotifiedTime = DateTime.UtcNow;
            }
        }

        public void TryNotifyByToast(string msg)
        {
            Plugin.ToastGui.ShowNormal(msg);
        }


        private static bool CanNotifyBySe()
            => (DateTime.UtcNow - lastSeNotifiedTime).TotalSeconds > 3;


        public void ResetTimer()
        {
            lastSeNotifiedTime = DateTime.MinValue;
        }
    }
}
