using RimWorld;
using System.Collections.Generic;
using System.Linq;
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
    }
}
