using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using FSO.Common;
using FSO.Files.Formats.IFF;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using Simitone.Client.Utils;

namespace Simitone.Client.Superstar
{
    internal static class SuperstarRuntime
    {
        public static FameRules Rules { get; private set; }
        public static string UnavailableReason { get; private set; }
        private static readonly Dictionary<uint, string> LastStatus = new Dictionary<uint, string>();
        public static bool PreviewToolsEnabled => File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "superstar-preview.marker"));
        public static readonly VMPersonDataVariable[] Skills = {
            VMPersonDataVariable.CookingSkill, VMPersonDataVariable.MechanicalSkill,
            VMPersonDataVariable.CharismaSkill, VMPersonDataVariable.BodySkill,
            VMPersonDataVariable.LogicSkill, VMPersonDataVariable.CreativitySkill
        };

        public static void Install(string gamePath)
        {
            Rules = null;
            LastStatus.Clear();
            try
            {
                var gameData = Child(gamePath, "GameData", true);
                var path = Child(gameData, "fame.iff", false);
                var globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");
                if (!globals.Resource.TuningCache.TryGetValue((uint)258 << 16, out var threshold))
                    throw new InvalidDataException("Global friendship tuning is missing.");
                Rules = FameRules.Read(new IffFile(path), threshold);
                UnavailableReason = null;
                Log("enabled; original fame tables loaded; friendship threshold=" + threshold +
                    "; preview policy: inclusive score/skill gates, mutual friends, one half-star per check");
            }
            catch (Exception error)
            {
                UnavailableReason = error.Message;
                Log("unavailable: " + error.Message);
            }
            RegisterHandler();
        }

        public static void RegisterHandler()
        {
            var previous = VMContext.Primitives[1];
            if (!(previous.GetHandler() is SuperstarGenericCall))
                VMContext.AddPrimitive(new VMPrimitiveRegistration(new SuperstarGenericCall(previous.GetHandler()))
                { Opcode = 1, Name = previous.Name, OperandModel = previous.OperandModel });
        }

        public static void PreviewAdvance(VMAvatar avatar)
        {
            if (!PreviewToolsEnabled || Rules == null) return;
            int rank = Math.Max(0, Math.Min(10, (int)avatar.GetPersonData(VMPersonDataVariable.TS1FameStarPower)));
            rank = Math.Min(10, rank + 1);
            avatar.SetPersonData(VMPersonDataVariable.JobType, 0);
            avatar.SetPersonData(VMPersonDataVariable.TS1FameStarPower, (short)rank);
            avatar.SetPersonData(VMPersonDataVariable.TS1FameScore,
                (short)Math.Max(avatar.GetPersonData(VMPersonDataVariable.TS1FameScore), Rules.Requirements[rank - 1].Score));
            Synchronize(avatar);
            Log("PREVIEW OVERRIDE: advanced selected Sim to rank " + rank);
        }

        public static void PreviewSkills(VMAvatar avatar)
        {
            if (!PreviewToolsEnabled) return;
            foreach (var skill in Skills) avatar.SetPersonData(skill, 1000);
            Synchronize(avatar);
            Log("PREVIEW OVERRIDE: selected Sim skills set to ten");
        }

        public static void PreviewReset(VMAvatar avatar)
        {
            if (!PreviewToolsEnabled) return;
            avatar.SetPersonData(VMPersonDataVariable.JobType, 0);
            avatar.SetPersonData(VMPersonDataVariable.TS1FameScore, 1);
            avatar.SetPersonData(VMPersonDataVariable.TS1FameStarPower, 0);
            avatar.SetPersonData(VMPersonDataVariable.TS1FameStarHighWatermark, 0);
            Synchronize(avatar);
            Log("PREVIEW OVERRIDE: selected Sim reset to enrolled, zero stars");
        }

        private static string Child(string parent, string name, bool directory)
        {
            var exact = Path.Combine(parent, name);
            if (directory ? Directory.Exists(exact) : File.Exists(exact)) return exact;
            var candidates = directory ? Directory.EnumerateDirectories(parent) : Directory.EnumerateFiles(parent);
            return candidates.FirstOrDefault(x => Path.GetFileName(x).Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? throw new FileNotFoundException("Superstar resource not found: " + name);
        }

        public static int FriendHalfStars(VMAvatar avatar, VM vm)
        {
            if (Rules == null) return 0;
            var neighborhood = FSO.Content.Content.Get().Neighborhood;
            var neighbor = neighborhood.GetNeighborByID(avatar.GetPersonData(VMPersonDataVariable.NeighborId));
            var liveRanks = new Dictionary<uint, short>();
            foreach (var live in vm.Context.ObjectQueries.Avatars.OfType<VMAvatar>())
                liveRanks[live.Object.OBJ.GUID] = live.GetPersonData(VMPersonDataVariable.TS1FameStarPower);
            return FameFriendships.TotalHalfStars(neighbor, neighborhood.Neighbors.Entries, Rules.FriendshipThreshold, liveRanks);
        }

        public static bool Synchronize(VMAvatar avatar)
        {
            var neighborhood = FSO.Content.Content.Get().Neighborhood;
            var neighbor = neighborhood.GetNeighborByID(avatar.GetPersonData(VMPersonDataVariable.NeighborId));
            if (neighbor == null || neighbor.GUID != avatar.Object.OBJ.GUID) return false;
            FameProgress.FromPersonData(avatar.GetPersonDataClone()).ApplyTo(neighbor.PersonData);
            return true;
        }

        public static bool Check(VMAvatar avatar, VM vm, bool reconcile)
        {
            if (Rules == null) return false;
            int oldRank = avatar.GetPersonData(VMPersonDataVariable.TS1FameStarPower);
            int score = avatar.GetPersonData(VMPersonDataVariable.TS1FameScore);
            var skills = Skills.Select(x => (int)avatar.GetPersonData(x)).ToArray();
            int friends = FriendHalfStars(avatar, vm);
            var decision = FameProgression.Check(Rules, oldRank, score,
                avatar.GetPersonData(VMPersonDataVariable.JobType), skills, friends, reconcile);
            var status = $"rank={oldRank}->{decision.Rank} score={score} friend-half-stars={friends} reason={decision.Reason}";
            uint guid = avatar.Object.OBJ.GUID;
            if (!LastStatus.TryGetValue(guid, out var last) || last != status)
            {
                Log("sim=" + guid.ToString("X8", CultureInfo.InvariantCulture) + " " + status);
                LastStatus[guid] = status;
            }
            if (decision.Rank == oldRank) return false;
            avatar.SetPersonData(VMPersonDataVariable.TS1FameStarPower, (short)decision.Rank);
            avatar.SetPersonData(VMPersonDataVariable.TS1FameStarHighWatermark,
                (short)Math.Max(avatar.GetPersonData(VMPersonDataVariable.TS1FameStarHighWatermark), score));
            Synchronize(avatar);
            return true;
        }

        private static void Log(string message)
        {
            Console.WriteLine("SUPERSTAR " + message);
            try { File.AppendAllText(Path.Combine(FSOEnvironment.UserDir, "superstar-preview.log"), DateTime.UtcNow.ToString("o") + " " + message + Environment.NewLine); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    internal sealed class SuperstarGenericCall : VMPrimitiveHandler
    {
        private readonly VMPrimitiveHandler Fallback;
        public SuperstarGenericCall(VMPrimitiveHandler fallback) { Fallback = fallback; }

        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args)
        {
            var call = ((VMGenericTS1CallOperand)args).Call;
            if (call != VMGenericTS1CallMode.PromoteFameIfNeeded && call != VMGenericTS1CallMode.DemoteFameIfNeeded &&
                call != VMGenericTS1CallMode.SaveSimPersistentData) return Fallback.Execute(context, args);
            var target = context.StackObject as VMAvatar;
            if (call == VMGenericTS1CallMode.SaveSimPersistentData) target = target ?? context.Caller as VMAvatar;
            if (target == null) return VMPrimitiveExitCode.GOTO_FALSE;
            bool changed = call == VMGenericTS1CallMode.SaveSimPersistentData
                ? SuperstarRuntime.Synchronize(target)
                : SuperstarRuntime.Check(target, context.VM, call == VMGenericTS1CallMode.DemoteFameIfNeeded);
            return changed ? VMPrimitiveExitCode.GOTO_TRUE : VMPrimitiveExitCode.GOTO_FALSE;
        }
    }
}
