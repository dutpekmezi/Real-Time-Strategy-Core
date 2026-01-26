namespace Game.Stat
{
    public enum ModifierOperation
    {
        FlatAdd = 0,
        PercentMultiply = 1
    }

    [System.Serializable]
    public class StatModifier
    {
        public readonly StatType Type;
        public readonly StatTarget Target;

        public readonly float Value;
        public readonly ModifierOperation Operation;
        public readonly object Source;

        public StatModifier(float value, ModifierOperation operation, StatType type)
        {
            Value = value;
            Operation = operation;
            Source = null;
            Type = type;
        }

        public StatModifier(float value, ModifierOperation operation, StatType type, object source, StatTarget target)
        {
            Value = value;
            Operation = operation;
            Source = null;
            Type = type;
            Target = target;
        }

        public StatModifier(float value, ModifierOperation operation, StatType type, object source)
        {
            Value = value;
            Operation = operation;
            Source = source;
            Type = type;
        }
    }
}