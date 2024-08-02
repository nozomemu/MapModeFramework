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
        private const float barWidth = 408f;
        private const float barHeight = 48f;
        private float curBaseX;
        private float curBaseY;

        private const float buttonSize = 32f;
        private const float buttonMargin = 8f;
        private const int MaxButtonsOnBar = (int)(barWidth / (buttonSize + buttonMargin));
        private int currentMapModeIndex;

        private readonly int[] mapModeBar = new int[MaxButtonsOnBar];

        private float timeToSwitch;
        private int activeMapModeButtonControl;
        private Vector3 activeMapModeButtonControlMouseStart;
        private MapMode draggedMapMode;

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
            draggable = true;
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

        private void BindWindowPosition()
        {
            windowRect.x = Mathf.Clamp(windowRect.x, 0f, UI.screenWidth - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0f, UI.screenHeight - windowRect.height);
        }

        public override void WindowUpdate()
        {
            if (!WorldRendererUtility.WorldRenderedNow)
            {
                Close();
                return;
            }
            base.WindowUpdate();
            if (timeToSwitch > 0)
            {
                timeToSwitch -= Time.deltaTime;
                if (timeToSwitch < 0)
                {
                    timeToSwitch = 0;
                }
            }
        }

        public override void WindowOnGUI()
        {
            BindWindowPosition();
            base.WindowOnGUI();
        }

        protected override void LateWindowOnGUI(Rect inRect)
        {
            base.LateWindowOnGUI(inRect);
            DoMapModeDragger();
            DrawDraggedMapMode();
        }

        private bool InBounds()
        {
            bool windowInBoundsX = UI.MousePositionOnUI.x >= windowRect.x && UI.MousePositionOnUI.x < windowRect.xMax;
            bool windowInBoundsY = UI.MousePositionOnUIInverted.y >= windowRect.y && UI.MousePositionOnUIInverted.y < windowRect.yMax;
            return windowInBoundsX && windowInBoundsY;
        }

        private void DoMapModeDragger()
        {
            if (draggedMapMode == null)
            {
                return;
            }
            int currentIndex = MapModes.IndexOf(draggedMapMode);
            if (Input.GetMouseButtonUp(0) || !InBounds())
            {
                Rect rect = windowRect.AtZero().ContractedBy(Margin);
                Rect rectCurrentMapMode = new Rect(rect) { height = Text.LineHeight };
                Rect rectMapModeBar = new Rect(rectCurrentMapMode) { y = rectCurrentMapMode.yMax, width = barWidth, height = barHeight };
                for (int i = 0; i < mapModeBar.Length; i++)
                {
                    Rect retButtonArea = new Rect(rect.x + buttonMargin + (buttonSize + buttonMargin) * i, buttonMargin, buttonSize, buttonSize).CenteredOnYIn(rectMapModeBar);
                    int mapModeIndex = mapModeBar[i];
                    if (Mouse.IsOver(retButtonArea) && mapModeIndex != -1 && mapModeIndex != -2)
                    {
                        SwapButtons(currentIndex, mapModeIndex);
                    }
                }
                activeMapModeButtonControl = 0;
                draggedMapMode = null;
                draggable = true;
                return;
            }
            if (InBounds() && timeToSwitch <= 0)
            {
                bool atEdgeRight = Input.mousePosition.x >= windowRect.x + barWidth + Margin - (buttonSize + buttonMargin);
                if (atEdgeRight && mapModeBar[MaxButtonsOnBar - 1] == -1)
                {
                    currentMapModeIndex++;
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                }
                bool atEdgeLeft = Input.mousePosition.x <= windowRect.x + Margin + (buttonSize + buttonMargin);
                if (atEdgeLeft && mapModeBar[0] == -1)
                {
                    currentMapModeIndex--;
                    SoundDefOf.DragSlider.PlayOneShotOnCamera();
                }
                timeToSwitch = 0.5f;
            }

            void SwapButtons(int indexA, int indexB)
            {
                SoundDefOf.Mouseover_Standard.PlayOneShotOnCamera();
                (MapModes[indexB], MapModes[indexA]) = (MapModes[indexA], MapModes[indexB]);
            }
        }

        private void DrawDraggedMapMode()
        {
            if (draggedMapMode != null)
            {
                float draggedX = UI.MousePositionOnUI.x - buttonSize / 2f - windowRect.x;
                float draggedY = UI.MousePositionOnUIInverted.y - buttonSize / 2f - windowRect.y;
                Rect rectDragged = new Rect(draggedX, draggedY, buttonSize, buttonSize);
                GUI.DrawTexture(new Rect(rectDragged).ContractedBy(4f), draggedMapMode.def.Icon);
            }
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
                for (int i = 0; i < MaxButtonsOnBar; i++)
                {
                    Rect rectMapModeButton = new Rect(buttonMargin + (buttonSize + buttonMargin) * i, buttonMargin, buttonSize, buttonSize).CenteredOnYIn(rectMapModeBar);
                    CreateMapModeButton(rectMapModeButton, i, MaxButtonsOnBar);
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
                mapModeBar[index] = -1;
            }
            else if (index == maxOnBar - 1 && currentMapModeIndex + (maxOnBar - 1) < MapModes.Count - 1)
            {
                DoSwitchButton(inRect, false);
                mapModeBar[index] = -1;
            }
            else
            {
                int mapModeIndex = currentMapModeIndex + index;
                if (mapModeIndex >= MapModes.Count)
                {
                    mapModeBar[index] = -2;
                    return;
                }
                DoMapModeButton(inRect, MapModes[mapModeIndex]);
                mapModeBar[index] = mapModeIndex;
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
            Widgets.DrawBoxSolidWithOutline(inRect, Color.clear, Color.white);
            int controlID = GUIUtility.GetControlID(FocusType.Passive, inRect);
            if (Input.GetMouseButtonDown(0) && Mouse.IsOver(inRect))
            {
                activeMapModeButtonControl = controlID;
                activeMapModeButtonControlMouseStart = Input.mousePosition;
                draggable = false;
            }
            if (activeMapModeButtonControl == controlID)
            {
                if (Input.GetMouseButtonUp(0))
                {
                    activeMapModeButtonControl = 0;
                    draggable = true;
                }
                else if (!Input.GetMouseButton(0))
                {
                    activeMapModeButtonControl = 0;
                    draggable = true;
                }
                else if (draggedMapMode == null && (activeMapModeButtonControlMouseStart - Input.mousePosition).sqrMagnitude > 20f)
                {
                    draggedMapMode = mapMode;
                }
            }
            if (draggedMapMode == mapMode)
            {
                return;
            }
            bool isCurrentMapMode = CurrentMapMode == mapMode;
            Color baseColor = isCurrentMapMode ? Color.yellow : Color.white;
            Color mouseOverColor = Mouse.IsOver(inRect) && !isCurrentMapMode ? GenUI.MouseoverColor : baseColor;
            string tooltip = $"{mapMode.def.LabelCap}\n{"MMF.UI.Tooltip.DragToSort".Translate().Colorize(Color.gray)}";
            TooltipHandler.TipRegion(new Rect(inRect), tooltip);
            if (Widgets.ButtonImage(inRect.ContractedBy(4f), mapMode.def.Icon, baseColor, mouseOverColor))
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
            bool? displayLabels = CurrentMapMode.def.displayLabels;
            if (displayLabels.HasValue && displayLabels.Value)
            {
                DoDrawSettingsCheckbox(ref inRect, "MMF.DrawSettings.DisplayLabels".Translate(), ref drawSettings.displayLabels, false);
            }
            bool? doTooltip = CurrentMapMode.def.doTooltip;
            if (doTooltip.HasValue && doTooltip.Value)
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
                parent.RegenerateNow();
            }
        }
    }
}
