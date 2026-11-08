// SPDX-License-Identifier: MPL-2.0
using System;

namespace FSO.Content.TS1.EnhancedSafety
{
    public static class ClothingNameClassifier
    {
        public static bool TryGetType(string name, out string type)
        {
            type = null;
            if (name == null || name.Length < 4 || !char.IsDigit(name[1]) ||
                !char.IsDigit(name[2]) || !char.IsDigit(name[3])) return false;
            var end = name.IndexOf('_');
            if (end >= 0 && end < 4) return false;
            if (end < 0) end = name.Length;
            type = name.Substring(4, end - 4);
            if (type.EndsWith("lgt", StringComparison.Ordinal) || type.EndsWith("med", StringComparison.Ordinal) ||
                type.EndsWith("drk", StringComparison.Ordinal)) type = type.Substring(0, type.Length - 3);
            return true;
        }
    }
}
