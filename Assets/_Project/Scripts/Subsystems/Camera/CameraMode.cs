namespace Laboratory.Subsystems.Camera
{
    /// <summary>
    /// Camera modes for different game genres
    /// Supports all 47 genres with appropriate camera perspectives
    /// </summary>
    public enum CameraMode
    {
        // Third-person modes
        ThirdPerson,            // Action, Adventure, RPG
        ThirdPersonOrbit,       // Combat, Boss fights
        ThirdPersonFixed,       // Survival Horror (Resident Evil style)

        // First-person modes
        FirstPerson,            // FPS, Walking Simulator
        FirstPersonFixed,       // Horror, Limited movement

        // Top-down modes
        TopDown,                // Strategy, Twin-stick shooters
        TopDownIsometric,       // RTS, City Builder, Tactics
        TopDownDiagonal,        // Classic RPG, Diablo-like

        // Side view modes
        SideScroller,           // 2D Platformer, Beat 'em up
        SideScrollerFixed,      // Classic platformer (no camera lead)
        SideScrollerParallax,   // With parallax background layers

        // Special modes for specific genres
        RacingThirdPerson,      // Racing games (behind vehicle)
        RacingFirstPerson,      // Racing games (cockpit view)
        RacingCinematic,        // Dynamic camera for racing

        FlyingFree,             // Flight simulator, Space games
        FlyingChase,            // Arcade flight games

        StrategyRTS,            // RTS games with pan, zoom, rotate
        StrategyTurnBased,      // Turn-based strategy with grid

        PuzzleFixed,            // Puzzle games with static camera
        PuzzleOrbital,          // 3D puzzle games

        FightingGame,           // 2.5D fighting games
        BeatEmUp,               // Side-scrolling beat 'em up

        Rhythm,                 // Rhythm games

        Detective,              // Point-and-click, Detective games
        VisualNovel,            // Visual novel static camera

        BulletHell,             // Top-down bullet hell

        // Hybrid modes
        Cinematic,              // Scripted cinematic camera
        Free,                   // Developer/debug free camera
        Spectator               // Multiplayer spectator
    }
}
