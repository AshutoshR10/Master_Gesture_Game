namespace Space
{
    using UnityEngine;

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class Projectile : MonoBehaviour
    {
        private BoxCollider2D boxCollider;
        public Vector3 direction = Vector3.up;
        public float speed = 20f;

        private void Awake()
        {
            boxCollider = GetComponent<BoxCollider2D>();
        }

        private void Update()
        {
            // ✅ PAUSE FIX: Don't update movement if game is paused (timeScale = 0)
            // Note: Time.deltaTime already becomes 0 when timeScale = 0, but explicit check is clearer
            if (Time.timeScale == 0f) return;

            transform.position += speed * Time.deltaTime * direction;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            CheckCollision(other);
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            CheckCollision(other);
        }

        private void CheckCollision(Collider2D other)
        {
            Bunker bunker = other.gameObject.GetComponent<Bunker>();

            if (bunker == null || bunker.CheckCollision(boxCollider, transform.position))
            {
                Destroy(gameObject);
            }
        }

    }
}
