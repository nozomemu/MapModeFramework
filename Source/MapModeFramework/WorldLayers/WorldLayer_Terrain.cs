using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class WorldLayer_MapMode_Terrain : WorldLayer_MapMode
    {
        public static WorldLayer_MapMode_Terrain Instance;

        public WorldLayer_MapMode_Terrain() : base()
        {
            Instance = this;
        }

        public override Material GetMaterial(int tile)
        {
            WorldGrid grid = Find.WorldGrid;
            MapModeDef def = CurrentMapMode.def;
            if (def == MapModeFrameworkDefOf.Biome)
            {
                MapMode_Biome mapModeBiome = (MapMode_Biome)CurrentMapMode;
                BiomeDef biome = mapModeBiome.selectedBiome;
                return grid[tile].biome == biome ? Materials.matGreenOverlay : Materials.matWhiteOverlay;
            }
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
            if (ModCompatibility.OverrideLabel(tile, out string label))
            {
                return label;
            }
            WorldGrid grid = Find.WorldGrid;
            MapModeDef def = CurrentMapMode.def;
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
            if (ModCompatibility.OverrideTooltip(tile, out string tooltip))
            {
                return tooltip;
            }
            WorldGrid grid = Find.WorldGrid;
            MapModeDef def = CurrentMapMode.def;
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
}
