using HarmonyLib;
using RimWorld;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    [StaticConstructorOnStartup]
    public class Core : Mod
    {
        public static List<BiomeDef> allNaturalBiomes;

        public static Settings settings;
        public static readonly Color MMF_Color = new Color(1f, 0.647f, 0f);
        public static readonly string prefix = "[Map Mode Framework]".Colorize(MMF_Color);

        public Core(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("NozoMe.MapModeFramework");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            settings = GetSettings<Settings>();
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                settings.InitializeSettings();
                CacheRelevantLists();
            });
        }

        public void CacheRelevantLists()
        {
            allNaturalBiomes = DefDatabase<BiomeDef>.AllDefsListForReading.Where(biome => biome.generatesNaturally).ToList();
        }

        public static async Task KillAllAsyncProcesses()
        {
            WorldRegenHandler.Interrupt();
            TaskHandler.KillQueue();
            if (!WorldRegenHandler.IsBusy && !TaskHandler.IsBusy)
            {
                return;
            }
            TimeSpan timeout = TimeSpan.FromSeconds(10);
            using (CancellationTokenSource cancelTokenSource = new CancellationTokenSource(timeout))
            {
                CancellationToken token = cancelTokenSource.Token;
                while ((WorldRegenHandler.IsBusy || TaskHandler.IsBusy) && !token.IsCancellationRequested)
                {
                    if (WorldRegenHandler.IsBusy || TaskHandler.IsBusy)
                    {
                        await Task.Delay(1000, token);
                    }
                }
            }
            WorldRegenHandler.Reset(true);
            MapModeComponent.Instance?.Reset(true);
        }

        public static void Message(string text)
        {
            Log.Message(prefix + " " + text);
        }

        public static void Warning(string text)
        {
            Log.Warning(prefix + " " + text);
        }

        public static void WarningOnce(string text, int key)
        {
            Log.WarningOnce(prefix + " " + text, key);
        }

        public static void Error(string text)
        {
            Log.Error(prefix + " " + text);
        }

        public static void ErrorOnce(string text, int key)
        {
            Log.ErrorOnce(prefix + " " + text, key);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            List<MapMode> mapModes = MapModeComponent.Instance?.mapModes;
            if (mapModes == null)
            {
                return;
            }
            foreach (MapMode_Cached mapMode in mapModes.OfType<MapMode_Cached>())
            {
                mapMode.ClearCache();
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory()
        {
            return base.Content.Name;
        }
    }

    [DefOf]
    public static class MapModeFrameworkDefOf
    {
        public static MapModeDef Default;
        public static MapModeDef Biome;
        public static MapModeDef Temperature;
        public static MapModeDef Elevation;
        public static MapModeDef Rainfall;
        public static MapModeDef GrowingPeriod;
        public static MapModeDef Features;
    }
}
