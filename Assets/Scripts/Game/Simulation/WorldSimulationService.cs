using System;
using System.Collections.Generic;
using Game.Installers;
using UnityEngine;

namespace Game.Simulation
{
    public class WorldSimulationService : IInitializable
    {
        private readonly Dictionary<TerrainType, TerrainProfile> _terrainProfiles = new();
        private readonly List<CityState> _cities = new();
        private readonly List<StrategicLocation> _strategicLocations = new();
        private readonly Dictionary<string, PlayerDiplomacyState> _diplomacyByPlayer = new();

        private int _turn;
        private WeeklyActionType _currentAction;

        public WeeklyActionType CurrentAction => _currentAction;

        public void Initialize()
        {
            SeedTerrainProfiles();
            SeedPrototypeWorld();
            _turn = 0;
            _currentAction = ResolveWeeklyAction(_turn);
        }

        public void Tick()
        {
            _turn++;
            _currentAction = ResolveWeeklyAction(_turn);

            for (int i = 0; i < _cities.Count; i++)
            {
                _cities[i].RegisterPeacefulTurn();
                if (_cities[i].PublicOrder < 30f && UnityEngine.Random.value < 0.15f)
                {
                    _cities[i].RegisterRebellion();
                }
            }
        }

        public bool TryApplyBattle(string cityId, float intensity)
        {
            if (_currentAction != WeeklyActionType.War)
            {
                return false;
            }

            CityState city = _cities.Find(x => x.CityId == cityId);
            if (city == null)
            {
                return false;
            }

            city.RegisterBattle(Mathf.Max(0f, intensity));
            return true;
        }

        public bool TrySignAgreement(string sourcePlayerId, string targetPlayerId, AgreementType type)
        {
            if (_currentAction != WeeklyActionType.Diplomacy)
            {
                return false;
            }

            PlayerDiplomacyState source = GetOrCreateDiplomacyState(sourcePlayerId);
            source.Agreements.Add(new DiplomacyAgreement
            {
                SourcePlayerId = sourcePlayerId,
                TargetPlayerId = targetPlayerId,
                Type = type,
                SignedTurn = _turn,
                IsBroken = false
            });
            source.MarkLoyalBehavior();
            return true;
        }

        public bool TryBreakAgreement(string sourcePlayerId, string targetPlayerId, AgreementType type)
        {
            PlayerDiplomacyState source = GetOrCreateDiplomacyState(sourcePlayerId);
            DiplomacyAgreement agreement = source.Agreements.Find(x =>
                x.TargetPlayerId == targetPlayerId &&
                x.Type == type &&
                !x.IsBroken);

            if (agreement == null)
            {
                return false;
            }

            agreement.IsBroken = true;
            source.MarkBreach();
            return true;
        }

        public int CalculateTransitTaxIncome(string locationId, int unitCount)
        {
            StrategicLocation location = _strategicLocations.Find(x => x.Id == locationId);
            if (location == null || !location.IsBridgeCrossing)
            {
                return 0;
            }

            return Mathf.Max(0, unitCount) * Mathf.Max(0, location.TransitTax);
        }

        public void DispatchAnalyst(string cityId, int maxDiscoveryCount = 1)
        {
            CityState city = _cities.Find(x => x.CityId == cityId);
            if (city == null)
            {
                return;
            }

            int discovered = 0;
            for (int i = 0; i < city.Deposits.Count; i++)
            {
                if (city.Deposits[i].IsDiscovered)
                {
                    continue;
                }

                city.Deposits[i].IsDiscovered = true;
                discovered++;

                if (discovered >= Mathf.Max(1, maxDiscoveryCount))
                {
                    break;
                }
            }
        }

        public float GetDefenseMultiplier(TerrainType terrain)
        {
            if (_terrainProfiles.TryGetValue(terrain, out TerrainProfile profile))
            {
                return profile.DefenseMultiplier;
            }

            return 1f;
        }

        private static WeeklyActionType ResolveWeeklyAction(int turn)
        {
            int day = turn % 7;
            if (day == 0 || day == 1)
            {
                return WeeklyActionType.Diplomacy;
            }

            if (day == 2 || day == 3)
            {
                return WeeklyActionType.ResourceCollection;
            }

            if (day == 4 || day == 5)
            {
                return WeeklyActionType.HeadquartersDevelopment;
            }

            return WeeklyActionType.War;
        }

        private PlayerDiplomacyState GetOrCreateDiplomacyState(string playerId)
        {
            if (_diplomacyByPlayer.TryGetValue(playerId, out PlayerDiplomacyState state))
            {
                return state;
            }

            state = new PlayerDiplomacyState
            {
                PlayerId = playerId,
                TrustTitle = TrustTitle.Neutral
            };
            _diplomacyByPlayer[playerId] = state;
            return state;
        }

        private void SeedTerrainProfiles()
        {
            _terrainProfiles.Clear();
            AddTerrainProfile(TerrainType.Plains, 1f, 1.2f, 0.8f);
            AddTerrainProfile(TerrainType.Mountain, 1.2f, 0.6f, 1.4f);
            AddTerrainProfile(TerrainType.Forest, 1.1f, 0.9f, 1.1f);
            AddTerrainProfile(TerrainType.Desert, 0.9f, 0.5f, 1f);
            AddTerrainProfile(TerrainType.River, 1f, 1.1f, 0.9f);
            AddTerrainProfile(TerrainType.BridgeCrossing, 1.05f, 1f, 1f);
        }

        private void AddTerrainProfile(TerrainType terrain, float defense, float farming, float mining)
        {
            _terrainProfiles[terrain] = new TerrainProfile
            {
                Terrain = terrain,
                DefenseMultiplier = defense,
                FarmingMultiplier = farming,
                MiningMultiplier = mining
            };
        }

        private void SeedPrototypeWorld()
        {
            _cities.Clear();
            _strategicLocations.Clear();

            CityState mountainCity = new CityState
            {
                CityId = "city-karadag",
                Name = "Karadag",
                Terrain = TerrainType.Mountain
            };
            mountainCity.Deposits.Add(new ResourceDeposit { Resource = ResourceType.Iron, Richness = 0.9f, IsDiscovered = false });
            mountainCity.Deposits.Add(new ResourceDeposit { Resource = ResourceType.Gold, Richness = 0.4f, IsDiscovered = false });

            CityState plainsCity = new CityState
            {
                CityId = "city-ovakent",
                Name = "Ovakent",
                Terrain = TerrainType.Plains
            };
            plainsCity.Deposits.Add(new ResourceDeposit { Resource = ResourceType.Grain, Richness = 0.95f, IsDiscovered = false });
            plainsCity.Deposits.Add(new ResourceDeposit { Resource = ResourceType.Stone, Richness = 0.25f, IsDiscovered = false });

            _cities.Add(mountainCity);
            _cities.Add(plainsCity);

            _strategicLocations.Add(new StrategicLocation
            {
                Id = "bridge-northpass",
                Name = "Northpass Bridge",
                IsBridgeCrossing = true,
                TransitTax = 6
            });
        }

        public void Dispose()
        {
            _terrainProfiles.Clear();
            _cities.Clear();
            _strategicLocations.Clear();
            _diplomacyByPlayer.Clear();
        }
    }
}
