namespace Laboratory.Core.TownBuilding.Types
{
    public enum BuildingType : byte
    {
        // Essential Facilities
        BreedingCenter,
        TrainingGround,
        ResearchLab,
        MonsterHabitat,
        EquipmentShop,
        SocialHub,

        // Activity Centers
        RacingTrack,
        CombatArena,
        PuzzleAcademy,
        StrategyCenter,
        MusicStudio,
        AdventureGuild,
        PlatformCourse,
        CraftingWorkshop,

        // Infrastructure
        PowerPlant,
        WaterTreatment,
        ResourceStorage,
        TransportHub,
        TownHall,
        Hospital,
        Market,
        Road,

        // Decorative
        Park,
        Monument,
        Garden,
        Fountain,

        // Special
        Portal,
        Observatory,
        Archive,
        Sanctuary
    }

    public enum BuildingTier : byte
    {
        Basic = 1,
        Advanced = 2,
        Expert = 3,
        Master = 4,
        Legendary = 5
    }

    public enum BuildingStatus : byte
    {
        Planning,
        UnderConstruction,
        Operational,
        Upgrading,
        Maintenance,
        Damaged,
        Abandoned
    }

    public enum ResourceType : byte
    {
        Food,
        Materials,
        Energy,
        Research,
        Currency,
        SpecialItems
    }

    public enum ServiceType : byte
    {
        None,
        Healthcare,
        Education,
        Entertainment,
        Security,
        Transport,
        Utilities,
        Research,
        Commerce
    }

    public enum DistrictType : byte
    {
        Residential,
        Commercial,
        Industrial,
        Recreational,
        Research,
        Administrative,
        Special
    }
}