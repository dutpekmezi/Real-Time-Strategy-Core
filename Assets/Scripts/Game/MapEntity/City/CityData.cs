using Game.Simulation;

namespace Game.Entity
{
    [System.Serializable]
    public class CityData : MapEntityData
    {
        [UnityEngine.SerializeField] private CityState cityState = new();

        public CityState CityState => cityState;

        public void SetCityState(CityState state)
        {
            cityState = state ?? new CityState();
        }
    }
}
