using System.Collections;
using Data;
using UnityEngine;

namespace World
{
    /// <summary>
    /// What a <see cref="Character"/> does during <see cref="Activity.Work"/> hours. Add one job component next to the
    /// character; it is stopped whenever the schedule moves on, and resumed from the top at the next work block.
    /// </summary>
    [RequireComponent(typeof(Character))]
    public abstract class Job : MonoBehaviour
    {
        /// <summary>The work loop. Runs as the character's entity behaviour, so it may loop forever.</summary>
        public abstract IEnumerator Work(Character worker);

        /// <summary>True for items this job makes; these are what the character hauls off during shipping.</summary>
        public abstract bool IsProduct(Item item);
    }
}
