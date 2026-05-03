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
        public const int MaxLoops = 5;
        public const int FragmentsRequiredForLoopAdvance = 5;
        public const float WorldScale = 1.6f;
        public const float MapHorizontalMultiplier = 2.5f;
        public const float MapVerticalMultiplier = 2.5f;
        public const float HorizontalWorldScale = WorldScale * MapHorizontalMultiplier;
        public const float VerticalWorldScale = WorldScale * MapVerticalMultiplier;
        public const float PlayerSizeMultiplier = 1.5f;
        public const float SoccerBallDiameter = 0.22f * PlayerSizeMultiplier;
        public const float SoccerBallRadius = SoccerBallDiameter * 0.5f;
        public const float SoccerBallMass = 0.43f;
        public const float LoopFallHeight = -12f;
        public const float PauseTimeScale = 0.2f;
    }
}
