using UnityEngine;
using Photon.Pun;

namespace GapeLabs.Gameplay
{
    /// <summary>
    /// Attached to the sword collider to detect hits on other players
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class SwordCollider : MonoBehaviour
    {
        private PlayerController ownerPlayer;
        private Collider swordCollider;

        private void Awake()
        {
            // Get the player controller from parent
            ownerPlayer = GetComponentInParent<PlayerController>();

            // Setup collider as trigger
            swordCollider = GetComponent<Collider>();
            swordCollider.isTrigger = true;

            if (ownerPlayer == null)
            {
                Debug.LogError("SwordCollider: Could not find PlayerController in parent!");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Only process on local player's sword
            if (ownerPlayer == null || !ownerPlayer.photonView.IsMine)
                return;

            // Check if hit another player
            PlayerController hitPlayer = other.GetComponent<PlayerController>();

            if (hitPlayer != null)
            {
                // Don't hit yourself
                if (hitPlayer.photonView.ViewID == ownerPlayer.photonView.ViewID)
                    return;

                // Notify owner player about the hit
                ownerPlayer.OnSwordHitPlayer(hitPlayer);

                //Debug.Log($"Sword hit player: {hitPlayer.photonView.Owner.NickName}");
            }
        }
    }
}