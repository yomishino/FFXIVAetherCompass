using AetherCompass.Game.SeFunctions;
using Dalamud.Game.Text.SeStringHandling;
using System.Threading.Tasks;

namespace AetherCompass.UI
{
    public static class Notifier
    {
        
        private static DateTime lastSeNotifiedTime = DateTime.MinValue;


        public static async void TryNotifyByChat(SeString msg, bool playSe, int macroSeId = 0)
        {
            Chat.PrintChat(msg);
            await Task.Run(() =>
            {
                if (playSe && CanNotifyBySe())
                {
                    Sound.PlaySoundEffect(macroSeId);
                    lastSeNotifiedTime = DateTime.UtcNow;
                }
            });
        }

        public static void TryNotifyByToast(string msg)
        {
            Plugin.ToastGui.ShowNormal(msg);
        }


        private static bool CanNotifyBySe()
            => (DateTime.UtcNow - lastSeNotifiedTime).TotalSeconds > 3;


        public static void ResetTimer()
        {
            lastSeNotifiedTime = DateTime.MinValue;
        }
    }
}
