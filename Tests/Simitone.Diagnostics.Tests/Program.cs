using Simitone.Client.Utils;
static void Check(bool value,string reason) { if(!value) throw new Exception(reason); }
var root=Path.Combine(Path.GetTempPath(),"simitone-diag-test-"+Guid.NewGuid().ToString("N"));
try {
 Directory.CreateDirectory(root);
 File.WriteAllText(Path.Combine(root,"save-sentinel.iff"),"must survive");
 var created=new List<string>();
 for(int i=0;i<7;i++) { DiagnosticLog.Start(root,"synthetic-build"); created.Add(DiagnosticLog.SessionDirectory); DiagnosticLog.Event("test"); }
 Check(created.Skip(2).All(Directory.Exists) && created.Take(2).All(x=>!Directory.Exists(x)),"newest session retention order failed");
 Check(Directory.GetDirectories(Path.Combine(root,"Diagnostics"),"session-*").Length==5,"retention failed");
 var foreign=Path.Combine(root,"Diagnostics","session-000000-foreign"); Directory.CreateDirectory(foreign); File.WriteAllText(Path.Combine(foreign,"keep"),"preserve");
 DiagnosticLog.Start(root,"synthetic-build");
 DiagnosticLog.AddPrivatePath(@"C:\Users\Example\Sims", "<game-install>");
 DiagnosticLog.Event("travel-request","from=1; to=81");
 DiagnosticLog.Event("private",root+" / C:/Users/Example/Sims/GameData");
 Parallel.For(0,100,i=>DiagnosticLog.Event("concurrent",i.ToString()));
 DiagnosticLog.SimAntics("Ordinary dialog","Do not record this dialogue");
 DiagnosticLog.SimAntics("SimAntics Exception!","A SimAntics Exception has occurred, and has been suppressed: synthetic BHAV");
 DiagnosticLog.Error("save-failed",new IOException(root+"/test.iff"));
 DiagnosticLog.Error("unhandled",new InvalidOperationException("synthetic-fatal"),true);
 var session=DiagnosticLog.SessionDirectory;
 var text=File.ReadAllText(Path.Combine(session,"session.log"));
 Check(text.Contains("synthetic-build") && text.Contains("travel-request") && text.Contains("concurrent 99"),"session context missing");
 Check(!text.Contains(root) && !text.Contains("C:/Users/Example/Sims") && text.Contains("<game-install>"),"path redaction failed");
 var errors=File.ReadAllText(Path.Combine(session,"errors.log"));
 Check(errors.Contains("synthetic BHAV") && errors.Contains("save-failed") && !errors.Contains("Do not record"),"error filtering failed");
 Check(File.ReadAllText(Path.Combine(session,"crash.txt")).Contains("synthetic-fatal"),"fatal report missing");
 for(int i=0;i<160;i++) DiagnosticLog.Event("large",new string('x',20000));
 Check(new FileInfo(Path.Combine(session,"session.log")).Length<=DiagnosticLog.FileLimit,"log exceeded cap");
 Check(File.Exists(Path.Combine(foreign,"keep")) && File.ReadAllText(Path.Combine(root,"save-sentinel.iff"))=="must survive","unowned files changed");
 var blocker=Path.Combine(root,"blocked"); File.WriteAllText(blocker,"file");
 DiagnosticLog.Start(blocker,"unwritable"); DiagnosticLog.Error("test",new IOException("unwritable"),true); DiagnosticLog.Event("test");
 Check(DiagnosticLog.SessionDirectory==null,"unavailable log destination not disabled");
 Console.WriteLine("PASS: diagnostic context, redaction, concurrent writes, SimAntics filtering, fatal reports, caps/retention, failure tolerance and save/unowned-file preservation (synthetic data).");
} finally { if(Directory.Exists(root)) Directory.Delete(root,true); }
