# ייבוא מודלים חיצוניים (Sketchfab / Kenney / Quaternius)

המשחק תומך במודלים חיצוניים בשיטת **drop-in**: שומרים קובץ מודל בשם ה־id של
היחידה בתיקייה `unity/Assets/Resources/Models/` — והוא מחליף אוטומטית את הגוף
הפרוצדורלי של אותה יחידה/מבנה. בלי לגעת בקוד.

## איך זה עובד

1. הורידו מודל (פורמטים: **FBX**, **OBJ**, **glTF/GLB** — נתמך דרך חבילת
   glTFast שכבר בפרויקט, או **.blend** אם Blender מותקן אצלכם).
2. שמרו/שנו שם לפי ה־id, למשל: `nx_harvester.glb`, `dm_mbt_walker.fbx`.
3. גררו לתיקייה `Assets/Resources/Models/` ב־Unity.
4. זהו. בזמן ריצה המודל:
   - **מותאם בגודלו** אוטומטית לטביעת־הרגל של היחידה,
   - **מוצמד לקרקע** (הנקודה הנמוכה שלו יושבת על 0),
   - אם יש בו אובייקט־ילד ששמו מכיל `turret` — הוא יהיה **הצריח המסתובב**,
   - מקבל collider לבחירה בעכבר ופס־זיהוי בצבע השחקן.
5. כיוונון עדין (אופציונלי) ב־`Assets/Resources/ModelOverrides.json`:
   ```json
   { "dm_mbt_walker": { "scale": 1.15, "rotY": 180, "y": 0.02 } }
   ```
   `scale` מכפיל את ההתאמה האוטומטית; `rotY` מתקן כיוון "קדימה"; `y` מרים/מוריד.
   מוסכמת הכיוון: החזית צריכה לפנות ל־**‎+Z**; אם המודל "נוסע אחורה" — `rotY: 180`.

## רישיונות — חשוב!

- **מ־Sketchfab** אפשר להוריד רק כשמחוברים לחשבון, ורק מודלים המסומנים
  **Downloadable**. סננו לפי רישיון: **CC0** (הכי בטוח) או **CC-BY** (מותר,
  חובה קרדיט — הוסיפו שורה ב־`docs/CREDITS.md`). **הימנעו מ־NC/ND** — הם אוסרים
  שימוש מסחרי/שינויים, ולא מתאימים למשחק שאולי יימכר.
- מקורות CC0 מומלצים (הכל חינם, בלי חשבון): **kenney.nl** (Tower Defense Kit,
  Space Kit), **quaternius.com** (Ultimate Mechs, Ultimate Space Kit,
  Cars/Military), **poly.pizza** (חיפוש נוח בכל ה־CC0).
- אסור להשתמש במודלים/טקסטורות של משחקי EA המקוריים — גם אם מישהו העלה אותם
  ל־Sketchfab (העלאה כזו היא עצמה הפרת זכויות).

## רשימת קניות מומלצת — יחידות

| קובץ (id) | היחידה | מה לחפש |
|---|---|---|
| `nx_harvester` | קציר | mining truck / dump truck low poly |
| `nx_mcv` | נגמ"ש בנייה | heavy truck / crane truck |
| `dm_wolverine` | הולך לינקס | small mech / battle walker |
| `dm_mbt_walker` | וורדן מק-1 | mech walker bipedal tank |
| `dm_mammoth` | קולוסוס | quad mech / heavy walker |
| `dm_apc` | נגמ"ש | APC / armored personnel carrier |
| `dm_hover_mlrs` | רחפת רקטות | hover tank / MLRS |
| `dm_disruptor` | משבש | futuristic heavy tank |
| `dm_orca` / `dm_orca_bomber` | מטוסי דומיניון | VTOL jet low poly |
| `so_scout_buggy` | באגי פשיטה | dune buggy / attack buggy |
| `so_attack_cycle` | אופנוע תקיפה | military motorcycle |
| `so_tick_tank` | טנק מתחפר | compact tank low poly |
| `so_artillery` | ארטילריה | self-propelled artillery |
| `so_stealth_tank` | טנק סמוי | stealth tank / futuristic tank |
| `so_devil_tongue` | להב תת־קרקעי | drill vehicle / tunneler |
| `so_harpy` / `so_banshee` | כלי טיס נחש | attack helicopter / dark jet |
| חי"ר (`dm_rifle`, `so_rifle`...) | חיילים | low poly soldier rigged |

מבנים: אותה שיטה בדיוק (`dm_factory.glb` יחליף את מפעל הנשק וכו'), אבל
המבנים הפרוצדורליים כבר טובים — התחילו מהרכבים.

## טיפים ל־Sketchfab

- חפשו עם המילים `low poly` + סינון **Downloadable**; מודלים "בסגנון בלנדר"
  יפים אך לרוב כבדים (מאות אלפי משולשים) — למשחק RTS עדיף עד ~10K משולשים למודל.
- אחרי ההורדה מ־Sketchfab מתקבל ZIP עם `scene.gltf` + טקסטורות — חלצו הכל
  לתיקייה `Models/` ושנו את שם קובץ ה־gltf ל־id (השאירו את הטקסטורות לידו),
  או פתחו ב־Blender וייצאו כ־GLB יחיד (הכי נקי).
- אם המודל נטען ורוד — חסרה חבילת glTFast: ב־Package Manager ודאו ש־
  `com.unity.cloud.gltfast` מותקנת (כבר הוספתי אותה ל־manifest).
