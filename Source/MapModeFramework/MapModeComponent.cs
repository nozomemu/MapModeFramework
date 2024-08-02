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

        public List<MapModeDef> mapModeDefs = new List<MapModeDef>();
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
            foreach (MapModeDef def in mapModeDefs.ToList())
            {
                Type mapModeType = def.mapModeClass ?? typeof(MapMode_Default);
                MapMode mapMode = Activator.CreateInstance(mapModeType, def) as MapMode;
                mapModes.Add(mapMode);
            }
            List<MapModeDef> defs = DefDatabase<MapModeDef>.AllDefsListForReading;
            foreach (MapModeDef def in defs.Where(x => !mapModes.Any(y => y.def == x)))
            {
                Type mapModeType = def.mapModeClass ?? typeof(MapMode_Default);
                MapMode mapMode = Activator.CreateInstance(mapModeType, def) as MapMode;
                mapModes.Add(mapMode);
                mapModeDefs.Add(def);
            }
            Reset();
            CacheMapModes();
            mapModesInitialized = true;
        }

        private void CacheMapModes()
        {
            int mapModeCount = mapModes.Count;
            for (int i = 0; i < mapModeCount; i++)
            {
                MapMode mapMode = mapModes[i];
                if (!(mapMode is MapMode_Cached mapModeCached))
                {
                    continue;
                }
                if (mapModeCached.CacheOnStart)
                {
                    TaskHandler.StartQueue(async (token) =>
                    {
                        await mapModeCached.PopulateCache(token);
                    });
                }
            }
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

        public void RequestMapModeSwitch(MapMode mapMode)
        {
            MapModeDef def = mapMode.def;
            if (mapMode.WorldLayerClass == null)
            {
                if (def != MapModeFrameworkDefOf.Default)
                {
                    Core.Warning($"Loaded MapModeDef {def.defName} that has no WorldLayerClass. This is likely unintended.");
                }
            }
            _ = WorldRegenHandler.RequestMapModeSwitch(mapMode);
        }

        public void SwitchMapMode(MapMode mapMode)
        {
            currentMapMode = mapMode;
            UpdateMapMode(mapMode.def);
            RegenerateNow();
        }

        public void UpdateMapMode(MapModeDef def)
        {
            drawSettings.Update(def);
            Find.WorldFeatures.UpdateFeatures();
        }

        public void Reset(bool fullReset = false)
        {
            currentMapMode = mapModes.First(x => x.def == MapModeFrameworkDefOf.Default);
            drawSettings.Reset();
            regenerateNow = false;
            if (fullReset)
            {
                mapModeDefs.Clear();
                mapModes.Clear();
                mapModesInitialized = false;
            }
        }

        public void RegenerateNow()
        {
            currentMapMode.DoPreRegenerate();
            regenerateNow = true;
        }

        public void Notify_RegenerationComplete(MapMode mapMode)
        {
            regenerateNow = false;
            if (mapMode is MapMode_Cached mapModeCached && mapModeCached.EnabledCaching)
            {
                mapModeCached.cached = true;
            }
        }

        public void Notify_CachingComplete(MapMode mapMode)
        {
            if (currentMapMode == mapMode)
            {
                RegenerateNow();
            }
        }

        public void Notify_RegionChanged()
        {
            RegenerateNow();
        }

        public void Notify_TileChanged(int tile)
        {
            int mapModesCount = mapModes.Count;
            for (int i = 0; i < mapModesCount; i++)
            {
                MapMode mapMode = mapModes[i];
                mapMode.Notify_TileChanged(tile);
            }
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                mapModeDefs.Clear();
                foreach (MapMode mapMode in mapModes)
                {
                    mapModeDefs.Add(mapMode.def);
                }
            }
            Scribe_Collections.Look(ref mapModeDefs, "MMF.MapModeDefs", LookMode.Def);
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                mapModeDefs ??= new List<MapModeDef>();
                mapModes ??= new List<MapMode>();
                foreach (MapModeDef mapModeDef in mapModeDefs)
                {
                    if (mapModeDef == null)
                    {
                        mapModeDefs.Remove(mapModeDef);
                    }
                }
            }
        }
    }
}
