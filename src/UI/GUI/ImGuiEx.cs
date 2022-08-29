using Dalamud.Interface;
using Dalamud.Interface.Components;
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
            ImGui.Checkbox(label, ref v);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static bool Button(string label, string? tooltip = null)
        {
            var r = ImGui.Button(label);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            return r;
        }

        public static bool IconButton(FontAwesomeIcon icon, int id, string? tooltip = null)
        {
            var r = ImGuiComponents.IconButton(id, icon);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            return r;
        }

        public static void InputInt(string label, int itemWidth, ref int v, string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(itemWidth * ImGuiHelpers.GlobalScale);
            ImGui.InputInt("##" + label, ref v);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static void DragInt(string label, string unit, int itemWidth, 
            ref int v, int v_spd, int v_min, int v_max, string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(itemWidth * ImGuiHelpers.GlobalScale);
            ImGui.DragInt($"{unit}##{label}", ref v, v_spd, v_min, v_max);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static void DragFloat(string label, int itemWidth, ref float v, float v_spd, 
            float v_min, float v_max, string v_fmt = "%.2f", string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.SameLine();
            ImGui.SetNextItemWidth(itemWidth * ImGuiHelpers.GlobalScale);
            ImGui.DragFloat("##" + label, ref v, v_spd, v_min, v_max, v_fmt);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
        }

        public static void DragFloat4(string label, ref Vector4 v, float v_spd, 
            float v_min, float v_max, string v_fmt = "%.1f", string? tooltip = null)
        {
            ImGui.Text(label + ": ");
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.Indent();
            ImGui.DragFloat4("##" + label, ref v, v_spd, v_min, v_max, v_fmt);
            if (tooltip != null && ImGui.IsItemHovered()) ImGui.SetTooltip(tooltip);
            ImGui.Unindent();
        }
    }
}
