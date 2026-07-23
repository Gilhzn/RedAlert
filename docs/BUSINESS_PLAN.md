# Tiberium Dusk — Business & Product Strategy

*Version 1.0 · July 2026 · Founder-facing working document*

> Scope: a serious, actionable go-to-market and monetization plan for **Tiberium Dusk**, an original-IP, browser-native 3D real-time-strategy game (C&C Tiberian Sun *style*, zero EA assets). Live demo: https://gilhzn.github.io/RedAlert/. This is a strategy document, not a forecast — numbers are order-of-magnitude, with the reasoning shown so you can update them as real data arrives.

---

## תקציר מנהלים

טיבריום דאסק הוא משחק אסטרטגיה תלת־מימדי בזמן־אמת, שרץ ישירות בדפדפן — בלי התקנה, בלי הורדה, קליק אחד ואתה בפנים. זהו היתרון המרכזי: ה"טריז" שלנו לשוק ב-2026 הוא **"עשר שניות עד הכיף"** בז'אנר שבדרך כלל דורש הורדה של גיגה-בייטים. הקהל: מעריצי C&C נוסטלגיים (בני 30–50), חובבי RTS מודרני, וקהל ה-.io שמחפש עומק. המודל העסקי המומלץ: **Free-to-Play מבוסס קוסמטיקה בלבד** — סקינים ליחידות ולבסיס, צביעת פלגים, ערכות HUD, אמוטים ואנימציות ניצחון — פלוס Battle Pass עונתי וחבילת מייסדים. **אסור בתכלית האיסור pay-to-win** במשחק תחרותי; זה הורג את הקהילה. הכנסה ראשונה: חבילת מייסדים עוד לפני ההשקה המלאה. המנוע לצמיחה: לינק-ריפליי משותף שמתפשט ויראלית, סטרימרים, ועורך מפות שהקהילה בונה בו תוכן. הסיכון הגדול: netcode, רמאות בדפדפן, ומשפטי (להישאר נקי מ-IP של EA). החפיר (moat) האמיתי: קהילה + תוכן משתמשים + IP מקורי משלנו.

---

## 1. Product Vision & Positioning

**One-line positioning:** *The RTS you can play right now, in the tab you already have open, with a friend, in 60 seconds — no download, no launcher, no 40GB.*

### Why browser-native is a real wedge in 2026

The RTS genre has a structural distribution problem: it is a "hard" genre (steep learning curve, long matches, APM-heavy) sold through a "high-friction" channel (buy → download → patch → launch → tutorial). Every step sheds players. Browser-native collapses the top of that funnel to a single click. The demo is *already the marketing*.

Three tailwinds make this timely:

1. **RTS is mid-revival.** *Battle Aces*, *ZeroSpace*, *Stormgate* (troubled but proof of appetite), *Tempest Rising* (2025, direct Tiberian-Sun homage that sold well) all signal that the "modern accessible RTS" thesis has market pull. Big studios are validating demand; none of them are browser-instant.
2. **WebGL/WebGPU maturity.** Three.js and Unity WebGL now render a 3D RTS at 60fps on a mid laptop. The technical barrier that made "browser 3D RTS" a joke in 2015 is gone.
3. **Nostalgia demographic has disposable income.** The Tiberian Sun generation is now 35–50, earning, and sentimental. They will not reinstall a 1999 game, but they will click a link in a Discord.

### The "10-seconds-to-fun" hook

The single most important product asset is the **instant-skirmish**: landing on the URL drops the player into a pre-warmed 1v1-vs-AI match with a friendly economy already running, or a 90-second "assault a base" scenario. No menu, no account, no faction-choice paralysis first. Fun *then* signup. Every design decision upstream of the first fight is friction to be deleted.

### Target audience segments (priority order)

| Segment | Size signal | Why they convert | Hook |
|---|---|---|---|
| **Nostalgic C&C / Tiberian-Sun fans** | r/commandandconquer ~150k; OpenRA active | Emotional pull, disposable income | "The Tib Sun feeling, in a browser" |
| **Modern RTS players** (SC2/AoE4/Battle Aces) | AoE4 peaked >70k concurrent | Want a lighter, faster ladder | Ranked ladder, clean netcode |
| **.io / browser-strategy crowd** | agar/generals.io: millions of sessions | Zero-friction, share-with-friends | Instant 1v1 link |
| **Streamers / content creators** | RTS is high-watchability | Free, shareable, novel | Spectator + replay links |

Do **not** try to out-StarCraft StarCraft. The wedge is *accessibility and distribution*, not mechanical depth ceiling. Win the "I have 15 minutes and want an RTS fix" moment.

