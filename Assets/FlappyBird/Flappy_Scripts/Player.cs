namespace Flappy
{
    using UnityEngine;

    public class Player : MonoBehaviour
    {
        public Sprite[] sprites;
        public float strength = 5f;
        public float gravity = -9.81f;
        public float tilt = 5f;

        //[Header("Top Boundary Settings")]
        public int maxTopHits = 3; // Maximum hits before game over
        public float resetDuration = 3f; // Time window to reset counter

        private SpriteRenderer spriteRenderer;
        private Vector3 direction;
        private int spriteIndex;

        // Top boundary hit tracking
        private int topHitCount = 0;
        private float lastTopHitTime = 0f;
        float maxHeight = 5.4f;
        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            InvokeRepeating(nameof(AnimateSprite), 0.15f, 0.15f);
        }

        // private void OnEnable()
        // {
        //     Vector3 position = transform.position;
        //     position.y = 0f;
        //     transform.position = position;
        //     direction = Vector3.zero;

        //     // Reset top hit counter when player respawns
        //     topHitCount = 0;
        //     lastTopHitTime = 0f;
        // }
        private void OnEnable()
        {
            direction = Vector3.zero;

            topHitCount = 0;
            lastTopHitTime = 0f;
        }
        public void ResetDirection()
        {
            direction = Vector3.zero;
            topHitCount = 0;
            lastTopHitTime = 0f;
        }

        private void Update()
        {
            if (Time.timeScale == 0f)
            {
                return;
            }
            if (Input.GetKeyDown(KeyCode.Space))
            {
                //direction = Vector3.up * strength;
                Jump();
            }

            // Check if we should reset the top hit counter
            if (Time.time - lastTopHitTime > resetDuration && topHitCount > 0)
            {
                topHitCount = 0;
                //Debug.Log("Top hit counter reset due to time limit");
            }
            if (Time.timeScale != 0f)
            {
                // Apply gravity and update the position
                direction.y += gravity * Time.deltaTime;
                transform.position += direction * Time.deltaTime;

                // Tilt the bird based on the direction
                Vector3 rotation = transform.eulerAngles;
                rotation.z = direction.y * tilt;
                transform.eulerAngles = rotation;

            }
            if (transform.position.y > maxHeight)
            {
                GameManager.Instance.GameOver();
            }
        }

        public void Jump()
        {
            direction = Vector3.up * strength;
        }
        private void AnimateSprite()
        {
            spriteIndex++;
            if (spriteIndex >= sprites.Length)
            {
                spriteIndex = 0;
            }
            if (spriteIndex < sprites.Length && spriteIndex >= 0)
            {
                spriteRenderer.sprite = sprites[spriteIndex];
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                GameManager.Instance.GameOver();
            }
            else if (other.gameObject.CompareTag("Scoring"))
            {
                GameManager.Instance.IncreaseScore();
            }
            else if (other.gameObject.name.Contains("Top"))
            {

                // Handle top boundary collision
                if (direction.y > 0)
                { // Only if moving upward
                    direction.y = -0.5f; // Stop upward movement

                    // Increment hit counter
                    topHitCount++;
                    lastTopHitTime = Time.time;

                    Debug.Log($"Top boundary hit! Count: {topHitCount}/{maxTopHits}");

                    // Check if player has hit the limit
                    if (topHitCount >= maxTopHits)
                    {
                        Debug.Log("Too many top boundary hits! Game Over!");
                        GameManager.Instance.GameOver();
                    }
                }
            }
        }
    }
}