using System;
using System.Collections.Generic;
using Game.Installers;
using UnityEngine;

namespace Game.Simulation
{
    public class WorldSimulationService : IInitializable
    {
        private const int MaxPlayerCount = 4;

        private readonly Dictionary<TerrainType, TerrainProfile> _terrainProfiles = new();
        private readonly List<CityState> _cities = new();
        private readonly List<StrategicLocation> _strategicLocations = new();
        private readonly Dictionary<string, PlayerDiplomacyState> _diplomacyByPlayer = new();
        private readonly Dictionary<string, PlayerProfile> _players = new();
        private readonly Dictionary<string, IPlayerAgent> _agentsByPlayer = new();
        private readonly Dictionary<string, DiplomacyRelation> _diplomacyRelations = new();
        private readonly List<SimulationCommand> _commandBuffer = new();

        private int _turn;
        private WeeklyActionType _currentAction;

        public WeeklyActionType CurrentAction => _currentAction;

        public IReadOnlyCollection<PlayerProfile> Players => _players.Values;

        public void Initialize()
        {
            SeedTerrainProfiles();
            SeedPrototypeWorld();
            SeedPrototypePlayers();
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

            ProcessAiTurns();
        }

        public bool TryRegisterPlayer(string playerId, string displayName, bool isHuman, IPlayerAgent agent = null)
        {
            if (string.IsNullOrWhiteSpace(playerId) || _players.ContainsKey(playerId) || _players.Count >= MaxPlayerCount)
            {
                return false;
            }

            _players[playerId] = new PlayerProfile
            {
                PlayerId = playerId,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? playerId : displayName,
                IsHuman = isHuman
            };

            if (!isHuman && agent != null)
            {
                _agentsByPlayer[playerId] = agent;
            }

            InitializeDiplomacyRelationsFor(playerId);
            AssignFreeCityTo(playerId);
            return true;
        }

        public void SetupPrototypeMultiplayerSession(int humanPlayerCount = 1, int botPlayerCount = 3)
        {
            _players.Clear();
            _agentsByPlayer.Clear();
            _diplomacyRelations.Clear();

            int clampedHumanPlayers = Mathf.Clamp(humanPlayerCount, 1, MaxPlayerCount);
            int clampedBotPlayers = Mathf.Clamp(botPlayerCount, 0, MaxPlayerCount - clampedHumanPlayers);

            for (int i = 0; i < clampedHumanPlayers; i++)
            {
                string id = $"player-human-{i + 1}";
                TryRegisterPlayer(id, $"Human {i + 1}", true);
            }

            for (int i = 0; i < clampedBotPlayers; i++)
            {
                string id = $"player-bot-{i + 1}";
                TryRegisterPlayer(id, $"Bot {i + 1}", false, new SimpleRuleBasedBotAgent());
            }
        }

        public bool TryApplyBattle(string cityId, float intensity)
        {
            if (_currentAction != WeeklyActionType.War)
            {
                return false;
            }

            CityState city = _cities.Find(x => x.CityData.Id == cityId);
            if (city == null)
            {
                return false;
            }

            city.RegisterBattle(Mathf.Max(0f, intensity));
            return true;
        }

