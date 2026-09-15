using System;
using System.Collections;
using AdvancedRimTalk.Integration;

namespace AdvancedRimTalk.PromptChecks
{
    internal static class MemoryLayerTransferChecks
    {
        internal static void Run()
        {
            var shared = new Entry { Layer = "Active", Id = 10 };
            var source = new ArrayList { shared };
            var target = new ArrayList();
            var moved = (Entry)MemoryLayerTransfer.Move(source, target, shared, "Active", "Archive",
                value => new Entry { Id = 11, OriginId = ((Entry)value).Id, Layer = ((Entry)value).Layer }, SetLayer);
            Check(source.Count == 0 && target.Count == 1 && ReferenceEquals(target[0], moved), "membership");
            Check(shared.Layer == "Active" && moved.Layer == "Archive" && moved.OriginId == shared.Id, "private copy preserves shared memory");
            MemoryLayerTransfer.Move(target, target, moved, "Archive", "Archive",
                value => throw new Exception("Same-layer move must not privatize."), SetLayer);
            Check(target.Count == 1, "same layer does not duplicate");

            foreach (bool failInsert in new[] { true, false })
            {
                var entry = new Entry { Layer = "Active" };
                IList from = failInsert ? new ArrayList { entry } : new FailingRemoveList { entry };
                IList to = failInsert ? new FailingInsertList() : new ArrayList();
                bool failed = false;
                try { MemoryLayerTransfer.Move(from, to, entry, "Active", "Archive", value => value, SetLayer); }
                catch (InvalidOperationException) { failed = true; }
                Check(failed && from.Contains(entry) && to.Count == 0 && entry.Layer == "Active", "rollback");
            }
        }

        private static void SetLayer(object entry, object layer) => ((Entry)entry).Layer = (string)layer;
        private static void Check(bool value, string name)
        {
            if (!value) throw new Exception("Memory transfer: " + name);
        }
        private sealed class Entry { internal string Layer; internal long Id, OriginId; }
        private sealed class FailingInsertList : ArrayList
        {
            public override void Insert(int index, object value) => throw new InvalidOperationException("insert failed");
        }
        private sealed class FailingRemoveList : ArrayList
        {
            public override void Remove(object value) => throw new InvalidOperationException("remove failed");
        }
    }
}
