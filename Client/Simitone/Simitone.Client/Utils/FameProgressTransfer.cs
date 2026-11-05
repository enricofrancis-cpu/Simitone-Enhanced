using System.Collections.Generic;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;

namespace Simitone.Client.Utils
{
    // Fame changes on community lots must survive the retained home VM being loaded.
    // Transfer the raw game fields; promotion rules remain the engine's responsibility.
    internal readonly struct FameProgress
    {
        public readonly short Score;
        public readonly short StarPower;
        public readonly short HighWatermark;

        public FameProgress(short score, short starPower, short highWatermark)
        {
            Score = score;
            StarPower = starPower;
            HighWatermark = highWatermark;
        }

        public void ApplyTo(short[] personData)
        {
            if (personData == null || personData.Length <= (int)VMPersonDataVariable.TS1FameStarHighWatermark) return;
            personData[(int)VMPersonDataVariable.TS1FameScore] = Score;
            personData[(int)VMPersonDataVariable.TS1FameStarPower] = StarPower;
            personData[(int)VMPersonDataVariable.TS1FameStarHighWatermark] = HighWatermark;
        }
    }

    internal static class FameProgressTransfer
    {
        public static Dictionary<uint, FameProgress> CaptureFamily(IEnumerable<uint> familyGuids,
            IEnumerable<KeyValuePair<uint, FameProgress>> liveProgress)
        {
            var members = new HashSet<uint>(familyGuids);
            var result = new Dictionary<uint, FameProgress>();
            foreach (var entry in liveProgress)
                if (members.Contains(entry.Key)) result[entry.Key] = entry.Value;
            return result;
        }

        public static void ApplyToHome(VMMarshal home, IReadOnlyDictionary<uint, FameProgress> progress)
        {
            if (home?.Entities == null) return;
            foreach (var entity in home.Entities)
            {
                if (entity is VMAvatarMarshal avatar && progress.TryGetValue(avatar.GUID, out var fame))
                    fame.ApplyTo(avatar.PersonData);
            }
        }
    }
}
