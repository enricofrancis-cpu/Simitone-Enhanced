using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;

namespace Simitone.Client.Superstar
{
    internal sealed class FameRequirement
    {
        public int Score { get; }
        public int FriendHalfStars { get; }
        public IReadOnlyList<int> Skills { get; }

        public FameRequirement(int score, int friendStars, int[] skills)
        {
            Score = score;
            FriendHalfStars = checked(friendStars * 2);
            Skills = Array.AsReadOnly((int[])skills.Clone());
        }
    }

    internal sealed class FameRules
    {
        public const int MaximumRank = 10;
        public IReadOnlyList<FameRequirement> Requirements { get; }
        public IReadOnlyList<string> Titles { get; }
        public int FriendshipThreshold { get; }

        private FameRules(FameRequirement[] requirements, string[] titles, int friendshipThreshold)
        {
            Requirements = Array.AsReadOnly(requirements);
            Titles = Array.AsReadOnly(titles);
            FriendshipThreshold = friendshipThreshold;
        }

        public static FameRules Read(IffFile resource, int friendshipThreshold)
        {
            var scores = resource.Get<STR>(5);
            var friends = resource.Get<STR>(4);
            var skills = resource.Get<STR>(6);
            var titles = resource.Get<STR>(1);
            if (scores == null || friends == null || skills == null || titles == null ||
                scores.Length < MaximumRank || friends.Length < MaximumRank ||
                skills.Length < MaximumRank || titles.Length < MaximumRank + 2 ||
                friendshipThreshold < 0 || friendshipThreshold > 100)
                throw new InvalidDataException("The Superstar fame tables are missing or incomplete.");

            var rows = new FameRequirement[MaximumRank];
            int previousScore = -1;
            for (int i = 0; i < rows.Length; i++)
            {
                var score = Number(scores.GetString(i), 1000);
                var friendStars = Number(friends.GetString(i), 1000);
                var skillPoints = skills.GetString(i).Split(',').Select(x => Number(x, 10)).ToArray();
                if (skillPoints.Length != 6 || score <= previousScore)
                    throw new InvalidDataException("Invalid Superstar requirement row " + i + ".");
                rows[i] = new FameRequirement(score, friendStars, skillPoints);
                previousScore = score;
            }
            // The resource's final zero row is a sentinel, not an eleventh promotion.
            return new FameRules(rows, Enumerable.Range(0, MaximumRank + 1)
                .Select(i => titles.GetString(i + 1) ?? "").ToArray(), friendshipThreshold);
        }

        private static int Number(string text, int maximum)
        {
            if (!int.TryParse(text?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ||
                value < 0 || value > maximum)
                throw new InvalidDataException("Invalid Superstar numeric tuning: " + text);
            return value;
        }
    }

    internal readonly struct FameDecision
    {
        public int Rank { get; }
        public string Reason { get; }
        public FameDecision(int rank, string reason) { Rank = rank; Reason = reason; }
    }

    internal static class FameProgression
    {
        // Preview policy: at most one half-star transition per native check. Call 36
        // reconciles both directions, as Studio Town's "Promotion Check" calls it.
        public static FameDecision Check(FameRules rules, int rank, int score, int job,
            IReadOnlyList<int> skillHundredths, int friendHalfStars, bool reconcile)
        {
            if (rank < 0 || rank > FameRules.MaximumRank) return new FameDecision(rank, "invalid rank");
            if (job != 0 || score <= 0) return new FameDecision(rank, "not enrolled");
            if (reconcile && rank > 0 && score < rules.Requirements[rank - 1].Score)
                return new FameDecision(rank - 1, "fame score fell below current rank");
            if (rank == FameRules.MaximumRank) return new FameDecision(rank, "maximum rank");
            var next = rules.Requirements[rank];
            if (score < next.Score) return new FameDecision(rank, "fame score");
            for (int i = 0; i < 6; i++)
                if (skillHundredths[i] < next.Skills[i] * 100)
                    return new FameDecision(rank, "skill requirement " + i);
            if (friendHalfStars < next.FriendHalfStars) return new FameDecision(rank, "celebrity friends");
            return new FameDecision(rank + 1, "promotion");
        }
    }

    internal static class FameFriendships
    {
        public static bool IsMutualFriend(Neighbour actor, Neighbour other, int threshold)
        {
            return actor != null && other != null && actor.NeighbourID != other.NeighbourID &&
                actor.Relationships != null && other.Relationships != null &&
                actor.Relationships.TryGetValue(other.NeighbourID, out var outgoing) && outgoing.Count > 0 &&
                other.Relationships.TryGetValue(actor.NeighbourID, out var incoming) && incoming.Count > 0 &&
                outgoing[0] > threshold && incoming[0] > threshold;
        }

        public static int TotalHalfStars(Neighbour actor, IEnumerable<Neighbour> neighbors,
            int threshold, IReadOnlyDictionary<uint, short> liveRanks)
        {
            if (actor?.PersonData == null || actor.PersonData.Length <= 61) return 0;
            int total = 0;
            var seen = new HashSet<uint>();
            foreach (var other in neighbors)
            {
                if (other.PersonData == null || other.PersonData.Length <= 81 || other.GUID == actor.GUID ||
                    other.PersonData[61] == actor.PersonData[61] || !IsMutualFriend(actor, other, threshold) ||
                    !seen.Add(other.GUID)) continue;
                int rank = liveRanks != null && liveRanks.TryGetValue(other.GUID, out var live)
                    ? live : other.PersonData[81];
                if (rank > 0 && rank <= FameRules.MaximumRank) total += rank;
            }
            return total;
        }
    }
}
