using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace MapModeFramework
{
    public class DrawSettings
    {
        public bool includeWater;
        public bool drawWorldObjects;
        public bool drawHills;
        public bool drawRivers;
        public bool drawRoads;
        public bool drawPollution;
        public bool disableFeaturesText;
        public bool displayLabels;
        public bool doTooltip;

        public DrawSettings()
        {
            Reset();
        }

        public void Reset()
        {
            includeWater = true;
            drawWorldObjects = true;
            drawHills = true;
            drawRivers = true;
            drawRoads = true;
            drawPollution = true;
            disableFeaturesText = false;
            displayLabels = true;
            doTooltip = true;
        }

        public void Update(MapModeDef def)
        {
            includeWater = def.includeWater;
            drawWorldObjects = def.drawWorldObjects ?? drawWorldObjects;
            drawHills = def.drawHills ?? drawHills;
            drawRivers = def.drawRivers ?? drawRivers;
            drawRoads = def.drawRoads ?? drawRoads;
            drawPollution = def.drawPollution ?? drawPollution;
            disableFeaturesText = def.disableFeaturesText ?? disableFeaturesText;
            displayLabels = def.displayLabels ?? displayLabels;
            doTooltip = def.doTooltip ?? doTooltip;
        }
    }

    public class MapModeComponent : GameComponent
    {
        public static MapModeComponent Instance;

        public List<MapMode> mapModes = new List<MapMode>();
        public bool mapModesInitialized;

        public MapMode currentMapMode;
        public DrawSettings drawSettings;

        public bool regenerateNow;

        public MapModeComponent(Game game)
        {
            Instance = this;
            drawSettings = new DrawSettings();
        }

        public override void FinalizeInit()
        {
            if (!mapModesInitialized)
            {
                InitializeMapModes();
            }
        }

        public void InitializeMapModes()
        {
            List<MapModeDef> defs = DefDatabase<MapModeDef>.AllDefsListForReading;
            foreach (MapModeDef def in defs.Where(x => !mapModes.Any(y => y.def == x)))
            {
                Type mapModeType = def.mapModeClass ?? typeof(MapMode);
                MapMode mapMode = Activator.CreateInstance(mapModeType, def) as MapMode;
                mapModes.Add(mapMode);
            }
            CacheMapModes();
            Reset();
            mapModesInitialized = true;
        }

        private void CacheMapModes()
        {
            MapMode storedMapMode = currentMapMode;
            int mapModeCount = mapModes.Count;
            for (int i = 0; i < mapModeCount; i++)
            {
                if (!(mapModes[i] is MapMode_Region mapModeRegion))
                {
                    continue;
                }
                if (mapModeRegion.def.RegionProperties.cacheOnLoad && !mapModeRegion.cached)
                {
                    currentMapMode = mapModeRegion;
                    int regionCount = mapModeRegion.regions.Count;
                    for (int j = 0; j < regionCount; j++)
                    {
                        mapModeRegion.WorldLayer.DoRegionBorders(mapModeRegion.regions[j], true);
                    }
                    mapModeRegion.cached = true;
                }
            }
            currentMapMode = storedMapMode;
        }

        public override void GameComponentUpdate()
        {
            MapModeUI mapModeUI = Find.WindowStack.WindowOfType<MapModeUI>();
            if (WorldRendererUtility.WorldRenderedNow && mapModeUI == null)
            {
                Find.WindowStack.Add(new MapModeUI(this));
            }
            currentMapMode?.MapModeUpdate();
        }

        public override void GameComponentOnGUI()
        {
            if (!WorldRendererUtility.WorldRenderedNow)
            {
                return;
            }
            currentMapMode?.MapModeOnGUI();
        }

        public void UpdateMapMode(MapModeDef def)
        {
            drawSettings.Update(def);
            Find.WorldFeatures.UpdateFeatures();
        }

        public void Reset()
        {
            currentMapMode = mapModes.First(x => x.def == MapModeFrameworkDefOf.Default);
            drawSettings.Reset();
            regenerateNow = false;
        }

        public void Notify_RegionChanged()
        {
            if (currentMapMode is MapMode_Region mapModeRegion)
            {
                mapModeRegion.DoPreRegenerate();
                regenerateNow = true;
            }
        }
    }
}
