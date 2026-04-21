using BaseLib.Config;

namespace LimbusCore.LimbusCoreCode
{
    internal class LimbusCoreConfig : SimpleModConfig
    {
        public static bool EnableInkBorders { get; set; } = true;
        public static bool EnableUiSuppression { get; set; } = true;
        public static bool EnableMapExtension { get; set; } = true;
        public static bool EnableParallax { get; set; } = true;
    }
}
