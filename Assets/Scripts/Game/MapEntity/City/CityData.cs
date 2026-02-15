using Game.Simulation;
using UnityEngine;

namespace Game.Entity
{
    [CreateAssetMenu(fileName = "CityData", menuName = "Game/Entity/City Data")]
    public class CityData : MapEntityData
    {
        [SerializeField] private CityState cityState = new();

        public CityState CityState => cityState;

        public void SetCityState(CityState state)
        {
            cityState = state ?? new CityState();
        }
    }
}
