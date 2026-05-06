using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using NipponQuest.Models;

namespace NipponQuest.Data
{
    public static class DbInitializer
    {
        public static void SeedKanaBlitzData(ApplicationDbContext context)
        {
            context.Database.EnsureCreated();

            var existingKeys = new HashSet<string>(
                context.KanaWords
                    .AsNoTracking()
                    .Select(w => w.WordKana + "|" + w.DifficultyLevel + "|" + w.Alphabet)
                    .ToList(),
                StringComparer.Ordinal
            );

            var words = new List<KanaWord>();

            // ╔════════════════════════════════════════════════════════════════╗
            // ║  EASY  —  Single Characters (Foundations)                      ║
            // ╚════════════════════════════════════════════════════════════════╝
            var easy = new List<(string kana, string romaji, string alphabet)>
            {
                // Hiragana Gojuon
                ("あ","A","hiragana"),("い","I","hiragana"),("う","U","hiragana"),("え","E","hiragana"),("お","O","hiragana"),
                ("か","KA","hiragana"),("き","KI","hiragana"),("く","KU","hiragana"),("け","KE","hiragana"),("こ","KO","hiragana"),
                ("さ","SA","hiragana"),("し","SHI","hiragana"),("す","SU","hiragana"),("せ","SE","hiragana"),("そ","SO","hiragana"),
                ("た","TA","hiragana"),("ち","CHI","hiragana"),("つ","TSU","hiragana"),("て","TE","hiragana"),("と","TO","hiragana"),
                ("な","NA","hiragana"),("に","NI","hiragana"),("ぬ","NU","hiragana"),("ね","NE","hiragana"),("の","NO","hiragana"),
                ("は","HA","hiragana"),("ひ","HI","hiragana"),("ふ","FU","hiragana"),("へ","HE","hiragana"),("ほ","HO","hiragana"),
                ("ま","MA","hiragana"),("み","MI","hiragana"),("む","MU","hiragana"),("め","ME","hiragana"),("も","MO","hiragana"),
                ("や","YA","hiragana"),("ゆ","YU","hiragana"),("よ","YO","hiragana"),
                ("ら","RA","hiragana"),("り","RI","hiragana"),("る","RU","hiragana"),("れ","RE","hiragana"),("ろ","RO","hiragana"),
                ("わ","WA","hiragana"),("を","WO","hiragana"),("ん","N","hiragana"),

                // Katakana Gojuon
                ("ア","A","katakana"),("イ","I","katakana"),("ウ","U","katakana"),("エ","E","katakana"),("オ","O","katakana"),
                ("カ","KA","katakana"),("キ","KI","katakana"),("ク","KU","katakana"),("ケ","KE","katakana"),("コ","KO","katakana"),
                ("サ","SA","katakana"),("シ","SHI","katakana"),("ス","SU","katakana"),("セ","SE","katakana"),("ソ","SO","katakana"),
                ("タ","TA","katakana"),("チ","CHI","katakana"),("ツ","TSU","katakana"),("テ","TE","katakana"),("ト","TO","katakana"),
                ("ナ","NA","katakana"),("ニ","NI","katakana"),("ヌ","NU","katakana"),("ネ","NE","katakana"),("ノ","NO","katakana"),
                ("ハ","HA","katakana"),("ヒ","HI","katakana"),("フ","FU","katakana"),("ヘ","HE","katakana"),("ホ","HO","katakana"),
                ("マ","MA","katakana"),("ミ","MI","katakana"),("ム","MU","katakana"),("メ","ME","katakana"),("モ","MO","katakana"),
                ("ヤ","YA","katakana"),("ユ","YU","katakana"),("ヨ","YO","katakana"),
                ("ラ","RA","katakana"),("リ","RI","katakana"),("ル","RU","katakana"),("レ","RE","katakana"),("ロ","RO","katakana"),
                ("ワ","WA","katakana"),("ヲ","WO","katakana"),("ン","N","katakana"),

                // Dakuten (hiragana voiced)
                ("が","GA","dakuten"),("ぎ","GI","dakuten"),("ぐ","GU","dakuten"),("げ","GE","dakuten"),("ご","GO","dakuten"),
                ("ざ","ZA","dakuten"),("じ","JI","dakuten"),("ず","ZU","dakuten"),("ぜ","ZE","dakuten"),("ぞ","ZO","dakuten"),
                ("だ","DA","dakuten"),("ぢ","DI","dakuten"),("づ","DU","dakuten"),("で","DE","dakuten"),("ど","DO","dakuten"),
                ("ば","BA","dakuten"),("び","BI","dakuten"),("ぶ","BU","dakuten"),("べ","BE","dakuten"),("ぼ","BO","dakuten"),
                ("ぱ","PA","dakuten"),("ぴ","PI","dakuten"),("ぷ","PU","dakuten"),("ぺ","PE","dakuten"),("ぽ","PO","dakuten"),

                // Dakuten (katakana voiced)
                ("ガ","GA","dakuten"),("ギ","GI","dakuten"),("グ","GU","dakuten"),("ゲ","GE","dakuten"),("ゴ","GO","dakuten"),
                ("ザ","ZA","dakuten"),("ジ","JI","dakuten"),("ズ","ZU","dakuten"),("ゼ","ZE","dakuten"),("ゾ","ZO","dakuten"),
                ("ダ","DA","dakuten"),("ヂ","DI","dakuten"),("ヅ","DU","dakuten"),("デ","DE","dakuten"),("ド","DO","dakuten"),
                ("バ","BA","dakuten"),("ビ","BI","dakuten"),("ブ","BU","dakuten"),("ベ","BE","dakuten"),("ボ","BO","dakuten"),
                ("パ","PA","dakuten"),("ピ","PI","dakuten"),("プ","PU","dakuten"),("ペ","PE","dakuten"),("ポ","PO","dakuten"),

                // Specials
                ("ゃ","small ya","dakuten"),("ゅ","small yu","dakuten"),("ょ","small yo","dakuten"),("っ","small tsu","dakuten"),
                ("ャ","small YA","dakuten"),("ュ","small YU","dakuten"),("ョ","small YO","dakuten"),("ッ","small TSU","dakuten"),
                ("ー","long vowel","dakuten"),
            };

            foreach (var e in easy)
            {
                var key = e.kana + "|easy|" + e.alphabet;
                if (existingKeys.Contains(key)) continue;
                existingKeys.Add(key);
                words.Add(new KanaWord
                {
                    WordKana = e.kana,
                    WordRomaji = e.romaji,
                    MeaningEnglish = "Single Character",
                    Alphabet = e.alphabet,
                    DifficultyLevel = "easy",
                    CategoryTag = e.alphabet == "dakuten" ? "Dakuten / Special" : "Core Syllabary",
                    IsAIGenerated = false,
                    GeneratedAt = null
                });
            }

            // ╔════════════════════════════════════════════════════════════════╗
            // ║  NORMAL  —  Common 2–3 Character Words                         ║
            // ╚════════════════════════════════════════════════════════════════╝
            var normal = new List<(string word, string missing, string meaning, string romaji, string alphabet, string tag)>
            {
                // ─── Hiragana ───
                ("ねこ","ね","Cat","neko","hiragana","Animals"),
                ("いぬ","い","Dog","inu","hiragana","Animals"),
                ("とり","と","Bird","tori","hiragana","Animals"),
                ("さかな","か","Fish","sakana","hiragana","Animals"),
                ("うま","う","Horse","uma","hiragana","Animals"),
                ("うし","う","Cow","ushi","hiragana","Animals"),
                ("ぞう","ぞ","Elephant","zou","hiragana","Animals"),
                ("くま","く","Bear","kuma","hiragana","Animals"),
                ("さる","さ","Monkey","saru","hiragana","Animals"),
                ("ねずみ","ね","Mouse","nezumi","hiragana","Animals"),
                ("やま","や","Mountain","yama","hiragana","Nature"),
                ("かわ","か","River","kawa","hiragana","Nature"),
                ("みず","み","Water","mizu","hiragana","Nature"),
                ("そら","そ","Sky","sora","hiragana","Nature"),
                ("はな","は","Flower","hana","hiragana","Nature"),
                ("つき","つ","Moon","tsuki","hiragana","Nature"),
                ("ほし","ほ","Star","hoshi","hiragana","Nature"),
                ("うみ","う","Sea","umi","hiragana","Nature"),
                ("もり","も","Forest","mori","hiragana","Nature"),
                ("いし","い","Stone","ishi","hiragana","Nature"),
                ("くも","く","Cloud","kumo","hiragana","Nature"),
                ("ゆき","ゆ","Snow","yuki","hiragana","Nature"),
                ("あめ","あ","Rain","ame","hiragana","Nature"),
                ("かぜ","か","Wind","kaze","hiragana","Nature"),
                ("くるま","る","Car","kuruma","hiragana","Transport"),
                ("いえ","い","House","ie","hiragana","Places"),
                ("みせ","せ","Shop","mise","hiragana","Places"),
                ("えき","え","Station","eki","hiragana","Places"),
                ("にわ","に","Garden","niwa","hiragana","Places"),
                ("みち","み","Road / Path","michi","hiragana","Places"),
                ("まち","ま","Town","machi","hiragana","Places"),
                ("むら","む","Village","mura","hiragana","Places"),
                ("くに","く","Country","kuni","hiragana","Places"),
                ("へや","へ","Room","heya","hiragana","Places"),
                ("つくえ","つ","Desk","tsukue","hiragana","Objects"),
                ("いす","い","Chair","isu","hiragana","Objects"),
                ("ほん","ほ","Book","hon","hiragana","Objects"),
                ("かばん","ば","Bag","kaban","hiragana","Objects"),
                ("とけい","と","Watch / Clock","tokei","hiragana","Objects"),
                ("かさ","か","Umbrella","kasa","hiragana","Objects"),
                ("くつ","く","Shoes","kutsu","hiragana","Objects"),
                ("ふく","ふ","Clothes","fuku","hiragana","Objects"),
                ("はこ","は","Box","hako","hiragana","Objects"),
                ("かみ","か","Paper","kami","hiragana","Objects"),
                ("ともだち","と","Friend","tomodachi","hiragana","People"),
                ("せんせい","せ","Teacher","sensei","hiragana","People"),
                ("かぞく","か","Family","kazoku","hiragana","People"),
                ("ひと","ひ","Person","hito","hiragana","People"),
                ("おとこ","お","Man","otoko","hiragana","People"),
                ("おんな","お","Woman","onna","hiragana","People"),
                ("ちち","ち","Father","chichi","hiragana","People"),
                ("はは","は","Mother","haha","hiragana","People"),
                ("あに","あ","Older Brother","ani","hiragana","People"),
                ("あね","あ","Older Sister","ane","hiragana","People"),
                ("いもうと","い","Younger Sister","imouto","hiragana","People"),
                ("おとうと","お","Younger Brother","otouto","hiragana","People"),
                ("たべる","た","To Eat","taberu","hiragana","Verbs"),
                ("のむ","の","To Drink","nomu","hiragana","Verbs"),
                ("みる","み","To See","miru","hiragana","Verbs"),
                ("いく","い","To Go","iku","hiragana","Verbs"),
                ("くる","く","To Come","kuru","hiragana","Verbs"),
                ("はなす","は","To Speak","hanasu","hiragana","Verbs"),
                ("かう","か","To Buy","kau","hiragana","Verbs"),
                ("うる","う","To Sell","uru","hiragana","Verbs"),
                ("つくる","つ","To Make","tsukuru","hiragana","Verbs"),
                ("かく","か","To Write","kaku","hiragana","Verbs"),
                ("よむ","よ","To Read","yomu","hiragana","Verbs"),
                ("きく","き","To Listen","kiku","hiragana","Verbs"),
                ("はしる","は","To Run","hashiru","hiragana","Verbs"),
                ("あるく","あ","To Walk","aruku","hiragana","Verbs"),
                ("ねる","ね","To Sleep","neru","hiragana","Verbs"),
                ("おきる","お","To Wake Up","okiru","hiragana","Verbs"),
                ("あさ","あ","Morning","asa","hiragana","Time"),
                ("ひる","ひ","Noon","hiru","hiragana","Time"),
                ("よる","よ","Night","yoru","hiragana","Time"),
                ("はる","は","Spring","haru","hiragana","Time"),
                ("なつ","な","Summer","natsu","hiragana","Time"),
                ("あき","あ","Autumn","aki","hiragana","Time"),
                ("ふゆ","ふ","Winter","fuyu","hiragana","Time"),
                ("きのう","き","Yesterday","kinou","hiragana","Time"),
                ("あした","あ","Tomorrow","ashita","hiragana","Time"),
                ("いま","い","Now","ima","hiragana","Time"),
                ("あか","あ","Red","aka","hiragana","Colors"),
                ("あお","あ","Blue","ao","hiragana","Colors"),
                ("しろ","し","White","shiro","hiragana","Colors"),
                ("くろ","く","Black","kuro","hiragana","Colors"),
                ("あつい","あ","Hot","atsui","hiragana","Adjectives"),
                ("さむい","さ","Cold","samui","hiragana","Adjectives"),
                ("たかい","た","Tall / Expensive","takai","hiragana","Adjectives"),
                ("やすい","や","Cheap","yasui","hiragana","Adjectives"),
                ("ちいさい","ち","Small","chiisai","hiragana","Adjectives"),
                ("ながい","な","Long","nagai","hiragana","Adjectives"),
                ("はやい","は","Fast / Early","hayai","hiragana","Adjectives"),
                ("おそい","お","Slow / Late","osoi","hiragana","Adjectives"),
                ("あたらしい","あ","New","atarashii","hiragana","Adjectives"),
                ("ふるい","ふ","Old","furui","hiragana","Adjectives"),
                // ── NEW Hiragana ──
                ("おちゃ","ち","Tea","ocha","hiragana","Food"),
                ("くち","く","Mouth","kuchi","hiragana","Body"),
                ("みみ","み","Ear","mimi","hiragana","Body"),
                ("はな","な","Nose","hana","hiragana","Body"),
                ("あたま","た","Head","atama","hiragana","Body"),
                ("かお","か","Face","kao","hiragana","Body"),
                ("おなか","な","Stomach","onaka","hiragana","Body"),
                ("こえ","こ","Voice","koe","hiragana","Body"),
                ("こころ","こ","Heart / Mind","kokoro","hiragana","Body"),
                ("からだ","ら","Body","karada","hiragana","Body"),
                ("ゆび","ゆ","Finger","yubi","hiragana","Body"),
                ("あし","あ","Leg / Foot","ashi","hiragana","Body"),
                ("おかね","か","Money","okane","hiragana","Daily Life"),
                ("ちかい","か","Near","chikai","hiragana","Adjectives"),
                ("とおい","お","Far","tooi","hiragana","Adjectives"),
                ("ひろい","ろ","Spacious","hiroi","hiragana","Adjectives"),
                ("せまい","ま","Cramped","semai","hiragana","Adjectives"),
                ("つよい","よ","Strong","tsuyoi","hiragana","Adjectives"),
                ("よわい","わ","Weak","yowai","hiragana","Adjectives"),
                ("おもい","も","Heavy","omoi","hiragana","Adjectives"),
                ("かるい","る","Light (weight)","karui","hiragana","Adjectives"),
                ("あまい","ま","Sweet","amai","hiragana","Adjectives"),
                ("からい","ら","Spicy","karai","hiragana","Adjectives"),
                ("にがい","が","Bitter","nigai","hiragana","Adjectives"),
                ("かなしい","な","Sad","kanashii","hiragana","Emotions"),
                ("うれしい","れ","Happy","ureshii","hiragana","Emotions"),
                ("こわい","わ","Scary","kowai","hiragana","Emotions"),
                ("いそがしい","そ","Busy","isogashii","hiragana","Adjectives"),
                ("おもしろい","し","Interesting","omoshiroi","hiragana","Adjectives"),
                ("つまらない","ま","Boring","tsumaranai","hiragana","Adjectives"),

                // ─── Katakana ───
                ("カメラ","カ","Camera","kamera","katakana","Technology"),
                ("テレビ","テ","Television","terebi","katakana","Technology"),
                ("ラジオ","ラ","Radio","rajio","katakana","Technology"),
                ("メール","メ","Email","meeru","katakana","Technology"),
                ("ネット","ネ","Internet","netto","katakana","Technology"),
                ("ロボット","ロ","Robot","robotto","katakana","Technology"),
                ("マイク","マ","Microphone","maiku","katakana","Technology"),
                ("コード","コ","Cord / Code","koodo","katakana","Technology"),
                ("ボタン","ボ","Button","botan","katakana","Technology"),
                ("カード","カ","Card","kaado","katakana","Objects"),
                ("ラーメン","ラ","Ramen","raamen","katakana","Food"),
                ("コーヒー","コ","Coffee","koohii","katakana","Food"),
                ("パン","パ","Bread","pan","katakana","Food"),
                ("ピザ","ピ","Pizza","piza","katakana","Food"),
                ("ケーキ","ケ","Cake","keeki","katakana","Food"),
                ("ミルク","ミ","Milk","miruku","katakana","Food"),
                ("ビール","ビ","Beer","biiru","katakana","Food"),
                ("ワイン","ワ","Wine","wain","katakana","Food"),
                ("カレー","カ","Curry","karee","katakana","Food"),
                ("サラダ","サ","Salad","sarada","katakana","Food"),
                ("スープ","ス","Soup","suupu","katakana","Food"),
                ("バナナ","バ","Banana","banana","katakana","Food"),
                ("メロン","メ","Melon","meron","katakana","Food"),
                ("レモン","レ","Lemon","remon","katakana","Food"),
                ("チーズ","チ","Cheese","chiizu","katakana","Food"),
                ("バター","バ","Butter","bataa","katakana","Food"),
                ("パスタ","パ","Pasta","pasuta","katakana","Food"),
                ("アイス","ア","Ice","aisu","katakana","Food"),
                ("トイレ","ト","Toilet","toire","katakana","Places"),
                ("ホテル","ホ","Hotel","hoteru","katakana","Places"),
                ("バス","バ","Bus","basu","katakana","Transport"),
                ("バイク","バ","Motorbike","baiku","katakana","Transport"),
                ("カフェ","カ","Cafe","kafe","katakana","Places"),
                ("ビル","ビ","Building","biru","katakana","Places"),
                ("ホーム","ホ","Platform / Home","hoomu","katakana","Places"),
                ("ペン","ペ","Pen","pen","katakana","Objects"),
                ("ノート","ノ","Notebook","nooto","katakana","Objects"),
                ("ナイフ","ナ","Knife","naifu","katakana","Objects"),
                ("スプーン","ス","Spoon","supuun","katakana","Objects"),
                ("ベッド","ベ","Bed","beddo","katakana","Objects"),
                ("ドア","ド","Door","doa","katakana","Objects"),
                ("テーブル","テ","Table","teeburu","katakana","Objects"),
                ("ソファ","ソ","Sofa","sofa","katakana","Objects"),
                ("メガネ","メ","Glasses","megane","katakana","Objects"),
                ("ベルト","ベ","Belt","beruto","katakana","Objects"),
                ("アメリカ","ア","America","amerika","katakana","Countries"),
                ("カナダ","カ","Canada","kanada","katakana","Countries"),
                ("ロシア","ロ","Russia","roshia","katakana","Countries"),
                ("インド","イ","India","indo","katakana","Countries"),
                ("スペイン","ス","Spain","supein","katakana","Countries"),
                // ── NEW Katakana ──
                ("マスク","マ","Mask","masuku","katakana","Objects"),
                ("タオル","タ","Towel","taoru","katakana","Objects"),
                ("ミシン","ミ","Sewing Machine","mishin","katakana","Objects"),
                ("ドレス","ド","Dress","doresu","katakana","Objects"),
                ("ボール","ボ","Ball","booru","katakana","Sports"),
                ("ゴール","ゴ","Goal","gooru","katakana","Sports"),
                ("テニス","テ","Tennis","tenisu","katakana","Sports"),
                ("サッカー","サ","Soccer","sakkaa","katakana","Sports"),
                ("プール","プ","Pool","puuru","katakana","Places"),
                ("ピアノ","ピ","Piano","piano","katakana","Music"),
                ("ギター","ギ","Guitar","gitaa","katakana","Music"),
                ("ドラム","ド","Drum","doramu","katakana","Music"),
                ("バンド","バ","Band","bando","katakana","Music"),
                ("クラス","ク","Class","kurasu","katakana","Education"),
                ("テスト","テ","Test","tesuto","katakana","Education"),
                ("ゲーム","ゲ","Game","geemu","katakana","Activities"),
                ("クイズ","ク","Quiz","kuizu","katakana","Activities"),
                ("カラオケ","カ","Karaoke","karaoke","katakana","Activities"),
                ("デート","デ","Date","deeto","katakana","Activities"),
                ("パーティ","パ","Party","paati","katakana","Activities"),
                ("ニュース","ニ","News","nyuusu","katakana","Media"),
                ("ストーリー","ス","Story","sutoorii","katakana","Media"),
                ("ドラマ","ド","Drama","dorama","katakana","Media"),
                ("チーム","チ","Team","chiimu","katakana","Daily Life"),
                ("レベル","レ","Level","reberu","katakana","Daily Life"),
                ("チャンス","チャ","Chance","chansu","katakana","Daily Life"),
                ("ストレス","ス","Stress","sutoresu","katakana","Daily Life"),
                ("プラン","プ","Plan","puran","katakana","Daily Life"),
                ("システム","シ","System","shisutemu","katakana","Technology"),
                ("データ","デ","Data","deeta","katakana","Technology"),

                // ─── Dakuten ───
                ("ごはん","ご","Rice / Meal","gohan","dakuten","Food"),
                ("でんき","で","Electricity","denki","dakuten","Daily Life"),
                ("ざっし","ざ","Magazine","zasshi","dakuten","Objects"),
                ("どうぶつ","ど","Animal","doubutsu","dakuten","Animals"),
                ("ぶどう","ぶ","Grapes","budou","dakuten","Food"),
                ("だいがく","だ","University","daigaku","dakuten","Places"),
                ("ぜんぶ","ぜ","Everything","zenbu","dakuten","Daily Life"),
                ("ぼうし","ぼ","Hat","boushi","dakuten","Objects"),
                ("ばしょ","ば","Place","basho","dakuten","Daily Life"),
                ("じかん","じ","Time","jikan","dakuten","Daily Life"),
                ("でんわ","で","Telephone","denwa","dakuten","Technology"),
                ("ぎんこう","ぎ","Bank","ginkou","dakuten","Places"),
                ("べんとう","べ","Bento Box","bentou","dakuten","Food"),
                ("たまご","ご","Egg","tamago","dakuten","Food"),
                ("りんご","ご","Apple","ringo","dakuten","Food"),
                ("みどり","ど","Green","midori","dakuten","Colors"),
                ("こども","ど","Child","kodomo","dakuten","People"),
                ("まど","ど","Window","mado","dakuten","Objects"),
                ("かぎ","ぎ","Key","kagi","dakuten","Objects"),
                ("でぐち","で","Exit","deguchi","dakuten","Places"),
                ("いりぐち","ぐ","Entrance","iriguchi","dakuten","Places"),
                ("げんき","げ","Healthy / Energy","genki","dakuten","Adjectives"),
                ("ざせき","ざ","Seat","zaseki","dakuten","Objects"),
                ("でんしゃ","で","Train","densha","dakuten","Transport"),
                ("がっこう","が","School","gakkou","dakuten","Places"),
                ("どこ","ど","Where","doko","dakuten","Question Words"),
                ("だれ","だ","Who","dare","dakuten","Question Words"),
                ("ぜろ","ぜ","Zero","zero","dakuten","Numbers"),
                ("ばんごう","ば","Number","bangou","dakuten","Numbers"),
                ("ガム","ガ","Gum","gamu","dakuten","Food"),
                ("ゴルフ","ゴ","Golf","gorufu","dakuten","Sports"),
                ("ジム","ジ","Gym","jimu","dakuten","Places"),
                ("ボール","ボ","Ball","booru","dakuten","Sports"),
                ("ペン","ペ","Pen","pen","dakuten","Objects"),
                ("ポスト","ポ","Mailbox","posuto","dakuten","Objects"),
                // ── NEW Dakuten ──
                ("ぎゅうにゅう","ぎゅ","Milk","gyuunyuu","dakuten","Food"),
                ("でぐち","ぐ","Exit (alt)","deguchi","dakuten","Places"),
                ("ばんごはん","ご","Dinner","bangohan","dakuten","Food"),
                ("ひるごはん","る","Lunch","hirugohan","dakuten","Food"),
                ("あさごはん","ご","Breakfast","asagohan","dakuten","Food"),
                ("じてんしゃ","じ","Bicycle","jitensha","dakuten","Transport"),
                ("ばんぐみ","ぐ","TV Program","bangumi","dakuten","Media"),
                ("げつまつ","げ","End of Month","getsumatsu","dakuten","Time"),
                ("じゅぎょう","じゅ","Lesson","jugyou","dakuten","Education"),
                ("てがみ","が","Letter","tegami","dakuten","Objects"),
                ("みなさん","な","Everyone","minasan","dakuten","People"),
                ("ぐあい","ぐ","Condition","guai","dakuten","Daily Life"),
                ("ぐらい","ぐ","About","gurai","dakuten","Daily Life"),
                ("ばんぜん","ば","Perfect","banzen","dakuten","Adjectives"),
                ("ぶし","ぶ","Samurai","bushi","dakuten","Culture"),
                ("げいじゅつ","げ","Art","geijutsu","dakuten","Culture"),
                ("だんし","だ","Boy","danshi","dakuten","People"),
                ("じょし","じょ","Girl","joshi","dakuten","People"),
                ("ぶんか","ぶ","Culture","bunka","dakuten","Culture"),
                ("ぶた","ぶ","Pig","buta","dakuten","Animals"),
            };

            foreach (var n in normal)
            {
                var key = n.word + "|normal|" + n.alphabet;
                if (existingKeys.Contains(key)) continue;
                existingKeys.Add(key);
                words.Add(new KanaWord
                {
                    WordKana = n.word,
                    WordRomaji = n.romaji,
                    MeaningEnglish = n.meaning,
                    Alphabet = n.alphabet,
                    DifficultyLevel = "normal",
                    MissingKana = n.missing,
                    DisplayHtml = BuildDisplay(n.word, n.missing, "normal"),
                    CategoryTag = n.tag,
                    IsAIGenerated = false,
                    GeneratedAt = null
                });
            }

            // ╔════════════════════════════════════════════════════════════════╗
            // ║  HARD  —  Yōon, Sokuon & Compound Sounds                       ║
            // ╚════════════════════════════════════════════════════════════════╝
            var hard = new List<(string word, string missing, string meaning, string romaji, string alphabet, string tag)>
            {
                // ─── Hiragana Yōon ───
                ("きょうしつ","きょ","Classroom","kyoushitsu","hiragana","Education"),
                ("しゃしん","しゃ","Photograph","shashin","hiragana","Objects"),
                ("じゅぎょう","じゅ","Class / Lesson","jugyou","hiragana","Education"),
                ("ちょうしょく","ちょ","Breakfast","choushoku","hiragana","Food"),
                ("ちゅうしょく","ちゅ","Lunch","chuushoku","hiragana","Food"),
                ("ゆうしょく","しょ","Dinner","yuushoku","hiragana","Food"),
                ("おちゃ","ちゃ","Green Tea","ocha","hiragana","Food"),
                ("りょうり","りょ","Cooking","ryouri","hiragana","Food"),
                ("しょくじ","しょ","Meal","shokuji","hiragana","Food"),
                ("りゅうがく","りゅ","Studying Abroad","ryuugaku","hiragana","Education"),
                ("ひこうき","こう","Airplane","hikouki","hiragana","Transport"),
                ("やきゅう","きゅ","Baseball","yakyuu","hiragana","Sports"),
                ("しゅくだい","しゅ","Homework","shukudai","hiragana","Education"),
                ("としょかん","しょ","Library","toshokan","hiragana","Places"),
                ("にゅうがく","にゅ","School Entrance","nyuugaku","hiragana","Education"),
                ("ひょう","ひょ","Chart / Table","hyou","hiragana","Objects"),
                ("みょうじ","みょ","Surname","myouji","hiragana","People"),
                ("おもちゃ","ちゃ","Toy","omocha","hiragana","Objects"),
                ("こうちょう","ちょ","Principal","kouchou","hiragana","People"),
                ("こうじょう","じょ","Factory","koujou","hiragana","Places"),
                ("しゅみ","しゅ","Hobby","shumi","hiragana","Daily Life"),
                ("しょうゆ","しょ","Soy Sauce","shouyu","hiragana","Food"),
                ("じしょ","じしょ","Dictionary","jisho","hiragana","Objects"),
                ("ひゃく","ひゃ","Hundred","hyaku","hiragana","Numbers"),
                ("せんしゅ","しゅ","Athlete","senshu","hiragana","People"),
                ("いっしょ","しょ","Together","issho","hiragana","Daily Life"),
                ("けっこん","けっ","Marriage","kekkon","hiragana","Events"),
                ("きって","きっ","Stamp","kitte","hiragana","Objects"),
                ("きっぷ","きっ","Ticket","kippu","hiragana","Objects"),
                ("ざっし","ざっ","Magazine","zasshi","hiragana","Objects"),
                ("ちょっと","ちょ","A little","chotto","hiragana","Daily Life"),
                // ── NEW Hiragana Hard ──
                ("しゅくはく","しゅ","Lodging","shukuhaku","hiragana","Places"),
                ("じゅんばん","じゅ","Order / Turn","junban","hiragana","Daily Life"),
                ("きょねん","きょ","Last Year","kyonen","hiragana","Time"),
                ("らいしゅう","しゅ","Next Week","raishuu","hiragana","Time"),
                ("せんしゅう","しゅ","Last Week","senshuu","hiragana","Time"),
                ("こんしゅう","しゅ","This Week","konshuu","hiragana","Time"),
                ("どうりょう","りょ","Coworker","douryou","hiragana","People"),
                ("しょうらい","しょ","Future","shourai","hiragana","Concepts"),
                ("ちゅうい","ちゅ","Caution","chuui","hiragana","Daily Life"),
                ("にゅういん","にゅ","Hospitalization","nyuuin","hiragana","Health"),
                ("たいいん","た","Discharge","taiin","hiragana","Health"),
                ("じょうほう","じょ","Information","jouhou","hiragana","Daily Life"),
                ("きょうみ","きょ","Interest","kyoumi","hiragana","Concepts"),
                ("しょうかい","しょ","Introduction","shoukai","hiragana","Daily Life"),
                ("はっぴょう","ぴょ","Presentation","happyou","hiragana","Education"),
                ("いっぴき","いっ","One Animal","ippiki","hiragana","Numbers"),
                ("はっぴゃく","ぴゃ","800","happyaku","hiragana","Numbers"),
                ("せんぱい","せ","Senior","senpai","hiragana","People"),
                ("こうはい","こう","Junior","kouhai","hiragana","People"),
                ("にっき","にっ","Diary","nikki","hiragana","Objects"),

                // ─── Katakana Long & Compounds ───
                ("コンピュータ","ピュ","Computer","konpyuuta","katakana","Technology"),
                ("チョコレート","チョ","Chocolate","chokoreeto","katakana","Food"),
                ("スマートフォン","フォ","Smartphone","sumaatofon","katakana","Technology"),
                ("インターネット","ター","Internet","intaanetto","katakana","Technology"),
                ("ハンバーガー","バー","Hamburger","hanbaagaa","katakana","Food"),
                ("アイスクリーム","クリ","Ice Cream","aisukuriimu","katakana","Food"),
                ("ショッピング","ショ","Shopping","shoppingu","katakana","Activities"),
                ("プレゼント","プレ","Present / Gift","purezento","katakana","Objects"),
                ("ニュース","ニュ","News","nyuusu","katakana","Media"),
                ("ティッシュ","ティ","Tissue","tisshu","katakana","Objects"),
                ("ファッション","ファ","Fashion","fasshon","katakana","Daily Life"),
                ("シャワー","シャ","Shower","shawaa","katakana","Daily Life"),
                ("チケット","チケ","Ticket","chiketto","katakana","Objects"),
                ("メッセージ","メッ","Message","messeeji","katakana","Technology"),
                ("ホームページ","ホー","Webpage","hoomupeeji","katakana","Technology"),
                ("デパート","デ","Department Store","depaato","katakana","Places"),
                ("エレベーター","エ","Elevator","erebeetaa","katakana","Objects"),
                ("エスカレーター","エス","Escalator","esukareetaa","katakana","Objects"),
                ("コンビニ","コン","Convenience Store","konbini","katakana","Places"),
                ("スーパー","スー","Supermarket","suupaa","katakana","Places"),
                ("マンション","マン","Apartment","manshon","katakana","Places"),
                ("プロジェクト","ジェ","Project","purojekuto","katakana","Activities"),
                ("ジャケット","ジャ","Jacket","jaketto","katakana","Objects"),
                ("シャンプー","シャ","Shampoo","shanpuu","katakana","Objects"),
                ("レシート","レ","Receipt","reshiito","katakana","Objects"),
                ("カップ","カッ","Cup","kappu","katakana","Objects"),
                // ── NEW Katakana Hard ──
                ("コンサート","コン","Concert","konsaato","katakana","Activities"),
                ("インタビュー","ビュ","Interview","intabyuu","katakana","Activities"),
                ("メニュー","ニュ","Menu","menyuu","katakana","Food"),
                ("シェフ","シェ","Chef","shefu","katakana","People"),
                ("ジュース","ジュ","Juice","juusu","katakana","Food"),
                ("チャレンジ","チャ","Challenge","charenji","katakana","Concepts"),
                ("キャンセル","キャ","Cancel","kyanseru","katakana","Daily Life"),
                ("アクション","ショ","Action","akushon","katakana","Concepts"),
                ("オフィス","オ","Office","ofisu","katakana","Places"),
                ("ファイル","ファ","File","fairu","katakana","Technology"),
                ("ソフトウェア","ソフ","Software","sofutowea","katakana","Technology"),
                ("ハードウェア","ハー","Hardware","haadowea","katakana","Technology"),
                ("プログラマー","プロ","Programmer","puroguramaa","katakana","Technology"),
                ("デザイナー","デ","Designer","dezainaa","katakana","People"),
                ("マネージャー","マ","Manager","maneejaa","katakana","People"),
                ("オリンピック","ピッ","Olympics","orinpikku","katakana","Sports"),
                ("オーケストラ","オー","Orchestra","ookesutora","katakana","Music"),
                ("マラソン","マ","Marathon","marason","katakana","Sports"),
                ("チャンピオン","ピオ","Champion","chanpion","katakana","Sports"),
                ("ファンタジー","タ","Fantasy","fantajii","katakana","Media"),

                // ─── Dakuten Hard ───
                ("ぎゅうにゅう","ぎゅ","Milk","gyuunyuu","dakuten","Food"),
                ("ぎゅうにく","ぎゅ","Beef","gyuuniku","dakuten","Food"),
                ("じてんしゃ","じ","Bicycle","jitensha","dakuten","Transport"),
                ("びょういん","びょ","Hospital","byouin","dakuten","Places"),
                ("びじゅつかん","じゅ","Art Museum","bijutsukan","dakuten","Places"),
                ("ぎじゅつ","ぎ","Technology","gijutsu","dakuten","Education"),
                ("じゃがいも","じゃ","Potato","jagaimo","dakuten","Food"),
                ("ぞうきん","ぞ","Cleaning Cloth","zoukin","dakuten","Objects"),
                ("ばんごはん","ば","Dinner","bangohan","dakuten","Food"),
                ("どうろ","ど","Road","douro","dakuten","Transport"),
                ("ぶんがく","ぶ","Literature","bungaku","dakuten","Education"),
                ("ぴょんぴょん","ぴょ","Hopping","pyonpyon","dakuten","Onomatopoeia"),
                ("ぎょうぎ","ぎょ","Manners","gyougi","dakuten","Daily Life"),
                ("じゅんび","じゅ","Preparation","junbi","dakuten","Daily Life"),
                ("どようび","ど","Saturday","doyoubi","dakuten","Time"),
                ("にちようび","び","Sunday","nichiyoubi","dakuten","Time"),
                ("げつようび","げ","Monday","getsuyoubi","dakuten","Time"),
                ("ジュース","ジュ","Juice","juusu","dakuten","Food"),
                ("ジャム","ジャ","Jam","jamu","dakuten","Food"),
                ("ジョギング","ジョ","Jogging","jogingu","dakuten","Sports"),
                ("ピンポン","ピン","Ping Pong","pinpon","dakuten","Sports"),
                ("プール","プー","Pool","puuru","dakuten","Places"),
                ("ペンギン","ペン","Penguin","pengin","dakuten","Animals"),
                ("ハンバーガー","バー","Hamburger","hanbaagaa","dakuten","Food"),
                ("バーゲン","バー","Bargain Sale","baagen","dakuten","Daily Life"),
                ("ボーリング","ボー","Bowling","booringu","dakuten","Sports"),
                ("デザート","デ","Dessert","dezaato","dakuten","Food"),
                ("ガソリン","ガ","Gasoline","gasorin","dakuten","Transport"),
                ("バッグ","バッ","Bag","baggu","dakuten","Objects"),
                // ── NEW Dakuten Hard ──
                ("じょうほう","じょ","Information","jouhou","dakuten","Daily Life"),
                ("しょうがっこう","がっ","Elementary School","shougakkou","dakuten","Places"),
                ("ちゅうがっこう","がっ","Middle School","chuugakkou","dakuten","Places"),
                ("こうこうせい","こう","High Schooler","koukousei","dakuten","People"),
                ("だいがくいん","がく","Graduate School","daigakuin","dakuten","Places"),
                ("ぎょうじ","ぎょ","Event","gyouji","dakuten","Events"),
                ("じゅうしょ","じゅ","Address","juusho","dakuten","Daily Life"),
                ("ばんごう","ば","Number","bangou","dakuten","Numbers"),
                ("でんごん","ご","Message","dengon","dakuten","Daily Life"),
                ("はつおん","つ","Pronunciation","hatsuon","dakuten","Education"),
                ("にゅうがくしき","しき","Entrance Ceremony","nyuugakushiki","dakuten","Events"),
                ("そつぎょうしき","しき","Graduation","sotsugyoushiki","dakuten","Events"),
                ("ぎんざ","ぎん","Ginza","ginza","dakuten","Places"),
                ("おばあさん","ば","Grandmother","obaasan","dakuten","People"),
                ("おじいさん","じ","Grandfather","ojiisan","dakuten","People"),
                ("だんじょ","だん","Male/Female","danjo","dakuten","People"),
                ("じょゆう","じょ","Actress","joyuu","dakuten","People"),
                ("だんゆう","だ","Actor","danyuu","dakuten","People"),
                ("ぶんぼうぐ","ぶ","Stationery","bunbougu","dakuten","Objects"),
                ("ざいりょう","りょ","Material / Ingredient","zairyou","dakuten","Daily Life"),
            };

            foreach (var h in hard)
            {
                var key = h.word + "|hard|" + h.alphabet;
                if (existingKeys.Contains(key)) continue;
                existingKeys.Add(key);
                words.Add(new KanaWord
                {
                    WordKana = h.word,
                    WordRomaji = h.romaji,
                    MeaningEnglish = h.meaning,
                    Alphabet = h.alphabet,
                    DifficultyLevel = "hard",
                    MissingKana = h.missing,
                    DisplayHtml = BuildDisplay(h.word, h.missing, "hard"),
                    CategoryTag = h.tag,
                    IsAIGenerated = false,
                    GeneratedAt = null
                });
            }

            // ╔════════════════════════════════════════════════════════════════╗
            // ║  INSANITY  —  Kanji + Furigana                                 ║
            // ╚════════════════════════════════════════════════════════════════╝
            var insane = new List<(string word, string missing, string meaning, string romaji, string alphabet, string tag)>
            {
                // ── Hiragana-furigana kanji ──
                ("学校 (がっこう)","がっ","School","gakkou","hiragana","Places"),
                ("先生 (せんせい)","せん","Teacher","sensei","hiragana","People"),
                ("学生 (がくせい)","がく","Student","gakusei","hiragana","People"),
                ("家族 (かぞく)","か","Family","kazoku","hiragana","People"),
                ("名前 (なまえ)","なま","Name","namae","hiragana","People"),
                ("時間 (じかん)","じ","Time","jikan","hiragana","Daily Life"),
                ("毎日 (まいにち)","まい","Every Day","mainichi","hiragana","Time"),
                ("今日 (きょう)","きょ","Today","kyou","hiragana","Time"),
                ("明日 (あした)","あ","Tomorrow","ashita","hiragana","Time"),
                ("昨日 (きのう)","き","Yesterday","kinou","hiragana","Time"),
                ("天気 (てんき)","てん","Weather","tenki","hiragana","Nature"),
                ("電車 (でんしゃ)","でん","Train","densha","hiragana","Transport"),
                ("自動車 (じどうしゃ)","どう","Automobile","jidousha","hiragana","Transport"),
                ("会社 (かいしゃ)","かい","Company","kaisha","hiragana","Places"),
                ("病院 (びょういん)","びょう","Hospital","byouin","hiragana","Places"),
                ("旅行 (りょこう)","りょ","Travel","ryokou","hiragana","Activities"),
                ("勉強 (べんきょう)","べん","Study","benkyou","hiragana","Education"),
                ("運動 (うんどう)","うん","Exercise","undou","hiragana","Sports"),
                ("結婚 (けっこん)","けっ","Marriage","kekkon","hiragana","Events"),
                // ── NEW Hiragana Insanity ──
                ("夏休み (なつやすみ)","やす","Summer Break","natsuyasumi","hiragana","Time"),
                ("冬休み (ふゆやすみ)","やす","Winter Break","fuyuyasumi","hiragana","Time"),
                ("春休み (はるやすみ)","やす","Spring Break","haruyasumi","hiragana","Time"),
                ("入学式 (にゅうがくしき)","にゅ","Entrance Ceremony","nyuugakushiki","hiragana","Events"),
                ("卒業式 (そつぎょうしき)","そつ","Graduation Ceremony","sotsugyoushiki","hiragana","Events"),
                ("運動会 (うんどうかい)","うん","Sports Day","undoukai","hiragana","Events"),
                ("文化祭 (ぶんかさい)","ぶん","Culture Festival","bunkasai","hiragana","Events"),
                ("修学旅行 (しゅうがくりょこう)","しゅ","School Trip","shuugakuryokou","hiragana","Activities"),
                ("料理 (りょうり)","りょ","Cooking","ryouri","hiragana","Food"),
                ("散歩 (さんぽ)","さん","Walk / Stroll","sanpo","hiragana","Activities"),

                // ── Katakana-style insanity ──
                ("亜米利加 (アメリカ)","メリ","America","amerika","katakana","Countries"),
                ("英国 (イギリス)","イ","Britain","igirisu","katakana","Countries"),
                ("仏蘭西 (フランス)","フラ","France","furansu","katakana","Countries"),
                ("独逸 (ドイツ)","ド","Germany","doitsu","katakana","Countries"),
                ("伊太利 (イタリア)","タリ","Italy","itaria","katakana","Countries"),
                ("珈琲 (コーヒー)","コー","Coffee","koohii","katakana","Food"),
                ("麦酒 (ビール)","ビー","Beer","biiru","katakana","Food"),
                ("天麩羅 (テンプラ)","プラ","Tempura","tenpura","katakana","Food"),
                ("寿司 (スシ)","ス","Sushi","sushi","katakana","Food"),
                ("型録 (カタログ)","タロ","Catalog","katarogu","katakana","Objects"),
                ("基督 (キリスト)","リス","Christ","kirisuto","katakana","Religion"),
                ("浪漫 (ロマン)","マ","Romance","roman","katakana","Concepts"),
                ("頁 (ページ)","ペー","Page","peeji","katakana","Objects"),
                ("葡萄 (ブドウ)","ブ","Grapes","budou","katakana","Food"),
                ("背広 (セビロ)","セ","Business Suit","sebiro","katakana","Objects"),
                // ── NEW Katakana Insanity ──
                ("珈琲店 (コーヒーテン)","コー","Coffee Shop","koohiiten","katakana","Places"),
                ("拉麺 (ラーメン)","ラー","Ramen","raamen","katakana","Food"),
                ("麺麭 (パン)","パ","Bread","pan","katakana","Food"),
                ("檸檬 (レモン)","レ","Lemon","remon","katakana","Food"),
                ("林檎 (リンゴ)","リン","Apple","ringo","katakana","Food"),

                // ── Dakuten-furigana kanji ──
                ("学校 (がっこう)","が","School (voiced)","gakkou","dakuten","Places"),
                ("銀行 (ぎんこう)","ぎん","Bank","ginkou","dakuten","Places"),
                ("大学 (だいがく)","だい","University","daigaku","dakuten","Places"),
                ("元気 (げんき)","げん","Energy / Healthy","genki","dakuten","Adjectives"),
                ("電気 (でんき)","でん","Electricity","denki","dakuten","Daily Life"),
                ("電話 (でんわ)","でん","Telephone","denwa","dakuten","Technology"),
                ("辞書 (じしょ)","じ","Dictionary","jisho","dakuten","Objects"),
                ("時間 (じかん)","じ","Time","jikan","dakuten","Daily Life"),
                ("自由 (じゆう)","じ","Freedom","jiyuu","dakuten","Concepts"),
                ("地下 (ちか)","ち","Underground","chika","dakuten","Places"),
                ("地下鉄 (ちかてつ)","てつ","Subway","chikatetsu","dakuten","Transport"),
                ("動物 (どうぶつ)","ぶつ","Animal","doubutsu","dakuten","Animals"),
                ("勉強 (べんきょう)","べん","Study","benkyou","dakuten","Education"),
                ("病気 (びょうき)","びょ","Sickness","byouki","dakuten","Health"),
                ("美術 (びじゅつ)","じゅ","Art","bijutsu","dakuten","Education"),
                ("美味しい (おいしい)","お","Delicious","oishii","dakuten","Adjectives"),
                ("文学 (ぶんがく)","ぶん","Literature","bungaku","dakuten","Education"),
                ("仏教 (ぶっきょう)","ぶっ","Buddhism","bukkyou","dakuten","Religion"),
                ("旅行 (りょこう)","りょ","Travel","ryokou","dakuten","Activities"),
                ("結婚式 (けっこんしき)","こん","Wedding","kekkonshiki","dakuten","Events"),
                ("自動車 (じどうしゃ)","じ","Automobile","jidousha","dakuten","Transport"),
                ("学者 (がくしゃ)","がく","Scholar","gakusha","dakuten","People"),
                ("郵便 (ゆうびん)","びん","Postal Mail","yuubin","dakuten","Daily Life"),
                ("番号 (ばんごう)","ばん","Number","bangou","dakuten","Numbers"),
                ("家族 (かぞく)","ぞ","Family","kazoku","dakuten","People"),
                ("漫画 (まんが)","が","Manga / Comic","manga","dakuten","Media"),
                ("音楽 (おんがく)","がく","Music","ongaku","dakuten","Music"),
                ("写真 (しゃしん)","しゃ","Photograph","shashin","dakuten","Objects"),
                ("会議 (かいぎ)","ぎ","Meeting","kaigi","dakuten","Daily Life"),
                ("駅 (えき)","え","Station","eki","dakuten","Places"),
                // ── NEW Dakuten Insanity ──
                ("挨拶 (あいさつ)","さつ","Greeting","aisatsu","dakuten","Daily Life"),
                ("健康 (けんこう)","けん","Health","kenkou","dakuten","Health"),
                ("研究 (けんきゅう)","きゅ","Research","kenkyuu","dakuten","Education"),
                ("経験 (けいけん)","けん","Experience","keiken","dakuten","Concepts"),
                ("協力 (きょうりょく)","りょ","Cooperation","kyouryoku","dakuten","Concepts"),
                ("成功 (せいこう)","せい","Success","seikou","dakuten","Concepts"),
                ("失敗 (しっぱい)","しっ","Failure","shippai","dakuten","Concepts"),
                ("両親 (りょうしん)","りょ","Parents","ryoushin","dakuten","People"),
                ("兄弟 (きょうだい)","だい","Siblings","kyoudai","dakuten","People"),
                ("姉妹 (しまい)","ま","Sisters","shimai","dakuten","People"),

                // ── Mixed insanity ──
                ("日本語 (にほんご)","ほん","Japanese Language","nihongo","mixed","Language"),
                ("新幹線 (しんかんせん)","かん","Bullet Train","shinkansen","mixed","Transport"),
                ("富士山 (ふじさん)","じ","Mount Fuji","fujisan","mixed","Geography"),
                ("自動販売機 (じどうはんばいき)","はん","Vending Machine","jidouhanbaiki","mixed","Technology"),
                ("図書館 (としょかん)","しょ","Library","toshokan","mixed","Places"),
                ("郵便局 (ゆうびんきょく)","びん","Post Office","yuubinkyoku","mixed","Places"),
                ("地下鉄 (ちかてつ)","ちか","Subway","chikatetsu","mixed","Transport"),
                ("大学生 (だいがくせい)","がく","University Student","daigakusei","mixed","People"),
                ("会社員 (かいしゃいん)","しゃ","Office Worker","kaishain","mixed","People"),
                ("天気予報 (てんきよほう)","よほ","Weather Forecast","tenkiyohou","mixed","Daily Life"),
                ("飛行機 (ひこうき)","こう","Airplane","hikouki","mixed","Transport"),
                ("美術館 (びじゅつかん)","じゅ","Art Museum","bijutsukan","mixed","Places"),
                ("運転免許 (うんてんめんきょ)","めん","Driver's License","untenmenkyo","mixed","Documents"),
                ("結婚式 (けっこんしき)","こん","Wedding Ceremony","kekkonshiki","mixed","Events"),
                ("一期一会 (いちごいちえ)","ご","Once in a Lifetime","ichigoichie","mixed","Idioms"),
                ("七転八起 (しちてんはっき)","てん","Fall Seven, Rise Eight","shichitenhakki","mixed","Idioms"),
                ("一石二鳥 (いっせきにちょう)","ちょ","Two Birds, One Stone","issekinichou","mixed","Idioms"),
                ("十人十色 (じゅうにんといろ)","じゅ","To Each Their Own","juunintoiro","mixed","Idioms"),
                ("百花繚乱 (ひゃっかりょうらん)","りょ","Many Flowers Blooming","hyakkaryouran","mixed","Idioms"),
                ("月見 (つきみ)","つき","Moon Viewing","tsukimi","mixed","Culture"),
                ("花見 (はなみ)","はな","Cherry Blossom Viewing","hanami","mixed","Culture"),
                ("茶道 (さどう)","さ","Tea Ceremony","sadou","mixed","Culture"),
                ("書道 (しょどう)","しょ","Calligraphy","shodou","mixed","Culture"),
                ("剣道 (けんどう)","けん","Kendo","kendou","mixed","Sports"),
                ("柔道 (じゅうどう)","じゅ","Judo","juudou","mixed","Sports"),
                ("空手 (からて)","から","Karate","karate","mixed","Sports"),
                ("着物 (きもの)","き","Kimono","kimono","mixed","Culture"),
                ("浴衣 (ゆかた)","ゆ","Yukata","yukata","mixed","Culture"),
                ("祭り (まつり)","まつ","Festival","matsuri","mixed","Culture"),
                ("神社 (じんじゃ)","じん","Shinto Shrine","jinja","mixed","Places"),
                ("寺 (てら)","て","Temple","tera","mixed","Places"),
                ("城 (しろ)","し","Castle","shiro","mixed","Places"),
                ("公園 (こうえん)","こう","Park","kouen","mixed","Places"),
                ("市役所 (しやくしょ)","やく","City Hall","shiyakusho","mixed","Places"),
                ("警察署 (けいさつしょ)","さつ","Police Station","keisatsusho","mixed","Places"),
                ("消防署 (しょうぼうしょ)","ぼう","Fire Station","shouboushou","mixed","Places"),
                ("空港 (くうこう)","くう","Airport","kuukou","mixed","Places"),
                ("港 (みなと)","みな","Harbor / Port","minato","mixed","Places"),
                ("橋 (はし)","は","Bridge","hashi","mixed","Places"),
                // ── NEW Mixed Insanity ──
                ("経済 (けいざい)","ざ","Economy","keizai","mixed","Concepts"),
                ("政治 (せいじ)","じ","Politics","seiji","mixed","Concepts"),
                ("環境 (かんきょう)","きょ","Environment","kankyou","mixed","Concepts"),
                ("文化 (ぶんか)","ぶん","Culture","bunka","mixed","Culture"),
                ("歴史 (れきし)","れ","History","rekishi","mixed","Concepts"),
                ("科学 (かがく)","が","Science","kagaku","mixed","Education"),
                ("数学 (すうがく)","がく","Mathematics","suugaku","mixed","Education"),
                ("物理 (ぶつり)","ぶ","Physics","butsuri","mixed","Education"),
                ("化学 (かがく)","か","Chemistry","kagaku","mixed","Education"),
                ("地理 (ちり)","ち","Geography","chiri","mixed","Education"),
                ("文学 (ぶんがく)","がく","Literature","bungaku","mixed","Education"),
                ("哲学 (てつがく)","がく","Philosophy","tetsugaku","mixed","Education"),
                ("音楽家 (おんがくか)","がく","Musician","ongakuka","mixed","People"),
                ("芸術家 (げいじゅつか)","じゅ","Artist","geijutsuka","mixed","People"),
                ("研究者 (けんきゅうしゃ)","きゅ","Researcher","kenkyuusha","mixed","People"),
                ("作曲家 (さっきょくか)","きょ","Composer","sakkyokuka","mixed","People"),
                ("不可能 (ふかのう)","か","Impossible","fukanou","mixed","Adjectives"),
                ("可能性 (かのうせい)","せ","Possibility","kanousei","mixed","Concepts"),
                ("想像力 (そうぞうりょく)","りょ","Imagination","souzouryoku","mixed","Concepts"),
                ("創造性 (そうぞうせい)","ぞ","Creativity","souzousei","mixed","Concepts"),
            };

            foreach (var i in insane)
            {
                var key = i.word + "|insanity|" + i.alphabet;
                if (existingKeys.Contains(key)) continue;
                existingKeys.Add(key);
                words.Add(new KanaWord
                {
                    WordKana = i.word,
                    WordRomaji = i.romaji,
                    MeaningEnglish = i.meaning,
                    Alphabet = i.alphabet,
                    DifficultyLevel = "insanity",
                    MissingKana = i.missing,
                    DisplayHtml = BuildDisplay(i.word, i.missing, "insanity"),
                    CategoryTag = i.tag,
                    IsAIGenerated = false,
                    GeneratedAt = null
                });
            }

            if (words.Count > 0)
            {
                context.KanaWords.AddRange(words);
                context.SaveChanges();
            }
        }