        public bool TryApplyBattle(string sourcePlayerId, string cityId, float intensity)
        {
            CityState city = _cities.Find(x => x.CityData.Id == cityId);
            if (city == null)
            {
                return false;
            }

            if (!CanAttack(sourcePlayerId, city.OwnerPlayerId))
            {
                return false;
            }

            bool attackApplied = TryApplyBattle(cityId, intensity);
            if (attackApplied)
            {
                city.HistoryLog.Add($"{sourcePlayerId} attacked with intensity {intensity:0.0}");
            }

            return attackApplied;
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

        public bool TrySetDiplomaticStance(string sourcePlayerId, string targetPlayerId, DiplomaticStance stance)
        {
            if (!_players.ContainsKey(sourcePlayerId) || !_players.ContainsKey(targetPlayerId) || sourcePlayerId == targetPlayerId)
            {
                return false;
            }

            DiplomacyRelation relation = GetOrCreateRelation(sourcePlayerId, targetPlayerId);
            relation.Stance = stance;
            relation.LastUpdatedTurn = _turn;

            DiplomacyRelation mirrored = GetOrCreateRelation(targetPlayerId, sourcePlayerId);
            mirrored.Stance = stance;
            mirrored.LastUpdatedTurn = _turn;
            return true;
        }

        public bool CanAttack(string sourcePlayerId, string targetPlayerId)
        {
            if (string.IsNullOrWhiteSpace(sourcePlayerId) || string.IsNullOrWhiteSpace(targetPlayerId) || sourcePlayerId == targetPlayerId)
            {
                return false;
            }

            DiplomacyRelation relation = GetOrCreateRelation(sourcePlayerId, targetPlayerId);
            return relation.Stance == DiplomaticStance.War;
        }

        public bool ExecuteCommand(SimulationCommand command)
        {
            if (command == null)
            {
                return false;
            }

            switch (command.Type)
            {
                case SimulationCommandType.DeclareWar:
                    return TrySetDiplomaticStance(command.SourcePlayerId, command.TargetPlayerId, DiplomaticStance.War);
                case SimulationCommandType.OfferPeace:
                    return TrySetDiplomaticStance(command.SourcePlayerId, command.TargetPlayerId, DiplomaticStance.Peace);
                case SimulationCommandType.OfferCeasefire:
                    return TrySetDiplomaticStance(command.SourcePlayerId, command.TargetPlayerId, DiplomaticStance.Ceasefire);
                case SimulationCommandType.BreakCeasefire:
                    return TrySetDiplomaticStance(command.SourcePlayerId, command.TargetPlayerId, DiplomaticStance.War);
                case SimulationCommandType.AttackCity:
                    return TryApplyBattle(command.SourcePlayerId, command.CityId, command.AttackIntensity);
                case SimulationCommandType.SignAgreement:
                    return TrySignAgreement(command.SourcePlayerId, command.TargetPlayerId, command.AgreementType);
                case SimulationCommandType.BreakAgreement:
                    return TryBreakAgreement(command.SourcePlayerId, command.TargetPlayerId, command.AgreementType);
                default:
                    return false;
            }
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
            CityState city = _cities.Find(x => x.CityData.Id == cityId);
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

        private void ProcessAiTurns()
        {
            _commandBuffer.Clear();

            foreach (KeyValuePair<string, IPlayerAgent> pair in _agentsByPlayer)
            {
                pair.Value.EnqueueTurnCommands(this, pair.Key, _commandBuffer);
            }

            for (int i = 0; i < _commandBuffer.Count; i++)
            {
                ExecuteCommand(_commandBuffer[i]);
            }
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
        {/*
            _cities.Clear();
            _strategicLocations.Clear();

            CityState mountainCity = new CityState
            {
                CityId = "city-karadag",
                Name = "Karadag",
                OwnerPlayerId = "player-human-1",
                Terrain = TerrainType.Mountain
            };
            mountainCity.Deposits.Add(new ResourceDeposit { Resource = ResourceType.Iron, Richness = 0.9f, IsDiscovered = false });
            mountainCity.Deposits.Add(new ResourceDeposit { Resource = ResourceType.Gold, Richness = 0.4f, IsDiscovered = false });

            CityState plainsCity = new CityState
            {
                CityData.Id = "city-ovakent",
                Name = "Ovakent",
                OwnerPlayerId = "player-bot-1",
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
            });*/
        }

        private void SeedPrototypePlayers()
        {/*
            SetupPrototypeMultiplayerSession();
            TrySetDiplomaticStance("player-human-1", "player-bot-1", DiplomaticStance.Peace);*/
        }

        private void InitializeDiplomacyRelationsFor(string playerId)
        {
            foreach (PlayerProfile profile in _players.Values)
            {
                if (profile.PlayerId == playerId)
                {
                    continue;
                }

                GetOrCreateRelation(playerId, profile.PlayerId);
                GetOrCreateRelation(profile.PlayerId, playerId);
            }
        }

        private DiplomacyRelation GetOrCreateRelation(string sourcePlayerId, string targetPlayerId)
        {
            string key = $"{sourcePlayerId}->{targetPlayerId}";
            if (_diplomacyRelations.TryGetValue(key, out DiplomacyRelation relation))
            {
                return relation;
            }

            relation = new DiplomacyRelation
            {
                SourcePlayerId = sourcePlayerId,
                TargetPlayerId = targetPlayerId,
                Stance = DiplomaticStance.Neutral,
                LastUpdatedTurn = _turn
            };

            _diplomacyRelations[key] = relation;
            return relation;
        }

        private void AssignFreeCityTo(string playerId)
        {
            CityState city = _cities.Find(x => string.IsNullOrWhiteSpace(x.OwnerPlayerId));
            if (city != null)
            {
                city.OwnerPlayerId = playerId;
            }
        }

        public void Dispose()
        {
            _terrainProfiles.Clear();
            _cities.Clear();
            _strategicLocations.Clear();
            _diplomacyByPlayer.Clear();
            _players.Clear();
            _agentsByPlayer.Clear();
            _diplomacyRelations.Clear();
            _commandBuffer.Clear();
        }
    }

    public class SimpleRuleBasedBotAgent : IPlayerAgent
    {
        public void EnqueueTurnCommands(WorldSimulationService simulation, string playerId, List<SimulationCommand> output)
        {
            if (simulation.CurrentAction == WeeklyActionType.Diplomacy)
            {
                output.Add(new SimulationCommand
                {
                    Type = UnityEngine.Random.value > 0.5f ? SimulationCommandType.OfferCeasefire : SimulationCommandType.DeclareWar,
                    SourcePlayerId = playerId,
                    TargetPlayerId = "player-human-1"
                });
            }

            if (simulation.CurrentAction == WeeklyActionType.War)
            {
                output.Add(new SimulationCommand
                {
                    Type = SimulationCommandType.AttackCity,
                    SourcePlayerId = playerId,
                    CityId = "city-karadag",
                    AttackIntensity = UnityEngine.Random.Range(0.4f, 1.2f)
                });
            }
        }
    }
}
