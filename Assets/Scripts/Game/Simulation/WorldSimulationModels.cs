using System;
using System.Collections.Generic;

namespace Game.Simulation
{
    public enum TerrainType
    {
        Plains,
        Mountain,
        Forest,
        Desert,
        River,
        BridgeCrossing
    }

    public enum ResourceType
    {
        Grain,
        Iron,
        Gold,
        Timber,
        Stone
    }

    public enum WeeklyActionType
    {
        Diplomacy,
        ResourceCollection,
        HeadquartersDevelopment,
        War
    }

    public enum AgreementType
    {
        NonAggression,
        Trade
    }

    public enum TrustTitle
    {
        Neutral,
        Loyal,
        Unreliable
    }

    [Serializable]
    public class TerrainProfile
    {
        public TerrainType Terrain;
        public float DefenseMultiplier = 1f;
        public float FarmingMultiplier = 1f;
        public float MiningMultiplier = 1f;
    }

    [Serializable]
    public class ResourceDeposit
    {
        public ResourceType Resource;
        public float Richness;
        public bool IsDiscovered;
    }

    [Serializable]
    public class CityState
    {
        public string CityId;
        public string Name;
        public TerrainType Terrain;

        public int Population = 1000;
        public float PublicOrder = 100f;
        public float LandFertility = 100f;
        public float BanditRisk;

        public readonly List<ResourceDeposit> Deposits = new();
        public readonly List<string> HistoryLog = new();

        public void RegisterBattle(float intensity)
        {
            LandFertility = Math.Max(5f, LandFertility - (intensity * 3f));
            PublicOrder = Math.Max(0f, PublicOrder - (intensity * 4f));
            BanditRisk = Math.Min(100f, BanditRisk + (intensity * 2f));
            HistoryLog.Add($"Battle scar intensity {intensity:0.0}");
        }

        public void RegisterPeacefulTurn()
        {
            LandFertility = Math.Min(100f, LandFertility + 0.7f);
            PublicOrder = Math.Min(100f, PublicOrder + 0.5f);
            BanditRisk = Math.Max(0f, BanditRisk - 0.4f);
        }

        public void RegisterRebellion()
        {
            PublicOrder = Math.Max(0f, PublicOrder - 8f);
            BanditRisk = Math.Min(100f, BanditRisk + 7f);
            HistoryLog.Add("Rebellion erupted");
        }
    }

    [Serializable]
    public class StrategicLocation
    {
        public string Id;
        public string Name;
        public bool IsBridgeCrossing;
        public int TransitTax;
    }

    [Serializable]
    public class DiplomacyAgreement
    {
        public string SourcePlayerId;
        public string TargetPlayerId;
        public AgreementType Type;
        public int SignedTurn;
        public bool IsBroken;
    }

    [Serializable]
    public class PlayerDiplomacyState
    {
        public string PlayerId;
        public TrustTitle TrustTitle;
        public readonly List<DiplomacyAgreement> Agreements = new();

        public void MarkBreach()
        {
            TrustTitle = TrustTitle.Unreliable;
        }

        public void MarkLoyalBehavior()
        {
            if (TrustTitle != TrustTitle.Unreliable)
            {
                TrustTitle = TrustTitle.Loyal;
            }
        }
    }
}
