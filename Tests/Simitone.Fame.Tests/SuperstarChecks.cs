using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using Simitone.Client.Superstar;
using Simitone.Client.Utils;

internal static class SuperstarChecks
{
    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    private static void Reject(Action action)
    {
        try { action(); }
        catch (InvalidDataException) { return; }
        catch (ArgumentException) { return; }
        throw new Exception("Invalid input accepted");
    }
    private static IffFile Tables()
    {
        var iff = new IffFile();
        void Table(ushort id, params string[] values)
        {
            var str = new STR { ChunkID = id, ChunkProcessed = true };
            str.LanguageSets[0].Strings = values.Select(x => new STRItem { Value = x, Comment = "" }).ToArray();
            iff.AddChunk(str);
        }
        Table(5, "2", "9", "21", "35", "52", "78", "156", "300", "550", "900", "0");
        Table(4, "0", "0", "0", "0", "2", "4", "7", "11", "14", "18", "0");
        Table(6, "0,0,0,0,0,0", "0,0,1,0,0,1", "0,0,2,1,0,2", "0,0,3,2,0,3", "0,0,4,3,0,4",
            "0,0,6,4,0,4", "0,0,6,5,0,6", "0,0,7,6,0,7", "0,0,8,7,0,8", "0,0,10,8,0,9", "0,0,0,0,0,0");
        Table(1, Enumerable.Range(0, 12).Select(i => "synthetic title " + i).ToArray());
        return iff;
    }
    private static Neighbour Neighbor(short id, uint guid, short family, short rank)
    {
        var result = new Neighbour { NeighbourID = id, GUID = guid, PersonData = new short[88], Relationships = new() };
        result.PersonData[61] = family;
        result.PersonData[81] = rank;
        return result;
    }

