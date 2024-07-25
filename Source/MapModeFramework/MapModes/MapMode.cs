using System;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class MapModeDef : Def
    {
        public Type mapModeClass;
        public Type worldLayerClass;
        public bool includeWater = false;

        public bool? drawWorldObjects;
        public bool? drawHills;
        public bool? drawRivers;
        public bool? drawRoads;
        public bool? drawPollution;
        public bool? disableFeaturesText;

        public bool? displayLabels;
        public bool? doTooltip;

        public string iconPath;

        [Unsaved(false)]
        private Texture2D icon;
        public Texture2D Icon
        {
            get
            {
                if (icon == null && iconPath != null)
                {
                    icon = ContentFinder<Texture2D>.Get(iconPath);
                }
                return icon ?? BaseContent.BadTex;
            }
        }

        public RegionProperties RegionProperties;
    }

    public class RegionProperties
    {
        public bool overrideSelector;
        public bool cacheOnLoad;
        public bool doBorders;
        public float borderWidth;
    }

    public class MapMode
    {
        public MapModeComponent Parent => MapModeComponent.Instance;
        public virtual string Name => def.LabelCap;
        public MapModeDef def;

        public Type WorldLayerClass => def.worldLayerClass;
        public virtual WorldLayer_MapMode WorldLayer { get; }
        public virtual bool CanToggleWater => false;
        public virtual bool HasLabels => def.displayLabels.HasValue;
        public virtual bool HasTooltip => def.doTooltip.HasValue;

        public MapMode() { }
        public MapMode(MapModeDef def)
        {
            this.def = def;
            Initialize();
        }

        public virtual void Initialize()
        {
        }

        public virtual void OnButtonClick()
        {
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (mapModeComponent.currentMapMode == this)
            {
                return;
            }
            mapModeComponent.currentMapMode = this;
            if (WorldLayerClass == null)
            {
                if (def != MapModeFrameworkDefOf.Default)
                {
                    Core.Warning($"Loaded MapModeDef {def.defName} that has no WorldLayerClass. This is likely unintended.");
                }
                mapModeComponent.Reset();
                return;
            }
            mapModeComponent.UpdateMapMode(def);
            DoPreRegenerate();
            mapModeComponent.regenerateNow = true;
        }

        public virtual void DoPreRegenerate()
        {
        }

        public virtual void MapModeUpdate()
        {
        }

        public  virtual void MapModeOnGUI()
        {
        }
    }
}
