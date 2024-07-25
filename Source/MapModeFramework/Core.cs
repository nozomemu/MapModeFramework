using HarmonyLib;
using RimWorld;
using System.Reflection;
using UnityEngine;
using Verse;

namespace MapModeFramework
{
    [StaticConstructorOnStartup]
    public class Core : Mod
    {
        public static readonly Color MMF_Color = new Color(1f, 0.647f, 0f);
        public static readonly string prefix = "[Map Mode Framework]".Colorize(MMF_Color);

        public Core(ModContentPack content) : base(content)
        {
            Harmony harmony = new Harmony("NozoMe.MapModeFramework");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
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
    }

    [DefOf]
    public static class MapModeFrameworkDefOf
    {
        public static MapModeDef Default;
        public static MapModeDef Biome;
        public static MapModeDef Temperature;
        public static MapModeDef Elevation;
        public static MapModeDef Rainfall;
        public static MapModeDef Features;
    }
}
