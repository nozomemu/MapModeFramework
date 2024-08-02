using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    public class CacheSetting
    {
        public bool EnableCache { get; set; }
        public bool CacheOnStart { get; set; }

        public CacheSetting(bool enableCache, bool cacheOnStart)
        {
            SetCacheSettings(enableCache, cacheOnStart);
        }

        public void SetCacheSettings(bool enableCache, bool cacheOnStart)
        {
            this.EnableCache = enableCache;
            this.CacheOnStart = cacheOnStart;
        }
    }

    public class Settings : ModSettings
    {
        public Dictionary<MapModeDef, CacheSetting> enabledCaching = new Dictionary<MapModeDef, CacheSetting>();

        public void InitializeSettings()
        {
            foreach (MapModeDef mapModeDef in DefDatabase<MapModeDef>.AllDefsListForReading.Where(def => def.canCache))
            {
                enabledCaching.TryAdd(mapModeDef, new CacheSetting(true, true));
            }
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listing_Standard = new Listing_Standard();
            listing_Standard.Begin(inRect);
            int enabledCachingCount = enabledCaching.Count;
            for (int i = 0; i < enabledCachingCount; i++)
            {
                var entry = enabledCaching.ElementAt(i);
                MapModeDef def = entry.Key;
                CacheSetting setting = entry.Value;

                bool enableCache = setting.EnableCache;
                listing_Standard.CheckboxLabeled(string.Format("{0}: {1}", "MMF.Settings.EnableCaching".Translate(), def.LabelCap), ref enableCache);

                bool cacheOnStart = setting.CacheOnStart;
                if (enableCache)
                {
                    listing_Standard.CheckboxLabeled("MMF.Settings.CacheOnStart".Translate(), ref cacheOnStart, 12f);
                }
                else
                {
                    cacheOnStart = false;
                }
                enabledCaching[def].SetCacheSettings(enableCache, cacheOnStart);
            }
            listing_Standard.End();
        }
    }
}
