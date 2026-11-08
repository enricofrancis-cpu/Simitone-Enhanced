// SPDX-License-Identifier: MPL-2.0
// Based on pinned FreeSO d8a3c175; Simitone Enhanced compatibility overlay.
using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Content.TS1;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.SimAntics.Utils
{
    public static class VMTS1PurchasableOutfitHelper
    {
        public static string[] OutfitTypes = new string[] { "b", "f", "s", "l", "w", "h" };
        public static short[] OutfitTypeToInd = new short[] { 1, 30, 31, 32, 33, 34 };

        public static Tuple<string, string>[] GetValidOutfits(VMAvatar avatar, short outfitType)
        {
            string skin = "";
            string collectionType, simtype;
            if (avatar == null || outfitType < 0)
            {
                collectionType = "b";
                simtype = (outfitType == -1) ? "dog" : "cat";
            }
            else {
                if (avatar.IsCat)
                {
                    collectionType = "b";
                    simtype = "cat";
                }
                else if (avatar.IsDog)
                {
                    collectionType = "b";
                    simtype = "dog";
                }
                else
                {
                    if (outfitType >= OutfitTypes.Length) return Array.Empty<Tuple<string, string>>();
                    collectionType = OutfitTypes[outfitType];

                    var bodyStrings = avatar.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(avatar.Object.OBJ.BodyStringID);
                    var body = bodyStrings?.GetString(1);
                    if (body == null || body.Length < 5) return Array.Empty<Tuple<string, string>>();
                    simtype = body.Substring(4);
                    var separator = simtype.IndexOf('_');
                    if (separator < 0) return Array.Empty<Tuple<string, string>>();
                    simtype = simtype.Substring(0, separator).ToLowerInvariant();
                    skin = (bodyStrings.GetString(14) ?? "").ToLowerInvariant();
                }
            }
            
            if (!Content.Content.Get().BCFGlobal.CollectionsByName.TryGetValue(collectionType, out var col) ||
                !col.ClothesByAvatarType.TryGetValue(simtype, out var catalog)) return Array.Empty<Tuple<string, string>>();
            var bodies = new List<string>(catalog); // Filtering must never mutate the shared catalogue.

            var tex = (TS1AvatarTextureProvider)Content.Content.Get().AvatarTextures;
            var texnames = tex.GetAllNames();

            var bodyTex = bodies.Select(x => RemoveExt(texnames.FirstOrDefault(y => y.StartsWith(ExtractID(x, skin))))).ToList();
            var handgroupTex = bodies.Select(x => (RemoveExt(texnames.FirstOrDefault(y => y == "huao" + FindHG(x))) ?? "huao" + skin).Substring(4)).ToList();

            var result = new List<Tuple<string, string>>();
            for (int i = 0; i < bodies.Count; i++)
            {
                if (bodyTex[i] == null)
                {
                    bodyTex.RemoveAt(i);
                    handgroupTex.RemoveAt(i);
                    bodies.RemoveAt(i--);
                } else
                {
                    result.Add(new Tuple<string, string>(
                        bodies[i] + ",BODY=" + bodyTex[i],
                        handgroupTex[i]
                        ));
                }
            }

            return result.ToArray();
        }

        public static short GetSuitIndex(VMAvatar avatar, short outfitType)
        {
            if (avatar == null || outfitType < 0 || outfitType >= OutfitTypeToInd.Length) return -1;
            var validSuits = GetValidOutfits(avatar, outfitType);
            var bodyStrings = avatar.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(avatar.Object.OBJ.BodyStringID);

            var bsInd = OutfitTypeToInd[outfitType];
            var current = bodyStrings?.GetString(bsInd)?.Split(';')[0];
            var prevInd = Array.FindIndex(validSuits, x => string.Equals(x.Item1, current, StringComparison.OrdinalIgnoreCase));
            return (short)prevInd;
        }

        public static short SetSuit(VMAvatar avatar, short outfitType, short newIndex)
        {
            if (avatar == null || outfitType < 0 || outfitType >= OutfitTypeToInd.Length) return -1;
            var validSuits = GetValidOutfits(avatar, outfitType);
            var bodyStrings = avatar.Object.Resource.Get<FSO.Files.Formats.IFF.Chunks.STR>(avatar.Object.OBJ.BodyStringID);

            var bsInd = OutfitTypeToInd[outfitType];
            if (bodyStrings == null || newIndex < 0 || newIndex >= validSuits.Length) return -1;
            var current = bodyStrings.GetString(bsInd)?.Split(';')[0];
            var prevInd = Array.FindIndex(validSuits, x => string.Equals(x.Item1, current, StringComparison.OrdinalIgnoreCase));

            var newSuit = validSuits[newIndex];
            bodyStrings.SetString(bsInd, newSuit.Item1, STRLangCode.EnglishUS);

            if (outfitType == 0 && newSuit.Item2 != "")
            {
                //right now only replace the handgroup when changing the default outfit.
                var hg = newSuit.Item2;
                bodyStrings.SetString(17, TilEQ(bodyStrings.GetString(17, STRLangCode.EnglishUS)) + "huao" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(18, TilEQ(bodyStrings.GetString(18, STRLangCode.EnglishUS)) + "huao" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(19, TilEQ(bodyStrings.GetString(19, STRLangCode.EnglishUS)) + "huap" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(20, TilEQ(bodyStrings.GetString(20, STRLangCode.EnglishUS)) + "huap" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(21, TilEQ(bodyStrings.GetString(21, STRLangCode.EnglishUS)) + "huac" + hg, STRLangCode.EnglishUS);
                bodyStrings.SetString(22, TilEQ(bodyStrings.GetString(22, STRLangCode.EnglishUS)) + "huac" + hg, STRLangCode.EnglishUS);
            }

            Content.Content.Get().Neighborhood.AvatarChanged(avatar.Object.OBJ.GUID);

            return (short)prevInd;
        }

        public static string TilEQ(string item)
        {
            var ind = item.IndexOf('=');
            if (ind != -1) item = item.Substring(ind);
            return item;
        }

        private static string FindHG(string item)
        {
            var ind = item.IndexOf('_');
            if (ind != -1) item = item.Substring(ind);
            return item;
        }

        private static string RemoveExt(string item)
        {
            if (item == null) return null;
            var ind = item.LastIndexOf('.');
            if (ind != -1) return item.Substring(0, ind);
            return item;
        }

        private static string ExtractID(string item, string skncol)
        {
            var ind = item.IndexOf('_');
            if (ind != -1) item = item.Substring(0, ind);
            return item + skncol;
        }

        private static string InsertSkinColor(string name, string skncol)
        {
            var ind = name.IndexOf('_');
            if (ind != -1) name = name.Insert(ind, skncol);
            return name;
        }
    }
}
