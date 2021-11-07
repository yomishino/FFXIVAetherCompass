using AetherCompass.UI.SeFunctions;
using Dalamud.Game.Text.SeStringHandling;
using System;

namespace AetherCompass.UI
{
    public class Notifier
    {
        private DateTime lastNotifiedTime = DateTime.MinValue;
        private static DateTime lastSeNotifiedTime = DateTime.MinValue;

        public void TryNotifyByChat(string compassName, SeString msg, bool playSe, int macroSeId = 0)
        {
            if (CanNotify())
            {
                Chat.PrintChat(msg.PrependText($"{compassName}: "));
                if (playSe && CanNotifyBySe())
                {
                    Sound.PlaySoundEffect(macroSeId);
                    lastSeNotifiedTime = DateTime.UtcNow;
                }
                lastNotifiedTime = DateTime.UtcNow;
            }
        }

        private bool CanNotify()
            => (DateTime.UtcNow - lastNotifiedTime).TotalSeconds > 60;

        private static bool CanNotifyBySe()
            => (DateTime.UtcNow - lastSeNotifiedTime).TotalSeconds > 1;


        public void ResetTimer()
        {
            lastNotifiedTime = DateTime.MinValue;
            lastSeNotifiedTime = DateTime.MinValue;
        }
    }
}
