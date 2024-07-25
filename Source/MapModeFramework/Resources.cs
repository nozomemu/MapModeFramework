using UnityEngine;
using Verse;

namespace MapModeFramework
{
    [StaticConstructorOnStartup]
    public static class Resources
    {
        public static readonly Texture2D switchLeft;
        public static readonly Texture2D switchRight;
        public static readonly Texture2D dropdown;
        public static readonly Texture2D dropdownExpanded;

        static Resources()
        {
            switchLeft = ContentFinder<Texture2D>.Get("UI/SwitchLeft");
            switchRight = ContentFinder<Texture2D>.Get("UI/SwitchRight");
            dropdown = ContentFinder<Texture2D>.Get("UI/Dropdown");
            dropdownExpanded = ContentFinder<Texture2D>.Get("UI/DropdownExpanded");
        }
    }
}
