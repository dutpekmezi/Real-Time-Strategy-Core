using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Utils.Logger;
using Utils.Signal;

namespace Utils.Scene
{
    public class SceneService : ISceneService
    {
        private Dictionary<string, GameObject> _loadedScenes = new Dictionary<string, GameObject>();
        private SceneServiceSettings _settings;

        public Dictionary<string, GameObject> LoadedScenes => _loadedScenes;

        public static SceneService Instance { get; private set; }

        public SceneService(SceneServiceSettings settings)
        {
            if (Instance != null)
                throw new System.Exception("Scene Service Already Has an Instance");

            Instance = this;

            this._settings = settings;
        }

        public async Task LoadStartupScenes()
        {
            if (_settings == null || _settings.SceneConfigs == null || _settings.SceneConfigs.Count == 0)
            {
                GameLogger.LogWarning("[SceneService] SceneServiceSettings is empty. Startup scenes will not be loaded.");
                return;
            }

            var baseSceneIndex = _settings.SceneConfigs.FindIndex(config => config.SceneKey == SceneKeys.InitialScene);
            if (baseSceneIndex < 0)
            {
                GameLogger.LogWarning("[SceneService] Base scene config not found. Startup scenes will not be loaded.");
                return;
            }

            var baseSceneConfig = _settings.SceneConfigs[baseSceneIndex];
            if (!_loadedScenes.ContainsKey(baseSceneConfig.SceneKey))
            {
                await LoadScene(baseSceneConfig.SceneKey);
            }

            var nextSceneIndex = baseSceneIndex + 1;
            if (nextSceneIndex >= _settings.SceneConfigs.Count)
            {
                return;
            }

            var nextSceneConfig = _settings.SceneConfigs[nextSceneIndex];
            if (!_loadedScenes.ContainsKey(nextSceneConfig.SceneKey))
            {
                await LoadScene(nextSceneConfig.SceneKey);
            }
        }

        public void Clear()
        {
            foreach (var scene in _loadedScenes)
            {
                ISceneObject sceneObject = scene.Value.GetComponent<ISceneObject>();

                if (sceneObject != null)
                {
                    _= sceneObject.Clear();
                }

                GameObject.Destroy(scene.Value);

                var config = _settings.GetSceneConfig(scene.Key);

                if (config != null)
                {
                    config.SceneReference.ReleaseAsset();
                }
            }

            _loadedScenes.Clear();
        }

        public async Task<GameObject> LoadScene(string sceneKey)
        {
            try
            {
                var config = _settings.GetSceneConfig(sceneKey);

                SignalBus.Get<OnSceneTransitionStarted>().Invoke(config);

                if (config.RemoveAllOtherScenes)
                {
                    Clear();
                }

                // Find prefab
                var sceneGameobject = await LoadSceneResource(sceneKey);

                if (sceneGameobject == null)
                {
                    GameLogger.LogError($"Scene '{sceneGameobject.name}' not found!");
                    return null;
                }

                // Instantiate
                var currentScene = GameObject.Instantiate(sceneGameobject);
                _loadedScenes.Add(sceneKey, currentScene);

                ISceneObject sceneObject = currentScene.GetComponent<ISceneObject>();
                if (sceneObject != null)
                {
                    await sceneObject.Initialize();
                }

                SignalBus.Get<OnSceneTransitionEnded>().Invoke(config);

                return currentScene;
            }
            catch (System.Exception e)
            {
                GameLogger.Log(e.ToString());
                return null;
            }
        }

        public async Task RemoveScene(string scene)
        {
            try
            {
                if (_loadedScenes.TryGetValue(scene, out var sceneGO))
                {
                    ISceneObject sceneObject = sceneGO.GetComponent<ISceneObject>();

                    if (sceneObject != null)
                    {
                        await sceneObject.Clear();
                    }

                    GameObject.Destroy(sceneGO);

                    var config = _settings.GetSceneConfig(scene);

                    if (config != null)
                    {
                        config.SceneReference.ReleaseAsset();
                    }

                    _loadedScenes.Remove(scene);
                }
            }
            catch (System.Exception e)
            {
                GameLogger.Log(e.Message);
            }
        }

        private async Task<GameObject> LoadSceneResource(string sceneKey)
        {
            var config = _settings.GetSceneConfig(sceneKey);

            if (config != null)
            {
                return await config.SceneReference.LoadAssetAsync<GameObject>().Task;
            }
            else
            {
                return null;
            }
        }
    }
}
