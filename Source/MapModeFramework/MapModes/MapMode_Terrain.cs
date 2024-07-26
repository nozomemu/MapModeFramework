using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class MapMode_Terrain : MapMode
    {
        public new WorldLayer_MapMode_Terrain WorldLayer => WorldLayer_MapMode_Terrain.Instance;
        public override bool CanToggleWater => true;

        public MapMode_Terrain() { }
        public MapMode_Terrain(MapModeDef def) : base(def) { }

        public override void MapModeOnGUI()
        {
            WorldLayer.OnGUI();
        }

        public override Material GetMaterial(int tile)
        {
            WorldGrid grid = Find.WorldGrid;
            if (def == MapModeFrameworkDefOf.Temperature)
            {
                return Materials.MatForTemperature(grid[tile].temperature);
            }
            if (def == MapModeFrameworkDefOf.Elevation)
            {
                return Materials.MatForElevation(grid[tile].elevation);
            }
            if (def == MapModeFrameworkDefOf.Rainfall)
            {
                return Materials.MatForRainfallOverlay(grid[tile].rainfall);
            }
            return base.GetMaterial(tile);
        }

        public override string GetTileLabel(int tile)
        {
            WorldGrid grid = Find.WorldGrid;
            if (def == MapModeFrameworkDefOf.Temperature)
            {
                return grid[tile].temperature.ToStringTemperature();
            }
            if (def == MapModeFrameworkDefOf.Elevation)
            {
                return grid[tile].elevation.ToString("F0") + "m";
            }
            if (def == MapModeFrameworkDefOf.Rainfall)
            {
                return grid[tile].rainfall.ToString("F0") + "mm";
            }
            return base.GetTileLabel(tile);
        }

        public override string GetTooltip(int tile)
        {
            WorldGrid grid = Find.WorldGrid;
            if (def == MapModeFrameworkDefOf.Temperature)
            {
                return string.Format("{0}:\n{1}", "AvgTemp".Translate(), GenTemperature.GetAverageTemperatureLabel(tile));
            }
            if (def == MapModeFrameworkDefOf.Elevation)
            {
                return string.Format("{0}: {1}", "Elevation".Translate(), grid[tile].elevation.ToString("F0") + "m");
            }
            if (def == MapModeFrameworkDefOf.Rainfall)
            {
                return string.Format("{0}: {1}", "Rainfall".Translate(), grid[tile].rainfall.ToString("F0") + "mm");
            }
            return base.GetTooltip(tile);
        }
    }

    public class MapMode_Biome : MapMode_Terrain
    {
        public override string Name => string.Format("{0} ({1})", base.Name, selectedBiome.LabelCap);
        public BiomeDef selectedBiome;

        public MapMode_Biome() { }
        public MapMode_Biome(MapModeDef def) : base(def) { }

        public override void OnButtonClick()
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (BiomeDef biome in DefDatabase<BiomeDef>.AllDefsListForReading.Where(x => x.generatesNaturally))
            {
                options.Add(new FloatMenuOption(biome.LabelCap, delegate
                {
                    selectedBiome = biome;
                    mapModeComponent.currentMapMode = this;
                    mapModeComponent.UpdateMapMode(def);
                    DoPreRegenerate();
                    mapModeComponent.regenerateNow = true;
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public override Material GetMaterial(int tile)
        {
            return Find.WorldGrid[tile].biome == selectedBiome ? Materials.matGreenOverlay : Materials.matWhiteOverlay;
        }
    }
}
