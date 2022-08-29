using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using System.Numerics;

namespace AetherCompass.UI
{
    public static class Chat
    {

        public static void PrintChat(string msg)
        {
            Plugin.ChatGui.Print("[AetherCompass] " + msg);
        }

        public static void PrintChat(SeString msg)
        {
            msg.Payloads.Insert(0, new TextPayload("[AetherCompass] "));
            Plugin.ChatGui.PrintChat(new XivChatEntry()
            {
                Message = msg,
            });
        }

        public static void PrintErrorChat(string msg)
        {
            Plugin.ChatGui.PrintError("[AetherCompass] " + msg);
        }

        public static SeString CreateMapLink(Common.FixedMapLinkPayload fixedMapPayload)
        {
            var nameString = $"{fixedMapPayload.PlaceName} {fixedMapPayload.CoordinateString}";

            var payloads = new List<Payload>(new Payload[]
            {
                fixedMapPayload,
                // arrow goes here
                new TextPayload(nameString),
                RawPayload.LinkTerminator,
            });
            payloads.InsertRange(1, SeString.TextArrowPayloads);

            return new(payloads);
        }

        public static SeString CreateMapLink(uint terrId, uint mapId, float xCoord, float yCoord)
        {
            var maplink = SeString.CreateMapLink(terrId, mapId, xCoord, yCoord, .01f);
            return maplink;
        }

        public static SeString CreateMapLink(uint terrId, uint mapId, Vector3 coord, bool showZ = false)
        {
            var maplink = SeString.CreateMapLink(terrId, mapId, coord.X, coord.Y, .01f);
            if (showZ)
                maplink.Append(new TextPayload($" Z:{coord.Z: 0.0}"));
            return maplink;
        }

        public static SeString PrependText(this SeString s, string text)
        {
            s.Payloads.Insert(0, new TextPayload(text));
            return s;
        }

        public static SeString AppendText(this SeString s, string text)
            => s.Append(new TextPayload(text));
    }
}
