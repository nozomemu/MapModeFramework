using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace MapModeFramework
{
    [StaticConstructorOnStartup]
    public static class ModCompatibility
    {
        public static Dictionary<string, string> patchedMods;
        public static List<Assembly> assemblies;
        private static bool foundRimworldExplorationMode;

        static ModCompatibility()
        {
            patchedMods = new Dictionary<string, string>
            {
                { "regrowth.botr.core", "ReGrowthCore" },
                { "thelastbulletbender.rwexploration", "RimworldExplorationMode" },
                { "m00nl1ght.geologicallandforms", "GeologicalLandforms" }
            };
            assemblies = new List<Assembly>();
        }

        public static void GetAssemblies()
        {
            List<ModContentPack> mods = LoadedModManager.RunningModsListForReading.Where(x => patchedMods.Keys.Contains(x.PackageId.ToLower())).ToList();
            foreach (ModContentPack mod in mods)
            {
                bool found = false;
                string modPackageId = mod.PackageId.ToLower();
                string assemblyName = patchedMods[modPackageId];
                foreach (Assembly loadedAssembly in mod.assemblies.loadedAssemblies)
                {
                    string name = loadedAssembly.GetName().Name;
                    if (name.Contains(assemblyName))
                    {
                        found = true;
                        assemblies.Add(loadedAssembly);
                        Core.Message("Assembly for " + mod.Name + " loaded. Will load compatibility patches.");
                        break;
                    }
                }
                if (!found)
                {
                    Core.Error($"Assembly ({assemblyName}) not found for running mod ({mod.Name}). Compatibility patches will not apply.");
                }
            }
            Core.Message($"Loading assemblies of patched mods done. Total assemblies loaded: {assemblies.Count}");
            ApplyPatches();
        }

        public static void ApplyPatches()
        {
            PatchWorldLayer("GeologicalLandforms", "WorldLayer_Landforms", settings => settings.drawHills);
            PatchWorldLayer("ReGrowthCore", "WorldDrawLayer_Beautification", settings => settings.drawHills);
            Assembly explorationMode = assemblies.FirstOrDefault(x => x.GetName().Name.Contains("RimworldExplorationMode"));
            foundRimworldExplorationMode = explorationMode != null;
            if (foundRimworldExplorationMode)
            {
                RimWorldExplorationMode.visibilityManagerType = explorationMode.GetTypes().FirstOrDefault(x => x.FullName.Contains("VisibilityManager"));
                RimWorldExplorationMode.worldFeatureManagerType = explorationMode.GetTypes().FirstOrDefault(x => x.FullName.Contains("WorldFeatureManager"));
            }
        }

        public static void PatchWorldLayer(string assemblyName, string worldLayerName, Func<DrawSettings, bool> setting)
        {
            Assembly assembly = assemblies.FirstOrDefault(x => x.GetName().Name.Contains(assemblyName));
            if (assembly == null)
            {
                Core.Warning($"Assembly named {assemblyName} not found. Render patch not applied.");
                return;
            }
            Type worldLayer = assembly.GetTypes().FirstOrDefault(x => x.FullName.Contains(worldLayerName));
            if (worldLayer != null)
            {
                WorldDrawLayerBase_Render_Patch.disableRendering.Add(worldLayer, setting);
            }
            else
            {
                Core.Error($"{worldLayerName} [{assemblyName}] not found. Render patch not applied.");
            }
        }

        public static bool DrawTile(int tile)
        {
            if (foundRimworldExplorationMode)
            {
                return RimWorldExplorationMode.TileVisible(tile);
            }
            return true;
        }

        public static bool OverrideLabel(int tile, out string label)
        {
            label = string.Empty;
            if (!foundRimworldExplorationMode)
            {
                return false;
            }
            return !RimWorldExplorationMode.TileVisible(tile);
        }

        public static bool OverrideTooltip(int tile, out string tooltip)
        {
            tooltip = string.Empty;
            if (!foundRimworldExplorationMode)
            {
                return false;
            }
            if (!(MapModeComponent.Instance.currentMapMode is MapMode_Features mapModeFeatures))
            {
                return !RimWorldExplorationMode.TileVisible(tile);
            }
            Region region = mapModeFeatures.GetRegion(tile);
            int featureIndex = mapModeFeatures.RegionList.FindIndex(x => x.name == region?.name);
            List<bool> learnedFeatures = RimWorldExplorationMode.GetLearnedFeatures();
            if (learnedFeatures.Count <= featureIndex && learnedFeatures[featureIndex])
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch]
    public static class VisibilityManager_UpdateGraphics_Patch
    {
        private static readonly Assembly assembly = ModCompatibility.assemblies.FirstOrDefault((Assembly x) => x.GetName().Name.Contains("RimworldExplorationMode"));
        private static readonly string typeName = "VisibilityManager";
        private static readonly string methodName = "UpdateGraphics";

        public static bool Prepare(MethodBase original)
        {
            return assembly != null;
        }

        public static MethodBase TargetMethod()
        {
            Type type = assembly.GetTypes().FirstOrDefault((Type x) => x.FullName.Contains(typeName));
            if (type == null)
            {
                Core.Error("Type " + typeName + " not found for assembly " + assembly.GetName().Name + ". UpdateGraphics patch not loaded.");
                return null;
            }
            MethodInfo methodInfo = type.GetMethods().FirstOrDefault((MethodInfo x) => x.Name.Contains(methodName));
            if (methodInfo == null)
            {
                Core.Error("Method " + methodName + " not found for type " + typeName + " of " + assembly.GetName().Name + ". UpdateGraphics patch not loaded.");
                return null;
            }
            return methodInfo;
        }

        public static void Postfix()
        {
            if (Find.World == null)
            {
                return;
            }
            MapModeComponent mapModeComponent = MapModeComponent.Instance;
            if (mapModeComponent?.currentMapMode?.WorldLayer == null)
            {
                return;
            }
            mapModeComponent.RegenerateNow();
        }
    }

    public static class RimWorldExplorationMode
    {
        public static Type visibilityManagerType;
        public static Type worldFeatureManagerType;
        public static MethodInfo tileVisible;
        public static FieldInfo learnedFeatures;

        public static GameComponent visibilityManager;
        public static WorldComponent worldFeatureManager;

        public static bool TileVisible(int tile)
        {
            if (visibilityManagerType == null)
            {
                return true;
            }
            GameComponent gameComponent = Current.Game.GetComponent(visibilityManagerType);
            if (visibilityManager == null || (visibilityManager != null && visibilityManager != gameComponent))
            {
                visibilityManager = gameComponent;
                tileVisible = visibilityManagerType.GetMethods().FirstOrDefault(x => x.Name.Contains("TileVisible"));
            }
            if (tileVisible != null)
            {
                bool isVisible = (bool)tileVisible.Invoke(visibilityManager, new object[] { tile });
                return isVisible;
            }
            return true;
        }

        public static List<bool> GetLearnedFeatures()
        {
            if (worldFeatureManagerType == null)
            {
                return null;
            }
            WorldComponent worldComponent = Find.World.GetComponent(worldFeatureManagerType);
            if (worldFeatureManager == null || worldFeatureManager != null && worldFeatureManager != worldComponent)
            {
                worldFeatureManager = worldComponent;
                learnedFeatures = worldFeatureManagerType.GetFields().FirstOrDefault(x => x.Name.Contains("learnedFeatures"));
            }
            if (learnedFeatures != null)
            {
                List<bool> learnedFeaturesList = (List<bool>)learnedFeatures.GetValue(worldFeatureManager);
                return learnedFeaturesList;
            }
            return null;
        }
    }
}