---

## 2. Market Analysis

### Landscape

- **PC RTS (paid, premium):** *Tempest Rising* (~$40, 2025), AoE4, SC2 (F2P core + paid). Deep, but download-gated. These are your *inspiration ceiling*, not your competitors — different distribution.
- **Modern F2P RTS:** *Battle Aces* (Uncapped/NetEase) — accessible, ranked, cosmetic-monetized. This is your closest *design and monetization* comparable. Learn from it; note it's still a downloaded client.
- **Browser / .io strategy:** generals.io, various agar-likes, browser tower-defense/strategy. Massive session volume, thin monetization, shallow depth. This is your *distribution comparable* — you bring their frictionlessness with far more depth.
- **Mobile RTS:** Clash-style and *Rise of Kingdoms*-style — huge revenue, but built on pay-to-win/4X-timer mechanics you are explicitly avoiding. Relevant only as a caution.

**The empty quadrant you occupy:** *high strategic depth × zero-friction browser distribution × ethical (cosmetic-only) monetization.* Nobody credible is sitting there. That gap is the whole thesis.

### TAM / SAM / SOM (order-of-magnitude)

Anchoring, not precision:

| Layer | Definition | Rough scale | Reasoning |
|---|---|---|---|
| **TAM** | Global PC/browser RTS-interested players | ~40–60M | SC2+AoE+CoH+C&C legacy + RTS-curious; genre is niche vs FPS but durable |
| **SAM** | English/Hebrew-reachable players who'll try a browser RTS | ~3–6M | Fraction of TAM reachable via Discord/Reddit/streamers/SEO without a marketing budget |
| **SOM (Yr 1)** | Realistic reachable + retainable | ~50k–150k registered, ~5k–15k MAU | What a bootstrapped indie can pull via organic loops in 12 months |

At a cosmetic-F2P ARPU of ~$0.50–$1.50/MAU (see §3), a **10k MAU** base implies roughly **$5k–$15k/month** gross — a realistic, defensible year-one target that funds the founder and a contractor artist. This is a *lifestyle-to-small-studio* business first; the venture-scale outcome depends on the UGC/IP flywheel compounding (§7).

**Honest read:** RTS is niche. You will not accidentally get 10M players. But niche + near-zero distribution cost + loyal-spender demographic = a viable, self-funding business well before it's a "big" one.

---

## 3. Monetization Model

### Recommendation: Cosmetic-only Free-to-Play, layered.

**This is non-negotiable for a competitive RTS.** In a symmetric skill game, any purchasable advantage destroys the only thing that matters — trust in the ladder. Pay-to-win is *fatal* here, not just distasteful. Every mechanic below sells *identity and status*, never *power*.

### The stack

| Layer | What it is | Indicative price | Role |
|---|---|---|---|
| **Free tier** | Full game, all units, ranked, skirmish, ads on the instant-browser funnel only | Free | The acquisition engine — never cripple it |
| **Founder / Supporter Pack** | Early-access, exclusive founder skin + HUD theme + name flair, permanent | **$15–$25** one-time | *First dollar*, pre-launch, before F2P infra exists |
| **Cosmetics (direct)** | Unit skins, base/building skins, faction paint schemes, HUD/terminal themes, emotes, victory animations, EVA announcer voice packs | **$3–$12** per item; bundles $15–$25 | Core ARPU; the EVA voice-pack and faction-paint are natural high-value SKUs |
| **Battle Pass / Season** | ~8–10 week seasons, free + premium track, ~40–60 cosmetic tiers | **$8–$10** / season | Retention + predictable recurring revenue |
| **Ranked ladder** | Free to play; monetize via prestige cosmetics, seasonal badges, animated rank borders | Free entry | Status economy, not a paywall |
| **Ad-supported free** | Interstitial between AI skirmishes on the non-logged-in funnel; **never** in ranked/multiplayer | — | Monetize the huge top-of-funnel that never signs up |

### On gacha

A cosmetic loot-box/gacha *can* work (see *Valorant*, *League*) but carries real risk: regulatory (EU/Belgium/Netherlands loot-box law), reputational (indie goodwill is your moat — don't spend it), and operational (needs volume you won't have early). **Verdict:** skip gacha at launch. If you ever add it, make odds transparent, cosmetic-only, and offer a direct-buy alternative for every item. Ship *direct sales + Battle Pass* first; they're cleaner and higher-trust.

### Price points, conversion & ARPU — with reasoning

