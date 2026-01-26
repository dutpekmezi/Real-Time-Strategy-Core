using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Utils.Signal;

namespace Game.Stat
{
    public class StatSystem : BaseSystem
    {
        private readonly StatConfigData _statConfigData;

        public StatConfigData StatConfigData => _statConfigData;

        public static StatSystem Instance { get; private set; }

        public StatSystem(StatConfigData statConfigData)
        {
            Instance = this;

            _statConfigData = statConfigData;

            OnInitialize();
        }

        protected void OnInitialize()
        {
            if (_statConfigData != null)
            {
                _statConfigData.InitializeLookup();
            }
        }

        public StatConfig GetStatConfig(StatType statType)
        {
            if (_statConfigData == null)
            {
                return new StatConfig { Type = statType, Color = Color.white, Icon = null };
            }

            return _statConfigData.GetConfig(statType);
        }

        public StatType GetRandomStatType(List<StatType> availableTypes)
        {
            if (availableTypes == null || availableTypes.Count == 0)
            {
                var allTypes = System.Enum.GetValues(typeof(StatType)).Cast<StatType>().ToList();
                return allTypes[Random.Range(0, allTypes.Count)];
            }

            return availableTypes[Random.Range(0, availableTypes.Count)];
        }

        public StatModifier CreateModifier(StatType type, float value, ModifierOperation operation = ModifierOperation.FlatAdd, object source = null)
        {
            StatConfig config = GetStatConfig(type);

            return new StatModifier(value, operation, type, source);
        }

        public float GetDefaultModifierValue(StatType type, float scaleFactor = 1)
        {
            StatConfig config = GetStatConfig(type);

            if (config.DefaultOperation == ModifierOperation.FlatAdd)
            {
                return config.DirectValue > 0f
                    ? config.DirectValue
                    : config.BaseFlatValue + (config.BaseFlatValuePerLevel * scaleFactor);
            }

            float value = config.BasePercentValuePerLevel * scaleFactor;

            if (type == StatType.CooldownReduction)
            {
                value += 0.02f;
            }

            return value;
        }

        public List<StatType> GetUpgradableStatTypes()
        {
            if (_statConfigData == null)
            {
                return new List<StatType>();
            }

            return _statConfigData.StatConfigs
                .Where(config => config.IsUpgradable)
                .Select(config => config.Type)
                .ToList();
        }

        public float ClampStatValue(StatType statType, float currentValue)
        {
            StatConfig config = GetStatConfig(statType);

            if (config.ShouldClamp)
            {
                return Mathf.Clamp(currentValue, config.MinValue, config.MaxValue);
            }
            return currentValue;
        }
        public override void OnDispose()
        {
            throw new System.NotImplementedException();
        }

        protected override void Tick()
        {
            throw new System.NotImplementedException();
        }

        public class OnStatSelection : Signal { }

        public class OnStatSelected : Signal<StatModifier> { }
    }
}
