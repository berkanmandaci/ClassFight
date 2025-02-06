using System.Collections.Generic;
using System.Linq;
using _Project.Core.Scripts;
using _Project.Runtime.Core.Extensions.Singleton;
using Cysharp.Threading.Tasks;
using ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;
namespace ProjectV3.Client._ProjectV3.Runtime.Client.Scripts.Core
{
    public class UIManager : SingletonBehaviour<UIManager>
    {
        [SerializeField] private List<GameObject> uiLayersList;

        public Dictionary<UILayerKey, Transform> UILayers;
        private Dictionary<string, BaseUIScreenView> uiElements = new Dictionary<string, BaseUIScreenView>();

        // Stack to keep track of the UI navigation history
        private Stack<BaseUIScreenView> uiHistoryStack = new Stack<BaseUIScreenView>();

        public void Init()
        {
            UILayers= new Dictionary<UILayerKey, Transform>();
            UILayers = uiLayersList.ToDictionary(x => x.GetComponent<UILayer>().Key, x => x.transform);
        }

        private async UniTask<BaseUIScreenView> InitializeUI(string prefabName)
        {
            if (uiElements.TryGetValue(prefabName, out BaseUIScreenView screenGo))
            {
                Debug.LogWarning($"UI Element {prefabName} is already initialized.");
                return screenGo;
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<GameObject>(prefabName);
                GameObject uiPrefab = await handle.Task;

                if (uiPrefab != null)
                {
                    GameObject uiInstance = Instantiate(uiPrefab);
                    var baseUIScreenView = uiInstance.GetComponent<BaseUIScreenView>();
                    baseUIScreenView.AddressableKey = prefabName;
                    uiElements[prefabName] = baseUIScreenView;

                    Debug.Log($"UI Element {prefabName} initialized successfully.");



                    if (UILayers.TryGetValue(baseUIScreenView.UILayerKey, out Transform layerTransform))
                    {
                        uiInstance.transform.SetParent(layerTransform, false);
                        uiInstance.transform.localPosition = Vector3.zero;
                    }

                    return baseUIScreenView;
                }
                else
                {
                    Debug.LogError("Failed to initialize UI prefab: Prefab is null.");
                    return null;
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to load UI prefab: {ex.Message}");
                return null;
            }
        }

        // Method to open the UI
        public async UniTask<BaseUIScreenView> OpenUI(string uiName)
        {
            if (!uiElements.TryGetValue(uiName, out BaseUIScreenView screenGo))
            {
                screenGo = await InitializeUI(uiName);
                if (screenGo == null)
                {
                    Debug.LogError($"Failed to open UI Element {uiName}: Prefab could not be loaded or initialized.");
                    return null;
                }
            }

            // Instantiate and display the new UI screen


            var baseUIScreenView = screenGo.GetComponent<BaseUIScreenView>();

            // Check if the current layer has an active screen and manage the history stack
            if (UILayers.TryGetValue(baseUIScreenView.UILayerKey, out Transform layerTransform))
            {

                screenGo.transform.SetParent(layerTransform, false);
                if (uiHistoryStack.Count > 0)
                {
                    var previousScreen = uiHistoryStack.Peek();
                    if (previousScreen.transform.parent == layerTransform)
                    {
                        previousScreen.gameObject.SetActive(false);
                    }

                }

            }

            uiHistoryStack.Push(screenGo);
            screenGo.gameObject.SetActive(true);
            return screenGo;
        }

        // Method to close the UI
        public void CloseUI(string uiName)
        {
            if (uiElements.TryGetValue(uiName, out BaseUIScreenView screenGo))
            {
                if (screenGo != null)
                {
                    CloseCurrentScreen();
                    Debug.Log($"UI Element {uiName} closed successfully.");
                }
                else
                {
                    Debug.LogWarning($"UI Element {uiName} is not currently active.");
                }
            }
            else
            {
                Debug.LogError("UI Element not found.");
            }
        }

        // Method to go back to the previous UI screen
        public void Back()
        {
            if (uiHistoryStack.Count > 1)
            {
                // Close the current active screen

                CloseCurrentScreen();

                // Re-enable the previous screen
                var previousScreen = uiHistoryStack.Peek();
                previousScreen.gameObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning("No previous UI screen to go back to.");
            }
        }
        private void CloseCurrentScreen()
        {
            var currentScreen = uiHistoryStack.Pop();
            uiElements.Remove(currentScreen.AddressableKey);
            Destroy(currentScreen.gameObject);
        }
    }
}
