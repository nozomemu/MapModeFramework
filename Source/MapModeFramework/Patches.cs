using HarmonyLib;
using RimWorld.Planet;
using RimWorld;
using System;
using System.Collections.Generic;
using Verse.Sound;

namespace MapModeFramework
{
    [HarmonyPatch(typeof(WorldSelector), "SelectUnderMouse")]
    public static class WorldSelector_SelectUnderMouse_Patch
    {
        public static bool Prefix(WorldSelector __instance, bool canSelectTile = true)
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (mapModeComponent == null)
            {
                return true;
            }
            if (!(mapModeComponent.currentMapMode is MapMode_Region mapModeRegion))
            {
                return true;
            }
            if (!mapModeRegion.def.RegionProperties.overrideSelector)
            {
                return true;
            }
            int tile = canSelectTile ? GenWorld.MouseTile() : -1;
            Region region = ModCompatibility.DrawTile(tile) ? mapModeRegion.GetRegion(tile) : null;
            if (region != null)
            {
                SoundDefOf.Click.PlayOneShotOnCamera();
            }
            WorldLayer_SelectedRegion.Instance.selectedRegion = region;
            __instance.selectedTile = -1;
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldFeatures), "UpdateFeatures")]
    public static class WorldFeatures_UpdateFeatures_Patch
    {
        public static bool Prefix(List<WorldFeatureTextMesh> ___texts)
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (mapModeComponent == null)
            {
                return true;
            }
            if (!mapModeComponent.drawSettings.disableFeaturesText)
            {
                SetTextsActive(___texts, true);
                return true;
            }
            SetTextsActive(___texts, false);
            return false;
        }

        public static void SetTextsActive(List<WorldFeatureTextMesh> ___texts, bool active)
        {
            for (int i = 0; i < ___texts.Count; i++)
            {
                ___texts[i].SetActive(active);
            }
        }
    }

    [HarmonyPatch(typeof(ExpandableWorldObjectsUtility), nameof(ExpandableWorldObjectsUtility.ExpandableWorldObjectsOnGUI))]
    public static class ExpandableWorldObjectsUtility_ExpandableWorldObjectsOnGUI_Patch
    {
        public static bool Prefix()
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (mapModeComponent == null)
            {
                return true;
            }
            if (mapModeComponent.drawSettings.drawWorldObjects)
            {
                return true;
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(WorldLayer), nameof(WorldLayer.Render))]
    public static class WorldLayer_Render_Patch
    {
        public static Dictionary<Type, Func<DrawSettings, bool>> disableRendering = new Dictionary<Type, Func<DrawSettings, bool>>()
        {
            { typeof(WorldLayer_Hills), settings => settings.drawHills },
            { typeof(WorldLayer_Rivers), settings => settings.drawRivers },
            { typeof(WorldLayer_Roads), settings => settings.drawRoads },
            { typeof(WorldLayer_Pollution), settings => settings.drawPollution }
        };

        public static bool Prefix(WorldLayer __instance)
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (mapModeComponent == null)
            {
                return true;
            }
            bool overrideSelector = mapModeComponent.currentMapMode is MapMode_Region mapModeRegion && mapModeRegion.def.RegionProperties.overrideSelector;
            bool isSelectorLayer = __instance is WorldLayer_MouseTile || __instance is WorldLayer_SelectedTile;
            if (overrideSelector && isSelectorLayer)
            {
                return false;
            }
            Type worldLayerType = __instance.GetType();
            if (disableRendering.TryGetValue(__instance.GetType(), out Func<DrawSettings, bool> drawSetting))
            {
                DrawSettings drawSettings = mapModeComponent.drawSettings;
                if (!drawSetting(drawSettings))
                {
                    return false;
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Page_SelectStartingSite), nameof(Page_SelectStartingSite.PostOpen))]
    public static class Page_SelectStartingSite_PostOpen_Patch
    {
        public static void Postfix()
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (!mapModeComponent.mapModesInitialized)
            {
                mapModeComponent.InitializeMapModes();
            }
        }
    }
}
