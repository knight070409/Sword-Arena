using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections;

/// <summary>
/// Loads assets using Unity Addressables system
/// </summary>
public class AddressablesLoader : MonoBehaviour
{
    [Header("Addressable Settings")]
    [SerializeField] private string addressableKey = "SwordEffect"; // Key/Label for addressable asset
    [SerializeField] private Transform spawnLocation;
    [SerializeField] private bool loadOnStart = true;

    [Header("Error Handling")]
    [SerializeField] private GameObject errorPanel;
    [SerializeField] private UnityEngine.UI.Button retryButton;

    private GameObject loadedObject;
    private AsyncOperationHandle<GameObject> loadHandle;

    private void Start()
    {
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(RetryLoad);
        }

        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }

        if (loadOnStart)
        {
            LoadAddressableAsync();
        }
    }

    /// <summary>
    /// Load addressable asset asynchronously
    /// </summary>
    public void LoadAddressableAsync()
    {
        Debug.Log($"Loading Addressable: {addressableKey}");

        // Load asset asynchronously
        loadHandle = Addressables.LoadAssetAsync<GameObject>(addressableKey);
        loadHandle.Completed += OnAddressableLoaded;
    }

    private void OnAddressableLoaded(AsyncOperationHandle<GameObject> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"Addressable loaded successfully: {addressableKey}");

            // Instantiate the loaded prefab
            Vector3 spawnPos = spawnLocation != null ? spawnLocation.position : Vector3.zero;
            loadedObject = Instantiate(handle.Result, spawnPos, Quaternion.identity);

            Debug.Log("Addressable object instantiated in scene");

            if (errorPanel != null)
            {
                errorPanel.SetActive(false);
            }
        }
        else
        {
            Debug.LogError($"Failed to load Addressable: {addressableKey}. Error: {handle.OperationException}");
            ShowError();
        }
    }

    private void ShowError()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(true);
        }
    }

    private void RetryLoad()
    {
        if (errorPanel != null)
        {
            errorPanel.SetActive(false);
        }

        // Clean up previous handle if exists
        if (loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
        }

        // Retry loading
        LoadAddressableAsync();
    }

    private void OnDestroy()
    {
        // Clean up: Release the addressable handle
        if (loadHandle.IsValid())
        {
            Addressables.Release(loadHandle);
        }

        // Destroy the instantiated object
        if (loadedObject != null)
        {
            Destroy(loadedObject);
        }
    }

    /// <summary>
    /// Public method to instantiate loaded addressable at specific position
    /// </summary>
    public GameObject InstantiateAddressable(Vector3 position, Quaternion rotation)
    {
        if (loadHandle.IsValid() && loadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            return Instantiate(loadHandle.Result, position, rotation);
        }

        Debug.LogWarning("Addressable not loaded yet!");
        return null;
    }
}