namespace BrickBreaker
{
    using UnityEngine;

    [RequireComponent(typeof(Rigidbody2D))]
    public class Ball : MonoBehaviour
    {
        private Rigidbody2D rb;
        public float speed = 10f;

        private void Awake()
        {
            //rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            //GetComponent<SpriteRenderer>().sortingLayerName = "Default";
            //GetComponent<SpriteRenderer>().sortingOrder = 10;
            ResetBall();
        }

        public void ResetBall()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.linearVelocity = Vector2.zero;
            gameObject.SetActive(true);
            //transform.position = new Vector2(0,3f);
            switch (GameManager.Instance.level)
            {
                case 1:
                    transform.position = new Vector3(0, 3f, 0); // Default position
                    break;
                case 2:
                    transform.position = new Vector2(0, 0); // Level 2 position
                    break;
                case 3:
                    transform.position = new Vector2(0, 0); // Level 3 position
                    break;
            }


            CancelInvoke();
            Invoke(nameof(SetRandomTrajectory), 1f);
        }
        // In your Ball class:
        //public void ResetBall()
        //{
        //    // Reset position
        //    transform.position = new Vector3(0, -1f, 0); // Adjust based on your scene

        //    // Reset velocity
        //    Rigidbody2D rb = GetComponent<Rigidbody2D>();
        //    if (rb != null)
        //    {
        //        rb.velocity = Vector2.zero;
        //    }

        //    // Make sure the ball is active
        //    gameObject.SetActive(true);
        //}

        private void FixedUpdate()
        {
            // Detect if ball is moving too horizontally
            if (Mathf.Abs(rb.linearVelocity.y) < 0.5f && rb.linearVelocity.magnitude > 1f)
            {
                // Add slight upward force to break horizontal movement
                Vector2 correction = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y > 0 ? 2f : -2f);
                rb.linearVelocity = correction.normalized * rb.linearVelocity.magnitude;
            }
        }


        private void SetRandomTrajectory()
        {
            float x = Random.Range(-1f, 1f);

            // Avoid too small vertical force (too horizontal)
            if (Mathf.Abs(x) < 0.3f) // avoid close to 0
            {
                x = x < 0 ? -0.3f : 0.3f;
            }

            Vector2 force = new Vector2(x, -1f);
            rb.AddForce(force.normalized * speed, ForceMode2D.Impulse);
        }

        //private void FixedUpdate()
        //{
        //    rb.velocity = rb.velocity.normalized * speed;
        //}

    }
}
