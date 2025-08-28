namespace Flappy
{
    using System.Collections.Generic;
    using UnityEngine;

    public class Pipes : MonoBehaviour
    {
        public Transform top;
        public Transform bottom;
        public float speed = 5f;
        public float gap = 5f;

        private float leftEdge;
        private GameObject scoringTrigger;

        private void Start()
        {
            leftEdge = Camera.main.ScreenToWorldPoint(Vector3.zero).x - 1f;
            top.position += Vector3.up * gap / 2;
            bottom.position += Vector3.down * gap / 2;

            EnsureScoringTrigger();
        }

        private void Update()
        {
            transform.position += speed * Time.deltaTime * Vector3.left;

            if (transform.position.x < leftEdge)
            {
                Destroy(gameObject);
            }
        }

        // Replace the EnsureScoringTrigger method in Pipes.cs with this version:

        // Replace the EnsureScoringTrigger method in Pipes.cs with this version:

        private void EnsureScoringTrigger()
        {
            // First, remove any existing scoring triggers
            RemoveExistingScoringTriggers();

            // Create a new scoring trigger in the gap between pipes
            GameObject scoringTrigger = new GameObject("ScoringTrigger");
            scoringTrigger.transform.parent = transform;
            scoringTrigger.transform.localPosition = Vector3.zero;
            scoringTrigger.tag = "Scoring";

            // Add box collider as trigger
            BoxCollider2D collider = scoringTrigger.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = new Vector2(0.5f, gap); // Thin vertical collider in the gap

            //Debug.Log($"Created new scoring trigger at {scoringTrigger.transform.position} with height {gap}");
        }

        // Add this new helper method
        private void RemoveExistingScoringTriggers()
        {
            // Find all children with the "Scoring" tag
            List<GameObject> scoringObjectsToRemove = new List<GameObject>();

            foreach (Transform child in transform)
            {
                if (child.CompareTag("Scoring"))
                {
                    scoringObjectsToRemove.Add(child.gameObject);
                }
            }

            // Remove all found scoring triggers
            foreach (GameObject obj in scoringObjectsToRemove)
            {
                Debug.Log($"Removing existing scoring trigger: {obj.name}");
                DestroyImmediate(obj);
            }
        }

    }
}
