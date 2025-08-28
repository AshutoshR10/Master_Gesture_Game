namespace BrickBreaker
{
    using UnityEngine;

    [RequireComponent(typeof(Rigidbody2D))]
    public class Paddle : MonoBehaviour
    {
        public static Paddle Instance;
        private Rigidbody2D rb;
        private Vector2 direction;

        public float speed = 30f;
        public float maxBounceAngle = 75f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            rb = GetComponent<Rigidbody2D>();
        }

        private void Start()
        {
            ResetPaddle();
        }

        public void ResetPaddle()
        {
            rb.linearVelocity = Vector2.zero;
            transform.position = new Vector2(0f, transform.position.y);
        }

        private void Update()
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                direction = Vector2.left;
            }
            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                direction = Vector2.right;
            }
            else
            {
                direction = Vector2.zero;
            }
        }

        public void LeftMovement()
        {
            direction = Vector2.left;
        }

        public void RightMovement()
        {
            direction = Vector2.right;
        }

        private void FixedUpdate()
        {
            if (direction != Vector2.zero)
            {
                rb.AddForce(direction * speed);
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Ball"))
            {
                return;
            }

            Rigidbody2D ball = collision.rigidbody;
            Collider2D paddle = collision.otherCollider;

            // Calculate hit position (-1 to 1, where 0 is center)
            float hitFactor = (ball.transform.position.x - paddle.bounds.center.x) / paddle.bounds.size.x;

            // Clamp to prevent extreme angles
            hitFactor = Mathf.Clamp(hitFactor, -1f, 1f);

            // Calculate new direction with guaranteed upward component
            float bounceAngle = hitFactor * maxBounceAngle * Mathf.Deg2Rad;
            Vector2 newDirection = new Vector2(Mathf.Sin(bounceAngle), Mathf.Cos(bounceAngle));

            // Ensure minimum vertical component (similar to your SetRandomTrajectory logic)
            if (Mathf.Abs(newDirection.y) < 0.3f)
            {
                newDirection.y = 0.3f; // Always bounce upward from paddle
            }

            // Apply new velocity
            ball.linearVelocity = newDirection.normalized * ball.linearVelocity.magnitude;
        }


    }
}
