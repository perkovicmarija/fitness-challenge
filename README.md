# Fitness Challenge

A workplace fitness challenge. People log activities, activities earn points, points make a
leaderboard, and everyone gets a dashboard of their own effort.

ASP.NET Core 10 · Angular 22 · SQLite · EF Core

---

## Running it

### With Docker — nothing else to install

```bash
docker compose up --build
```

Then open **http://localhost:8080**.

One image serves both halves: the Angular app is built with Node, the API with the .NET SDK, and
the API serves the built site from the same origin. Same origin means no CORS layer and no second
container to keep in step. The database is a file on a named volume, so rebuilding the image does
not reset the challenge.

### Without Docker — needs .NET 10 and Node 22

```bash
./run.sh
```

Starts both and shuts both down on Ctrl+C.

| | |
|---|---|
| Web | http://localhost:4200 |
| API | http://localhost:5032 |
| OpenAPI | http://localhost:5032/openapi/v1.json |

The Angular dev server proxies `/api` to the API (`web/proxy.conf.json`), so the browser only ever
talks to one origin here too.

---

## What you should see

Both commands seed a challenge already in progress: five people, about 400 activities over twenty
weeks. Seeding runs only into an empty database, so it never overwrites anything you log yourself.

The seed is shaped, not random. The leaderboard's trend column compares today's ranking with the
ranking a week ago, and data spread evenly would leave every row reading "no change" — the feature
would be there and invisible. So:

- **Iva** was quiet all period and had a heavy final week. She climbs three places.
- **Luka** trained hard all period and logged nothing this week. He falls three places.
- **Ana** and **Sara** are steady, and show no movement — which is how you can tell the dash is a
  real state and not a bug.
- **Boris** joined this week. He has no rank to compare against, so he shows as *new* rather than
  as unchanged.

Turn seeding off with `Seed=false`.

---

## The AI coach

An addition, not part of the assignment.

**The arithmetic is done in C#; the language model only interprets.** The API computes what it
would actually take to move up a place — by reading the scoring table backwards — along with the
person's own weekly average, how many of the last 28 days they trained, and what each sport pays
them per session. Those go to the model, which is told to use them, quote them exactly, and
calculate nothing of its own. Models are unreliable at arithmetic and this application is entirely
arithmetic, so the two jobs are separated.

The figures are chosen so there is something to say. A rank and a gap can only be read back out
loud; a person's own average is a comparison, and a comparison is an observation — *this week is
unusual for you*, *you have never swum*, *one swim is worth two trips to the gym*. The briefing
asks for one such observation before any advice, and forbids repeating the list of ways to move up
because the screen is already showing it.

The gap is `their points − yours + 1`, not the difference. Equal totals are ordered by who reached
them first, and that is always the person above, so matching them changes nothing. A test logs
exactly the quantity the screen suggests and asserts the ranking really moved.

**It is optional.** Without Azure credentials the figures still appear — they never needed a
model — and the chat says it is switched off rather than failing. That is the state anyone who
clones this repository runs in.

To switch it on:

```bash
cp .env.example .env     # then fill in the three values
```

You need three things, not two: the endpoint, the key, and the **deployment name**, which in Azure
is not the same as the model name. The endpoint may be either the resource root or the full target
URI the portal offers for copying — the second is the obvious thing to paste, so both are accepted. One `.env` file is read by both `docker compose` and `run.sh`;
it is git-ignored, and `.env.example` is the committed template.

Compose reads `.env` when it *creates* a container, so after editing the file run `docker compose
up -d` again. It recreates the container with the new values — no rebuild needed. Until you do,
the coach will still report itself switched off.

Other competitors are sent to Azure as ranks, never as names — "201 points would move them up one
place", not "Ana is 201 ahead". The advice is just as useful and no colleague's name reaches a
third party. A test asserts the names are absent from what gets sent.

---

## How it is put together

```
src/FitnessChallenge.Domain    scoring rules and point calculation — no dependencies at all
src/FitnessChallenge.Api       endpoints, EF Core, the coach
web/                           the Angular app
tests/…Domain.Tests            the rules, tested without a database or a web host
tests/…Api.Tests               every endpoint, over a real HTTP stack
```

The domain is a separate project so that the scoring rules cannot quietly acquire a dependency on
EF Core or on ASP.NET Core. If they ever needed to, the compiler would have to be told, and that
is a conversation worth having.

`ActivityRules` is one table and the single source of truth. Point calculation, submission
validation, the sport list the API serves, the coach's suggestions and the briefing sent to Azure
all derive from it, so adding an activity type is one line and no change anywhere else.

