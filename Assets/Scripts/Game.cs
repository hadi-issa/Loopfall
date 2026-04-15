namespace Game
{
    // Global configuration values for the game
    public static class Config
    {
        // How many pickups must be collected to win
        public const int numberOfObjects = 12;

        // Player movement speed
        public const float speed = 10f;
    }

    // Centralized tag names used across the project
    public static class Tag
    {
        // Must match the tag assigned to your pickup objects in Unity
        public const string PickUp = "Pick Up";

        // Must match the tag assigned to your Player object
        public const string Player = "Player";
    }
}
