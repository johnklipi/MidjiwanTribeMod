using BepInEx.Logging;
using Polytopia.Data;

namespace MidjiwanTribeMod;

public class Main
{
    public static ManualLogSource? modLogger;
    public static void Load(ManualLogSource logger)
    {
        modLogger = logger;
    }
}
