namespace MapModeFramework
{
    public class WorldLayer_MapMode_Terrain : WorldLayer_MapMode
    {
        public static WorldLayer_MapMode_Terrain Instance;

        public WorldLayer_MapMode_Terrain() : base()
        {
            Instance = this;
        }
    }
}
