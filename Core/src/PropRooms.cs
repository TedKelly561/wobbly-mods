using System;
using System.Collections.Generic;
using System.Linq;

namespace WobblyMods.Core
{
    public enum Room
    {
        Bedroom,
        Living,
        Kitchen,
        Bathroom,
        Kids,
        Garden,
        Other
    }

    public static class PropRooms
    {
        private static readonly string[] KidsKeywords = { "toy", "toys", "teddy", "plush", "doll", "block", "blocks", "crib", "rocking", "dinosaur", "castle", "ball", "puzzle", "crayon", "bunk" };
        private static readonly string[] BathroomKeywords = { "toilet", "bath", "bathtub", "shower", "sink", "basin", "towel", "mirror", "toothbrush", "bathroom" };
        private static readonly string[] KitchenKeywords = { "fridge", "oven", "stove", "cooker", "microwave", "kitchen", "counter", "cupboard", "toaster", "kettle", "dishwasher", "pan", "pot", "plate", "cup", "mug", "bowl" };
        private static readonly string[] BedroomKeywords = { "bed", "wardrobe", "dresser", "nightstand", "bedside", "pillow", "duvet", "closet" };
        private static readonly string[] LivingKeywords = { "sofa", "couch", "armchair", "chair", "table", "tv", "television", "lamp", "rug", "bookshelf", "bookcase", "shelf", "cabinet", "fireplace", "painting", "picture", "clock", "plant", "vase", "curtain", "desk", "stool", "bench", "piano" };
        private static readonly string[] GardenKeywords = { "tree", "bush", "flower", "flowers", "fence", "swing", "slide", "pool", "bbq", "grill", "hedge", "garden", "gnome", "rock", "trampoline", "sandbox", "birdbath", "mailbox", "outdoor" };

        private static readonly string[] FurnitureExclusions = { "vehicle", "npc", "player", "character", "fx", "vfx", "particle", "effect", "ui", "collider", "trigger", "spawner", "test", "debug", "lod", "wall", "floor", "ceiling", "door", "window", "road", "building", "terrain" };

        public static Room Classify(string address)
        {
            if (string.IsNullOrEmpty(address))
                return Room.Other;

            try
            {
                // Check folder wins first
                string lowerPath = address.ToLowerInvariant();
                
                // Define folder mappings: folder name -> Room
                var folderRules = new[]
                {
                    ("toys", Room.Kids),
                    ("bathroom", Room.Bathroom),
                    ("kitchen", Room.Kitchen),
                    ("bedroom", Room.Bedroom),
                    ("garden", Room.Garden),
                    ("outdoor", Room.Garden) // Outdoor maps to Garden per rules
                };

                foreach (var rule in folderRules)
                {
                    string folderMarker = "/" + rule.Item1 + "/";
                    if (lowerPath.Contains(folderMarker))
                    {
                        return rule.Item2;
                    }
                }

                // If no folder match, look at filename words
                string fileName = System.IO.Path.GetFileName(address);
                if (string.IsNullOrEmpty(fileName))
                    return Room.Other;

                string nameWithoutExt = RemoveExtension(fileName);
                var words = SplitWords(nameWithoutExt);

                // Check in order: Kids, Bathroom, Kitchen, Bedroom, Living, Garden
                if (HasKeyword(words, KidsKeywords)) return Room.Kids;
                if (HasKeyword(words, BathroomKeywords)) return Room.Bathroom;
                if (HasKeyword(words, KitchenKeywords)) return Room.Kitchen;
                if (HasKeyword(words, BedroomKeywords)) return Room.Bedroom;
                if (HasKeyword(words, LivingKeywords)) return Room.Living;
                if (HasKeyword(words, GardenKeywords)) return Room.Garden;

                return Room.Other;
            }
            catch
            {
                return Room.Other;
            }
        }

        public static string NiceName(string address)
        {
            if (string.IsNullOrEmpty(address))
                return "thing";

            try
            {
                string fileName = System.IO.Path.GetFileName(address);
                if (string.IsNullOrEmpty(fileName))
                    return "thing";

                string nameWithoutExt = RemoveExtension(fileName);
                var words = SplitWords(nameWithoutExt);

                var resultWords = new List<string>();
                foreach (var word in words)
                {
                    string lowerWord = word.ToLowerInvariant();
                    // Drop numbers, "prefab", "lod0", "lod1"
                    if (lowerWord == "prefab") continue;
                    // Words are split on digits, so "lod0" / "lod1" arrive here as plain "lod".
                    if (lowerWord == "lod" || lowerWord == "lod0" || lowerWord == "lod1") continue;
                    
                    // Check if it's purely numeric
                    bool isNumeric = true;
                    foreach (char c in word)
                    {
                        if (!char.IsDigit(c))
                        {
                            isNumeric = false;
                            break;
                        }
                    }
                    if (isNumeric && word.Length > 0) continue;

                    resultWords.Add(lowerWord);
                }

                if (resultWords.Count == 0)
                    return "thing";

                return string.Join(" ", resultWords);
            }
            catch
            {
                return "thing";
            }
        }

        public static bool LooksLikeFurniture(string address)
        {
            if (string.IsNullOrEmpty(address))
                return false;

            try
            {
                // Must contain /Props/ or /Furniture/
                string lowerPath = address.ToLowerInvariant();
                bool hasPropFolder = lowerPath.Contains("/props/") || lowerPath.Contains("/furniture/");
                if (!hasPropFolder)
                    return false;

                // Get filename words
                string fileName = System.IO.Path.GetFileName(address);
                if (string.IsNullOrEmpty(fileName))
                    return true; // If no filename, but path has props, assume furniture? Or false? 
                                 // Rules say "return false if the address's words include any of...". 
                                 // If no words, it doesn't include exclusions. But usually empty means invalid.
                                 // Let's stick to: if path is valid prop path, and no exclusion words found.

                string nameWithoutExt = RemoveExtension(fileName);
                var words = SplitWords(nameWithoutExt);

                foreach (var word in words)
                {
                    string lowerWord = word.ToLowerInvariant();
                    foreach (var exclusion in FurnitureExclusions)
                    {
                        if (lowerWord == exclusion)
                            return false;
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string RemoveExtension(string fileName)
        {
            int lastDot = fileName.LastIndexOf('.');
            if (lastDot > 0 && lastDot < fileName.Length - 1)
            {
                return fileName.Substring(0, lastDot);
            }
            return fileName;
        }

        private static string[] SplitWords(string input)
        {
            // Split on _ - space digits and camelCase
            // First, handle camelCase by inserting spaces
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (i > 0 && char.IsLower(input[i - 1]) && char.IsUpper(c))
                {
                    sb.Append(' ');
                }
                sb.Append(c);
            }
            string camelCaseSplit = sb.ToString();

            // Split on _ - space digits
            // Replace delimiters with spaces
            for (int i = 0; i < camelCaseSplit.Length; i++)
            {
                char c = camelCaseSplit[i];
                if (c == '_' || c == '-' || c == ' ')
                {
                    sb[i] = ' ';
                }
                else if (char.IsDigit(c))
                {
                    sb[i] = ' ';
                }
            }

            string replaced = sb.ToString();
            
            // Split by space and filter empty
            return replaced.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        }

        private static bool HasKeyword(string[] words, string[] keywords)
        {
            foreach (string word in words)
            {
                string lowerWord = word.ToLowerInvariant();
                foreach (string keyword in keywords)
                {
                    if (lowerWord == keyword)
                        return true;
                }
            }
            return false;
        }
    }
}
