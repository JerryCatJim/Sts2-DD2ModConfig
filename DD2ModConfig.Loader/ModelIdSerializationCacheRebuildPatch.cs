using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Serialization;

namespace DD2ModConfig.Loader
{
    /// <summary>
    /// After ModelIdSerializationCache.Init() runs, ensures that all models
    /// from dynamically loaded variant assemblies have proper net IDs in the
    /// serialization cache. This prevents state divergence in multiplayer
    /// caused by missing or incorrect CategorySortingId / EntrySortingId.
    ///
    /// Based on RitsuLib's ModelIdSerializationCacheDynamicContentPatch.
    /// </summary>
    [HarmonyPatch(typeof(ModelIdSerializationCache), nameof(ModelIdSerializationCache.Init))]
    [HarmonyPriority(int.MaxValue)]
    internal static class ModelIdSerializationCacheRebuildPatch
    {
        private static readonly FieldInfo? _catMapField =
            typeof(ModelIdSerializationCache).GetField("_categoryNameToNetIdMap",
                BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo? _catListField =
            typeof(ModelIdSerializationCache).GetField("_netIdToCategoryNameMap",
                BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo? _entMapField =
            typeof(ModelIdSerializationCache).GetField("_entryNameToNetIdMap",
                BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo? _entListField =
            typeof(ModelIdSerializationCache).GetField("_netIdToEntryNameMap",
                BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly PropertyInfo? _catBitSizeProp =
            typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.CategoryIdBitSize),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly PropertyInfo? _entBitSizeProp =
            typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.EntryIdBitSize),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly PropertyInfo? _hashProp =
            typeof(ModelIdSerializationCache).GetProperty(nameof(ModelIdSerializationCache.Hash),
                BindingFlags.Public | BindingFlags.Static);

        private static readonly FieldInfo? _contentByIdField =
            typeof(ModelDb).GetField("_contentById",
                BindingFlags.NonPublic | BindingFlags.Static);

        /// <summary>
        /// Harmony Postfix on ModelIdSerializationCache.Init().
        /// Catches the case where Init runs AFTER our variant DLL is loaded.
        /// </summary>
        static void Postfix()
        {
            TryRebuild();
        }

        /// <summary>
        /// Ensures all models in ModelDb.All have entries in the serialization cache.
        /// Called both from the Harmony Postfix and proactively from Bootstrap after
        /// variant DLL loading (to handle the case where Init already ran).
        /// </summary>
        public static void TryRebuild()
        {
            if (_catMapField == null || _catListField == null ||
                _entMapField == null || _entListField == null)
            {
                Log.Warn("[DD2ModConfig.Loader] ModelIdSerializationCache internals not accessible; skipping rebuild.");
                return;
            }

            var catMap = (Dictionary<string, int>)_catMapField.GetValue(null)!;
            var catList = (List<string>)_catListField.GetValue(null)!;
            var entMap = (Dictionary<string, int>)_entMapField.GetValue(null)!;
            var entList = (List<string>)_entListField.GetValue(null)!;

            // Access ModelDb._contentById to get all registered models
            var contentById = _contentByIdField?.GetValue(null) as IDictionary;
            if (contentById == null || contentById.Count == 0)
                return;

            // Collect missing entries in deterministic order (sorted alphabetically)
            var missingCategories = new SortedSet<string>(StringComparer.Ordinal);
            var missingEntries = new SortedSet<string>(StringComparer.Ordinal);

            foreach (DictionaryEntry kv in contentById)
            {
                if (kv.Key is not ModelId id) continue;
                if (!catMap.ContainsKey(id.Category))
                    missingCategories.Add(id.Category);
                if (!entMap.ContainsKey(id.Entry))
                    missingEntries.Add(id.Entry);
            }

            if (missingCategories.Count == 0 && missingEntries.Count == 0)
                return;

            foreach (var cat in missingCategories)
            {
                catMap[cat] = catList.Count;
                catList.Add(cat);
            }

            foreach (var ent in missingEntries)
            {
                entMap[ent] = entList.Count;
                entList.Add(ent);
            }

            Log.Info($"[DD2ModConfig.Loader] Rebuilt ModelIdSerializationCache: " +
                     $"+{missingCategories.Count} categories, +{missingEntries.Count} entries " +
                     $"(total: {catList.Count} categories, {entList.Count} entries).");

            // Recompute bit sizes
            if (_catBitSizeProp != null)
                _catBitSizeProp.GetSetMethod(true)?.Invoke(null,
                    [(int)Math.Ceiling(Math.Log2(Math.Max(catList.Count, 2)))]);

            if (_entBitSizeProp != null)
                _entBitSizeProp.GetSetMethod(true)?.Invoke(null,
                    [(int)Math.Ceiling(Math.Log2(Math.Max(entList.Count, 2)))]);

            // Recompute hash using stable ordering
            if (_hashProp != null)
            {
                var newHash = ComputeStableHash(catList, entList);
                _hashProp.GetSetMethod(true)?.Invoke(null, [newHash]);
            }
        }

        private static uint ComputeStableHash(List<string> categories, List<string> entries)
        {
            // Use FNV-1a 32-bit hash for deterministic, cross-platform consistency.
            // This matches what the game expects from ModelIdSerializationCache.Hash.
            unchecked
            {
                uint hash = 2166136261u;
                foreach (var cat in categories)
                {
                    foreach (char c in cat)
                    {
                        hash ^= c;
                        hash *= 16777619u;
                    }
                    hash ^= 0xFFu;
                    hash *= 16777619u;
                }
                foreach (var ent in entries)
                {
                    foreach (char c in ent)
                    {
                        hash ^= c;
                        hash *= 16777619u;
                    }
                    hash ^= 0xFFu;
                    hash *= 16777619u;
                }
                return hash;
            }
        }
    }
}
