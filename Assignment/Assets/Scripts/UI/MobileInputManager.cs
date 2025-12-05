using GapeLabs.Gameplay;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GapeLabs.UI
{
    /// <summary>
    /// Handles mobile touch input with virtual joystick and attack button
    /// </summary>
    public class MobileInputManager : MonoBehaviour
    {
        [Header("Joystick References")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickHandle;
        [SerializeField] private float joystickRange = 50f;

        [Header("Attack Button")]
        [SerializeField] private Button attackButton;

        [Header("Player Reference")]
        [SerializeField] private GapeLabs.Gameplay.PlayerController localPlayer;

        private Vector2 joystickInput;
        private int joystickTouchId = -1;
        private Vector2 joystickStartPos;

        private void Start()
        {
            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            if (canvas == null)
            {
                Debug.LogError("MobileInputManager: No Canvas found!");
            }
                
            // Setup attack button
            if (attackButton != null)
            {
                attackButton.onClick.AddListener(OnAttackButtonClicked);
            }

            // Find local player if not assigned
            if (localPlayer == null)
            {
                FindLocalPlayer();
            }
        }

        private void Update()
        {
            HandleJoystickInput();

            // Send input to player
            if (localPlayer != null)
            {
                localPlayer.Move(joystickInput);
            }
        }

        private void HandleJoystickInput()
        {
            // Handle touch input for mobile
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    Touch touch = Input.GetTouch(i);

                    // Check if touch is on joystick area
                    if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        joystickBackground,
                        touch.position,
                        canvas.worldCamera,
                        out Vector2 localPoint))
                    {
                        if (touch.phase == TouchPhase.Began && joystickTouchId == -1)
                        {
                            joystickTouchId = touch.fingerId;
                            joystickStartPos = localPoint;
                        }
                        else if (touch.fingerId == joystickTouchId)
                        {
                            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                            {
                                ProcessJoystickInput(localPoint);
                            }
                            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                            {
                                ResetJoystick();
                            }
                        }
                    }
                }
            }

            // Mouse input for testing in editor
#if UNITY_EDITOR
            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickBackground,
                    Input.mousePosition,
                    canvas.worldCamera,
                    out Vector2 localPoint))
                {
                    joystickStartPos = localPoint;
                    joystickTouchId = 0;
                }
            }
            else if (Input.GetMouseButton(0) && joystickTouchId == 0)
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    joystickBackground,
                    Input.mousePosition,
                    canvas.worldCamera,
                    out Vector2 localPoint))
                {
                    ProcessJoystickInput(localPoint);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                ResetJoystick();
            }
#endif
        }

        private void ProcessJoystickInput(Vector2 localPoint)
        {
            Vector2 offset = localPoint - joystickStartPos;
            Vector2 direction = Vector2.ClampMagnitude(offset, joystickRange);

            // Update handle position
            joystickHandle.anchoredPosition = joystickStartPos + direction;

            // Normalize input (-1 to 1)
            joystickInput = direction / joystickRange;
        }

        private void ResetJoystick()
        {
            joystickTouchId = -1;
            joystickHandle.anchoredPosition = Vector2.zero;
            joystickInput = Vector2.zero;
        }

        private void OnAttackButtonClicked()
        {
            if (localPlayer != null)
            {
                localPlayer.OnAttackButtonPressed();
            }
        }

        private void FindLocalPlayer()
        {
            // Find the local player in the scene
            GapeLabs.Gameplay.PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var player in players)
            {
                if (player.photonView.IsMine)
                {
                    localPlayer = player;
                    break;
                }
            }
        }

        // Call this from GameManager after player is spawned
        public void SetLocalPlayer(GapeLabs.Gameplay.PlayerController player)
        {
            localPlayer = player;
        }
    }
}