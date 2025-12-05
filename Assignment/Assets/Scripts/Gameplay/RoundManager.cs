using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace GapeLabs.Gameplay
{
    /// <summary>
    /// Manages 3-round match system with scoring
    /// Winner = first player to die in each round
    /// After 3 rounds, player with most points wins
    /// </summary>
    public class RoundManager : MonoBehaviourPunCallbacks
    {
        [Header("Round Settings")]
        [SerializeField] private int totalRounds = 3;
        [SerializeField] private float roundStartDelay = 3f;
        [SerializeField] private float roundEndDelay = 3f;
        [SerializeField] private float roundUIHideDelay = 2f;

        [Header("UI References")]
        [SerializeField] private GameObject roundUI;
        [SerializeField] private TextMeshProUGUI roundNumberText;
        [SerializeField] private TextMeshProUGUI roundStatusText;
        [SerializeField] private GameObject scoreboard;
        [SerializeField] private Transform scoreboardContent;
        [SerializeField] private GameObject scoreItemPrefab;
        [SerializeField] private GameObject finalResultPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button exitButton;

        // Game state
        private int currentRound = 0;
        private bool roundActive = false;
        private bool matchEnded = false;

        // Player scores (stored in Photon Custom Properties)
        private const string SCORE_KEY = "score";

        private static RoundManager instance;
        public static RoundManager Instance => instance;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }

            // Verify PhotonView exists
            if (photonView == null)
            {
                Debug.LogError("[RoundManager] PhotonView component is missing! Add PhotonView to RoundManager GameObject!");
            }
            else
            {
                Debug.Log($"[RoundManager] PhotonView found - ViewID: {photonView.ViewID}");
            }
        }

        private void Start()
        {
            // Setup UI
            if (finalResultPanel != null)
                finalResultPanel.SetActive(false);

            if (playAgainButton != null)
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);

            if (exitButton != null)
                exitButton.onClick.AddListener(OnExitClicked);

            // Initialize scores for all players
            if (PhotonNetwork.IsMasterClient)
            {
                InitializePlayerScores();
                // Start first round after delay
                Invoke(nameof(StartNextRound), roundStartDelay);
            }

            UpdateScoreboard();
        }

        /// <summary>
        /// Initialize all players with 0 score
        /// </summary>
        private void InitializePlayerScores()
        {
            foreach (Player player in PhotonNetwork.PlayerList)
            {
                Hashtable props = new Hashtable
                {
                    { SCORE_KEY, 0 }
                };
                player.SetCustomProperties(props);
            }
        }

        /// <summary>
        /// Start the next round
        /// </summary>
        private void StartNextRound()
        {
            if (!PhotonNetwork.IsMasterClient || matchEnded) return;

            currentRound++;

            if (currentRound > totalRounds)
            {
                // Match ended, show results
                photonView.RPC("RPC_EndMatch", RpcTarget.All);
                return;
            }

            // Start round on all clients
            photonView.RPC("RPC_StartRound", RpcTarget.All, currentRound);
        }

        [PunRPC]
        private void RPC_StartRound(int roundNumber)
        {
            currentRound = roundNumber;
            roundActive = true;

            //Debug.Log($"[RoundManager] Round {currentRound} started!");

            // Update UI
            if (roundUI != null)
                roundUI.SetActive(true);

            if (roundNumberText != null)
                roundNumberText.text = $"ROUND {currentRound}/{totalRounds}";

            if (roundStatusText != null)
                roundStatusText.text = "FIGHT!";

            // Start timer to hide the round UI panel after a short delay
            StopAllCoroutines(); // make sure no old hide coroutine is running
            if (roundUIHideDelay > 0f && roundUI != null)
            {
                StartCoroutine(HideRoundUIAfterDelay());
            }

            // NEW: RESET EVERY PLAYER FOR THE NEW ROUND
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

            foreach (var p in players)
            {
                p.photonView.RPC(nameof(PlayerController.ResetForNewRound), RpcTarget.All);
            }

            // Enable player controls
            EnableAllPlayers();

            UpdateScoreboard();
        }

        private IEnumerator HideRoundUIAfterDelay()
        {
            yield return new WaitForSeconds(roundUIHideDelay);

            // Only hide if match hasn't ended (so final result panel can still show)
            if (!matchEnded && roundUI != null)
            {
                roundUI.SetActive(false);
            }
        }

        /// <summary>
        /// Called when a player dies (from PlayerController)
        /// </summary>

        public void OnPlayerDied(int killerActorNumber, int victimActorNumber, string victimName)
        {
            if (!roundActive || matchEnded) return;

            if (PhotonNetwork.IsMasterClient)
            {
                Player killer = PhotonNetwork.CurrentRoom.GetPlayer(killerActorNumber);

                if (killer != null && !killer.IsInactive)
                {
                    int currentScore = GetPlayerScore(killer);
                    Hashtable props = new Hashtable { { SCORE_KEY, currentScore + 1 } };
                    killer.SetCustomProperties(props);

                    //Debug.Log($"[RoundManager] {killer.NickName} killed {victimName}! {killer.NickName} gets 1 point!");

                    photonView.RPC("RPC_RoundEnd", RpcTarget.All, killerActorNumber, killer.NickName);
                }
                else
                {
                    Debug.LogWarning($"[RoundManager] Killer not found or inactive");
                }
            }
        }


        [PunRPC]
        private void RPC_RoundEnd(int winnerActorNumber, string winnerName)
        {
            roundActive = false;

            //Debug.Log($"[RoundManager] Round {currentRound} ended! Winner: {winnerName}");

            // Update UI
            if (roundUI != null)
                roundUI.SetActive(true);

            if (roundStatusText != null)
                roundStatusText.text = $"{winnerName} WINS ROUND {currentRound}!";

            // Stop any pending hide coroutine (we want the UI visible until next round)
            StopAllCoroutines();

            // Disable player controls
            DisableAllPlayers();

            UpdateScoreboard();

            // Master client starts next round after delay
            if (PhotonNetwork.IsMasterClient)
            {
                Invoke(nameof(StartNextRound), roundEndDelay);
            }
        }

        [PunRPC]
        private void RPC_EndMatch()
        {
            matchEnded = true;
            roundActive = false;

            Debug.Log("[RoundManager] Match ended!");

            // Ensure round UI is visible so we can show final info
            if (roundUI != null)
                roundUI.SetActive(true);

            // Stop any hide-round-UI coroutine
            StopAllCoroutines();

            // Calculate winner
            Player matchWinner = GetMatchWinner();

            // Show final results
            if (finalResultPanel != null)
            {
                finalResultPanel.SetActive(true);
            }

            if (winnerText != null)
            {
                if (matchWinner != null)
                {
                    winnerText.text = $"{matchWinner.NickName} WINS!\n{GetPlayerScore(matchWinner)} - {GetOpponentScore(matchWinner)}";
                }
                else
                {
                    winnerText.text = "DRAW!";
                }
            }

            // Disable players
            DisableAllPlayers();

            UpdateScoreboard();
        }

        /// <summary>
        /// Get player's current score
        /// </summary>
        private int GetPlayerScore(Player player)
        {
            if (player.CustomProperties.ContainsKey(SCORE_KEY))
            {
                return (int)player.CustomProperties[SCORE_KEY];
            }
            return 0;
        }

        /// <summary>
        /// Get opponent's score
        /// </summary>
        private int GetOpponentScore(Player player)
        {
            foreach (Player p in PhotonNetwork.PlayerList)
            {
                if (p.ActorNumber != player.ActorNumber)
                {
                    return GetPlayerScore(p);
                }
            }
            return 0;
        }

        /// <summary>
        /// Calculate match winner based on scores
        /// </summary>
        private Player GetMatchWinner()
        {
            Player winner = null;
            int highestScore = -1;
            bool isDraw = false;

            foreach (Player player in PhotonNetwork.PlayerList)
            {
                int score = GetPlayerScore(player);

                if (score > highestScore)
                {
                    highestScore = score;
                    winner = player;
                    isDraw = false;
                }
                else if (score == highestScore)
                {
                    isDraw = true;
                }
            }

            return isDraw ? null : winner;
        }

        /// <summary>
        /// Update scoreboard UI
        /// </summary>
        private void UpdateScoreboard()
        {
            if (scoreboard == null || scoreboardContent == null) return;

            // Clear existing items
            foreach (Transform child in scoreboardContent)
            {
                Destroy(child.gameObject);
            }

            // Sort players by score (descending)
            var sortedPlayers = PhotonNetwork.PlayerList.OrderByDescending(p => GetPlayerScore(p));

            // Create score items
            foreach (Player player in sortedPlayers)
            {
                GameObject item;

                if (scoreItemPrefab != null)
                {
                    item = Instantiate(scoreItemPrefab, scoreboardContent);
                }
                else
                {
                    // Fallback: create simple text
                    item = new GameObject("ScoreItem");
                    item.transform.SetParent(scoreboardContent);
                    item.AddComponent<TextMeshProUGUI>();
                }

                TextMeshProUGUI text = item.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    int score = GetPlayerScore(player);
                    text.text = $"{player.NickName}: {score}";
                    text.fontSize = 24;
                    text.color = player.IsLocal ? Color.green : Color.white;
                    text.alignment = TextAlignmentOptions.Center;
                }
            }
        }

        /// <summary>
        /// Enable all local player controls
        /// </summary>
        private void EnableAllPlayers()
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController player in players)
            {
                if (player.photonView.IsMine)
                {
                    player.EnableControls(true);
                }
            }
        }

        /// <summary>
        /// Disable all local player controls
        /// </summary>
        private void DisableAllPlayers()
        {
            PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (PlayerController player in players)
            {
                if (player.photonView.IsMine)
                {
                    player.EnableControls(false);
                }
            }
        }

        /// <summary>
        /// Check if round is currently active
        /// </summary>
        public bool IsRoundActive()
        {
            return roundActive;
        }

        /// <summary>
        /// Get current round number
        /// </summary>
        public int GetCurrentRound()
        {
            return currentRound;
        }

        /// <summary>
        /// Play again button
        /// </summary>
        private void OnPlayAgainClicked()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.LoadLevel("WaitingRoomScene");
            }
        }

        /// <summary>
        /// Exit to launcher
        /// </summary>
        private void OnExitClicked()
        {
            PhotonNetwork.LeaveRoom();
        }

        public override void OnLeftRoom()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("LauncherScene");
        }

        public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
        {
            // Update scoreboard when any player's properties change
            if (changedProps.ContainsKey(SCORE_KEY))
            {
                UpdateScoreboard();
            }
        }

        // FIXED: Handle player disconnect during match
        public override void OnPlayerLeftRoom(Player otherPlayer)
        {
            Debug.Log($"[RoundManager] Player left: {otherPlayer.NickName}");

            if (matchEnded) return;

            if (PhotonNetwork.IsMasterClient)
            {
                // If only 1 player remains in the room
                if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
                {
                    // Award remaining rounds to last player
                    Player lastPlayer = PhotonNetwork.LocalPlayer;
                    int remainingRounds = (totalRounds + 1) - currentRound;

                    if (remainingRounds > 0)
                    {
                        int currentScore = GetPlayerScore(lastPlayer);
                        Hashtable props = new Hashtable { { SCORE_KEY, currentScore + remainingRounds } };
                        lastPlayer.SetCustomProperties(props);
                    }

                    // End match
                    CancelInvoke(); // Cancel any pending round starts
                    Invoke(nameof(EndMatchAfterDisconnect), 0.5f); // Small delay to ensure score updates
                }
                else if (roundActive)
                {
                    // Check if remaining players are enough to continue
                    int alivePlayers = 0;
                    Player lastAlivePlayer = null;

                    PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                    foreach (var p in players)
                    {
                        if (!p.IsDead())
                        {
                            alivePlayers++;
                            lastAlivePlayer = p.photonView.Owner;
                        }
                    }

                    // If only 1 player remains alive in current round, they win the round
                    if (alivePlayers == 1 && lastAlivePlayer != null)
                    {
                        //Debug.Log($"[RoundManager] Only {lastAlivePlayer.NickName} alive - wins round!");

                        // Award point to last standing player
                        int currentScore = GetPlayerScore(lastAlivePlayer);
                        Hashtable props = new Hashtable { { SCORE_KEY, currentScore + 1 } };
                        lastAlivePlayer.SetCustomProperties(props);

                        photonView.RPC("RPC_RoundEnd", RpcTarget.All, lastAlivePlayer.ActorNumber, lastAlivePlayer.NickName);
                    }
                }
            }
        }

        /// <summary>
        /// End match after player disconnect (with delay for score sync)
        /// </summary>
        private void EndMatchAfterDisconnect()
        {
            if (!PhotonNetwork.IsMasterClient || matchEnded) return;

            //Debug.Log("[RoundManager] Ending match after disconnect");
            photonView.RPC("RPC_EndMatch", RpcTarget.All);
        }
    }
}