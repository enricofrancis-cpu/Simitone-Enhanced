using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using FSO.Common.Content;
using FSO.Content;
using FSO.Content.Framework;
using FSO.Content.TS1;
using FSO.Content.TS1.EnhancedSafety;
using FSO.Files.Formats.IFF;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Vitaboy;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static void Reject(Action action, string message) {
    try { action(); } catch (IOException) { return; } catch (InvalidDataException) { return; }
    throw new Exception(message);
}
static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
static IffFile Character(uint guid) {
    var file=new IffFile();
    file.AddChunk(new OBJD { ChunkID=1,ChunkProcessed=true,ChunkLabel="Synthetic character",GUID=guid,ObjectType=OBJDType.Person });
    return file;
}
static void WriteCharacter(string path,uint guid) { using var output=File.Create(path); Character(guid).Write(output); }
static void MakeNeighborhood(string path) {
    Directory.CreateDirectory(Path.Combine(path,"Characters")); Directory.CreateDirectory(Path.Combine(path,"Houses"));
    foreach (var name in new[]{"Neighborhood.iff","LotLocations.iff","LotZoning.iff","StreetNames.iff","Houses/NeighborhoodDesc.iff","Houses/STDesc.iff","Houses/MTDesc.iff"})
        File.WriteAllText(Path.Combine(path,name),"synthetic "+name);
    File.WriteAllText(Path.Combine(path,"Characters","keep.txt"),"source must remain intact");
}
static BCF DecodeCmx(string text)
{
    using var input = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(text));
    return (BCF)new FSO.Content.Codecs.CMXCodec().GenDecode(input);
}
const string appearanceOnly = "0\n1\nSynthetic\n0\n0\n1\nROOT\nmesh\n0\n0";
Check(DecodeCmx("Version 300\n" + appearanceOnly + "\n0\u001a").Appearances[0].Name == "Synthetic",
    "uppercase CMX header or DOS EOF marker broke the appearance");
Check(DecodeCmx("version 300\n" + appearanceOnly).Animations.Length == 0,
    "a complete appearance ending before the optional animation count was rejected");
Check(DecodeCmx("Custom mesh by Author\n" + appearanceOnly).Appearances[0].Bindings.Length == 1,
    "headerless legacy CMX appearance was rejected");
Reject(() => DecodeCmx("version 300\n" + appearanceOnly[..^2]),
    "CMX with an incomplete required binding was accepted");
using (var binaryCmx = new MemoryStream())
{
    new BCF(Array.Empty<Skeleton>(), Array.Empty<Appearance>(), Array.Empty<Animation>()).Write(binaryCmx, false);
    using var binaryInput = new MemoryStream(binaryCmx.ToArray());
    var bcf = (BCF)new FSO.Content.Codecs.CMXCodec().GenDecode(binaryInput);
    Check(bcf.Appearances.Length == 0, "binary BCF with a CMX extension was rejected");
}