F2P conversion is typically **1–3%** of active users paying. Cosmetic-only, niche-but-loyal audiences skew to the *high* end of engagement-per-payer but *lower* raw conversion (fewer impulse buyers than a casual mobile title). Working assumptions:

- **Paying-user rate:** 2% of MAU (conservative for a loyal RTS niche).
- **Monthly ARPPU** (avg revenue per *paying* user): **$8–$12** (a Battle Pass amortized + occasional skin).
- ⇒ **Blended ARPU ≈ $0.16–$0.24/MAU/month** from IAP alone. Add ads on the anonymous funnel (~$0.02–$0.05 eCPM-driven per free session at scale) — modest but real at volume.
- The Battle Pass is the workhorse: if even **5% of MAU** buy a $9 season pass, that alone is **$0.45/MAU** over the season. **Prioritize shipping the pass** over one-off skins.

**Reality check:** these numbers only matter above ~3–5k MAU. Below that, revenue comes from *founder packs and superfans*, not ARPU curves. Plan the first $10k as **~500 founders × $20**, not as a conversion-rate spreadsheet.

### What to avoid (write this on the wall)

- ❌ Pay-to-win: purchasable units, stat boosts, faster harvesting, XP-locked balance.
- ❌ Energy/timer gates on playing.
- ❌ Selling ladder rank or matchmaking advantage.
- ❌ Cosmetics that change silhouette/readability (a skin must never confuse unit identification — competitive integrity *and* fairness).

---

## 4. Live-Ops & Retention

Retention is the whole game for F2P. Acquisition without retention is a bucket with a hole. The lockstep architecture already gives you **replays, spectators, and deterministic re-simulation for free** (per `architecture.md`) — that is a live-ops goldmine most indies have to build from scratch. Exploit it.

| System | What | Why it retains |
|---|---|---|
| **Seasons** (~8–10 wk) | Battle Pass, ladder reset, themed cosmetics, 1–2 balance patches | Rhythm + reason to return; caps ladder anxiety |
| **Daily/weekly challenges** | "Win with Serpent Order", "harvest 5000 Veridium", "win a 4-min rush" | Habit loop, light XP toward pass |
| **Map rotation** | Curated ranked pool rotating per season | Freshness without fragmenting matchmaking |
| **Replays** | Deterministic order-log playback; shareable link | Learning, content, viral loop (§5) |
| **Spectator** | Live observe via relay | Streaming, tournaments, community events |
| **UGC map editor** | Community-built maps + scenarios | *The* long-term retention + acquisition flywheel |

### The UGC flywheel is the strategic core

The editor is not a feature — it's the **engine of durable retention and free content**. StarCraft's mod scene birthed DOTA and Tower Defense; Warcraft III's editor created entire genres. For a small team, **community-created maps are infinite content you don't have to build.** Ship a capable editor early, feature community maps in-client, run map contests with cosmetic prizes, and let the best UGC maps rotate into ranked. Each creator brings their own audience. This is how a two-person studio produces the content volume of a fifty-person one.

**Sequencing:** replays/spectator (nearly free from the architecture) → daily challenges/seasons → editor. The editor is a Phase-2 investment but should be on the roadmap from day one because it changes the retention math permanently.

---

## 5. Growth / Go-to-Community (GTC)

No paid UA budget assumed. Growth must be **organic and loop-driven.**

### Loop 1 — The shareable replay/instant-match link (primary viral loop)

Because matches are deterministic order-logs, a replay is a tiny shareable URL that re-simulates in any browser. And because the game *is* a URL, "play me → click, you're in a 1v1 with me in 30 seconds" has no install step. This is the FPS/battle-royale "join my lobby" mechanic, but with **zero download** — a genuinely stronger loop than the genre has ever had. Instrument and optimize this relentlessly: every match-end screen should have prominent "Share this game" and "Challenge a friend" buttons.

### Loop 2 — Creators & streamers

RTS is high-watchability, low-creator-cost content. Spectator mode + replay links make the game trivial to feature. Tactics: seed 20–50 mid-tier RTS/C&C YouTubers and Twitch streamers with founder packs and exclusive creator cosmetics; run a monthly community tournament they can cast; make "cast this replay" a one-click flow. One mid-size streamer featuring a browser RTS ("you can just click and play this") is a spike you can't buy.

### Loop 3 — SEO & the "play RTS in browser" intent

Own the long tail: *"play RTS in browser", "command and conquer browser", "free RTS no download", "tiberian sun style game".* These are low-competition, high-intent queries a static site can rank for. The playable demo *is* the landing page — best conversion asset possible. Add a lightweight content layer (unit guides, strategy posts, patch notes) for SEO surface.

