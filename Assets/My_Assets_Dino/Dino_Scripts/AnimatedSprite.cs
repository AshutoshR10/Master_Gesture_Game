namespace Dino
{
    using UnityEngine;

    [RequireComponent(typeof(SpriteRenderer))]
    public class AnimatedSprite : MonoBehaviour
    {
        public Sprite[] sprites;
        private SpriteRenderer spriteRenderer;
        private int frame;
        private float animationDelay;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void OnEnable()
        {
            // Start the animation
            frame = 0;
            UpdateAnimationDelay();
            Invoke(nameof(Animate), 0f);
        }

        private void OnDisable()
        {
            CancelInvoke();
        }

        private void Animate()
        {
            // Cycle through the frames
            frame++;

            if (frame >= sprites.Length)
            {
                frame = 0;
            }

            if (sprites.Length > 0)
            {
                spriteRenderer.sprite = sprites[frame];
            }

            // Update the animation delay in case game speed has changed
            UpdateAnimationDelay();
            Invoke(nameof(Animate), animationDelay);
        }

        private void UpdateAnimationDelay()
        {
            // Safeguard: Prevent division by zero or extremely slow speeds
            float speed = GameManager.Instance != null && GameManager.Instance.gameSpeed > 0f
                ? GameManager.Instance.gameSpeed
                : 1f; // Fallback to a default speed if necessary

            animationDelay = 1f / Mathf.Max(speed, 0.1f); // Prevent unreasonably high delays
        }
    }
}
