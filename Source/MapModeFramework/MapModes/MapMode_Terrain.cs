using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class MapMode_Terrain : MapMode
    {
        public override WorldLayer_MapMode WorldLayer => WorldLayer_MapMode_Terrain.Instance;
        public override bool CanToggleWater => true;

        public MapMode_Terrain() { }
        public MapMode_Terrain(MapModeDef def) : base(def) { }

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

    public abstract class MapMode_Dropdown<T> : MapMode_Terrain where T: Def
    {
        public override string Name => string.Format("{0} ({1})", base.Name, selected.LabelCap);
        public T selected;

        public MapMode_Dropdown() { }
        public MapMode_Dropdown(MapModeDef def) : base(def) { }

        public override void OnButtonClick()
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            foreach (T def in DefDatabase<T>.AllDefsListForReading.Where(x => ValidDef(x)))
            {
                options.Add(new FloatMenuOption(def.LabelCap, delegate
                {
                    selected = def;
                    mapModeComponent.RequestMapModeSwitch(this);
                }));
            }
            Find.WindowStack.Add(new FloatMenu(options));
        }

        public virtual bool ValidDef(T def)
        {
            return true;
        }

        public virtual bool IsSelectedTile(int tile)
        {
            return false;
        }

        public override Material GetMaterial(int tile)
        {
            if (IsSelectedTile(tile))
            {
                return Materials.matGreenOverlay;
            }
            return Materials.matWhiteOverlay;
        }
    }

    public class MapMode_Biome : MapMode_Dropdown<BiomeDef>
    {
        public MapMode_Biome() { }
        public MapMode_Biome(MapModeDef def) : base(def) { }

        public override bool ValidDef(BiomeDef def)
        {
            return def.generatesNaturally;
        }

        public override bool IsSelectedTile(int tile)
        {
            return Find.WorldGrid[tile].biome == selected;
        }
    }

    public class MapMode_AnimalCommonality : MapMode_Dropdown<PawnKindDef>
    {
        public MapMode_AnimalCommonality() { }
        public MapMode_AnimalCommonality(MapModeDef def) : base(def) { }

        public override bool ValidDef(PawnKindDef def)
        {
            int naturalBiomesCount = Core.allNaturalBiomes.Count;
            for (int i = 0; i < naturalBiomesCount; i++)
            {
                BiomeDef biome = Core.allNaturalBiomes[i];
                if (biome.AllWildAnimals.Contains(def))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsSelectedTile(int tile)
        {
            BiomeDef biome = Find.WorldGrid[tile].biome;
            float commonality = biome.CommonalityOfAnimal(selected);
            if (commonality > 0f)
            {
                return true;
            }
            return false;
        }

        public override Material GetMaterial(int tile)
        {
            if (IsSelectedTile(tile))
            {
                BiomeDef biome = Find.WorldGrid[tile].biome;
                float commonality = biome.CommonalityOfAnimal(selected);
                return Materials.MatForCommonalityOverlay(commonality);
            }
            return base.GetMaterial(tile);
        }

        public override string GetTileLabel(int tile)
        {
            if (IsSelectedTile(tile))
            {
                BiomeDef biome = Find.WorldGrid[tile].biome;
                float commonality = biome.CommonalityOfAnimal(selected);
                return commonality.ToStringPercent();
            }
            return base.GetTileLabel(tile);
        }

        public override string GetTooltip(int tile)
        {
            if (IsSelectedTile(tile))
            {
                BiomeDef biome = Find.WorldGrid[tile].biome;
                float commonality = biome.CommonalityOfAnimal(selected);
                return $"{selected.LabelCap}\n- {"MMF.Commonality".Translate()}: {commonality.ToStringPercent()}";
            }
            return base.GetTooltip(tile);
        }
    }

    public class MapMode_PlantCommonality : MapMode_Dropdown<ThingDef>
    {
        public MapMode_PlantCommonality() { }
        public MapMode_PlantCommonality(MapModeDef def) : base(def) { }

        public override bool ValidDef(ThingDef def)
        {
            int naturalBiomesCount = Core.allNaturalBiomes.Count;
            for (int i = 0; i < naturalBiomesCount; i++)
            {
                BiomeDef biome = Core.allNaturalBiomes[i];
                if (biome.AllWildPlants.Contains(def))
                {
                    return true;
                }
            }
            return false;
        }

        public override bool IsSelectedTile(int tile)
        {
            BiomeDef biome = Find.WorldGrid[tile].biome;
            float commonality = biome.CommonalityOfPlant(selected);
            if (commonality > 0f)
            {
                return true;
            }
            return false;
        }

        public override Material GetMaterial(int tile)
        {
            if (IsSelectedTile(tile))
            {
                BiomeDef biome = Find.WorldGrid[tile].biome;
                float commonality = biome.CommonalityOfPlant(selected);
                return Materials.MatForCommonalityOverlay(commonality);
            }
            return base.GetMaterial(tile);
        }

        public override string GetTileLabel(int tile)
        {
            if (IsSelectedTile(tile))
            {
                BiomeDef biome = Find.WorldGrid[tile].biome;
                float commonality = biome.CommonalityOfPlant(selected);
                return commonality.ToStringPercent();
            }
            return base.GetTileLabel(tile);
        }

        public override string GetTooltip(int tile)
        {
            if (IsSelectedTile(tile))
            {
                BiomeDef biome = Find.WorldGrid[tile].biome;
                float commonality = biome.CommonalityOfPlant(selected);
                return $"{selected.LabelCap}\n- {"MMF.Commonality".Translate()}: {commonality.ToStringPercent()}";
            }
            return base.GetTooltip(tile);
        }
    }

    //Couldn't get to work right now since the stack always overflows. Revisit sometime.
    //public class MapMode_RockTypes : MapMode_Dropdown<ThingDef>
    //{
    //    private readonly List<ThingDef> allNaturalRockDefs = DefDatabase<ThingDef>.AllDefs.Where((ThingDef d) => d.IsNonResourceNaturalRock).ToList();

    //    public MapMode_RockTypes() { }
    //    public MapMode_RockTypes(MapModeDef def) : base(def) { }

    //    public override bool ValidDef(ThingDef def)
    //    {
    //        return def.IsNonResourceNaturalRock;
    //    }

    //    public override bool IsSelectedTile(int tile)
    //    {
    //        var rockTypes = NaturalRockTypesIn(tile);
    //        return rockTypes.Contains(selected);
    //    }

    //    //Refactor of original code
    //    public IEnumerable<ThingDef> NaturalRockTypesIn(int tile)
    //    {
    //        Rand.PushState();
    //        Rand.Seed = tile;
    //        var allRocks = allNaturalRockDefs;
    //        int num = Math.Min(Rand.RangeInclusive(2, 3), allRocks.Count);
    //        var rockTypes = allRocks.OrderBy(x => Rand.Value).Take(num).ToList();
    //        Rand.PopState();
    //        return rockTypes;
    //    }
    //}

    public class MapMode_GrowingPeriod : MapMode_Cached
    {
        public Dictionary<int, int> cache = new Dictionary<int, int>();
        public override WorldLayer_MapMode WorldLayer => WorldLayer_MapMode_Terrain.Instance;
        public override bool CanToggleWater => false;

        public MapMode_GrowingPeriod() { }
        public MapMode_GrowingPeriod(MapModeDef def) : base(def) { }

        protected override void StartCaching(CancellationToken token)
        {
            WorldGrid grid = Find.WorldGrid;
            int tilesCount = grid.TilesCount;

            var allTiles = Enumerable.Range(0, tilesCount - 1);
            tilesToCache = allTiles.Where(x => WorldLayer.ValidTile(x)).Count();

            for (int i = 0; i < tilesCount; i++)
            {
                token.ThrowIfCancellationRequested();
                if (grid[i].WaterCovered)
                {
                    continue;
                }
                cache.Add(i, GenTemperature.TwelfthsInAverageTemperatureRange(i, 6f, 42f).Count);
                tilesCached++;
            }
        }

        protected override void DoCacheClearing()
        {
            cache.Clear();
        }

        public override void Notify_TileChanged(int tile)
        {
            cache.Remove(tile);
        }

        public override Material GetMaterial(int tile)
        {
            if (!cache.TryGetValue(tile, out int growingPeriod))
            {
                growingPeriod = GenTemperature.TwelfthsInAverageTemperatureRange(tile, 6f, 42f).Count;
                if (EnabledCaching)
                {
                    cache[tile] = growingPeriod;
                    tilesCached++;
                }
            }
            return Materials.MatForGrowingPeriodOverlay(growingPeriod);
        }

        public override string GetTooltip(int tile)
        {
            return string.Format("{0}:\n{1}", "OutdoorGrowingPeriod".Translate(), Zone_Growing.GrowingQuadrumsDescription(tile));
        }
    }
}
