using System.IO;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model.TS1Platform;

namespace Simitone.Client.Utils
{
    internal static class VacationLotSnapshot
    {
        public static VMMarshal Read(byte[] bytes, int lot)
        {
            var marshal = new VMMarshal();
            using (var reader = new BinaryReader(new MemoryStream(bytes))) marshal.Deserialize(reader);
            if (!marshal.TS1 || marshal.Version > VMMarshal.LATEST_VERSION ||
                marshal.Context?.Architecture == null || marshal.Context.Clock == null ||
                marshal.Entities == null || marshal.Threads == null ||
                marshal.Entities.Length != marshal.Threads.Length || marshal.MultitileGroups == null ||
                !(marshal.PlatformState is VMTS1LotState) ||
                marshal.GlobalState == null || marshal.GlobalState.Length <= 10 || marshal.GlobalState[10] != lot)
                throw new InvalidDataException("The saved vacation contains an invalid lot snapshot.");
            return marshal;
        }

        public static byte[] Write(VMMarshal marshal)
        {
            using (var stream = new MemoryStream())
            {
                marshal.SerializeInto(new BinaryWriter(stream));
                return stream.ToArray();
            }
        }
    }
}
