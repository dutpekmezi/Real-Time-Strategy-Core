using UnityEngine;
using System.Collections.Generic;

namespace Game.Stat
{
    [System.Serializable]
    public struct StatConfig
    {
        public StatType Type;
        public Color Color;
        public Sprite Icon;

        public bool IsUpgradable;
        public bool IsCollectable;


        [Header("Modifier Defaults")]
        public ModifierOperation DefaultOperation;
        public float BaseFlatValuePerLevel;
        public float BasePercentValuePerLevel;
        public float BaseFlatValue;
        public float FlatAddChance;
        public float DirectValue;

        [Header("Clamping")]
        public bool ShouldClamp;
        public float MinValue;
        public float MaxValue;

        public StatTarget Target;
    }

    [CreateAssetMenu(fileName = "StatConfigData", menuName = "Game/Scriptable Objects/Stat/Stat Config Data")]
    public class StatConfigData : ScriptableObject
    {
        public List<StatConfig> StatConfigs;

        public int SelectableStatCount;

        private Dictionary<StatType, StatConfig> _configLookup;

        private static readonly StatConfig DefaultConfig = new StatConfig { Type = 0, Color = Color.white, Icon = null };

        public void InitializeLookup()
        {
            if (_configLookup == null)
            {
                _configLookup = new Dictionary<StatType, StatConfig>();

                foreach (var item in StatConfigs)
                {
                    if (!_configLookup.ContainsKey(item.Type))
                    {
                        _configLookup.Add(item.Type, item);
                    }
                    else
                    {
                        Debug.LogWarning($"Duplicate StatType found in StatConfigData: {item.Type}. Skipping duplicate entry.");
                    }
                }
            }
        }

        public StatConfig GetConfig(StatType type)
        {
            if (_configLookup == null) InitializeLookup();

            if (_configLookup.TryGetValue(type, out StatConfig config))
            {
                return config;
            }

            return DefaultConfig;
        }
    }
}
