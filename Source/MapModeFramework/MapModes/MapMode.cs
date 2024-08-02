using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class MapModeDef : Def
    {
        public Type mapModeClass;
        public Type worldLayerClass;
        public bool canCache;

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
        public bool doBorders;
        public float borderWidth;
    }

    public abstract class MapMode
    {
        public MapModeComponent Parent => MapModeComponent.Instance;
        public bool IsRegenerating => WorldRegenHandler.regeneratingMapMode == this;
        public virtual string Name => def.LabelCap;
        public MapModeDef def;

        public Type WorldLayerClass => def.worldLayerClass;
        public abstract WorldLayer_MapMode WorldLayer { get; }
        public virtual bool CanToggleWater => false;
        
        public virtual bool Active => true;

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
            mapModeComponent.RequestMapModeSwitch(this);
        }

        public virtual void DoPreRegenerate()
        {
        }

        public virtual void MapModeUpdate()
        {
        }

        public  virtual void MapModeOnGUI()
        {
            if (Active)
            {
                if (IsRegenerating)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    int tilesPrepared = WorldRegenHandler.tilesPrepared;
                    int tilesToPrepare = WorldRegenHandler.tilesToPrepare;
                    string progressText = "MMF.UI.LoadProgress".Translate(tilesPrepared, tilesToPrepare);
                    float statusWidth = UI.screenWidth;
                    float statusHeight = Text.LineHeight;
                    Rect rectStatus = new Rect(UI.screenWidth / 2f - statusWidth / 2f, UI.screenHeight * 3f / 4f, statusWidth, statusHeight);
                    Widgets.Label(rectStatus, progressText);
                    Text.Font = GameFont.Small;
                    Text.Anchor = TextAnchor.UpperLeft;
                }
                else
                {
                    WorldLayer?.OnGUI();
                }
            }
        }

        public virtual void Notify_TileChanged(int tile)
        {
        }

        public virtual Material GetMaterial(int tile) => BaseContent.ClearMat;
        public virtual string GetTileLabel(int tile) => string.Empty;
        public virtual string GetTooltip(int tile) => string.Empty;
    }

    public abstract class MapMode_Cached : MapMode
    {
        public bool EnabledCaching => def.canCache && Core.settings.enabledCaching[def].EnableCache;
        public bool CacheOnStart => EnabledCaching && Core.settings.enabledCaching[def].CacheOnStart;

        public override bool Active => !cachingNow;
        public bool cachingNow;
        public bool cached;

        public int tilesCached;
        public int tilesToCache;

        public MapMode_Cached() { }
        public MapMode_Cached(MapModeDef def) : base(def) { }

        public async Task PopulateCache(CancellationToken token)
        {
            if (IsRegenerating || cachingNow)
            {
                return;
            }
            cachingNow = true;
            try
            {
                await Task.Run(() => StartCaching(token), token);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Core.Error($"Error during asynchronous caching of {this}: {ex.Message}");
            }
            finally
            {
                cachingNow = false;
                cached = true;
                Parent.Notify_CachingComplete(this);
            }
        }

        protected abstract void StartCaching(CancellationToken token);

        public void ClearCache()
        {
            DoCacheClearing();
            tilesCached = 0;
            tilesToCache = 0;
            cached = false;
        }

        protected abstract void DoCacheClearing();

        public override void MapModeOnGUI()
        {
            base.MapModeOnGUI();
            bool regeneratingOrCaching = IsRegenerating || cachingNow;
            if (regeneratingOrCaching)
            {
                TimeSpan elapsed = DateTime.Now - WorldRegenHandler.startTime;
                int tilesPrepared = IsRegenerating ? WorldRegenHandler.tilesPrepared : tilesCached;
                int tilesToPrepare = IsRegenerating ? WorldRegenHandler.tilesToPrepare : tilesToCache;
                string progressText = "MMF.UI.LoadProgress".Translate(tilesPrepared, tilesToPrepare);
                float statusWidth = UI.screenWidth;
                float statusHeight = Text.LineHeight;
                Rect rectStatus = new Rect(UI.screenWidth / 2f - statusWidth / 2f, UI.screenHeight * 3f / 4f, statusWidth, statusHeight);
                if (!Active)
                {
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Text.Font = GameFont.Medium;
                    Widgets.Label(rectStatus, progressText);
                    Text.Font = GameFont.Small;
                }
                if (elapsed.TotalSeconds >= 10 && !EnabledCaching)
                {
                    string tryCacheText = "MMF.UI.TryCache".Translate();
                    Vector2 tryCacheTextSize = Text.CalcSize(tryCacheText);
                    Widgets.Label(new Rect(UI.screenWidth / 2f - tryCacheTextSize.x / 2f, rectStatus.yMax, tryCacheTextSize.x, tryCacheTextSize.y), tryCacheText);
                }
                else if (EnabledCaching && !cached)
                {
                    string cachingText = "MMF.UI.Caching".Translate() + GenText.MarchingEllipsis();
                    Vector2 cachingTextSize = Text.CalcSize(cachingText);
                    Widgets.Label(new Rect(UI.screenWidth / 2f - cachingTextSize.x / 2f, rectStatus.yMax, cachingTextSize.x, cachingTextSize.y), cachingText);
                }
                Text.Anchor = TextAnchor.UpperLeft;
            }
        }
    }

    public class MapMode_Default : MapMode
    {
        public override WorldLayer_MapMode WorldLayer => null;

        public MapMode_Default() { }
        public MapMode_Default(MapModeDef def) : base(def) { }
    }
}
