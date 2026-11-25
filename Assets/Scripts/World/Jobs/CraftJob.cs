using System.Collections;
using Data;
using UnityEngine;

namespace World
{
    /// <summary>
    /// Turns ingredients into goods with a <see cref="Recipe"/>: fetch the ingredients (from home, otherwise from the
    /// <see cref="ShippingPoint"/>), craft at the workstation, bring the result home, repeat. A baker uses a
    /// Wheat -> Bread recipe at an oven; a cook uses a Meat -> cooked meat recipe at a campfire.
    /// </summary>
    public class CraftJob : Job
    {
        // Seconds between work animations while crafting.
        private const float AnimationInterval = 2f;

        [SerializeField] private Recipe recipe;
        [Tooltip("Where the worker stands to craft, e.g. in front of an oven, facing along its blue (forward) axis. " +
                 "Place it on the navmesh. Leave empty to craft at the home entrance.")]
        [SerializeField] private Transform workstation;

        [Header("Tuning")]
        [Tooltip("Seconds one batch takes.")]
        [SerializeField] private float craftSeconds = 10f;
        [Tooltip("Seconds to wait before looking again when there are no ingredients.")]
        [SerializeField] private float idleSeconds = 500f;

        public override bool IsProduct(Item item) => recipe && recipe.output.ContainsKey(item);

        public override IEnumerator Work(Character worker)
        {
            if (!recipe || recipe.input.Count == 0 || recipe.output.Count == 0)
            {
                Debug.LogError($"{name}: CraftJob needs a recipe with inputs and outputs.", this);
                yield break;
            }

            var idleWait = new WaitForSeconds(idleSeconds);

            while (true)
            {
                if (!worker.Carried.Contains(recipe.input))
                {
                    yield return FetchIngredients(worker);
                    if (!worker.Carried.Contains(recipe.input))
                    {
                        yield return worker.Congregate(AuraType.Fun);
                        yield return idleWait;
                    }
                }

                if (workstation)
                {
                    yield return worker.WalkTo(workstation.position, this);
                }

                yield return Craft(worker.Entity);

                foreach (var (item, amount) in recipe.input)
                {
                    worker.Carried.TryRemove(item, amount);
                }
                foreach (var (item, amount) in recipe.output)
                {
                    worker.Carried.Add(item, amount);
                }

                yield return worker.WalkHome();
            }
        }

        private IEnumerator FetchIngredients(Character worker)
        {
            // Walking home also drops off anything still carried, so ingredients are always taken as a full set.
            yield return worker.WalkHome();
            if (worker.Home.Storage.TryMoveTo(worker.Carried, recipe.input))
            {
                yield break;
            }

            var shippingPoint = ShippingPoint.Instance;
            if (shippingPoint && shippingPoint.Storage.Contains(recipe.input))
            {
                yield return worker.WalkTo(shippingPoint.DropOffPosition, shippingPoint);
                // Someone may have taken them while we walked; then we just come back empty-handed.
                shippingPoint.Storage.TryMoveTo(worker.Carried, recipe.input);
            }
        }

        private IEnumerator Craft(Entity self)
        {
            var endTime = Time.time + craftSeconds;
            while (Time.time < endTime)
            {
                if (workstation)
                {
                    self.FaceTowards(workstation.position + workstation.forward);
                }
                self.PlayItemAnimation(AnimationType.Pull);
                yield return new WaitForSeconds(Mathf.Min(AnimationInterval, endTime - Time.time));
            }
        }
    }
}
