using System;
using System.Collections;

namespace AdvancedRimTalk.Integration
{
    internal static class MemoryLayerTransfer
    {
        internal static object Move(IList source, IList target, object entry, object oldLayer, object newLayer,
            Func<object, object> privatize, Action<object, object> setLayer)
        {
            if (source == null || target == null || !source.Contains(entry))
                throw new InvalidOperationException("The memory is not in its declared source layer, or the target layer is unavailable.");
            if (Equals(oldLayer, newLayer)) return entry;
            if (source.IsReadOnly || source.IsFixedSize || target.IsReadOnly || target.IsFixedSize)
                throw new InvalidOperationException("The memory layer cannot be modified.");
            object moved = privatize(entry);
            if (moved == null) throw new InvalidOperationException("Memory privatization returned null.");
            setLayer(moved, newLayer);
            try { target.Insert(0, moved); }
            catch
            {
                setLayer(moved, oldLayer);
                throw;
            }
            try { source.Remove(entry); }
            catch
            {
                target.Remove(moved);
                setLayer(moved, oldLayer);
                throw;
            }
            return moved;
        }
    }
}
