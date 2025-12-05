using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

namespace GapeLabs.AssetBundles
{
    /// <summary>
    /// Downloads and loads Asset Bundles from cloud storage at runtime
    /// </summary>
    public class AssetBundleLoader : MonoBehaviour
    {
        [Header("Asset Bundle Settings")]
        [SerializeField] private string assetBundleURL = "https://drive.google.com/uc?export=download&id=1lus0k3M1UvAWuGEOag72F5SmZljqh9OQ";
        [SerializeField] private string assetNameToLoad = "BarrelProp"; // Name of asset inside bundle
        [SerializeField] private Transform spawnLocation;
        [SerializeField] private bool loadOnStart = true;

        [Header("Error Handling")]
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private UnityEngine.UI.Button retryButton;
        [SerializeField] private TMPro.TextMeshProUGUI statusText;

        private AssetBundle loadedBundle;
        private GameObject loadedObject;

        private void Start()
        {
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(RetryDownload);
            }

            if (errorPanel != null)
            {
                errorPanel.SetActive(false);
            }

            if (loadOnStart)
            {
                StartCoroutine(DownloadAndLoadAssetBundle());
            }
        }

        /// <summary>
        /// Download asset bundle from URL and load it
        /// </summary>

        public IEnumerator DownloadAndLoadAssetBundle()
        {
            UpdateStatus("Downloading Asset Bundle...");
            Debug.Log($"Downloading Asset Bundle from: {assetBundleURL}");

            using (UnityWebRequest request = UnityWebRequestAssetBundle.GetAssetBundle(assetBundleURL))
            {
                yield return request.SendWebRequest();

                // Check for errors
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Failed to download Asset Bundle: {request.error}");
                    UpdateStatus("Download failed!");
                    ShowError();
                    yield break;
                }

                UpdateStatus("Loading Asset Bundle...");

                // Get the AssetBundle directly from the download handler
                loadedBundle = DownloadHandlerAssetBundle.GetContent(request);

                if (loadedBundle == null)
                {
                    Debug.LogError("Failed to load Asset Bundle from request");
                    UpdateStatus("Bundle loading failed!");
                    ShowError();
                    yield break;
                }

                Debug.Log($"Asset Bundle loaded successfully. Contains: {loadedBundle.GetAllAssetNames().Length} assets");

                // Load the specific asset from bundle
                AssetBundleRequest assetRequest = loadedBundle.LoadAssetAsync<GameObject>(assetNameToLoad);
                yield return assetRequest;

                if (assetRequest.asset == null)
                {
                    Debug.LogError($"Failed to load asset '{assetNameToLoad}' from bundle");
                    UpdateStatus("Asset not found in bundle!");
                    ShowError();
                    yield break;
                }

                // Instantiate the loaded asset
                GameObject prefab = assetRequest.asset as GameObject;
                Vector3 spawnPos = spawnLocation != null ? spawnLocation.position : new Vector3(5, 1, 0);
                loadedObject = Instantiate(prefab, spawnPos, Quaternion.identity);

                UpdateStatus("Asset Bundle loaded successfully!");
                Debug.Log($"Asset '{assetNameToLoad}' instantiated from Asset Bundle");

                if (errorPanel != null)
                {
                    errorPanel.SetActive(false);
                }
            }
        }

        private void ShowError()
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(true);
            }
        }

        private void RetryDownload()
        {
            if (errorPanel != null)
            {
                errorPanel.SetActive(false);
            }

            // Clean up previous bundle
            if (loadedBundle != null)
            {
                loadedBundle.Unload(true);
                loadedBundle = null;
            }

            if (loadedObject != null)
            {
                Destroy(loadedObject);
            }

            // Retry download
            StartCoroutine(DownloadAndLoadAssetBundle());
        }

        private void UpdateStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"AssetBundle Status: {message}");
        }

        private void OnDestroy()
        {
            // Clean up: Unload asset bundle
            if (loadedBundle != null)
            {
                loadedBundle.Unload(true);
            }

            if (loadedObject != null)
            {
                Destroy(loadedObject);
            }
        }

        /// <summary>
        /// Public method to get all asset names in loaded bundle
        /// </summary>
        public string[] GetAllAssetNames()
        {
            if (loadedBundle != null)
            {
                return loadedBundle.GetAllAssetNames();
            }
            return new string[0];
        }
    }
}