using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace MapModeFramework
{
    public class MapModeUI : Window
    {
        public MapModeComponent parent;
        private List<MapMode> MapModes => parent.mapModes;
        public MapMode CurrentMapMode => parent.currentMapMode;

        public override Vector2 InitialSize => new Vector2(curBaseX + Margin * 2f, curBaseY + Margin * 2f);
        private const float barWidth = 400f;
        private const float barHeight = 48f;
        float curBaseX;
        float curBaseY;

        int currentMapModeIndex;

        float expandedHeight;
        bool windowExpanded;
        float drawSettingsHeight;
        bool drawSettingsExpanded;

        public MapModeUI(MapModeComponent parent)
        {
            this.parent = parent;
            doCloseX = false;
            doCloseButton = false;
            closeOnAccept = false;
            closeOnCancel = false;
            closeOnClickedOutside = false;
            preventCameraMotion = false;
            doWindowBackground = false;
            drawShadow = false;
        }

        protected override void SetInitialSizeAndPosition()
        {
            curBaseX = barWidth;
            curBaseY = Text.LineHeight + expandedHeight + drawSettingsHeight;
            Vector2 initialSize = InitialSize;
            windowRect = new Rect(0f, 0f, initialSize.x, initialSize.y);
            windowRect = windowRect.Rounded();
        }

        private void UpdateWindowSize()
        {
            curBaseX = barWidth;
            curBaseY = Text.LineHeight + expandedHeight + drawSettingsHeight;
            windowRect.width = curBaseX + Margin * 2f;
            windowRect.height = curBaseY + Margin * 2f;
            windowRect = windowRect.Rounded();
        }

        public override void WindowUpdate()
        {
            if (!WorldRendererUtility.WorldRenderedNow)
            {
                Close();
                return;
            }
            base.WindowUpdate();
        }

        public override void DoWindowContents(Rect inRect)
        {
            expandedHeight = 0f;
            Text.Font = GameFont.Small;
            string mapModeText = string.Format("{0}: {1}", "MMF.CurrentMapMode".Translate(), CurrentMapMode?.Name);
            Rect rectCurrentMapMode = new Rect(0f, 0f, Text.CalcSize(mapModeText).x + Widgets.CheckboxSize, Text.LineHeight);
            DoExpandButton(ref rectCurrentMapMode, mapModeText, ref windowExpanded, false);
            if (windowExpanded)
            {
                Rect rectMapModeBar = new Rect(0f, rectCurrentMapMode.yMax, barWidth, barHeight);
                Widgets.DrawShadowAround(new Rect(rectMapModeBar) { width = rectMapModeBar.width, height = rectMapModeBar.height + drawSettingsHeight });
                Widgets.DrawWindowBackground(rectMapModeBar);

                float buttonSize = 32f;
                float buttonMargin = 8f;
                int maxExclusive = (int)(barWidth / (buttonSize + buttonMargin)); //Fill the bar (can accommodate 10 buttons)
                for (int i = 0; i < maxExclusive; i++)
                {
                    Rect rectMapModeButton = new Rect(buttonMargin + (buttonSize + buttonMargin) * i, buttonMargin, buttonSize, buttonSize).CenteredOnYIn(rectMapModeBar);
                    CreateMapModeButton(rectMapModeButton, i, maxExclusive);
                }

                drawSettingsHeight = 0f;
                Rect rectDrawSettings = new Rect(rectMapModeBar) { x = Margin, y = rectMapModeBar.yMax, width = (rectMapModeBar.width / 2f) - Margin, height = Text.LineHeight };
                DoExpandButton(ref rectDrawSettings, "MMF.DrawSettings".Translate(), ref windowExpanded, true);
                if (drawSettingsExpanded)
                {
                    DoDrawSettingsExpanded(ref rectDrawSettings);
                }
            }
        }

        private void CreateMapModeButton(Rect inRect, int index, int maxOnBar)
        {
            if (index == 0 && currentMapModeIndex > 0)
            {
                DoSwitchButton(inRect, true);
            }
            else if (index == maxOnBar - 1 && currentMapModeIndex + (maxOnBar - 1) < MapModes.Count - 1)
            {
                DoSwitchButton(inRect, false);
            }
            else
            {
                int mapModeIndex = currentMapModeIndex + index;
                if (mapModeIndex < MapModes.Count)
                {
                    DoMapModeButton(inRect, MapModes[mapModeIndex]);
                }
            }
        }

        private void DoExpandButton(ref Rect inRect, string label, ref bool expanded, bool drawSettings)
        {
            if (expanded)
            {
                float height = drawSettings ? Text.LineHeight : barHeight;
                if (drawSettings)
                {
                    drawSettingsHeight += height;
                }
                else
                {
                    expandedHeight += height;
                }
            }
            else
            {
                drawSettingsHeight = 0f;
            }
            UpdateWindowSize();
            ref bool toExpand = ref drawSettings ? ref drawSettingsExpanded : ref windowExpanded;
            Widgets.CheckboxLabeled(inRect, label, ref toExpand, placeCheckboxNearText: !drawSettings, texChecked: Resources.dropdownExpanded, texUnchecked: Resources.dropdown);
            if (drawSettings)
            {
                inRect.y += Text.LineHeight;
            }
        }

        private void DoSwitchButton(Rect inRect, bool left)
        {
            Texture2D icon = left ? Resources.switchLeft : Resources.switchRight;
            if (Widgets.ButtonImage(inRect, icon))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                currentMapModeIndex = left ? currentMapModeIndex - 1 : currentMapModeIndex + 1;
            }
        }

        private void DoMapModeButton(Rect inRect, MapMode mapMode)
        {
            bool currentMapMode = CurrentMapMode == mapMode;
            Color baseColor = currentMapMode ? Color.yellow : Color.white;
            GUI.color = Mouse.IsOver(inRect) && !currentMapMode ? GenUI.MouseoverColor : baseColor;
            GUI.DrawTexture(inRect, mapMode.def.Icon);
            GUI.color = baseColor;
            TooltipHandler.TipRegion(inRect, mapMode.def.LabelCap);
            if (Widgets.ButtonInvisible(inRect, true))
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
                mapMode.OnButtonClick();
            }
            GUI.color = Color.white;
        }

        public void DoDrawSettingsExpanded(ref Rect inRect)
        {
            DrawSettings drawSettings = parent.drawSettings;
            if (CurrentMapMode.CanToggleWater)
            {
                DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.IncludeWater".Translate(), ref drawSettings.includeWater, true);
            }
            DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DrawWorldObjects".Translate(), ref drawSettings.drawWorldObjects, false);
            DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DrawHills".Translate(), ref drawSettings.drawHills, false);
            DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DrawRivers".Translate(), ref drawSettings.drawRivers, false);
            DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DrawRoads".Translate(), ref drawSettings.drawRoads, false);
            DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DrawPollution".Translate(), ref drawSettings.drawPollution, false);
            DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DisableFeaturesText".Translate(), ref drawSettings.disableFeaturesText, false);
            if (CurrentMapMode.HasLabels)
            {
                DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DisplayLabels".Translate(), ref drawSettings.displayLabels, false);
            }
            if (CurrentMapMode.HasTooltip)
            {
                DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DoTooltip".Translate(), ref drawSettings.doTooltip, false);
            }
        }

        public void DoDrawSettingsCheckbox(ref Rect inRect, string label, ref bool setting, bool regenerateOnChange)
        {
            bool storedSetting = setting;
            drawSettingsHeight += Text.LineHeight;
            UpdateWindowSize();
            Widgets.CheckboxLabeled(inRect, label, ref setting);
            inRect.y += Text.LineHeight;
            if (regenerateOnChange && storedSetting != setting)
            {
                parent.regenerateNow = true;
            }
        }
    }
}
