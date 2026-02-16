using System.Text;
using Game.Entity;
using Game.Simulation;
using UnityEngine;

public class City : Entity
{
    [SerializeField] private string cityId;
    [SerializeField] private TerrainType terrainType;
    [SerializeField] private float defenseMultiplier = 1f;
    [SerializeField] private float farmingMultiplier = 1f;
    [SerializeField] private float miningMultiplier = 1f;

    public string CityId => cityId;

    public TerrainType TerrainType => terrainType;

    public float DefenseMultiplier => defenseMultiplier;

    public float FarmingMultiplier => farmingMultiplier;

    public float MiningMultiplier => miningMultiplier;

    public void ApplyGeneratedData(CityData generatedData)
    {
        if (generatedData == null)
        {
            return;
        }

        entityData = generatedData;
        cityId = generatedData.Id;
        terrainType = generatedData.CityState.Terrain;
        defenseMultiplier = generatedData.CityState.DefenseMultiplier;
        farmingMultiplier = generatedData.CityState.FarmingMultiplier;
        miningMultiplier = generatedData.CityState.MiningMultiplier;
    }

    public override string GetSelectionDetails()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine($"{DisplayName} (ID: {cityId})");
        sb.AppendLine(DisplayDescription);
        sb.AppendLine($"Terrain: {terrainType}");
        sb.AppendLine($"Defence x{defenseMultiplier:0.00}");
        sb.AppendLine($"Farming x{farmingMultiplier:0.00}");
        sb.Append($"Mining x{miningMultiplier:0.00}");
        return sb.ToString();
    }
}
