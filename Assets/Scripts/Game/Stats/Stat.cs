using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace Game.Stat
{
    [System.Serializable]
    public class Stat
    {
        public StatType Type;

        [SerializeField] private float baseValue;
        public float BaseValue
        {
            get { return baseValue; }
            set
            {
                if (baseValue != value)
                {
                    baseValue = value;
                    isDirty = true;
                }
            }
        }

        private bool isDirty = true;
        private float calculatedValue;
        public float Value
        {
            get
            {
                if (isDirty)
                {
                    calculatedValue = CalculateFinalValue();
                    isDirty = false;
                }
                return calculatedValue;
            }
        }

        private readonly List<StatModifier> statModifiers;
        public readonly ReadOnlyCollection<StatModifier> Modifiers;

        public Stat(BaseStatConfig statConfig)
        {
            Initialize(statConfig);
            statModifiers = new List<StatModifier>();
            Modifiers = statModifiers.AsReadOnly();
        }

        private void Initialize(BaseStatConfig statConfig)
        {
            this.baseValue = statConfig.BaseStat.BaseValue;
            Type = statConfig.BaseStat.Type;
        }

        public void AddModifier(StatModifier mod)
        {
            statModifiers.Add(mod);
            isDirty = true;
        }

        public bool RemoveModifier(StatModifier mod)
        {
            if (statModifiers.Remove(mod))
            {
                isDirty = true;
                return true;
            }
            return false;
        }

        public bool RemoveAllModifiersFromSource(object source)
        {
            int numRemoved = statModifiers.RemoveAll(mod => mod.Source == source);

            if (numRemoved > 0)
            {
                isDirty = true;
                return true;
            }
            return false;
        }

        private float CalculateFinalValue()
        {
            float finalValue = baseValue;

            statModifiers.Sort((a, b) => a.Operation.CompareTo(b.Operation));

            float sumPercentAdd = 0;

            for (int i = 0; i < statModifiers.Count; i++)
            {
                StatModifier mod = statModifiers[i];

                if (mod.Operation == ModifierOperation.FlatAdd)
                {
                    finalValue += mod.Value;
                }
                else if (mod.Operation == ModifierOperation.PercentMultiply)
                {
                    sumPercentAdd += mod.Value;
                }
            }

            finalValue *= (1 + sumPercentAdd);

            finalValue = (float)Mathf.Round(finalValue * 100) * 0.01f;

            if (StatSystem.Instance != null)
            {
                finalValue = StatSystem.Instance.ClampStatValue(Type, finalValue);
            }
            else
                Debug.Log($"Stat sistem yok haci");

            return finalValue;
        }
    }

    public enum StatType
    {
        MaxHealth,
        MoveSpeed,
        BodyDamage,
        WeaponOrbitSpeed,
        WeaponOrbitRadius,
        WeaponSelfOrbitSpeed,
        CooldownReduction,
        AbilityCooldown,
        Energy,
        HealthRegen,
        EnergyRegen,
        LifeSteel,
        ExpToLevelUp,
        ExpOnDeath,
        Scale,
        PushForce,
        Difficulty
    }

    public enum StatTarget
    {
        Entity,
        Weapon,
        Wave
    }

    [Serializable]
    public class BaseStatConfig
    {
        public Stat BaseStat;
        public bool IsUpgradable = true;

        public BaseStatConfig(Stat stat)
        {
            BaseStat = stat;
        }
    }

    public static class StatTypeExtensions
    {
        public static string GetName(this StatType statType)
        {
            string name = statType.ToString();

            string result = "";
            foreach (char c in name)
            {
                if (char.IsUpper(c) && result.Length > 0 && result[result.Length - 1] != ' ')
                {
                    result += " ";
                }
                result += c;
            }

            return result.Trim();
        }
    }
}
