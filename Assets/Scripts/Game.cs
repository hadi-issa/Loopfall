using UnityEngine;

namespace Game
{
    public enum FragmentType
    {
        Stabilizing,
        Corrupted,
    }

    public static class Config
    {
        public const string MainSceneName = "Loopfall";
        public const int MaxLoops = 10;
        public const float LoopFallHeight = -12f;
        public const float PauseTimeScale = 0.2f;
    }
}
