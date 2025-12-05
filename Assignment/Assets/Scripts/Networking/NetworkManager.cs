using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

namespace GapeLabs.Networking
{
    /// <summary>
    /// Handles Photon connection, room creation/joining, and player spawning
    /// </summary>
    public class NetworkManager : MonoBehaviourPunCallbacks
    {
        [Header("UI References")]
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private GameObject errorPanel;
        [SerializeField] private Button retryButton;
        [SerializeField] private TextMeshProUGUI errorMessageText;

        [Header("Settings")]
        [SerializeField] private string waitingRoomSceneName = "WaitingRoomScene";
        [SerializeField] private byte maxPlayersPerRoom = 4;

        private const string PLAYER_ID_KEY = "PlayerID";
        private string playerID;

        private void Start()
        {
            // Generate or load PlayerID from PlayerPrefs
            if (!PlayerPrefs.HasKey(PLAYER_ID_KEY))
            {
                // Generate random number between 1-4 with proper formatting
                int randomNum = Random.Range(1, 5);
                playerID = "Player" + randomNum.ToString("00"); // Formats as "01", "02", etc.
                PlayerPrefs.SetString(PLAYER_ID_KEY, playerID);
                PlayerPrefs.Save();
                //Debug.Log($"[NetworkManager] Generated new PlayerID: {playerID}");
            }
            else
            {
                playerID = PlayerPrefs.GetString(PLAYER_ID_KEY);
            }

            PhotonNetwork.NickName = playerID;

            // Enable automatic scene synchronization
            PhotonNetwork.AutomaticallySyncScene = true;
            //Debug.Log("[NetworkManager] AutomaticallySyncScene enabled");

            // Configure network update rates
            PhotonNetwork.SendRate = 20;           // Send 20 times per second
            PhotonNetwork.SerializationRate = 10;  // Serialize 10 times per second
            //Debug.Log("[OPTIMIZATION] Network rates configured: SendRate=20, SerializationRate=10");

            // Setup UI
            playButton.onClick.AddListener(OnPlayButtonClicked);
            retryButton.onClick.AddListener(RetryConnection);
            errorPanel.SetActive(false);

            // Disconnect if coming back from waiting room
            if (PhotonNetwork.IsConnected)
            {
                //Debug.Log("[NetworkManager] Already connected, disconnecting first...");
                PhotonNetwork.Disconnect();
            }

            UpdateStatusText("Ready to play!");
        }

        public void OnPlayButtonClicked()
        {
            playButton.interactable = false;
            UpdateStatusText("Connecting to server...");

            // Only connect if not already connected
            if (!PhotonNetwork.IsConnected)
            {
                PhotonNetwork.ConnectUsingSettings();
            }
            else
            {
                // If already connected but in launcher, disconnect first
                //Debug.Log("[NetworkManager] Already connected, reconnecting...");
                PhotonNetwork.Disconnect();
                // Will reconnect in OnDisconnected callback
            }
        }

        #region Photon Callbacks

        public override void OnConnectedToMaster()
        {
            //Debug.Log("Connected to Master Server");
            UpdateStatusText("Connected! Finding match...");
            JoinOrCreateRoom();
        }

        public override void OnJoinedLobby()
        {
            //Debug.Log("Joined Lobby");
            UpdateStatusText("In lobby, searching for rooms...");
        }

        public override void OnJoinedRoom()
        {
            //Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}");
            UpdateStatusText($"Joined room with {PhotonNetwork.CurrentRoom.PlayerCount} players");

            // Load waiting room scene for all clients
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel(waitingRoomSceneName);
            }
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            //Debug.Log("No rooms available, creating new room...");
            UpdateStatusText("Creating new room...");

            // Create a new room with random name
            RoomOptions roomOptions = new RoomOptions
            {
                MaxPlayers = maxPlayersPerRoom,
                IsVisible = true,
                IsOpen = true
            };

            PhotonNetwork.CreateRoom(null, roomOptions);
        }

        public override void OnCreatedRoom()
        {
            //Debug.Log($"Room created: {PhotonNetwork.CurrentRoom.Name}");
            UpdateStatusText("Room created! Waiting for players...");
        }

        public override void OnCreateRoomFailed(short returnCode, string message)
        {
            //Debug.LogError($"Room creation failed: {message}");
            ShowError($"Failed to create room: {message}");
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.Log($"Disconnected from Photon: {cause}");

            // Only show error if it wasn't an intentional disconnect
            if (cause != DisconnectCause.DisconnectByClientLogic)
            {
                ShowError($"Connection lost: {cause}");
            }
            else
            {
                // User clicked play button after leaving room
                // Reconnect automatically
                if (playButton != null && !playButton.interactable)
                {
                    UpdateStatusText("Reconnecting...");
                    PhotonNetwork.ConnectUsingSettings();
                }
                else
                {
                    // Just disconnected to reset, enable play button
                    playButton.interactable = true;
                    UpdateStatusText("Ready to play!");
                }
            }
        }

        #endregion

        private void JoinOrCreateRoom()
        {
            // Try to join any available room, or create one if none exist
            PhotonNetwork.JoinRandomRoom();
        }

        private void RetryConnection()
        {
            errorPanel.SetActive(false);
            playButton.interactable = true;
            UpdateStatusText("Ready to play!");

            // Disconnect if already connected
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }

        private void UpdateStatusText(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
            Debug.Log($"Status: {message}");
        }

        private void ShowError(string errorMessage)
        {
            playButton.interactable = true;
            errorPanel.SetActive(true);
            errorMessageText.text = errorMessage;
            UpdateStatusText("Connection failed");
        }

        private void OnApplicationQuit()
        {
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
            }
        }
    }
}