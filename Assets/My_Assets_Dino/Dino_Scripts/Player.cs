namespace Dino
{
    using UnityEngine;

    [RequireComponent(typeof(CharacterController))]
    public class Player : MonoBehaviour
    {
        private CharacterController character;
        private Vector3 direction;

        public float jumpForce = 8f;
        public float gravity = 9.81f * 2f;

        // private bool canDoubleJump = false;

        private void Awake()
        {
            character = GetComponent<CharacterController>();
        }
        private void Start()
        {
            // Ensure we have a reference to GameManager
            if (GameManager.Instance == null)
            {
                Debug.LogError("GameManager instance not found!");
            }
        }
        private void OnEnable()
        {
            direction = Vector3.zero;
            // canDoubleJump = false;
        }

        private void Update()
        {
            if (PauseMenu.isPaused) return;

            // Only process player input when in Playing state
            if (GameManager.Instance == null || GameManager.Instance.currentGameState != GameManager.GameState.Playing)
                return;

            // Apply gravity
            direction += gravity * Time.deltaTime * Vector3.down;

            if (character.isGrounded)
            {
                // Reset direction to stick to ground
                //direction = Vector3.down;

                // First jump - only when in Playing state
                /* if (Input.GetButtonDown("Jump"))
                 {
                     //Debug.Log($"[{Time.time}] Jump pressed during gameplay");
                     direction = Vector3.up * jumpForce;
                 }*/

                /*if (Input.GetMouseButtonDown(0))
                {
                    Jump();
                }*/

            }

            // Move character
            character.Move(direction * Time.deltaTime);
        }

        public void ResetState()
        {
            direction = Vector3.zero;

        }

        public void Jump()
        {
            /*if (GameManager.Instance.startPanel != null)
            {
                GameManager.Instance.startPanel.SetActive(false);
            }*/
            // Debug.Log("MouseJumoNewFunction");
            if (character.isGrounded &&
                !PauseMenu.isPaused &&
                GameManager.Instance != null &&
                GameManager.Instance.currentGameState == GameManager.GameState.Playing)
            {
                direction = Vector3.up * jumpForce;
                KeyBinding.Instance?.AddDebug("Jump Success!");
            }
            else
            {
                string failReason = !character.isGrounded ? "Not grounded" :
                                  PauseMenu.isPaused ? "Game paused" :
                                  "Game not in playing state";
                KeyBinding.Instance?.AddDebug($"Jump Failed: {failReason}");
            }
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Obstacle"))
            {
                GameManager.Instance.GameOver();
            }
        }
    }
}