var root=Path.Combine(Path.GetTempPath(),"simitone-engine-safety-"+Guid.NewGuid().ToString("N"));
try {
    Check(typeof(CharacterFileStore).Assembly==typeof(TS1NeighborhoodProvider).Assembly,"safety helpers were not built into the real engine");
    var characters=Path.Combine(root,"Characters"); Directory.CreateDirectory(characters);
    WriteCharacter(Path.Combine(characters,"User00000.iff"),0x1111);
    File.WriteAllText(Path.Combine(characters,"desktop.ini"),"ignored");
    WriteCharacter(Path.Combine(characters,"User00002.IFF"),0x2222);
    File.WriteAllText(Path.Combine(characters,"User99999.iff.backup"),"ignored");
    WriteCharacter(Path.Combine(characters,"custom.iff"),0x3333);
    var snapshots=Directory.GetFiles(characters).ToDictionary(p=>p,Hash);
    // Bypass only the asset-loading constructor; exercise the production LoadCharacters method.
    var content=(Content)RuntimeHelpers.GetUninitializedObject(typeof(Content));
    var jobLevel = (JobLevel)RuntimeHelpers.GetUninitializedObject(typeof(JobLevel));
    jobLevel.Salary = 250;
    jobLevel.CarType = 3;
    var jobs = (TS1JobProvider)RuntimeHelpers.GetUninitializedObject(typeof(TS1JobProvider));
    jobs.JobResource = new IffFile();
    jobs.JobResource.AddChunk(new CARR { ChunkID = 1, ChunkProcessed = true, JobLevels = new[] { jobLevel } });
    Check(jobs.GetJobData(1, -1, 0) == 1, "career level count requires a current level");
    Check(jobs.GetJobData(1, -1, 21) == 0 && jobs.GetJobData(1, 1, 21) == 0, "invalid car lookup level escaped the guard");
    Check(jobs.GetJobData(1, 0, -1) == 0 && jobs.GetJobData(1, 0, 23) == 0, "invalid job selector escaped the guard");
    Check(jobs.GetJobData(1, 0, 1) == 250 && jobs.GetJobData(1, 0, 21) == 3, "valid salary or car lookup changed");
    Check(jobs.GetJobData(2, 0, 21) == 0, "missing career lookup changed");

    content.TS1 = true;
    content.TS1BasePath = root;
    content.TS1AllFiles = new[] { "GameData/Roofs/default.bmp", "GameData/Roofs/tile.bmp" };
    var roofs = new WorldRoofProvider(content);
    roofs.Init();
    Check(roofs.IDToName(-1) == "default.bmp" && roofs.IDToName(2) == "default.bmp", "missing roof did not select the default");
    Check(roofs.IDToName(1) == "tile.bmp", "valid roof selection changed");
    content.TS1AllFiles = Array.Empty<string>();
    var emptyRoofs = new WorldRoofProvider(content);
    emptyRoofs.Init();
    Check(emptyRoofs.IDToName(0) == "", "empty roof catalog did not report a missing texture");

    content.TS1 = false;
    content.TS1BasePath = null;
    content.TS1AllFiles = null;
    var objects=new TS1ObjectProvider(content,new TS1Provider(content)) { Entries=new() };
    content.WorldObjects=objects;
    var neighborhood=(TS1NeighborhoodProvider)RuntimeHelpers.GetUninitializedObject(typeof(TS1NeighborhoodProvider));
    neighborhood.ContentManager=content; neighborhood.UserPath=root;
    neighborhood.LoadCharacters(false);
    Check(objects.Entries.Keys.Order().SequenceEqual(new ulong[]{0x1111,0x2222,0x3333}),"real loader skipped valid characters after unrelated files or uppercase extension");
    Check(neighborhood.NextSim==3,"real loader's filename scan is incomplete");
    neighborhood.LoadCharacters(true);
    Check(objects.Entries.Count==3,"repeated load lost character registrations");
    // Force a stale allocator hint and verify real SaveNewNeighbour cannot replace User00000.
    neighborhood.NextSim=0;
    var iff=Character(0x4444); var objd=iff.Get<OBJD>(1);
    var obj=new GameObject {GUID=0x4444,OBJ=objd,Resource=new GameObjectResource(iff,null,null,"synthetic",content)};
    neighborhood.SaveNewNeighbour(obj);
    Check(objects.Entries[0x4444].FileName.EndsWith("User00001.iff",StringComparison.Ordinal),"collision did not select an unused filename");
    Check(new IffFile(objects.Entries[0x4444].FileName).Get<OBJD>(1).GUID==0x4444,"saved character IFF did not roundtrip");
    foreach(var pair in snapshots) Check(Hash(pair.Key)==pair.Value,"existing character or unrelated file was overwritten");
    var broken=Character(0x5555);
    broken.AddChunk(new FailingChunk {ChunkID=2,ChunkType="FAIL",ChunkLabel="fault injection",ChunkProcessed=true});
    var beforeFailedSave=neighborhood.NextSim;
    var beforeFailedFiles=Directory.GetFiles(characters).ToDictionary(p=>p,Hash);
    var brokenObj=new GameObject {GUID=0x5555,OBJ=broken.Get<OBJD>(1),Resource=new GameObjectResource(broken,null,null,"synthetic",content)};
    Reject(()=>neighborhood.SaveNewNeighbour(brokenObj),"real SaveNewNeighbour swallowed a serialization failure");
    Check(neighborhood.NextSim==beforeFailedSave && !objects.Entries.ContainsKey(0x5555),
        "real failed save changed allocation or registration");
    Check(Directory.GetFiles(characters).Length==beforeFailedFiles.Count,
        "real failed save published a partial character or leaked a temporary file");
    foreach(var pair in beforeFailedFiles) Check(Hash(pair.Key)==pair.Value,
        "real failed save changed an existing character file");
    var next=5;
    Reject(()=>CharacterFileStore.WriteNew(characters,ref next,stream=>{ stream.WriteByte(1); throw new IOException("injected serializer failure"); }),"serializer failure was swallowed");
    Check(next==5 && !File.Exists(Path.Combine(characters,"User00005.iff")) && !Directory.EnumerateFiles(characters,"*.tmp").Any(),"failed character write published partial state");
    // A competing writer claims the selected path while serialization is in progress.
    next=7;
    var raced=CharacterFileStore.WriteNew(characters,ref next,stream=>{
        File.WriteAllText(Path.Combine(characters,"User00007.iff"),"competing writer"); stream.WriteByte(9);
    });
    Check(raced.EndsWith("User00008.iff") && File.ReadAllText(Path.Combine(characters,"User00007.iff"))=="competing writer","competing character was overwritten");
    File.WriteAllText(Path.Combine(characters,"USER00009.IFF"),"uppercase collision"); next=9;
    var caseSafe=CharacterFileStore.WriteNew(characters,ref next,stream=>stream.WriteByte(10));
    Check(caseSafe.EndsWith("User00010.iff") && File.ReadAllText(Path.Combine(characters,"USER00009.IFF"))=="uppercase collision","case-insensitive collision was ignored");
    Check(CharacterFileStore.NextIndex(new[]{"User99999.IFF","User00000.iff"})==1,"exhausted high-water hint did not find a gap");

    // Decode a real synthetic BCF, then exercise the real catalogue provider Init.
    var names=new[]{"","b","b1","b12","b001mafitlgt_one","c002faheadmed_two","b003mafit"};
    var bcf=new BCF(Array.Empty<Skeleton>(),names.Select(n=>new Appearance {Name=n,Bindings=Array.Empty<AppearanceBinding>()}).ToArray(),Array.Empty<Animation>());
    using var bcfBytes=new MemoryStream(); bcf.Write(bcfBytes,false); bcfBytes.Position=0;
    var decoded=new BCF(bcfBytes,false);
    var catalog=new TS1BCFProvider(content,null) {
        BCFProvider=new FixtureSubProvider<BCF>(new(){{"fixture.bcf",new FixtureReference(decoded)}}),
        CFPProvider=new FixtureSubProvider<CFP>(new())
    };
    catalog.Init();
    Check(catalog.FailedFiles.Count==0 && catalog.SkinHostBCF.Count==names.Length,"short/non-clothing appearances were discarded with their whole file");
    Check(catalog.CollectionsByName["b"].ClothesByAvatarType["mafit"].Count==2 &&
        catalog.CollectionsByName["c"].ClothesByAvatarType["fahead"].Single()=="c002faheadmed_two","valid clothing or skin-tone classification changed");
    Check(((Appearance)catalog.Get("b1.apr",typeof(Appearance))).Name=="b1","non-clothing appearance is no longer resolvable");
    foreach(var name in new[]{"","b","b1","b12","_001bad","a_not_clothing"})
        Check(!ClothingNameClassifier.TryGetType(name,out _),"unsafe name classified as clothing");

    var source=Path.Combine(root,"Original","UserData"); var destination=Path.Combine(root,"TestProfile","UserData");
    MakeNeighborhood(source);
    var originals=Directory.GetFiles(source,"*",SearchOption.AllDirectories).ToDictionary(p=>p,Hash);
    var copied=0;
    Reject(()=>NeighborhoodImport.EnsureCopied(source,destination,(from,to)=>{
        if(++copied==3) throw new IOException("injected copy failure"); File.Copy(from,to);
    }),"copy failure was swallowed");
    Check(!Directory.Exists(destination) && !Directory.EnumerateDirectories(Path.GetDirectoryName(destination),".simitone-import-*").Any(),"failed first import published incomplete data or leaked staging");
    NeighborhoodImport.EnsureCopied(source,destination);
    Check(File.ReadAllText(Path.Combine(destination,"Characters","keep.txt"))=="source must remain intact","retry did not complete import");
    File.WriteAllText(Path.Combine(destination,"Neighborhood.iff"),"existing edited save");
    NeighborhoodImport.EnsureCopied(source,destination,(_,_)=>throw new Exception("existing saves must not be recopied"));
    Check(File.ReadAllText(Path.Combine(destination,"Neighborhood.iff"))=="existing edited save","established profile was overwritten");
    File.Delete(Path.Combine(destination,"LotLocations.iff"));
    Reject(()=>NeighborhoodImport.EnsureCopied(source,destination),"legacy incomplete profile was silently accepted");
    Check(File.ReadAllText(Path.Combine(destination,"Neighborhood.iff"))=="existing edited save","incomplete existing profile was repaired destructively");
    Reject(()=>NeighborhoodImport.EnsureCopied(source,source),"source and destination may not coincide");
    Reject(()=>NeighborhoodImport.EnsureCopied(source,Path.Combine(source,"nested")),"destination may not be inside source");
    var racingDestination=Path.Combine(root,"RacingProfile","UserData");
    var competingCopy=false;
    Reject(()=>NeighborhoodImport.EnsureCopied(source,racingDestination,(from,to)=>{
        File.Copy(from,to);
        if(!competingCopy) {
            competingCopy=true; MakeNeighborhood(racingDestination);
            File.WriteAllText(Path.Combine(racingDestination,"Neighborhood.iff"),"other import won");
        }
    }),"concurrent import should not replace the winning destination");
    Check(File.ReadAllText(Path.Combine(racingDestination,"Neighborhood.iff"))=="other import won" &&
        !Directory.EnumerateDirectories(Path.GetDirectoryName(racingDestination),".simitone-import-*").Any(),"concurrent import replaced destination data or leaked staging");
    foreach(var pair in originals) Check(Hash(pair.Key)==pair.Value,"original neighbourhood files changed");
    // Exercise discovery through the actual engine, including files that must not be opened as FAR archives.
    content.TS1BasePath=root + Path.DirectorySeparatorChar;
    content.TS1AllFiles=new[]{"GameData/Skins/lower.cmx","GameData/Skins/upper.CMX","gamedata/skins/folder.cmx",
        "GameData/Skins/lower.cfp","GameData/Skins/upper.CFP","Downloads/lower.iff","Downloads/upper.IFF",
        "GameData/Skins/hand.TGA","ExpansionShared/SkinsBuy/outfit.tga","GameData/Skins/ignored.tga.bak","GameData/Skins/ignored.cmx.backup","Downloads/ignored.iff.bak","Downloads/not-an-archive.far.backup"};
    var discovery=new TS1Provider(content); discovery.Init();
    Check(discovery.BuildDictionary(".cmx","globals").Keys.Order().SequenceEqual(new[]{"folder.cmx","lower.cmx","upper.cmx"}),"mixed-case CMX discovery or backup exclusion failed");
    Check(discovery.BuildDictionary(".cfp","globals").Keys.Order().SequenceEqual(new[]{"lower.cfp","upper.cfp"}),"mixed-case CFP discovery failed");
    Check(discovery.BuildDictionary(".iff","globals").Keys.Order().SequenceEqual(new[]{"lower.iff","upper.iff"}),"mixed-case object discovery failed");
    Check(discovery.BuildDictionary(".tga","globals").Keys.Order().SequenceEqual(new[]{"hand.tga","outfit.tga"}),"loose TGA texture discovery or backup exclusion failed");
    var animation=new Animation {Name="fixture-animation",XSkillName="fixture",TranslationCount=1,RotationCount=1};
    var badCfp=new CFP {Data=Array.Empty<byte>()};
    var animationProvider=new TS1BCFProvider(content,null) {
        BCFProvider=new FixtureSubProvider<BCF>(new(){{"fixture.bcf",new FixtureReference(new BCF(Array.Empty<Skeleton>(),Array.Empty<Appearance>(),new[]{animation}))}}),
        CFPProvider=new FixtureSubProvider<CFP>(new(){{"fixture.cfp",new FixtureReference(badCfp)}})
    };
    animationProvider.Init();
    for(int retry=0;retry<2;retry++) {
        Reject(()=>animationProvider.Get("fixture-animation.anim",typeof(Animation)),"truncated CFP returned an incomplete cached animation");
        Check(animation.Translations==null && animation.Rotations==null,"failed decoder published partial arrays");
    }
    using(var output=new MemoryStream()) {
        using(var writer=new BinaryWriter(output,System.Text.Encoding.UTF8,true))
            foreach(float value in new[]{1f,2f,3f,0f,0f,0f,1f}) {writer.Write((byte)255);writer.Write(value);}
        badCfp.Data=output.ToArray();
    }
    var validAnimation=(Animation)animationProvider.Get("fixture-animation.anim",typeof(Animation));
    Check(validAnimation.Translations[0].X==-1 && validAnimation.Translations[0].Y==2 && validAnimation.Rotations[0].W==-1,"valid CFP retry failed");
    Console.WriteLine("PASS: actual engine mixed-case discovery/backup exclusion and atomic CFP enrichment with failed retries and valid recovery.");
    Console.WriteLine("PASS: real engine character load/save and BCF catalogue initialization; overwrite/failure/case protection; staged import recovery and original/existing-save preservation (synthetic files, no game assets).");
} finally { if(Directory.Exists(root)) Directory.Delete(root,true); }

sealed class FixtureReference(object value) : IContentReference {
    public object GetGeneric()=>value;
    public object GetThrowawayGeneric()=>value;
    public override string ToString()=>"fixture.bcf";
}
sealed class FailingChunk : IffChunk {
    public override void Read(IffFile iff,Stream stream) { }
    public override bool Write(IffFile iff,Stream stream) => throw new IOException("injected real IFF serializer failure");
}
sealed class FixtureSubProvider<T>(Dictionary<string,IContentReference> entries) : TS1SubProvider<T>(null,".fixture") {
    public override void Init() => typeof(TS1SubProvider<T>).GetField("Entries",BindingFlags.Instance|BindingFlags.NonPublic)!.SetValue(this,entries);
}