        // (Kept for back-compat / pre-rendering. Actual displayed HTML is now
        // generated at runtime by KanaBlitzController.BuildRandomMissing so the
        // missing kana position varies per request.)
        private static string BuildDisplay(string word, string missing, string difficultyLevel)
        {
            if (difficultyLevel == "insanity" && word.Contains("(") && word.Contains(")"))
            {
                int open = word.IndexOf('(');
                int close = word.IndexOf(')');
                string kanji = word.Substring(0, open).Trim();
                string furigana = word.Substring(open + 1, close - open - 1);

                string furiBlanked = ReplaceFirst(furigana, missing,
                    $"<span class=\"missing-placeholder\">{missing}</span>");

                return $"<div class=\"kanji-stack\">" +
                       $"<div class=\"kanji-top\">{kanji}</div>" +
                       $"<div class=\"kana-bottom\">{furiBlanked}</div>" +
                       $"</div>";
            }

            return ReplaceFirst(word, missing,
                $"<span class=\"missing-placeholder\">{missing}</span>");
        }

        private static string ReplaceFirst(string text, string search, string replace)
        {
            int idx = text.IndexOf(search, StringComparison.Ordinal);
            if (idx < 0) return text;
            return text.Substring(0, idx) + replace + text.Substring(idx + search.Length);
        }
    }
}