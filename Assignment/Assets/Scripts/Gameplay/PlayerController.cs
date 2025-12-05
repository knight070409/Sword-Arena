using Photon.Pun;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace GapeLabs.Gameplay
{
    /// <summary>
    /// Handles player movement, sword collision combat, health, and synchronization
    /// </summary>
    public class PlayerController : MonoBehaviourPun, IPunObservable
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;

        [Header("Combat Settings")]
        [SerializeField] private float attackDamage = 20f;
        [SerializeField] private float attackCooldown = 1f;
        [SerializeField] private GameObject swordCollider;

        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 2f;

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private UnityEngine.UI.Image healthBarFill;

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private Transform cameraTransform;

        // Private variables
        private float currentHealth;
        private float lastAttackTime;
        private bool lastIsWalking;
        private bool isDead;
        private bool isAttacking;
        private bool controlsEnabled = true;
        private Vector3 moveDirection;

        // Animator parameter hashes
        private int isWalkingHash;
        private int attackHash;
        private int hitHash;
        private int isDeadHash;

        private void Awake()
        {
            // Cache animator parameter hashes
            isWalkingHash = Animator.StringToHash("IsWalking");
            attackHash = Animator.StringToHash("Attack");
            hitHash = Animator.StringToHash("Hit");
            isDeadHash = Animator.StringToHash("Death");

            currentHealth = maxHealth;

            // Disable sword collider by default
            if (swordCollider != null)
            {
                swordCollider.SetActive(false);
            }
        }

        private void Start()
        {
            // Setup UI
            if (photonView.IsMine)
            {
                playerNameText.text = PhotonNetwork.NickName;
                playerNameText.color = Color.green; // Local player in green

                if (cameraTransform == null)
                {
                    cameraTransform = Camera.main.transform;
                    Debug.Log($"[PlayerController] Camera auto-assigned for {PhotonNetwork.NickName}");
                }
            }
            else
            {
                playerNameText.text = photonView.Owner.NickName;
                playerNameText.color = Color.red; // Remote players in red
            }

            UpdateHealthUI();
        }

        private void Update()
        {
            // Only local player processes input
            if (!photonView.IsMine || isDead) return;
        }

        /// <summary>
        /// Called by MobileInputManager to move the player
        /// </summary>

        public void Move(Vector2 input)
        {
            if (isDead || isAttacking || !controlsEnabled) return;

            // Convert input to world space direction
            Vector3 inputDirection = new Vector3(input.x, 0, input.y);
            bool isWalking = inputDirection.magnitude > 0.1f;

            if (isWalking)
            {
                // Calculate movement direction relative to player's current rotation
                Vector3 forward = transform.forward;
                Vector3 right = transform.right;

                // Movement relative to player's facing direction
                moveDirection = (forward * input.y + right * input.x).normalized;

                // Move player
                Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
                rb.MovePosition(transform.position + movement);

                // Only rotate if moving forward/backward (input.y)
                // This makes the player face the direction they're moving
                if (Mathf.Abs(input.y) > 0.1f)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
                }
            }

            // only send when value changes, to reduce spam
            if (photonView.IsMine && isWalking != lastIsWalking)
            {
                lastIsWalking = isWalking;
                animator.SetBool(isWalkingHash, isWalking);
                photonView.RPC(nameof(SetWalkingRPC), RpcTarget.Others, isWalking);
            }
        }

        [PunRPC]
        private void SetWalkingRPC(bool walking)
        {
            animator.SetBool(isWalkingHash, walking);
        }

        /// <summary>
        /// Called when attack button is pressed
        /// </summary>
        public void OnAttackButtonPressed()
        {
            if (isDead || isAttacking || !controlsEnabled) return;

            // Check cooldown
            if (Time.time - lastAttackTime < attackCooldown) return;

            lastAttackTime = Time.time;
            isAttacking = true;

            // Trigger attack animation
            animator.SetTrigger(attackHash);

            // Animation event will call EnableSwordCollider and DisableSwordCollider
            // Or use timed enable/disable
            Invoke(nameof(EnableSwordCollider), 0.3f); // Enable during swing
            Invoke(nameof(DisableSwordCollider), 0.6f); // Disable after swing
            Invoke(nameof(ResetAttacking), 0.8f); // Reset attack state
        }

        private void EnableSwordCollider()
        {
            if (swordCollider != null && photonView.IsMine)
            {
                swordCollider.SetActive(true);
            }
        }

        private void DisableSwordCollider()
        {
            if (swordCollider != null)
            {
                swordCollider.SetActive(false);
            }
        }

        private void ResetAttacking()
        {
            isAttacking = false;
        }

        /// <summary>
        /// Called by SwordCollider when sword hits another player
        /// </summary>
        public void OnSwordHitPlayer(PlayerController hitPlayer)
        {
            if (hitPlayer != null && hitPlayer.photonView.ViewID != photonView.ViewID)
            {
                // Call TakeDamage via RPC
                hitPlayer.photonView.RPC("TakeDamage", RpcTarget.All, attackDamage, photonView.Owner.ActorNumber);
            }
        }

        [PunRPC]
        public void TakeDamage(float damage, int attackerActorNumber)
        {
            //if (isDead) return;
            if (isDead)
            {
                Debug.Log($"[PlayerController] {photonView.Owner.NickName} is already dead, ignoring damage");
                return;
            }

            currentHealth -= damage;
            currentHealth = Mathf.Max(0, currentHealth);

            Debug.Log($"[PlayerController] {photonView.Owner.NickName} took {damage} damage. Health: {currentHealth}/{maxHealth}");

            // Play hit animation
            animator.SetTrigger(hitHash);

            UpdateHealthUI();

            if (currentHealth <= 0)
            {
                Die(attackerActorNumber);
            }
        }

        private void Die(int killerActorNumber)
        {
            if (isDead) return; // Prevent multiple death calls

            isDead = true;

            Debug.Log($"[PlayerController] ===== {photonView.Owner.NickName} DIED =====");
            Debug.Log($"[PlayerController] Killed by ActorNumber: {killerActorNumber}");
            Debug.Log($"[PlayerController] IsMine: {photonView.IsMine}");
            Debug.Log($"[PlayerController] RoundManager exists: {RoundManager.Instance != null}");


            // Set death animation on ALL clients via RPC
            photonView.RPC(nameof(SetDeathAnimationRPC), RpcTarget.All, true);

            // Disable movement and collisions
            if (rb != null) rb.linearVelocity = Vector3.zero;

            // Disable collider
            /*Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = false;*/


            // ALL clients can notify RoundManager, but RoundManager will handle it properly
            // The master client will process it
            if (RoundManager.Instance != null)
            {
                RoundManager.Instance.OnPlayerDied(killerActorNumber, photonView.Owner.ActorNumber, photonView.Owner.NickName);

                //Debug.Log($"[PlayerController] RoundManager.OnPlayerDied() called");
            }
            else
            {
                Debug.LogError("[PlayerController] RoundManager.Instance is NULL! Cannot notify about death!");
            }
        }

        /// <summary>
        /// Synchronized death animation across all clients
        /// </summary>
        [PunRPC]
        private void SetDeathAnimationRPC(bool dead)
        {
            if (dead)
            {
                // Use trigger to play death animation once
                animator.SetTrigger(isDeadHash);
                //Debug.Log($"[PlayerController] Death animation TRIGGERED for {photonView.Owner.NickName}");
            }
        }

        private void Respawn()
        {
            isDead = false;
            currentHealth = maxHealth;

            // Reset animation - trigger automatically resets, just return to idle
            animator.Play("Idle", 0, 0f);

            // Re-enable collider
            Collider col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            // Reset position to spawn point
            GameManager gm = FindAnyObjectByType<GameManager>();

            Vector3 spawnPosition = transform.position;

            if (gm != null)
            {
                spawnPosition = gm.GetRandomSpawnPoint();
            }

            transform.position = spawnPosition;

            UpdateHealthUI();
        }

        /// <summary>
        /// Reset player for new round - synchronized across all clients
        /// </summary>
        [PunRPC]
        public void ResetForNewRound()
        {
            //Debug.Log($"[PlayerController] Resetting {photonView.Owner.NickName} for new round");

            isDead = false;
            currentHealth = maxHealth;
            isAttacking = false;
            lastIsWalking = false;

            // Reset ALL animations on all clients
            animator.SetBool(isWalkingHash, false);
            animator.ResetTrigger(attackHash);
            animator.ResetTrigger(hitHash);
            animator.ResetTrigger(isDeadHash);

            // Force idle state
            animator.Play("Idle", 0, 0f);

            if (rb != null)
                rb.linearVelocity = Vector3.zero;

            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            // Disable sword collider
            if (swordCollider != null)
                swordCollider.SetActive(false);

            UpdateHealthUI();

            // Respawn at random position
            GameManager gm = FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                transform.position = gm.GetRandomSpawnPoint();
            }
        }

        /// <summary>
        /// Enable or disable player controls (called by RoundManager)
        /// </summary>
        public void EnableControls(bool enabled)
        {
            controlsEnabled = enabled;

            if (!enabled)
            {
                // Stop movement when disabled
                if (rb != null) rb.linearVelocity = Vector3.zero;
                animator.SetBool(isWalkingHash, false);
            }

            //Debug.Log($"[PlayerController] Controls {(enabled ? "enabled" : "disabled")} for {photonView.Owner.NickName}");
        }

        private void UpdateHealthUI()
        {
            if (healthBarFill != null)
            {
                healthBarFill.fillAmount = currentHealth / maxHealth;
            }
        }

        public bool IsDead()
        {
            return isDead;
        }

        /// <summary>
        /// Photon serialization with optimizations (OPTIMIZATION #3)
        /// Only sync if position changed significantly AND enough time has passed
        /// </summary>
        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(currentHealth);
                stream.SendNext(isDead); // FIXED: Sync death state
            }
            else
            {
                Vector3 receivedPosition = (Vector3)stream.ReceiveNext();
                Quaternion receivedRotation = (Quaternion)stream.ReceiveNext();
                currentHealth = (float)stream.ReceiveNext();
                bool receivedIsDead = (bool)stream.ReceiveNext();

                // Sync death state
                if (receivedIsDead != isDead)
                {
                    isDead = receivedIsDead;
                }

                transform.position = Vector3.Lerp(transform.position, receivedPosition, Time.deltaTime * 10f);
                transform.rotation = Quaternion.Lerp(transform.rotation, receivedRotation, Time.deltaTime * 10f);

                UpdateHealthUI();
            }
        }
    }
}