### Loop 4 — Discord community

The home base. Founders, playtesters, tournament org, map-sharing, feedback, and the emotional core of the moat. Stand it up now, before launch. Nostalgia communities (r/commandandconquer, OpenRA forums, C&C Discords) are warm audiences — engage authentically as a fellow fan building original-IP homage, not as a marketer.

### Later — Steam / itch.io port & wishlists

A Steam page (even for the same WebGL/desktop build) unlocks the wishlist machine and a second discovery surface. Do this *after* the browser loop is proven — a Steam page with a trailer full of real community footage converts far better than a cold launch. itch.io is a cheap early beachhead (per `deploy.md`, the build already ships there trivially).

---

## 6. Unit Economics & Funding

### Cost structure (the good news: it's light)

An RTS-in-a-browser has an unusually favorable cost base — **no per-unit COGS, near-zero distribution cost.** The three real cost centers:

1. **Infrastructure** — static hosting (near-free: GitHub Pages/Netlify/S3+CloudFront) + the **lockstep relay server**. Crucially, lockstep relays *orders, not game state* (`architecture.md`): bandwidth per match is tiny (a few kbps), and the server does light validation, not simulation of every client. A single modest VPS handles hundreds of concurrent matches. **Server cost per MAU is on the order of cents, not dollars** — likely **$0.01–$0.05/MAU/month** at small-to-mid scale. This is the structural advantage: you are not renting GPU or streaming state.
2. **Art / cosmetics** — the actual variable cost. Skins, faction paints, HUD themes, victory animations. Contract 3D/VFX artists per-SKU. This is where money goes *and* where revenue comes from, so it's a healthy self-funding loop once the store exists.
3. **Live-ops labor** — founder time + eventually a part-time community/ops person. Seasons, balance, tournaments, moderation.

### Break-even reasoning

Bootstrapped, solo-founder + occasional contractor, monthly burn might be **$1k–$3k** (contract art + server + tools; founder unpaid or minimally paid early). Break-even is therefore **hundreds of paying users, not thousands** — reachable via founder packs alone before F2P even scales. This is a business that can be *default-alive* early, which is exactly what you want as an indie: it buys time for the flywheel to compound.

### Bootstrapped vs seed-funded

| Path | When it fits | Trade-off |
|---|---|---|
| **Bootstrapped** (recommended default) | You can self-fund $1–3k/mo burn; want control; validating thesis | Slower art/content cadence; founder wears every hat |
| **Seed / angel** | *After* the browser viral loop shows real retention (D30 > ~10%, organic k-factor near/above 1) | Only raise on proof; RTS indies raising pre-traction is a graveyard (cf. Stormgate) |

**Opinion:** stay bootstrapped until the retention and viral-loop metrics are undeniable. RTS is littered with over-funded corpses that raised on ambition. Raise to *pour fuel on a fire*, never to *start* one.

### Phased "money-in-the-door" roadmap

1. **First:** Founder/Supporter Pack ($15–$25) — sell *access + status* before F2P infra exists. Validates willingness-to-pay and funds the first artist.
2. **Then:** Direct cosmetic store (a handful of high-value SKUs: faction paints, EVA voice pack, victory anims).
3. **Then:** Battle Pass / seasons — the recurring-revenue backbone.
4. **Later:** Ad-supported anonymous funnel; Steam wishlists → paid supporter tiers.

Charge for **identity first, recurring second, ads last.**

---

## 7. Risks & Moats

### Risks

| Risk | Severity | Mitigation |
|---|---|---|
| **Legal — EA / C&C IP** | High if sloppy | Already well-handled in the design: original names (Veridium not Tiberium, Dominion/Serpent Order not GDI/Nod), original art/audio, EA `reference/` files git-ignored. **Hold this line absolutely** — no leaked EA assets, no "totally like Tiberian Sun™" marketing, no trademarked names anywhere shippable. Homage in *feel*, original in *everything nameable*. Consider a trademark search before committing the name commercially. |
| **Cheating in a browser** | High (competitive integrity) | Client is untrusted. Lean on determinism: the relay re-validates orders against the sim (`architecture.md` already designs for headless server verification); state-hash desync detection catches divergence. Server-authoritative validation of *legality* of orders (not just relay). Fog-of-war maphacks are the hardest class — mitigate with server-side fog enforcement where feasible for ranked. Accept some cheating in casual; protect ranked hard. |
| **Netcode / desync** | Medium-High | Lockstep is unforgiving but already the chosen, tested architecture (105 tests incl. real-socket lockstep, replay reproduction). Reconnect grace and desync dumps exist in design. Keep determinism discipline non-negotiable. |
| **Niche demand ceiling** | Medium | Accept it; the model is built for niche economics (§6). Don't over-invest against a mass-market fantasy. |
| **Founder bandwidth / bus factor** | Medium | The UGC flywheel and community offload content; document ruthlessly; the sim/view split keeps the codebase testable and onboardable. |
| **Balance / competitive health** | Medium | Data-driven balance (`data/*.json`) enables fast iteration; seasonal patch cadence; community feedback via Discord. |

