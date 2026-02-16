using System;
using System.Collections.Generic;
using System.Text;
using Game.Entity;
using Game.Simulation;
using UnityEngine;

namespace Game.Map
{
    public class CityDatabase : MonoBehaviour
    {
        private const string DefaultCityCsvResourcePath = "Data/cities";
        private const string RegionRootName = "Regions";

        private static readonly Dictionary<TerrainType, StatRangeProfile> TerrainProfiles = new()
        {
            { TerrainType.Plains, new StatRangeProfile(0.8f, 1.05f, 1.2f, 1.6f, 0.7f, 1.0f) },
            { TerrainType.Mountain, new StatRangeProfile(1.3f, 1.8f, 0.5f, 0.9f, 1.2f, 1.8f) },
            { TerrainType.Forest, new StatRangeProfile(1.0f, 1.3f, 0.8f, 1.1f, 0.8f, 1.2f) },
            { TerrainType.Desert, new StatRangeProfile(0.7f, 1.0f, 0.3f, 0.7f, 1.0f, 1.5f) },
            { TerrainType.River, new StatRangeProfile(0.9f, 1.2f, 1.1f, 1.5f, 0.8f, 1.1f) },
            { TerrainType.BridgeCrossing, new StatRangeProfile(1.1f, 1.4f, 0.9f, 1.2f, 0.9f, 1.3f) }
        };

        [SerializeField] private string cityCsvResourcePath = DefaultCityCsvResourcePath;
        [SerializeField] private string cityIdPrefix = "city";

        private readonly List<CityNameRecord> _cityNames = new();
        private readonly Dictionary<string, CityData> _cityDataById = new();

        private bool _isInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapFromScene()
        {
            CityDatabase existing = FindObjectOfType<CityDatabase>();
            if (existing != null)
            {
                existing.InitializeIfNeeded();
                return;
            }

            GameObject regions = GameObject.Find(RegionRootName);
            if (regions == null)
            {
                Debug.LogWarning($"[{nameof(CityDatabase)}] Could not find '{RegionRootName}' root in scene.");
                return;
            }

            CityDatabase database = regions.GetComponentInParent<CityDatabase>();
            if (database == null)
            {
                database = regions.transform.root.gameObject.AddComponent<CityDatabase>();
            }

            database.InitializeIfNeeded();
        }

        private void Awake()
        {
            InitializeIfNeeded();
        }

        public bool TryGet(string cityId, out CityData cityData)
        {
            InitializeIfNeeded();
            return _cityDataById.TryGetValue(cityId, out cityData);
        }

        public void InitializeIfNeeded()
        {
            if (_isInitialized)
            {
                return;
            }

            _cityNames.Clear();
            _cityDataById.Clear();
            LoadCityNameRecords();
            BindRegionCities();
            _isInitialized = true;
        }

        private void LoadCityNameRecords()
        {
            TextAsset csvFile = Resources.Load<TextAsset>(cityCsvResourcePath);
            if (csvFile == null)
            {
                Debug.LogWarning($"[{nameof(CityDatabase)}] Could not load city CSV at Resources/{cityCsvResourcePath}.csv");
                return;
            }

            List<string[]> rows = CsvParser.Parse(csvFile.text);
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 3)
                {
                    continue;
                }

