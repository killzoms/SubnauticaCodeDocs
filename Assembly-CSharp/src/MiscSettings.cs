using Gendarme;

namespace AssemblyCSharp
{
    [SuppressMessage("Gendarme.Rules.Concurrency", "NonConstantStaticFieldsShouldNotBeVisibleRule")]
    public static class MiscSettings
    {
        public const float defaultFieldOfView = 60f;

        public const float minFieldOfView = 40f;

        public const float maxFieldOfView = 90f;

        public static string consoleHistory = "";

        public static bool cameraBobbing = true;

        public static string email = "";

        public static bool rememberEmail = false;

        public static bool hideEmailBox = false;

        public static float fieldOfView = 60f;
    }
}