### The durable moat

Not the code (rebuildable), not the art (copyable), not "3D RTS in browser" (a big studio could do it). The moat is the **compounding trio**:

1. **Community** — a loyal Discord + ladder + tournament culture is not clonable overnight.
2. **UGC ecosystem** — once creators build maps/scenarios *inside your editor for your audience*, switching cost is real and content compounds for free.
3. **Original IP** — Dominion vs Serpent Order, Veridium, the EVA announcer, the world lore. Owning the IP means you can extend it (campaigns, merch, lore, sequels) without EA's lawyers, and it's the asset that appreciates. C&C fans can't get *this specific thing* anywhere else, legally.

A big studio can copy the format. They cannot copy *your community's maps, your ladder's history, and an IP the fans have adopted as their own.* Invest there.

---

## 8. 12-Month Execution Roadmap (tied to revenue milestones)

The engine is largely built (per README: all 10 technical phases implemented). So this roadmap is **product-polish + business**, not core engineering. Each stage has *one thing to prove.*

| Phase | Months | Revenue milestone | The ONE thing to prove | Key moves |
|---|---|---|---|---|
| **0 · Prove the hook** | 1–2 | $0 (instrument) | **10-seconds-to-fun works**: anonymous visitor reaches a fun fight fast, and comes back (D1 retention signal) | Instant-skirmish landing, analytics, Discord live, SEO landing page, seed 10 playtesters |
| **1 · First dollar** | 2–4 | **First $** → toward **$1k total** | **People will pay** for this world before it's "done" | Ship Founder Pack ($20), first faction-paint + EVA voice SKU, streamer seeding, share-a-match button |
| **2 · First $1k MRR** | 4–7 | **$1k MRR** | **Recurring** revenue exists: Battle Pass renews, ladder retains | Season 1 + Battle Pass, ranked ladder, daily challenges, replay-link viral loop instrumented + optimized |
| **3 · First $10k MRR** | 7–12 | **$10k MRR** | **The flywheel compounds**: UGC + creators drive acquisition, seasons drive retention, ARPU holds | Map editor + featured community maps, ad-supported anonymous funnel, Steam wishlist page, first community tournament, hire part-time artist/ops |

### Milestone logic

- **First dollar (~$1k total):** ≈ 50 founders × $20. Proves willingness-to-pay. If you can't sell 50 founder packs to a warm C&C-nostalgia audience, the thesis needs rework *before* you build a store.
- **$1k MRR:** ≈ needs a few thousand MAU with a live Battle Pass, or a smaller base of devoted seasonal buyers. Proves *recurring*, not one-time, revenue — the real business.
- **$10k MRR:** ≈ 8–15k MAU at cosmetic ARPU + ad funnel. This is the "small sustainable studio" line — funds founder + a contractor. Getting here **requires the UGC/creator flywheel doing acquisition work**; you cannot grind to 10k MAU on founder goodwill alone.

### North-star metric

Not revenue — **weekly retained players who've shared at least one match link.** That single metric captures fun (they stayed), the viral loop (they shared), and the health of the whole model. Revenue follows it.

---

## Appendix: The three things that matter most

1. **Guard the ladder with your life.** Cosmetic-only, forever. The moment RTS players smell pay-to-win, the community — your entire moat — evaporates.
2. **The map editor is the business, not a feature.** It's how two people ship the content of fifty. Get it on the roadmap and into creators' hands as early as the store.
3. **Sell status before you sell scale.** First $10k comes from ~500 founders who love this world, not from an ARPU spreadsheet. Build the community first; the monetization curve is a consequence, not a cause.

*Honest closing note:* RTS is a hard genre and a niche market, and browser 3D is a rendering/anti-cheat challenge. None of this is a get-rich-quick play. But the cost structure is genuinely light, the distribution wedge is genuinely novel, and the architecture you've already built (deterministic sim, replays, lockstep, data-driven balance) is exactly the foundation a live-ops F2P game needs. The technical risk is largely behind you. The remaining risk is a *market and community* problem — which is the fun kind to have.
