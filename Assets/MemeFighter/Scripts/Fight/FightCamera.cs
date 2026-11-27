using System.Collections.Generic;
using UnityEngine;

namespace MemeFighter.Fight
{
    /// <summary>Keeps every fighter in view: follows their midpoint and pulls back as they spread out.</summary>
    public class FightCamera : MonoBehaviour
    {
        public List<Transform> targets = new List<Transform>();
        public float minDistance = 11f;
        public float maxDistance = 22f;
        public float spreadFactor = 0.9f;
        public float heightOffset = 1.6f;
        public float pitch = 6f;
        public float smoothTime = 0.25f;
        public Vector2 limitX = new Vector2(-8f, 8f);
        public Vector2 limitY = new Vector2(-1f, 8f);

        Vector3 velocity;

        void LateUpdate()
        {
            if (targets.Count == 0) return;
            var b = new Bounds(targets[0].position, Vector3.zero);
            foreach (var t in targets) if (t) b.Encapsulate(t.position);

            float spread = Mathf.Max(b.size.x, b.size.y * 1.6f);
            float dist = Mathf.Clamp(minDistance + spread * spreadFactor, minDistance, maxDistance);
            Vector3 focus = new Vector3(Mathf.Clamp(b.center.x, limitX.x, limitX.y),
                                        Mathf.Clamp(b.center.y, limitY.x, limitY.y) + heightOffset, 0f);
            Vector3 goal = focus + Quaternion.Euler(pitch, 0f, 0f) * new Vector3(0f, 0f, -dist);
            transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime);
            transform.rotation = Quaternion.LookRotation(focus - transform.position, Vector3.up);
        }
    }
}
