using System.Collections;
using System.Collections.Generic;
using Data;
using UnityEngine;

namespace World
{
    /// <summary>
    /// Collects a resource by destroying the entities that drop it: walk to the nearest one, hit it until it dies,
    /// bring the drop home, repeat. A woodcutter targets trees that drop Wood with an axe; a hunter targets critters
    /// that drop Meat with a knife.
    /// </summary>
    public class GathererJob : Job
    {
        // Matches the player's swing: time from starting the animation to the hit landing.
        private const float HitDelay = 0.7f;
        // Extra distance a target may move away mid-swing before the worker stops hitting and chases it.
        private const float ReachSlack = 0.5f;
        // How long to skip a target that couldn't be reached, so the worker tries another one.
        private const float UnreachableIgnoreSeconds = 30f;

        [Tooltip("What to collect, e.g. Wood or Meat. Only entities that drop this item are targeted.")]
        [SerializeField] private Item resource;
        [Tooltip("What the worker hits with. Only entities vulnerable to its damage type are targeted; " +
                 "it also sets the damage per hit and the swing animation.")]
        [SerializeField] private Item tool;

        [Header("Tuning")]
        [Tooltip("How far from the worker to look for targets.")]
        [SerializeField] private float searchRadius = 50f;
        [Tooltip("How close the worker must be to hit a target.")]
        [SerializeField] private float reach = 2.5f;
        [Tooltip("Seconds between hits.")]
        [SerializeField] private float swingInterval = 1.5f;
        [Tooltip("Seconds to spend walking to (or chasing) a target before giving up on it.")]
        [SerializeField] private float approachTimeout = 20f;
        [Tooltip("Seconds to wait before looking again when there is nothing to collect.")]
        [SerializeField] private float idleSeconds = 5f;

        private readonly Dictionary<Entity, float> _ignoredUntil = new();

        public override bool IsProduct(Item item) => item == resource;

        public override IEnumerator Work(Character worker)
        {
            if (!resource || !tool || tool.damage <= 0)
            {
                Debug.LogError($"{name}: GathererJob needs a resource and a tool that does damage.", this);
                yield break;
            }

            var self = worker.Entity;
            var idleWait = new WaitForSeconds(idleSeconds);

            while (true)
            {
                var target = FindTarget(self);
                if (!target)
                {
                    yield return idleWait;
                    continue;
                }

                yield return self.Approach(target.transform, reach, approachTimeout);
                if (!IsTargetable(target))
                {
                    // Someone else got to it first.
                    continue;
                }
                if (!self.IsWithin(target.transform.position, reach))
                {
                    _ignoredUntil[target] = Time.time + UnreachableIgnoreSeconds;
                    continue;
                }

                yield return Harvest(worker, target);

                if (worker.Carried.Count(resource) > 0)
                {
                    yield return worker.WalkHome();
                }
            }
        }

        /// <summary>Hits <paramref name="target"/> until it dies or gets out of reach.</summary>
        private IEnumerator Harvest(Character worker, Entity target)
        {
            var self = worker.Entity;
            var hitDelay = new WaitForSeconds(HitDelay);
            var recovery = new WaitForSeconds(Mathf.Max(0f, swingInterval - HitDelay));

            while (IsTargetable(target) && self.IsWithin(target.transform.position, reach + ReachSlack))
            {
                self.FaceTowards(target.transform.position);
                self.PlayItemAnimation(tool.animationType);
                yield return hitDelay;

                if (IsTargetable(target) && self.IsWithin(target.transform.position, reach + ReachSlack))
                {
                    // The drop lands in the worker's hands rather than the player's inventory.
                    target.TakeDamage(tool.damage, worker.Carried);
                }
                yield return recovery;
            }
        }

        private Entity FindTarget(Entity self)
        {
            Entity nearest = null;
            var nearestDistance = searchRadius * searchRadius;
            var origin = self.transform.position;

            foreach (var candidate in Entity.All)
            {
                if (candidate == self || !IsTargetable(candidate) ||
                    (_ignoredUntil.TryGetValue(candidate, out var until) && Time.time < until))
                {
                    continue;
                }

                var distance = (candidate.transform.position - origin).sqrMagnitude;
                if (distance < nearestDistance)
                {
                    nearest = candidate;
                    nearestDistance = distance;
                }
            }

            return nearest;
        }

        private bool IsTargetable(Entity entity) =>
            entity && entity.IsAlive && !entity.IsHidden &&
            entity.DropItem == resource && entity.vulnerability == tool.damageType;
    }
}