---

## Decisions worth knowing about

**Points are `decimal`, never `double`.** Scoring floors a product, and binary floating point
cannot represent 0.29 exactly. Measured across the 10,000 distances at 0.01 km resolution up to
100 km: **573 of them score one point lower with `double`** — 0.29 km earns 28 points instead of
29. Nothing about that failure announces itself, and the assignment's own worked example
(1.55 km walking = 77) is correct either way, so testing only the given sample proves nothing.

**Rounding happens twice, in two different places.** Duration and steps are rounded down *before*
scoring: `mm:ss` becomes whole minutes, and a step is worth 0.01 points so incomplete hundreds
vanish. Distance is the only measurement whose fraction survives to the multiplication and is
floored *after*. Both are in the assignment; collapsing them into one rule would score distance
wrongly.

**Discarded seconds hit short sessions hardest.** A 1:55 swim scores the same as 1:00, so it earns
15 points where a proportional rule would award 28 — 48% less. On a 40-minute session the same
rule costs about 1%. This is what the assignment specifies and it is implemented as specified; it
is noted here because it is the kind of thing a real challenge would eventually argue about.

**The day comes from the client, not from a server timezone.** Each activity stores its instant,
the offset the client submitted, and the resulting local date. Anything that groups by day — the
calendar, the daily chart, the streak — uses the local date, so an activity at 01:30 lands on the
day the person actually experienced. No timezone is stored on the user, because nothing said
everyone is in one place.

**Ties are broken by who got there first.** Without a total order the same request twice can return
two different rankings, and its tests flicker.

**Rank history is recomputed, not stored.** Every activity carries its instant and its points, so
any past ranking is derivable with one aggregate — no snapshot table, no background job. At
production scale this becomes a materialised daily snapshot; at this size recomputation is both
cheaper and correct by construction.

**The frontend's response types are hand-written.** They mirror `docs/contract.md` rather than
being generated from `/openapi/v1.json`, so a change to a response shape would break at runtime in
the browser rather than at compile time. What actually catches such a change today is the backend's
integration tests, which pin every response shape. Generating the types would move that check one
step earlier, and it is the first thing I would add next.

**The layout is responsive because the assignment asks for one.** The same components reflow;
there is no separate mobile view. What is actually checked is that the page never scrolls
sideways — wide content scrolls inside its own container instead — measured on every screen at
390, 430, 768 and 1280 pixels wide, against the competitor with the most data. An empty dashboard
would pass that check without proving anything.

**Points are calculated once, at ingest.** The read side never recomputes them, so a leaderboard
query cannot disagree with what a person was told when they logged the activity.

---

## Tests

```bash
dotnet test             # 139: rules, scoring, and every endpoint over real HTTP
npm --prefix web test   # 39: components, forms and chart maths
```

The API tests run against an in-memory SQLite database, one per test, so nothing needs cleaning up
between runs and no test can see another's data. The clock is injected, so the rank trend and the
streak assert against a fixed instant instead of whatever time the suite happens to run at. The
coach is tested through a stub, so the suite needs no Azure key, no network, and gets the same
reply every time.

Ones worth reading:

- `PointsGapTests` scores every suggestion the coach makes and asserts it really earns the gap, so
  the two directions of the rule table cannot drift apart.
- `CoachEndpointTests.DoingWhatItSuggestsActuallyMovesYouUp` logs the suggested distance and checks
  the leaderboard changed.
- `ActivityRulesTests.EverySportHasExactlyOneRule` fails the build if a sport is added without a
  rule, rather than throwing the first time somebody submits it.
- `LogActivitySpec` checks the form sends the measurement the chosen sport is scored on. It exists
  because that broke once: the choice was held in a plain field, the computed reading it never
  recalculated, and every sport silently asked for a distance. Every backend test still passed —
  the fault only existed in the browser.

CI runs all of it on every push, and builds the container image too — the reviewer's own path into
the app is the one under test.

---

## Toolchain notes

- **.NET 10** and **Node 22**.
- **npm 12 is required.** npm 10 crashes resolving this dependency tree (`canvas`, an optional peer
  of jsdom, which Angular 22 pulls in for testing). `npm install --global npm@12` fixes it; the
  Dockerfile and CI do this themselves.

---

## The full design record

`docs/contract.md` holds the design gate written before any code: the activity rules, the rounding
rules, the timezone decision with the alternatives that were rejected, every endpoint and its
status codes, the complete validation matrix, and the non-goals — each with what it is, why it is
not built, and what would trigger building it.
