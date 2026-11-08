using System;
using System.Collections.Generic;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace Simitone.Client.Utils
{
    public static class FamilyDeletion
    {
        // VMPersonDataVariable.TS1FamilyNumber in the pinned engine.
        public const int FamilyNumberIndex = 61;

        /// <summary>Remove a CAS family and detach its neighbours before its chunk ID is reused.</summary>
        public static bool DeleteUnplaced(IffFile neighborhood, FAMI family)
        {
            if (neighborhood == null || family == null || family.HouseNumber != 0 ||
                family.ChunkID == 0 || family.ChunkID >= 1000 ||
                neighborhood.Get<FAMI>(family.ChunkID) != family) return false;

            var members = new HashSet<uint>(family.FamilyGUIDs ?? Array.Empty<uint>());
            var neighbors = neighborhood.List<NBRS>();
            if (neighbors != null)
            {
                foreach (var table in neighbors)
                foreach (var member in table.Entries)
                {
                    var data = member.PersonData;
                    if (members.Contains(member.GUID) && data != null && data.Length > FamilyNumberIndex &&
                        data[FamilyNumberIndex] == family.ChunkID)
                        data[FamilyNumberIndex] = 0;
                }
            }

            family.FamilyGUIDs = Array.Empty<uint>();
            family.RuntimeSubset = Array.Empty<uint>();
            var names = neighborhood.Get<FAMs>(family.ChunkID);
            neighborhood.FullRemoveChunk(family);
            if (names != null) neighborhood.FullRemoveChunk(names);
            return true;
        }
    }
}
