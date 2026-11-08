using System;
using System.Collections.Generic;

namespace Simitone.Client.Utils
{
    internal static class SkinCatalog
    {
        public static bool HasCompatibleSelection(ICollection<string> heads, ICollection<string> bodies)
            => heads != null && bodies != null && heads.Count > 0 && bodies.Count > 0;

        // Texture availability depends on the selected skin tone. Never remove
        // entries from the shared BCF catalogue while building a UI selection.
        public static List<string> FilterAvailable(IEnumerable<string> catalog,
            List<string> textures, params List<string>[] companionTextures)
            => FilterAvailable(catalog, textures, null, companionTextures);

        public static List<string> FilterAvailable(IEnumerable<string> catalog,
            List<string> textures, Predicate<string> canRender, params List<string>[] companionTextures)
        {
            var available = new List<string>(catalog);
            for (int i = available.Count - 1; i >= 0; i--)
            {
                if (textures[i] != null && (canRender == null || canRender(available[i]))) continue;
                available.RemoveAt(i);
                textures.RemoveAt(i);
                foreach (var companion in companionTextures) companion.RemoveAt(i);
            }
            return available;
        }
    }
}
