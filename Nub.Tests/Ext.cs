using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Nub.Tests
{
    static class Ext
    {
        public static IReadOnlyList<TItem> DequeueAll<TItem>(this ConcurrentQueue<TItem> queue)
        {
            if (queue == null) throw new ArgumentNullException(nameof(queue));

            var items = new List<TItem>();

            while (queue.TryDequeue(out var item))
            {
                items.Add(item);
            }

            return items;
        }
    }
}