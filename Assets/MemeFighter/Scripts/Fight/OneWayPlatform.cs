using System.Collections.Generic;
using UnityEngine;

namespace MemeFighter.Fight
{
    /// <summary>
    /// A platform you can jump up through and drop down from (hold down).
    /// Fighters call <see cref="UpdateFor"/> every physics step.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OneWayPlatform : MonoBehaviour
    {
        static readonly List<OneWayPlatform> All = new List<OneWayPlatform>();

        Collider col;

        float TopY => col.bounds.max.y;

        void Awake() { col = GetComponent<Collider>(); }
        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>Lets the fighter pass through platforms it is below, moving up through, or dropping from.</summary>
        public static void UpdateFor(Collider fighter, float feetY, bool passThrough)
        {
            foreach (var p in All)
            {
                if (p.col == null) continue;
                bool ignore = passThrough || feetY < p.TopY - 0.05f;
                Physics.IgnoreCollision(fighter, p.col, ignore);
            }
        }

        public static bool IsOneWay(Collider c) => c != null && c.GetComponent<OneWayPlatform>() != null;
    }
}
