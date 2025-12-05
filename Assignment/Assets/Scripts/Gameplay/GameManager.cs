using UnityEngine;
using Photon.Pun;

namespace GapeLabs.Gameplay
{
    /// <summary>
    /// Manages game state and player spawning
    /// </summary>
    public class GameManager : MonoBehaviourPunCallbacks
    {
        [Header("Spawn Settings")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private string playerPrefabName = "Player"; // Name in Resources folder

        [Header("References")]
        [SerializeField] private GapeLabs.UI.MobileInputManager mobileInput;
        [SerializeField] private RoundManager roundManager; // NEW: Reference to RoundManager

        private bool hasSpawned = false;

        private void Start()
        {
            // IMPORTANT: Don't spawn immediately, wait for scene to be fully loaded
            if (PhotonNetwork.IsConnectedAndReady && !hasSpawned)
            {
                // Small delay to ensure all clients are ready
                Invoke(nameof(SpawnPlayer), 0.5f);
            }
        }

        // NEW: public helper for any script (PlayerController) to get a spawn
        public Vector3 GetRandomSpawnPoint()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                int randomIndex = Random.Range(0, spawnPoints.Length);
                return spawnPoints[randomIndex].position;
            }

            // fallback if no spawn points set
            return new Vector3(
                Random.Range(-5f, 5f),
                1f,
                Random.Range(-5f, 5f)
            );
        }

        private void SpawnPlayer()
        {
            // Prevent multiple spawns
            if (hasSpawned) return;
            hasSpawned = true;

            // Get random spawn position
            Vector3 spawnPosition =GetRandomSpawnPoint();


            // Spawn player prefab over network
            GameObject playerObject = PhotonNetwork.Instantiate(
                playerPrefabName,
                spawnPosition,
                Quaternion.identity
            );

            // Connect to mobile input
            if (mobileInput != null)
            {
                PlayerController player = playerObject.GetComponent<PlayerController>();
                mobileInput.SetLocalPlayer(player);
            }

            // Connect camera to follow local player
            ThirdPersonCamera camera = Camera.main.GetComponent<ThirdPersonCamera>();
            if (camera != null)
            {
                camera.SetTarget(playerObject.transform);
                Debug.Log("[GameManager] Camera set to follow local player");
            }

            //Debug.Log($"Player spawned: {PhotonNetwork.NickName}");
        }

        public override void OnPlayerEnteredRoom(Photon.Realtime.Player newPlayer)
        {
            Debug.Log($"Player joined game: {newPlayer.NickName}");
        }

        public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
        {
            Debug.Log($"Player left game: {otherPlayer.NickName}");
        }
    }
}