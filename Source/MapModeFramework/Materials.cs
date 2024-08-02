using UnityEngine;
using Verse;

namespace MapModeFramework
{
    [StaticConstructorOnStartup]
    public static class Materials
    {
        private const int selectorRenderQueue = 3560;
        private const int terrainOverlayRenderQueue = 3510;

        private const int numMatsGrowingPeriod = 7;

        public static readonly Material matWhiteOverlay;
        public static readonly Material matGreenOverlay;
        public static readonly Material matMouseRegion;
        public static readonly Material matSelectedRegion;
        public static readonly Material matFeaturesBorder;

        private static Material[] matsFertility;
        private static readonly Color[] FertilitySpectrum;
        private static Material[] matsTemperature;
        private static readonly Color[] TemperatureSpectrum;
        private static Material[] matsElevation;
        private static readonly Color[] ElevationSpectrum;
        private static Material[] matsRainfall;
        private static readonly Color[] RainfallSpectrum;
        private static Material[] matsGrowingPeriod;
        private static readonly Color[] GrowingPeriodSpectrum;

        public static Material[] matsCommonality;
        private static readonly Color[] CommonalitySpectrum;
        private static int NumMatsPerMode;

        static Materials()
        {
            matWhiteOverlay = BaseContent.WhiteMat;
            matWhiteOverlay.renderQueue = terrainOverlayRenderQueue;
            matGreenOverlay = SolidColorMaterials.SimpleSolidColorMaterial(Color.green);
            matGreenOverlay.renderQueue = terrainOverlayRenderQueue;
            matMouseRegion = new Material(ShaderDatabase.WorldOverlayAdditive)
            {
                renderQueue = selectorRenderQueue,
                color = new Color(1f, 1f, 1f, 0.1f)
            };
            matSelectedRegion = new Material(ShaderDatabase.WorldOverlayAdditive)
            {
                renderQueue = selectorRenderQueue,
                color = new Color(1f, 1f, 1f, 0.1f)
            };
            matFeaturesBorder = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.MetaOverlay, new Color(1f, 1f, 1f, 0.1f), 3510);
            NumMatsPerMode = 50;

            FertilitySpectrum = new Color[2]
            {
                new Color(0f, 1f, 0f, 0f),
                new Color(0f, 1f, 0f, 0.5f)
            };
            TemperatureSpectrum = new Color[8]
            {
                new Color(1f, 1f, 1f),
                new Color(0f, 0f, 1f),
                new Color(0.25f, 0.25f, 1f),
                new Color(0.6f, 0.6f, 1f),
                new Color(0.5f, 0.5f, 0.5f),
                new Color(0.5f, 0.3f, 0f),
                new Color(1f, 0.6f, 0.18f),
                new Color(1f, 0f, 0f)
            };
            ElevationSpectrum = new Color[4]
            {
                new Color(0.224f, 0.18f, 0.15f),
                new Color(0.447f, 0.369f, 0.298f),
                new Color(0.6f, 0.6f, 0.6f),
                new Color(1f, 1f, 1f)
            };
            RainfallSpectrum = new Color[12]
            {
                new Color(0.9f, 0.9f, 0.9f),
                GenColor.FromBytes(190, 190, 190),
                new Color(0.58f, 0.58f, 0.58f),
                GenColor.FromBytes(196, 112, 110),
                GenColor.FromBytes(200, 179, 150),
                GenColor.FromBytes(255, 199, 117),
                GenColor.FromBytes(255, 255, 84),
                GenColor.FromBytes(145, 255, 253),
                GenColor.FromBytes(0, 255, 0),
                GenColor.FromBytes(63, 198, 55),
                GenColor.FromBytes(13, 150, 5),
                GenColor.FromBytes(5, 112, 94)
            };
            GrowingPeriodSpectrum = new Color[2]
            {
                new Color(1f, 1f, 1f, 1f),
                new Color(0f, 1f, 0f, 1f)
            };
            CommonalitySpectrum = new Color[11]
            {
                new Color(1f, 1f, 1f), //White or 0
                new Color(1f, 1f, 0f),
                new Color(1f, 0.84f, 0f),
                new Color(1f, 0.64f, 0f),
                new Color(1f, 0.54f, 0f),
                new Color(1f, 0.49f, 0f),
                new Color(1f, 0.38f, 0.27f),
                new Color(1f, 0.27f, 0f),
                new Color(1f, 0.2f, 0f),
                new Color(1f, 0.1f, 0f),
                new Color(1f, 0f, 0f),
            };
            GenerateMats(ref matsFertility, FertilitySpectrum, NumMatsPerMode);
            GenerateMats(ref matsTemperature, TemperatureSpectrum, NumMatsPerMode);
            GenerateMats(ref matsElevation, ElevationSpectrum, NumMatsPerMode);
            GenerateMats(ref matsRainfall, RainfallSpectrum, NumMatsPerMode);
            GenerateMats(ref matsGrowingPeriod, GrowingPeriodSpectrum, numMatsGrowingPeriod);
            GenerateMats(ref matsCommonality, CommonalitySpectrum, NumMatsPerMode);
        }

        private static void GenerateMats(ref Material[] mats, Color[] colorSpectrum, int numMats)
        {
            mats = new Material[numMats];
            for (int i = 0; i < numMats; i++)
            {
                Color color = ColorsFromSpectrum.Get(colorSpectrum, (float)i / (float)numMats);
                Material mat = MaterialPool.MatFrom(BaseContent.WhiteTex, ShaderDatabase.MetaOverlay, color, 3510);
                mats[i] = mat;
            }
        }

        public static Material MatForFertilityOverlay(float fert)
        {
            int value = Mathf.FloorToInt(fert * (float)NumMatsPerMode);
            return matsFertility[Mathf.Clamp(value, 0, NumMatsPerMode - 1)];
        }

        public static Material MatForTemperature(float temp)
        {
            int value = Mathf.FloorToInt(Mathf.InverseLerp(-50f, 50f, temp) * (float)NumMatsPerMode);
            return matsTemperature[Mathf.Clamp(value, 0, NumMatsPerMode - 1)];
        }

        public static Material MatForElevation(float elev)
        {
            int value = Mathf.FloorToInt(Mathf.InverseLerp(0f, 5000f, elev) * (float)NumMatsPerMode);
            return matsElevation[Mathf.Clamp(value, 0, NumMatsPerMode - 1)];
        }

        public static Material MatForRainfallOverlay(float rain)
        {
            int value = Mathf.FloorToInt(Mathf.InverseLerp(0f, 5000f, rain) * (float)NumMatsPerMode);
            return matsRainfall[Mathf.Clamp(value, 0, NumMatsPerMode - 1)];
        }

        public static Material MatForGrowingPeriodOverlay(float growingPeriod)
        {
            int value = Mathf.FloorToInt(growingPeriod / 2f);
            return matsGrowingPeriod[Mathf.Clamp(value, 0, numMatsGrowingPeriod - 1)];
        }

        public static Material MatForCommonalityOverlay(float commonality)
        {
            int value = Mathf.FloorToInt(Mathf.InverseLerp(0f, 1f, commonality) * (float)NumMatsPerMode);
            return matsCommonality[Mathf.Clamp(value, 0, NumMatsPerMode - 1)];
        }
    }
}
