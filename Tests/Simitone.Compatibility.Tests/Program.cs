using Simitone.Client.Utils;

// Run the production filtering code without graphics, game assets or NuGet test packages.
static void Equal(IEnumerable<string> expected, IEnumerable<string> actual, string message)
{
    if (!expected.SequenceEqual(actual)) throw new Exception(message);
}

var catalog = new List<string> { "custom_light", "shared", "custom_dark" };
var original = catalog.ToArray();
var lightTextures = new List<string> { "light.bmp", "shared_light.bmp", null };
var lightHands = new List<string> { "light_hands", "shared_light_hands", "dark_hands" };
var light = SkinCatalog.FilterAvailable(catalog, lightTextures, lightHands);
Equal(new[] { "custom_light", "shared" }, light, "Light selection");
Equal(new[] { "light.bmp", "shared_light.bmp" }, lightTextures, "Light textures");
Equal(new[] { "light_hands", "shared_light_hands" }, lightHands, "Hand alignment");
Equal(original, catalog, "Filtering must preserve the shared BCF catalogue");

// Switching tone must recover a skin excluded by the previous selection.
var darkTextures = new List<string> { null, "shared_dark.bmp", "dark.bmp" };
var dark = SkinCatalog.FilterAvailable(catalog, darkTextures);
Equal(new[] { "shared", "custom_dark" }, dark, "Switching tone lost custom skin");
Equal(new[] { "shared_dark.bmp", "dark.bmp" }, darkTextures, "Dark texture alignment");
Equal(original, catalog, "Repeated selections mutated catalogue");

var none = SkinCatalog.FilterAvailable(catalog, new List<string> { null, null, null });
Equal(Array.Empty<string>(), none, "All unavailable");
Equal(original, catalog, "All-unavailable selection mutated catalogue");
var all = SkinCatalog.FilterAvailable(catalog, new List<string> { "a", "b", "c" });
all.Clear();
Equal(original, catalog, "All-available selection must still return an independent list");

var adjacentTextures = new List<string> { null, null, "c" };
var adjacentHands = new List<string> { "a", "b", "c" };
Equal(new[] { "custom_dark" }, SkinCatalog.FilterAvailable(catalog, adjacentTextures, adjacentHands),
    "Adjacent missing entries skipped");
Equal(new[] { "c" }, adjacentHands, "Adjacent companion alignment");
Equal(Array.Empty<string>(), SkinCatalog.FilterAvailable(Array.Empty<string>(), new List<string>()),
    "Empty catalogue");
Console.WriteLine("PASS: tone switching, catalogue preservation, texture/hand alignment, adjacent missing entries, empty/all/none available.");

if (SkinCatalog.HasCompatibleSelection(null, catalog) || SkinCatalog.HasCompatibleSelection(catalog, Array.Empty<string>()) ||
    !SkinCatalog.HasCompatibleSelection(catalog, new[]{"body"})) throw new Exception("CAS availability gate failed");
Console.WriteLine("PASS: CAS availability gates reject missing/empty catalogues and accept populated selections.");
