using System.Collections.Generic;
using System.Linq;

namespace Data
{
    /// <summary>
    /// A bag of items with amounts. Used for what a villager is carrying and what is stored in buildings.
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<Item, int> _items = new();

        public IReadOnlyDictionary<Item, int> Items => _items;

        public bool IsEmpty => _items.Count == 0;

        public int Count(Item item) => item && _items.TryGetValue(item, out var amount) ? amount : 0;

        public void Add(Item item, int amount)
        {
            if (!item || amount <= 0)
            {
                return;
            }

            _items[item] = Count(item) + amount;
        }

        /// <summary>Removes <paramref name="amount"/> of <paramref name="item"/>, or nothing if there isn't enough.</summary>
        public bool TryRemove(Item item, int amount)
        {
            var current = Count(item);
            if (amount <= 0 || current < amount)
            {
                return false;
            }

            if (current == amount)
            {
                _items.Remove(item);
            }
            else
            {
                _items[item] = current - amount;
            }
            return true;
        }

        /// <summary>True if every item in <paramref name="required"/> is present in at least the given amount.</summary>
        public bool Contains(IReadOnlyDictionary<Item, int> required)
        {
            return required.All(pair => Count(pair.Key) >= pair.Value);
        }

        /// <summary>
        /// Moves exactly <paramref name="items"/> into <paramref name="destination"/>.
        /// Moves nothing and returns false if any of them is missing.
        /// </summary>
        public bool TryMoveTo(Inventory destination, IReadOnlyDictionary<Item, int> items)
        {
            if (!Contains(items))
            {
                return false;
            }

            foreach (var (item, amount) in items)
            {
                TryRemove(item, amount);
                destination.Add(item, amount);
            }
            return true;
        }

        /// <summary>Moves every item that matches <paramref name="filter"/> (or all items) into <paramref name="destination"/>.</summary>
        public void MoveAllTo(Inventory destination, System.Predicate<Item> filter = null)
        {
            foreach (var item in _items.Keys.ToList())
            {
                if (filter == null || filter(item))
                {
                    destination.Add(item, _items[item]);
                    _items.Remove(item);
                }
            }
        }
    }
}
