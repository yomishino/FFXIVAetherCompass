using Dalamud.Interface;
using ImGuiNET;
using System.Numerics;

namespace AetherCompass.UI.GUI
{
    public static class ImGuiEx
    {
        public static void IconText(FontAwesomeIcon icon, bool nextSameLine = false)
        {
            ImGui.PushFont(UiBuilder.IconFont);
            ImGui.Text(FontAwesomeExtensions.ToIconString(icon));
            ImGui.PopFont();
            if (nextSameLine) ImGui.SameLine();
        }

        public static void IconTextCompass(bool nextSameLine = false)
            => IconText(FontAwesomeIcon.Compass, nextSameLine);

        public static void IconTextMapMarker(bool nextSameLine = false)
            => IconText(FontAwesomeIcon.MapMarkerAlt, nextSameLine);

        public static void Separator(bool prependNewLine = false, bool appendNewLine = false)
        {
            if (prependNewLine) ImGui.NewLine();
            ImGui.Separator();
            if (appendNewLine) ImGui.NewLine();
        }

        public static void Checkbox(string label, ref bool v, string? tooltip = null)
        {
            ImGui.Checkbox(label + (tooltip == null ? string.Empty : " (?)"), ref v);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static void InputInt(string label, ref int v, string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * .9f);
            ImGui.InputInt((tooltip == null ? "##" : " (?)##") + label, ref v);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static void DragFloat(string label, ref float v, float v_spd, float v_min, float v_max, string v_fmt = "%.2f", string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.SameLine();
            ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X * .9f);
            ImGui.DragFloat((tooltip == null ? "##" : " (?)##") + label, ref v, v_spd, v_min, v_max, v_fmt);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static void DragFloat4(string label, ref Vector4 v, float v_spd, float v_min, float v_max, string v_fmt = "%.1f", string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.Indent();
            ImGui.DragFloat4((tooltip == null ? "##" : " (?)##") + label, ref v, v_spd, v_min, v_max, v_fmt);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.Unindent();
        }
    }
}
