using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

namespace GapeLabs.Networking
{
    /// <summary>
    /// Manages waiting room - requires minimum players before starting game
    /// </summary>
    public class WaitingRoomManager : MonoBehaviourPunCallbacks
    {
        [Header("Waiting Room Settings")]
        [SerializeField] private int minPlayersToStart = 2;
        [SerializeField] private float countdownTime = 5f; // Countdown before game starts

        [Header("UI References")]
        [SerializeField] private GameObject waitingRoomPanel;
        [SerializeField] private TextMeshProUGUI playerCountText;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI countdownText;


        [Header("Game Settings")]
        [SerializeField] private string gameSceneName = "GameScene";

        private bool isCountingDown = false;
        private float currentCountdown;

        private void Awake()
        {
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        private void Start()
        {
            // Show waiting room panel
            if (waitingRoomPanel != null)
            {
                waitingRoomPanel.SetActive(true);
            }

            if (PhotonNetwork.InRoom)
            {
                UpdateWaitingRoomUI();
                CheckPlayersAndStartCountdown();
            }
            else
            {
                if (statusText != null)
                    statusText.text = "Joining room...";
            }
        }

        private void Update()
        {
            // Handle countdown
            if (isCountingDown)
            {
                currentCountdown -= Time.deltaTime;

                if (countdownText != null)
                {
                    countdownText.text = $"Starting in {Mathf.Ceil(currentCountdown)}...";
                }

                if (currentCountdown <= 0)
                {
                    // stop countdown on ALL clients so it doesn't go negative
                    isCountingDown = false;

                    // only master actually starts the game (Photon will sync the scene)
                    if (PhotonNetwork.IsMasterClient)
                    {
                        StartGame();
                    }
                }
            }
        }

        #region Photon Callbacks

        public override void OnJoinedRoom()
        {
            //Debug.Log("[WaitingRoom] OnJoinedRoom");
            UpdateWaitingRoomUI();
            CheckPlayersAndStartCountdown();
        }

        public override void OnPlayerEnteredRoom(Player newPlayer)
        {
            //Debug.Log($"[WaitingRoom] Player joined: {newPlayer.NickName}");
            UpdateWaitingRoomUI();
            CheckPlayersAndStartCountdown();
        }

        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            //Debug.Log($"[WaitingRoom] Player left: {otherPlayer.NickName}");
            UpdateWaitingRoomUI();

            // Cancel countdown if not enough players
            if (PhotonNetwork.CurrentRoom.PlayerCount < minPlayersToStart)
            {
                CancelCountdown();
            }
        }

        #endregion

        /// <summary>
        /// Update the waiting room UI with current player count
        /// </summary>
        private void UpdateWaitingRoomUI()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

            int currentPlayers = PhotonNetwork.CurrentRoom.PlayerCount;
            int maxPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;

            // Update player count text
            if (playerCountText != null)
            {
                playerCountText.text = $"Players: {currentPlayers}/{maxPlayers}";
            }

            // Update status text
            if (statusText != null)
            {
                if (currentPlayers < minPlayersToStart)
                {
                    int needed = minPlayersToStart - currentPlayers;
                    statusText.text = $"Waiting for {needed} more player{(needed > 1 ? "s" : "")}...";
                }
                else if (isCountingDown)
                {
                    statusText.text = "Get ready!";
                }
                else
                {
                    statusText.text = "Starting soon...";
                }
            }
        }

        /// <summary>
        /// Check if enough players and start countdown
        /// </summary>
        private void CheckPlayersAndStartCountdown()
        {
            if (PhotonNetwork.CurrentRoom.PlayerCount >= minPlayersToStart && !isCountingDown)
            {
                // Only master client starts countdown
                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RPC("StartCountdown", RpcTarget.All);
                }
            }
        }

        /// <summary>
        /// Start the countdown (synchronized across all clients)
        /// </summary>
        [PunRPC]
        private void StartCountdown()
        {
            if (isCountingDown) return; // Already counting down

            isCountingDown = true;
            currentCountdown = countdownTime;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(true);
                countdownText.text = $"Starting in {Mathf.Ceil(currentCountdown)}...";
            }

            //Debug.Log("[WaitingRoom] Countdown started!");
        }

        /// <summary>
        /// Cancel the countdown if a player leaves
        /// </summary>
        private void CancelCountdown()
        {
            if (!isCountingDown) return;

            // Only master client can cancel
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RPC("CancelCountdownRPC", RpcTarget.All);
            }
        }

        [PunRPC]
        private void CancelCountdownRPC()
        {
            isCountingDown = false;

            if (countdownText != null)
            {
                countdownText.gameObject.SetActive(false);
            }

            UpdateWaitingRoomUI();
            //Debug.Log("[WaitingRoom] Countdown cancelled - not enough players");
        }

        /// <summary>
        /// Start the game (only master client loads the scene)
        /// </summary>
        private void StartGame()
        {
            if (!PhotonNetwork.IsMasterClient) return;

            //Debug.Log("[WaitingRoom] Starting game!");

            // Close the room so no new players can join during game
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.CurrentRoom.IsVisible = false;

            // IMPORTANT: Ensure automatic scene sync is enabled
            PhotonNetwork.AutomaticallySyncScene = true;

            // Load game scene for all clients
            PhotonNetwork.LoadLevel(gameSceneName);
        }

        /// <summary>
        /// Leave room button (optional)
        /// </summary>
        public void OnLeaveRoomButtonClicked()
        {
            //Debug.Log("[WaitingRoom] Leaving room...");

            // Leave the room and disconnect completely
            if (PhotonNetwork.InRoom)
            {
                PhotonNetwork.LeaveRoom();
            }
            else if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }

        public override void OnLeftRoom()
        {
            //Debug.Log("[WaitingRoom] Left room successfully");

            // Disconnect from Photon completely before returning to launcher
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
            else
            {
                // Already disconnected, go back to launcher
                ReturnToLauncher();
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            //Debug.Log($"[WaitingRoom] Disconnected: {cause}");

            // Return to launcher scene
            ReturnToLauncher();
        }

        private void ReturnToLauncher()
        {
            //Debug.Log("[WaitingRoom] Returning to launcher...");
            SceneManager.LoadScene("LauncherScene");
        }
    }
}