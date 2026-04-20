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
        public const float WorldScale = 1.6f;
        public const float SoccerBallDiameter = 0.22f;
        public const float SoccerBallRadius = SoccerBallDiameter * 0.5f;
        public const float SoccerBallMass = 0.43f;
        public const float LoopFallHeight = -12f;
        public const float PauseTimeScale = 0.2f;
    }
}