                _cityNames.Add(new CityNameRecord
                {
                    Id = row[0].Trim(),
                    Name = row[1].Trim(),
                    Description = row[2].Trim()
                });
            }
        }

        private void BindRegionCities()
        {
            GameObject regionsRoot = GameObject.Find(RegionRootName);
            if (regionsRoot == null)
            {
                Debug.LogWarning($"[{nameof(CityDatabase)}] Could not find '{RegionRootName}' root in scene.");
                return;
            }

            Transform rootTransform = regionsRoot.transform;
            int cityCounter = 0;

            for (int i = 0; i < rootTransform.childCount; i++)
            {
                Transform cityTransform = rootTransform.GetChild(i);
                City city = cityTransform.GetComponent<City>();
                if (city == null)
                {
                    city = cityTransform.gameObject.AddComponent<City>();
                }

                CityNameRecord nameRecord = ResolveNameRecord(cityCounter);
                CityData generatedData = CreateRandomCityData(cityCounter, nameRecord);
                city.ApplyGeneratedData(generatedData);
                _cityDataById[generatedData.Id] = generatedData;
                cityCounter++;
            }
        }

        private CityNameRecord ResolveNameRecord(int cityIndex)
        {
            if (_cityNames.Count == 0)
            {
                return new CityNameRecord
                {
                    Id = $"{cityIdPrefix}-{cityIndex + 1}",
                    Name = $"City {cityIndex + 1}",
                    Description = "Procedurally generated city."
                };
            }

            CityNameRecord seededRecord = _cityNames[cityIndex % _cityNames.Count];
            if (!string.IsNullOrWhiteSpace(seededRecord.Id))
            {
                return seededRecord;
            }

            seededRecord.Id = $"{cityIdPrefix}-{cityIndex + 1}";
            return seededRecord;
        }

        private static CityData CreateRandomCityData(int cityIndex, CityNameRecord record)
        {
            TerrainType terrain = (TerrainType)UnityEngine.Random.Range(0, Enum.GetValues(typeof(TerrainType)).Length);
            StatRangeProfile profile = TerrainProfiles[terrain];

            CityState state = new CityState
            {
                Terrain = terrain,
                DefenseMultiplier = UnityEngine.Random.Range(profile.DefenseMin, profile.DefenseMax),
                FarmingMultiplier = UnityEngine.Random.Range(profile.FarmingMin, profile.FarmingMax),
                MiningMultiplier = UnityEngine.Random.Range(profile.MiningMin, profile.MiningMax)
            };

            CityData data = ScriptableObject.CreateInstance<CityData>();
            data.Id = string.IsNullOrWhiteSpace(record.Id) ? $"city-{cityIndex + 1}" : record.Id;
            data.Name = string.IsNullOrWhiteSpace(record.Name) ? $"City {cityIndex + 1}" : record.Name;
            data.Description = string.IsNullOrWhiteSpace(record.Description)
                ? "Generated city data."
                : record.Description;
            data.SetCityState(state);
            state.CityData = data;

            return data;
        }

        [Serializable]
        private class CityNameRecord
        {
            public string Id;
            public string Name;
            public string Description;
        }

        private readonly struct StatRangeProfile
        {
            public readonly float DefenseMin;
            public readonly float DefenseMax;
            public readonly float FarmingMin;
            public readonly float FarmingMax;
            public readonly float MiningMin;
            public readonly float MiningMax;

            public StatRangeProfile(float defenseMin, float defenseMax, float farmingMin, float farmingMax, float miningMin, float miningMax)
            {
                DefenseMin = defenseMin;
                DefenseMax = defenseMax;
                FarmingMin = farmingMin;
                FarmingMax = farmingMax;
                MiningMin = miningMin;
                MiningMax = miningMax;
            }
        }

        private static class CsvParser
        {
            public static List<string[]> Parse(string csv)
            {
                List<string[]> rows = new List<string[]>();
                if (string.IsNullOrWhiteSpace(csv))
                {
                    return rows;
                }

                string[] lines = csv.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < lines.Length; i++)
                {
                    rows.Add(ParseLine(lines[i]));
                }

                return rows;
            }

            private static string[] ParseLine(string line)
            {
                List<string> fields = new List<string>();
                StringBuilder current = new StringBuilder();
                bool isQuoted = false;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];

                    if (c == '"')
                    {
                        if (isQuoted && i + 1 < line.Length && line[i + 1] == '"')
                        {
                            current.Append('"');
                            i++;
                        }
                        else
                        {
                            isQuoted = !isQuoted;
                        }

                        continue;
                    }

                    if (c == ',' && !isQuoted)
                    {
                        fields.Add(current.ToString());
                        current.Clear();
                        continue;
                    }

                    current.Append(c);
                }

                fields.Add(current.ToString());
                return fields.ToArray();
            }
        }
    }
}
