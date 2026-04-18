using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Automatic keyword/alias generator for inventory items.
    ///
    /// Three-layer pipeline applied to every item name token:
    ///   1. English tokens  → BrandAliases / ProductAliases lookup
    ///   2. Sinhala tokens  → SinhalaWordOverrides lookup (exact match, highest fidelity)
    ///   3. Sinhala tokens  → character-level transliteration fallback (covers unknown words)
    ///
    /// Tokenisation splits on whitespace and common punctuation so that mixed-script
    /// names like "MDK ඉදිආප්ප පිටි 1kg (සුදු)" yield separate tokens per word.
    /// </summary>
    public static class KeywordGenerator
    {
        // ── 1. English brand aliases ────────────────────────────────────────────────
        private static readonly Dictionary<string, string[]> BrandAliases =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["edin"]         = new[] { "edin", "edinborough" },
            ["edinborough"]  = new[] { "edin", "edinborough" },
            ["raigam"]       = new[] { "raigam", "rayigam" },
            ["rayigam"]      = new[] { "raigam", "rayigam" },
            ["manchi"]       = new[] { "manchi", "manchee" },
            ["manchee"]      = new[] { "manchi", "manchee" },
            ["maliban"]      = new[] { "maliban" },
            ["mdk"]          = new[] { "mdk" },
            ["laoji"]        = new[] { "laoji", "laaoji", "lawoji" },
            ["heladiva"]     = new[] { "heladiva", "heladiwa" },
            ["heladiwa"]     = new[] { "heladiva", "heladiwa" },
            ["watawala"]     = new[] { "watawala", "vatawala" },
            ["vatawala"]     = new[] { "watawala", "vatawala" },
            ["zesta"]        = new[] { "zesta", "sesta" },
            ["steuart"]      = new[] { "steuart", "stuwert", "stuart" },
            ["harischandra"] = new[] { "harischandra" },
            ["cargills"]     = new[] { "cargills", "cargils" },
            ["cargils"]      = new[] { "cargills", "cargils" },
            ["sunquick"]     = new[] { "sunquick", "sunquik" },
            ["sunquik"]      = new[] { "sunquick", "sunquik" },
            ["cheris"]       = new[] { "cheris", "cherish" },
            ["cherish"]      = new[] { "cheris", "cherish" },
            ["sustagen"]     = new[] { "sustagen", "sastajen", "sustajen" },
            ["vitagen"]      = new[] { "vitagen", "witagen", "vitajen", "witajen" },
            ["witagen"]      = new[] { "vitagen", "witagen", "vitajen", "witajen" },
            ["diamond"]      = new[] { "diamond", "diamand", "dayamand" },
            ["diamand"]      = new[] { "diamond", "diamand", "dayamand" },
            ["dayamand"]     = new[] { "diamond", "diamand", "dayamand" },
            ["vijaya"]       = new[] { "vijaya" },
            ["melko"]        = new[] { "melko" },
            ["araliya"]      = new[] { "araliya" },
            ["jayathilaka"]  = new[] { "jayathilaka", "jayathilake" },
            ["jayathilake"]  = new[] { "jayathilaka", "jayathilake" },
            ["mortin"]       = new[] { "mortin", "morteen" },
            ["morteen"]      = new[] { "mortin", "morteen" },
            ["ninja"]        = new[] { "ninja" },
            ["anchor"]       = new[] { "anchor" },
            ["milo"]         = new[] { "milo" },
            ["nestomalt"]    = new[] { "nestomalt", "nestomat" },
            ["ovaltine"]     = new[] { "ovaltine", "ovaltin" },
            ["diana"]        = new[] { "diana" },
            ["uswatta"]      = new[] { "uswatta" },
            ["minty"]        = new[] { "minty" },
            ["tulip"]        = new[] { "tulip", "tiyulip", "telip" },
        };

        // ── 2. English product word aliases ────────────────────────────────────────
        private static readonly Dictionary<string, string[]> ProductAliases =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["milk"]        = new[] { "kiri", "keeri", "milk" },
            ["powder"]      = new[] { "piti", "powder" },
            ["tea"]         = new[] { "the", "tea", "kahata" },
            ["coffee"]      = new[] { "kopi", "coffee" },
            ["sugar"]       = new[] { "seeni", "sugar" },
            ["flour"]       = new[] { "piti", "flour" },
            ["rice"]        = new[] { "haal", "rice" },
            ["biscuit"]     = new[] { "biskat", "bisket", "biscuit" },
            ["biscuits"]    = new[] { "biskat", "bisket", "biscuit" },
            ["cracker"]     = new[] { "kraker", "craker", "cracker" },
            ["crackers"]    = new[] { "kraker", "craker", "cracker" },
            ["wafer"]       = new[] { "wapas", "vafer", "wafer" },
            ["wafers"]      = new[] { "wapas", "vafers", "wafers" },
            ["chocolate"]   = new[] { "choco", "choklat", "chokolat", "choklet", "chocolate" },
            ["sauce"]       = new[] { "sos", "sause", "sauce", "sorse" },
            ["sause"]       = new[] { "sos", "sause", "sauce", "sorse" },
            ["oil"]         = new[] { "thel", "oil" },
            ["soap"]        = new[] { "saban", "soap" },
            ["water"]       = new[] { "wathura", "vathura", "water" },
            ["mackerel"]    = new[] { "mackerel", "makaral", "saman", "seman" },
            ["vinegar"]     = new[] { "winakiri", "vinakiri", "vineger", "vinegar" },
            ["shampoo"]     = new[] { "shampu", "sampu", "shampoo" },
            ["cream"]       = new[] { "kreem", "krim", "creem", "cream" },
            ["toothpaste"]  = new[] { "tuth", "toothpaste" },
            ["toothbrush"]  = new[] { "tuth", "toothbrush" },
            ["chutney"]     = new[] { "chatny", "chutny", "chutney" },
            ["mayonnaise"]  = new[] { "mayo", "meyo", "mayonnaise" },
            ["mayo"]        = new[] { "mayo", "meyo", "mayonnaise" },
            ["mustard"]     = new[] { "mustad", "mustard" },
            ["pickle"]      = new[] { "pickle" },
            ["cordial"]     = new[] { "codial", "kodiyal", "coordial", "cordial" },
            ["yoghurt"]     = new[] { "yogat", "yoget", "yoghurt" },
            ["diapers"]     = new[] { "pampers", "pampas", "dayapers", "diapers" },
            ["diaper"]      = new[] { "pampers", "pampas", "diaper" },
            ["jelly"]       = new[] { "jeli", "geli", "jelly" },
            ["gelatin"]     = new[] { "jalatin", "jeletin", "gelatin", "jelatin" },
            ["custard"]     = new[] { "custord", "custod", "kusted", "custard" },
            ["magic"]       = new[] { "majic", "majik", "magic" },
            ["spray"]       = new[] { "sprey", "spray" },
            ["jam"]         = new[] { "jam" },
            ["matches"]     = new[] { "petti", "matches" },
            ["match"]       = new[] { "petti", "match" },
            ["cookies"]     = new[] { "biskat", "biscuit", "cookies" },
            ["cookie"]      = new[] { "biskat", "biscuit", "cookie" },
            ["ice"]         = new[] { "ayis", "ais", "ice" },
            ["puff"]        = new[] { "puff" },
            ["bag"]         = new[] { "bakat", "bag" },
            ["tin"]         = new[] { "tin" },
            ["packet"]      = new[] { "pakat", "packet" },
            ["tomato"]      = new[] { "thakkali", "tomato" },
            ["strawberry"]  = new[] { "stroberry", "stobery", "strawberry" },
            ["mango"]       = new[] { "amba", "aba", "mango" },
            ["pineapple"]   = new[] { "annasi", "pineapple" },
            ["lime"]        = new[] { "dehi", "lime" },
            ["fish"]        = new[] { "maalu", "malu", "fish" },
            ["sesame"]      = new[] { "thala", "sesame", "sesami", "sesemi" },
            ["coconut"]     = new[] { "pol", "coconut" },
            ["palm"]        = new[] { "pam", "palm" },
            ["onion"]       = new[] { "onian", "aniyan", "onion" },
            ["cheese"]      = new[] { "chise", "cheese" },
            ["vanilla"]     = new[] { "vanila", "vanilla" },
            ["lemon"]       = new[] { "lemon" },
            ["short"]       = new[] { "soat", "short" },
            ["eats"]        = new[] { "soat", "eats" },
            ["tiffin"]      = new[] { "tifin", "tipin", "tiffin" },
            ["hawaiian"]    = new[] { "hawayin", "hawaiian" },
            ["hawain"]      = new[] { "hawayin", "hawain" },
            ["light"]       = new[] { "lite", "light" },
            ["bbq"]         = new[] { "babaq", "barbarq", "bbq" },
            ["gold"]        = new[] { "gold" },
            ["nice"]        = new[] { "nice" },
            ["fresh"]       = new[] { "see", "fresh" },
            ["assortment"]  = new[] { "assortment" },
            ["snackers"]    = new[] { "snakers", "snackers" },
            ["oyster"]      = new[] { "oistar", "oyster" },
            ["soya"]        = new[] { "soya" },
            ["bean"]        = new[] { "bean" },
            ["peas"]        = new[] { "pees", "pis", "peas" },
            ["green"]       = new[] { "green" },
            ["salmon"]      = new[] { "saman", "semon", "salmon" },
            ["corn"]        = new[] { "kon", "corn" },
            ["baking"]      = new[] { "bekin", "beking", "baking" },
            ["liquid"]      = new[] { "diyara", "likwid", "likvid", "liquid" },
            ["wash"]        = new[] { "wosh", "wash" },
            ["cleaner"]     = new[] { "cleaner" },
            ["coil"]        = new[] { "koil", "koyil", "coil" },
            ["razor"]       = new[] { "resar", "rasar", "razor" },
            ["razors"]      = new[] { "resar", "rasar", "razors" },
            ["cake"]        = new[] { "kek", "ceke", "kake", "cake" },
            ["toffee"]      = new[] { "topi", "tofi", "toffee" },
            ["malt"]        = new[] { "malt" },
            ["soup"]        = new[] { "sup", "supe", "soup" },
            ["kithul"]      = new[] { "kithul" },
            ["papadam"]     = new[] { "papadam" },
            ["curd"]        = new[] { "mudawapu", "mudavapu", "mee kiri", "mi kiri", "curd" },
        };

        // ── 3. Sinhala word → cashier alias array ──────────────────────────────────
        // Keys are exact Sinhala Unicode words as stored in db_stc.inventory.item_name.
        // Values are the transliterations/aliases the cashiers actually type to search.
        // Takes priority over character-level transliteration.
        private static readonly Dictionary<string, string[]> SinhalaWordOverrides =
            new Dictionary<string, string[]>()
        {
            // Colours / qualifiers
            ["සුදු"]           = new[] { "sudu", "white" },
            ["රතු"]            = new[] { "rathu", "red" },
            ["ලංකා"]           = new[] { "lanka" },
            ["රට"]             = new[] { "rata" },
            ["අමු"]            = new[] { "amu", "raw" },
            ["බැදපු"]          = new[] { "badapu", "roasted" },
            ["වියලි"]          = new[] { "wiyali", "dried" },
            ["අලුත්"]          = new[] { "aluth", "fresh" },

            // Staples
            ["සීනි"]           = new[] { "seeni", "sini", "sugar" },
            ["සීනී"]           = new[] { "seeni", "sini", "sugar" },
            ["පිටි"]           = new[] { "piti", "flour", "powder" },
            ["හාල්"]           = new[] { "haal", "rice" },
            ["සහල්"]           = new[] { "haal", "rice" },
            ["ලූණු"]           = new[] { "lunu", "luunu", "salt" },
            ["ලුණු"]           = new[] { "lunu", "salt" },
            ["ලූනු"]           = new[] { "lunu", "luunu", "salt" },
            ["කිරි"]           = new[] { "kiri", "keeri", "milk" },
            ["කිරිඅල"]         = new[] { "kirala" },

            // Lentils / pulses / grains
            ["පරිප්පු"]        = new[] { "parippu" },
            ["කඩල"]            = new[] { "kadala", "chickpea" },
            ["සෝයා"]           = new[] { "soya" },
            ["කව්පි"]          = new[] { "kawpi", "kavpi" },
            ["මුං"]            = new[] { "mun" },
            ["ඇට"]             = new[] { "ata", "eta" },
            ["උදු"]            = new[] { "udu", "undu" },
            ["ආටා"]            = new[] { "ata", "aata", "aataa" },
            ["කුරක්කන්"]       = new[] { "kurakkan", "kurahan" },
            ["පපඩම්"]          = new[] { "papadam" },

            // Vegetables / fruits
            ["අල"]             = new[] { "ala", "potato" },
            ["ඉදි"]            = new[] { "indi", "idi" },
            ["මිදි"]           = new[] { "midi", "grapes" },
            ["රටකජු"]          = new[] { "ratakaju", "cashew" },

            // Spices
            ["ගම්මිරිස්"]      = new[] { "gammiris", "pepper" },
            ["මිරිස්"]         = new[] { "miris", "chili" },
            ["කහ"]             = new[] { "kaha", "turmeric" },
            ["කොත්තමල්ලි"]     = new[] { "koththamalli", "coriander" },
            ["සූදුරු"]         = new[] { "suduru", "cumin" },
            ["මහදුරු"]         = new[] { "mahaduru", "maaduru" },
            ["අබ"]             = new[] { "aba", "mustard" },
            ["උලුහාල්"]        = new[] { "uluhal", "fenugreek" },
            ["ගොරකා"]          = new[] { "goraka" },
            ["කරාබු"]          = new[] { "karabu", "cloves" },
            ["නැටි"]           = new[] { "nati" },
            ["එනසාල්"]         = new[] { "enasal", "anasal", "cardamom" },
            ["ඉදිආප්ප"]        = new[] { "idiappa" },
            ["කුරුදු"]         = new[] { "kurudu", "cinnamon" },
            ["පොතු"]           = new[] { "pothu" },
            ["තුනපහ"]          = new[] { "thunapaha" },
            ["අසමෝදගම්"]       = new[] { "asamodagam" },

            // Rice varieties / qualifiers
            ["නාඩු"]           = new[] { "naadu", "nadu" },
            ["කැකුලු"]         = new[] { "kakulu" },
            ["කැඩුනු"]         = new[] { "kadunu", "broken" },
            ["රොටී"]           = new[] { "roti" },
            ["සම්බා"]          = new[] { "samba" },
            ["කරල්"]           = new[] { "karal" },

            // Oils / fats
            ["තෙල්"]           = new[] { "thel", "oil" },
            ["පොල්"]           = new[] { "pol", "coconut" },

            // Tea / drinks
            ["තේ"]             = new[] { "the", "tea" },
            ["කහට"]            = new[] { "kahata", "the", "tea" },
            ["කෝඩියල්"]        = new[] { "kodiyal", "codial", "coordial", "cordial" },
            ["කෝපි"]           = new[] { "kopi", "coffee" },
            ["බීම"]            = new[] { "beema", "bima", "drink" },
            ["වතුර"]           = new[] { "wathura", "vathura", "water" },
            ["බෝතල්"]          = new[] { "bothal", "bottle" },
            ["කිතුල්"]         = new[] { "kithul" },
            ["පැණි"]           = new[] { "pani" },
            ["සෝඩා"]           = new[] { "soda" },
            ["ආප්ප"]           = new[] { "appa", "aappa" },

            // Fish / protein
            ["සැමන්"]          = new[] { "saman", "semon", "salmon" },
            ["මාළු"]           = new[] { "maalu", "malu", "fish" },
            ["තක්කාලි"]        = new[] { "thakkali", "tomato" },

            // Biscuits / bakery (Sinhala spellings of loanwords)
            ["බිස්කට්"]        = new[] { "biskat", "biscuit", "bisket" },
            ["ක්‍රීම්"]         = new[] { "cream", "kreem", "krim" },
            ["ක්‍රැකර්"]        = new[] { "cracker", "kraker", "craker" },
            ["චොක්ලට්"]        = new[] { "chocolate", "choklat", "choco" },
            ["වේෆර්ස්"]        = new[] { "wafers", "wapas", "vafers" },
            ["මාරි"]           = new[] { "maari", "mari" },
            ["නයිස්"]          = new[] { "nice" },
            ["ලෙමන්"]          = new[] { "lemon" },
            ["පෆ්"]            = new[] { "puff" },
            ["මිල්ක්"]         = new[] { "milk", "kiri", "keeri" },
            ["ශෝට්"]           = new[] { "short", "soat", "shot" },
            ["ඊට්ස්"]          = new[] { "eats" },
            ["හවායින්"]        = new[] { "hawain", "hawayin" },
            ["කුකීස්"]         = new[] { "cookies" },
            ["ඉගුරු"]          = new[] { "iguru", "inguru", "ginger" },
            ["ලයිට්"]          = new[] { "lite", "light" },
            ["කොමේ"]           = new[] { "kome", "comee" },
            ["ටිෆින්"]         = new[] { "tiffin", "tifin", "tipin" },
            ["අනියන්"]         = new[] { "onion", "onian", "aniyan" },
            ["චීස්"]           = new[] { "cheese", "chise" },
            ["බටන්ස්"]         = new[] { "buttons" },
            ["ජෙම්"]           = new[] { "gem", "jem" },
            ["ගිෆ්ට්"]         = new[] { "gift" },
            ["ඇසෝර්ට්මන්ට්"]   = new[] { "assortment" },
            ["ටින්"]           = new[] { "tin" },
            ["ස්නැකර්ස්"]      = new[] { "snackers", "snakers" },
            ["ස්ටික්ස්"]       = new[] { "sticks" },
            ["ගෝල්ඩ්"]         = new[] { "gold" },
            ["ගෝල්ඩන්"]        = new[] { "golden" },
            ["කස්ටර්ඩ්"]       = new[] { "custard", "custord", "custod", "kusted" },
            ["ටිකිරි"]         = new[] { "tikiti" },
            ["ජෙලි"]           = new[] { "jeli", "geli", "jelly" },
            ["ජෙලටින්"]        = new[] { "jalatin", "jeletin", "gelatin" },
            ["ජෑම්"]           = new[] { "jam" },
            ["අයිස්"]          = new[] { "ayis", "ais", "ice" },
            ["යෝගට්"]          = new[] { "yoghurt", "yogat", "yoget" },
            ["ටොෆී"]           = new[] { "toffee", "topi", "tofi" },
            ["කේක්"]           = new[] { "cake", "kek" },
            ["ශෝස්"]           = new[] { "sos", "sause", "sauce" },
            ["සෝස්"]           = new[] { "sos", "sause", "sauce" },

            // Household / cleaning
            ["සබන්"]           = new[] { "saban", "soap" },
            ["රෙදි"]           = new[] { "redi" },
            ["සෝදන"]           = new[] { "sodana" },
            ["සුවද"]           = new[] { "suwada" },
            ["බේබි"]           = new[] { "baby", "bebi", "babi" },
            ["විම්"]           = new[] { "vim", "wim" },
            ["දියර"]           = new[] { "diyara", "liquid", "likwid" },
            ["ශැම්පූ"]         = new[] { "shampu", "sampu", "shampoo" },
            ["ශොපින්"]         = new[] { "shopin", "shopping", "sopen" },
            ["බෑග්"]           = new[] { "bag", "bakat" },
            ["ලන්ච්"]          = new[] { "lunch", "lanch" },
            ["ශීට්"]           = new[] { "sheet" },
            ["ග්‍රොසරි"]        = new[] { "grosari", "gosari", "grocery" },
            ["මදුරු"]          = new[] { "maduru" },
            ["කොයිල්"]         = new[] { "koil", "koyil", "coil" },
            ["රේසර්"]          = new[] { "resar", "rasar", "razor" },
            ["ෆ්‍රෙශ්නර්"]      = new[] { "freshner" },
            ["එයාර්"]          = new[] { "air", "eya", "ayar" },
            ["ටයිල්"]          = new[] { "tile", "tayil", "tail" },
            ["ක්ලීනර්"]        = new[] { "cleaner" },
            ["හෑන්ඩ්"]         = new[] { "hand" },
            ["වොශ්"]           = new[] { "wosh", "wash" },
            ["පවුඩර්"]         = new[] { "powder", "pavdar", "povdar" },

            // Baking / cooking ingredients
            ["කෝන්"]           = new[] { "corn", "kon" },
            ["ෆ්ලවර්"]         = new[] { "flour", "flower", "fla" },
            ["බේකින්"]         = new[] { "baking", "bekin", "beking" },
            ["අජිනමොටෝ"]       = new[] { "ajinamoto", "msg", "agenamoto", "aginamoto" },
            ["විනාකිරි"]       = new[] { "winakiri", "vinakiri", "vineger" },

            // Health / baby
            ["පැම්පස්"]        = new[] { "pampers", "pampas", "diapers" },
            ["මුදවපු"]         = new[] { "mudawapu", "mudavapu", "curd" },
            ["මෝල්ට්"]         = new[] { "malt" },

            // Misc / packaging
            ["ගිනි"]           = new[] { "gini" },
            ["පෙට්ටි"]         = new[] { "petti", "matches" },
            ["පැකට්"]          = new[] { "pakat", "packet" },
            ["සෙලපින්"]        = new[] { "selapin", "celapin" },
            ["බකට්"]           = new[] { "bakat", "bucket" },
            ["ටූත්"]           = new[] { "tuth", "tooth" },
            ["පේස්ට්"]         = new[] { "paste" },
            ["බ්‍රශ්"]          = new[] { "brush" },
            ["තල"]             = new[] { "thala" },
            ["සුප්"]           = new[] { "sup", "soup", "supe" },
            ["කැට"]            = new[] { "kata" },
            ["අයිසින්"]        = new[] { "aisin", "ayisin", "icing" },

            // Brands in Sinhala script
            ["රයිගම්"]         = new[] { "raigam", "rayigam" },
            ["ජයතිලක"]         = new[] { "jayathilaka", "jayathilake" },
            ["හරිස්චන්ද්‍ර"]    = new[] { "harischandra" },
            ["හෙලදිව"]         = new[] { "heladiva", "heladiwa" },
            ["වටවල"]           = new[] { "watawala", "vatawala" },
            ["ලාඕජී"]          = new[] { "laoji", "laaoji", "lawoji" },
            ["මන්චී"]          = new[] { "manchi", "manchee" },
            ["මැලිබන්"]        = new[] { "maliban" },
            ["ලිට්ල්"]         = new[] { "little" },
            ["ලයන්"]           = new[] { "lion" },
            ["චෙරිශ්"]         = new[] { "cheris", "cherish" },
            ["උස්වත්ත"]        = new[] { "uswatta" },
            ["රන්"]            = new[] { "ran" },
            ["රස"]             = new[] { "rasa" },
            ["රත්න"]           = new[] { "rathna" },
            ["හිරු"]           = new[] { "hiru" },
            ["විජය"]           = new[] { "vijaya" },
            ["ඉසුරු"]          = new[] { "isuru" },
            ["රසෝජා"]          = new[] { "rasoja" },
            ["මෙල්කෝ"]         = new[] { "melko" },
            ["ප්‍රීමා"]         = new[] { "preema", "prima" },
            ["අරලිය"]          = new[] { "araliya" },
            ["කොටගල"]          = new[] { "kotagala" },
            ["තලවකැලේ"]        = new[] { "thalawakale", "thalavakale" },
            ["මස්කරි"]         = new[] { "maskari" },
        };

        // ── 4. Sinhala character transliteration maps ───────────────────────────────
        // Used as a fallback for any Sinhala word not in SinhalaWordOverrides.
        // Unicode references: https://unicode.org/charts/PDF/U0D80.pdf

        // Consonants – emit phoneme + inherent 'a'; virama or vowel sign removes the 'a'
        private static readonly Dictionary<char, string> SinhalaConsonants =
            new Dictionary<char, string>
        {
            ['\u0D9A'] = "k",   // ක
            ['\u0D9B'] = "k",   // ඛ
            ['\u0D9C'] = "g",   // ග
            ['\u0D9D'] = "g",   // ඝ
            ['\u0D9E'] = "ng",  // ඞ
            ['\u0D9F'] = "nd",  // ඟ
            ['\u0DA0'] = "ch",  // ච
            ['\u0DA1'] = "ch",  // ඡ
            ['\u0DA2'] = "j",   // ජ
            ['\u0DA3'] = "j",   // ඣ
            ['\u0DA4'] = "ny",  // ඤ
            ['\u0DA5'] = "gn",  // ඥ
            ['\u0DA7'] = "t",   // ට
            ['\u0DA8'] = "t",   // ඨ
            ['\u0DA9'] = "d",   // ඩ
            ['\u0DAA'] = "d",   // ඪ
            ['\u0DAB'] = "n",   // ණ
            ['\u0DAC'] = "nd",  // ඬ
            ['\u0DAD'] = "th",  // ත
            ['\u0DAE'] = "th",  // ථ
            ['\u0DAF'] = "d",   // ද
            ['\u0DB0'] = "d",   // ධ
            ['\u0DB1'] = "n",   // න
            ['\u0DB3'] = "nd",  // ඳ
            ['\u0DB4'] = "p",   // ප
            ['\u0DB5'] = "p",   // ඵ
            ['\u0DB6'] = "b",   // බ
            ['\u0DB7'] = "b",   // භ
            ['\u0DB8'] = "m",   // ම
            ['\u0DB9'] = "mb",  // ඹ
            ['\u0DBA'] = "y",   // ය
            ['\u0DBB'] = "r",   // ර
            ['\u0DBD'] = "l",   // ල
            ['\u0DC0'] = "v",   // ව  (also covers 'w' phonetically)
            ['\u0DC1'] = "sh",  // ශ
            ['\u0DC2'] = "sh",  // ෂ
            ['\u0DC3'] = "s",   // ස
            ['\u0DC4'] = "h",   // හ
            ['\u0DC5'] = "l",   // ළ  (retroflex l)
            ['\u0DC6'] = "f",   // ෆ
        };

        // Vowel diacritics – replace the preceding consonant's inherent 'a'
        private static readonly Dictionary<char, string> SinhalaVowelSigns =
            new Dictionary<char, string>
        {
            ['\u0DCF'] = "a",   // ා (long ā — same as inherent for search)
            ['\u0DD0'] = "ae",  // ැ
            ['\u0DD1'] = "ae",  // ෑ
            ['\u0DD2'] = "i",   // ි
            ['\u0DD3'] = "i",   // ී
            ['\u0DD4'] = "u",   // ු
            ['\u0DD6'] = "u",   // ූ
            ['\u0DD8'] = "ri",  // ෘ
            ['\u0DD9'] = "e",   // ෙ
            ['\u0DDA'] = "e",   // ේ
            ['\u0DDB'] = "ai",  // ෛ
            ['\u0DDC'] = "o",   // ො
            ['\u0DDD'] = "o",   // ෝ
            ['\u0DDE'] = "au",  // ෞ
        };

        // Stand-alone vowel letters
        private static readonly Dictionary<char, string> SinhalaIndependentVowels =
            new Dictionary<char, string>
        {
            ['\u0D85'] = "a",   // අ
            ['\u0D86'] = "a",   // ආ
            ['\u0D87'] = "ae",  // ඇ
            ['\u0D88'] = "ae",  // ඈ
            ['\u0D89'] = "i",   // ඉ
            ['\u0D8A'] = "i",   // ඊ
            ['\u0D8B'] = "u",   // උ
            ['\u0D8C'] = "u",   // ඌ
            ['\u0D8D'] = "ri",  // ඍ
            ['\u0D91'] = "e",   // එ
            ['\u0D92'] = "e",   // ඒ
            ['\u0D93'] = "ai",  // ඓ
            ['\u0D94'] = "o",   // ඔ
            ['\u0D95'] = "o",   // ඕ
            ['\u0D96'] = "au",  // ඖ
        };

        // ── 5. Public API ───────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a space-separated alias string for the given item name.
        /// Handles English-only, Sinhala-only, and mixed names.
        /// </summary>
        public static string Generate(string itemName)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return string.Empty;

            // Split on whitespace and common punctuation; keep non-empty tokens
            string[] tokens = Regex.Split(itemName.Trim(), @"[\s\(\)\[\]\/\-\,\.&\+]+")
                                   .Where(t => !string.IsNullOrWhiteSpace(t))
                                   .ToArray();

            var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (string token in tokens)
            {
                // Skip pure size/quantity tokens: "400g", "1kg", "200ml", "x100"
                if (Regex.IsMatch(token, @"^\d")) continue;

                if (ContainsSinhala(token))
                {
                    foreach (string t in GetSinhalaAliases(token)) terms.Add(t);
                }
                else if (Regex.IsMatch(token, @"^[A-Za-z]+$"))
                {
                    if (BrandAliases.TryGetValue(token, out string[] brandTerms))
                        foreach (string t in brandTerms) terms.Add(t);

                    if (ProductAliases.TryGetValue(token, out string[] productTerms))
                        foreach (string t in productTerms) terms.Add(t);
                }
            }

            return string.Join(" ", terms);
        }

        /// <summary>
        /// Appends generated aliases to existingKeywords, skipping duplicates.
        /// Safe to call with null/empty existingKeywords.
        /// </summary>
        public static string MergeWithGenerated(string existingKeywords, string itemName)
        {
            string generated = Generate(itemName);
            if (string.IsNullOrWhiteSpace(generated))
                return existingKeywords ?? string.Empty;

            if (string.IsNullOrWhiteSpace(existingKeywords))
                return generated;

            var existing = new HashSet<string>(
                existingKeywords.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries),
                StringComparer.OrdinalIgnoreCase);

            string appended = string.Join(" ",
                generated.Split(' ')
                         .Where(w => !string.IsNullOrWhiteSpace(w) && !existing.Contains(w)));

            return string.IsNullOrWhiteSpace(appended)
                ? existingKeywords
                : existingKeywords.TrimEnd() + " " + appended;
        }

        // ── 6. Private helpers ──────────────────────────────────────────────────────

        private static bool ContainsSinhala(string token) =>
            token.Any(c => c >= '\u0D80' && c <= '\u0DFF');

        /// <summary>
        /// Returns aliases for a Sinhala token: override dict first, transliteration fallback.
        /// </summary>
        private static string[] GetSinhalaAliases(string token)
        {
            if (SinhalaWordOverrides.TryGetValue(token, out string[] overrides))
                return overrides;

            string transliterated = TransliterateSinhala(token);
            return string.IsNullOrWhiteSpace(transliterated)
                ? Array.Empty<string>()
                : new[] { transliterated };
        }

        /// <summary>
        /// Character-level Sinhala → Roman transliteration.
        ///
        /// Algorithm (abugida state machine):
        ///   • Each consonant emits its phoneme + an inherent 'a'.
        ///   • A following vowel sign removes the pending 'a' and appends the actual vowel.
        ///   • A virama (al-lakuna, ්) removes the pending 'a' (consonant cluster join).
        ///   • ZWJ / ZWNJ are skipped (used in ක්‍ร cluster forms).
        ///   • Anusvara (ං) emits 'n'.
        ///   • Stand-alone vowel letters are emitted directly.
        /// </summary>
        private static string TransliterateSinhala(string word)
        {
            var sb = new StringBuilder();
            bool pendingInherentVowel = false;

            foreach (char c in word)
            {
                // ZWJ / ZWNJ — used inside consonant clusters, skip
                if (c == '\u200D' || c == '\u200C') continue;

                // Virama — suppress the pending inherent 'a'
                if (c == '\u0DCA')
                {
                    if (pendingInherentVowel)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        pendingInherentVowel = false;
                    }
                    continue;
                }

                // Anusvara ං → nasalisation → 'n'
                if (c == '\u0D82')
                {
                    pendingInherentVowel = false;
                    sb.Append('n');
                    continue;
                }

                // Stand-alone vowel
                if (SinhalaIndependentVowels.TryGetValue(c, out string iv))
                {
                    pendingInherentVowel = false;
                    sb.Append(iv);
                    continue;
                }

                // Consonant — emit phoneme + inherent 'a'
                if (SinhalaConsonants.TryGetValue(c, out string con))
                {
                    pendingInherentVowel = false;   // close any unclosed prior inherent
                    sb.Append(con);
                    sb.Append('a');                 // inherent vowel (may be removed later)
                    pendingInherentVowel = true;
                    continue;
                }

                // Vowel sign — replaces the preceding consonant's pending 'a'
                if (SinhalaVowelSigns.TryGetValue(c, out string vs))
                {
                    if (pendingInherentVowel)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        pendingInherentVowel = false;
                    }
                    sb.Append(vs);
                    continue;
                }

                // Unknown character (ASCII punctuation inside token, etc.) — skip
            }

            return sb.ToString();
        }
    }
}