    public static void Run(string[] args)
    {
        var rules = FameRules.Read(Tables(), 50);
        Check(rules.Requirements.Count == 10 && rules.Titles.Count == 11, "Sentinel became an extra rank");
        for (int rank = 0; rank < 10; rank++)
        {
            var row = rules.Requirements[rank];
            var skills = row.Skills.Select(x => x * 100).ToArray();
            Check(FameProgression.Check(rules, rank, row.Score, 0, skills, row.FriendHalfStars, false).Rank == rank + 1,
                "Exact promotion boundary failed at rank " + rank);
            Check(FameProgression.Check(rules, rank, row.Score - 1, 0, skills, 1000, false).Rank == rank,
                "Promoted below score boundary");
            for (int skill = 0; skill < 6; skill++)
            {
                if (skills[skill] == 0) continue;
                var below = (int[])skills.Clone(); below[skill]--;
                Check(FameProgression.Check(rules, rank, row.Score, 0, below, 1000, false).Rank == rank,
                    "Promoted below a skill boundary");
            }
            if (row.FriendHalfStars > 0)
                Check(FameProgression.Check(rules, rank, row.Score, 0, skills, row.FriendHalfStars - 1, false).Rank == rank,
                    "Promoted below friend-power boundary");
        }
        var maximumSkills = Enumerable.Repeat(1000, 6).ToArray();
        int level = 0;
        for (int i = 0; i < 20; i++) level = FameProgression.Check(rules, level, 1000, 0, maximumSkills, 1000, true).Rank;
        Check(level == 10, "Could not progress from enrollment to five stars");
        for (int i = 0; i < 10; i++) level = FameProgression.Check(rules, level, 1, 0, maximumSkills, 1000, true).Rank;
        Check(level == 0, "Decay could not demote back to nobody");
        Check(FameProgression.Check(rules, 4, 1, 0, maximumSkills, 1000, false).Rank == 4, "Promotion-only check demoted");
        Check(FameProgression.Check(rules, 0, 1000, 1, maximumSkills, 1000, true).Rank == 0, "Ordinary career bypassed enrollment gate");
        Check(FameProgression.Check(rules, 2, 0, 0, maximumSkills, 1000, true).Rank == 2, "Unenrolled data silently rewritten");
        Reject(() => FameRules.Read(new IffFile(), 50));
        var corrupt = Tables(); corrupt.Get<STR>(6).LanguageSets[0].Strings[1].Value = "0,0,no,0,0,0";
        Reject(() => FameRules.Read(corrupt, 50));

        var actor = Neighbor(1, 101, 1, 1);
        var celebrity = Neighbor(2, 102, 2, 10);
        actor.Relationships[2] = new List<short> { 51, 0, 0 };
        celebrity.Relationships[1] = new List<short> { 51, 0, 0 };
        Check(FameFriendships.TotalHalfStars(actor, new[] { celebrity }, 50, null) == 10, "Five-star friend not counted");
        actor.Relationships[2][0] = 50;
        Check(!FameFriendships.IsMutualFriend(actor, celebrity, 50), "Strict original friendship threshold lost");
        actor.Relationships[2][0] = 51; celebrity.Relationships[1][0] = 50;
        Check(!FameFriendships.IsMutualFriend(actor, celebrity, 50), "One-way friendship counted");
        celebrity.Relationships[1][0] = 51;
        Check(FameFriendships.TotalHalfStars(actor, new[] { celebrity, celebrity }, 50,
            new Dictionary<uint, short> { [102] = 7 }) == 7, "Duplicate or stale celebrity rank counted");
        celebrity.PersonData[61] = 1;
        Check(FameFriendships.TotalHalfStars(actor, new[] { celebrity }, 50, null) == 0, "Household member counted as celebrity friend");

        var source = new short[101]; source[80] = 78; source[81] = 5; source[82] = 90;
        foreach (var field in FameProgress.SkillFields) source[(int)field] = 750;
        var captured = FameProgress.FromPersonData(source);
        source[(int)VMPersonDataVariable.CreativitySkill] = 0;
        var restored = new short[88]; restored[61] = 123;
        restored[(int)VMPersonDataVariable.JobType] = 5;
        restored[(int)VMPersonDataVariable.JobPromotionLevel] = 8;
        captured.ApplyTo(restored);
        Check(restored[(int)VMPersonDataVariable.CreativitySkill] == 750 && restored[61] == 123,
            "Away skill gains lost, aliased or overwrote unrelated data");
        Check(restored[(int)VMPersonDataVariable.JobType] == 0 && restored[(int)VMPersonDataVariable.JobPromotionLevel] == 0,
            "Agency enrollment restored a stale ordinary career");

        var fallback = new Fallback();
        var handler = new SuperstarGenericCall(fallback);
        var previousRegistration = FSO.SimAntics.VMContext.Primitives[1];
        try
        {
            FSO.SimAntics.VMContext.Primitives[1] = new VMPrimitiveRegistration(fallback)
                { Opcode = 1, Name = "test", OperandModel = typeof(VMGenericTS1CallOperand) };
            SuperstarRuntime.RegisterHandler();
            var registration = FSO.SimAntics.VMContext.Primitives[1];
            Check(registration.GetHandler() is SuperstarGenericCall, "Fame wrapper not installed before assembly");
            SuperstarRuntime.RegisterHandler();
            Check(ReferenceEquals(registration, FSO.SimAntics.VMContext.Primitives[1]), "Handler was wrapped twice");
        }
        finally { FSO.SimAntics.VMContext.Primitives[1] = previousRegistration; }
        Check(handler.Execute(null, new VMGenericTS1CallOperand { Call = VMGenericTS1CallMode.TakeTaxiHook }) == VMPrimitiveExitCode.GOTO_TRUE &&
            fallback.Count == 1, "Unrelated native call was not delegated");
        foreach (var call in new[] { VMGenericTS1CallMode.PromoteFameIfNeeded, VMGenericTS1CallMode.DemoteFameIfNeeded, VMGenericTS1CallMode.SaveSimPersistentData })
            Check(handler.Execute(new VMStackFrame(), new VMGenericTS1CallOperand { Call = call }) == VMPrimitiveExitCode.GOTO_FALSE,
                "Missing target generated false-success feedback");

        var root = Path.Combine(Path.GetTempPath(), "simitone-preview-" + Guid.NewGuid().ToString("N"));
        try
        {
            var lib = Path.Combine(root, "lib"); Directory.CreateDirectory(lib);
            Check(PreviewProfile.Resolve(Array.Empty<string>(), "normal", lib) == "normal", "Normal build default changed");
            File.WriteAllText(Path.Combine(lib, "superstar-preview.marker"), "preview");
            var profile = PreviewProfile.Resolve(Array.Empty<string>(), "normal", lib);
            Check(Path.GetFullPath(profile).TrimEnd('\\', '/') == Path.Combine(root, "TestProfile"), "Bundle used normal profile");
            Check(PreviewProfile.Resolve(new[] { "-profile=" + root }, "normal").TrimEnd('\\', '/') == root, "Explicit absolute profile lost");
            Reject(() => PreviewProfile.Resolve(new[] { "-profile=" }, "normal"));
            Reject(() => PreviewProfile.Resolve(new[] { "-profile=relative" }, "normal"));
            Reject(() => PreviewProfile.Resolve(new[] { "-profile" }, "normal"));
            Reject(() => PreviewProfile.Resolve(new[] { "-profile=" + root, "-profile=" + root }, "normal"));
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

        if (args.Length == 2 && args[0] == "--fame-resource")
        {
            var original = FameRules.Read(new IffFile(args[1]), 50);
            Check(original.Requirements[0].Score == 2 && original.Requirements[9].Score == 900, "Original tuning parser mismatch");
            Console.WriteLine("PASS: original local fame.iff parsed at runtime; ten requirements and eleven ranks.");
        }
        Console.WriteLine("PASS: all half-star gates, full progression/decay, friend policy, skill transfer, native delegation/false results, and isolated preview profiles.");
    }

    private sealed class Fallback : VMPrimitiveHandler
    {
        public int Count;
        public override VMPrimitiveExitCode Execute(VMStackFrame context, VMPrimitiveOperand args) { Count++; return VMPrimitiveExitCode.GOTO_TRUE; }
    }
}
