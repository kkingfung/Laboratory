namespace Laboratory.Core.Equipment.Types
{
    public enum EquipmentType : byte
    {
        // Racing Gear
        SpeedBoots,
        AerodynamicHelmet,
        LightweightHarness,
        TurboBooster,

        // Combat Gear
        WeaponMelee,
        WeaponRanged,
        ArmorHeavy,
        ArmorLight,
        Shield,
        CombatStimulant,

        // Puzzle Gear
        ThinkingCap,
        ConcentrationAid,
        MemoryEnhancer,
        LogicProcessor,

        // Strategy Gear
        TacticalVisor,
        CommandBadge,
        StrategicAnalyzer,
        LeadershipSymbol,

        // Music Gear
        InstrumentWind,
        InstrumentString,
        InstrumentPercussion,
        RhythmAccessory,

        // Adventure Gear
        ExplorationPack,
        SurvivalGear,
        ClimbingEquipment,
        AdventureBoots,

        // Platforming Gear
        JumpEnhancer,
        GripGloves,
        BalanceAid,
        MobilityBooster,

        // Crafting Gear
        CraftingTools,
        PrecisionInstruments,
        QualityEnhancer,
        EfficiencyBooster,

        // Universal Gear
        EnergyCore,
        HealthBooster,
        ExperienceMultiplier,
        StatusProtection
    }

    public enum EquipmentRarity : byte
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythic = 5
    }

    public enum EquipmentSlot : byte
    {
        Head,
        Body,
        Weapon,
        Accessory,
        Special
    }
}