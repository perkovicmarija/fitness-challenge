# F0101 — Scope & contract

Design gate for the whole project. Nothing in phases 2–6 is built until this is
approved. This document is the single source the API, the scoring engine, the
validation rules and the tests are derived from.

Everything here is a decision already taken, not a question left open.

---

## 1. Activity rules

Six activity types. Each has exactly one metric and one conversion rate.

| Activity type | `sport` on the wire | Metric | Rate |
| --- | --- | --- | --- |
| Running | `"running"` | Distance (km) | 100 points / km |
| Walking | `"walking"` | Distance (km) | 50 points / km |
| Cycling | `"cycling"` | Distance (km) | 25 points / km |
| Swimming | `"swimming"` | Duration (`mm:ss`) | 15 points / minute |
| Gym | `"gym"` | Duration (`mm:ss`) | 5 points / minute |
| Daily steps | *field absent* | Count (steps) | 1 point / 100 steps |

This table is the **single source of truth**. Validation, scoring and
`GET /api/sports` are all derived from it. Adding an activity type is one entry
and touches no other code.

**Domain name vs. wire value.** Internally there are six activity types,
including `DailySteps`. On the wire, daily steps is expressed as the *absence*
of `sport`, exactly as the assignment specifies. The domain never models
"no type"; the API mapping owns that translation.

### Rounding

Three metrics, but **two different rounding stages**. This distinction is the
core of the scoring engine.

- **Distance** — round the **points**, after multiplying.
  `points = floor(distance × rate)`
  `1.55 km walking → 77.5 → 77`
- **Duration** — round the **quantity**, before multiplying. Only whole minutes
  count. `minutes = floor(totalSeconds / 60)`, then `points = minutes × rate`.
  `1:55 swimming → 1 minute → 15`
- **Steps** — round the **quantity**, before dividing. Only complete blocks of
  100 count. `points = floor(steps / 100)`
  `399 steps → 300 → 3`

### Decimal, never floating point

Distance arithmetic uses `decimal`. With `double`, `floor(0.29 × 100)` yields
**28** instead of 29, because 0.29 has no exact binary representation. Measured
over 10 000 distance values at 0.01 km resolution, 1 004 of them produce the
wrong point total under `double`.

The assignment's own example (1.55 km walking = 77) is correct under both, so
the defect is invisible if you only test the given sample. It is covered by an
explicit test.

---

## 2. Time and days

### The problem

Points are a total, but the dashboard groups by **day** — the volume chart, the
consistency heatmap, the streak, and the notion of "daily" steps. So every
activity must belong to a day, and the timestamp alone does not say which one.

An activity at 01:30 local time in a UTC+2 zone is `2026-06-30T23:30:00Z`.
Counted in UTC it falls on 30 June; on the user's own calendar it is 1 July.

### The decision

**The day comes from the offset the client already sends.** ISO 8601 carries it:
`2026-07-01T01:30:00+02:00` states both the instant and the wall-clock time the
user saw.

Each activity therefore stores two values, both written once at ingest:

- **`OccurredAtUtc`** — the instant. Used for ordering, point totals, the
  leaderboard and the rank trend. Anything that compares users must use one
  common measure.
- **`LocalDate`** — the calendar date on the user's own clock, derived from the
  submitted offset. Used for everything that groups by day: the volume chart,
  the heatmap and the streak.

### Why not the alternatives

- **A fixed application timezone** (`Europe/Zagreb` or similar) invents a fact
  nobody gave us. Nothing in the assignment says where users are. It would be a
  hardcoded assumption wearing a configuration setting as a disguise.
- **A timezone per user** is the most correct answer and is rejected only
  because the registration contract carries `firstName` and `lastName` and
  nothing else. Adding the field would change an API the assignment specified.

The chosen approach needs no configuration, invents no field, and uses only
information the client already supplies.

### Consequences, stated plainly

- A timestamp **must** carry an explicit offset (`Z` or `±hh:mm`). A naive
  timestamp such as `2026-06-30T10:30:00` is rejected with 400: without an
  offset the user's calendar day is genuinely unknowable.
- A client that always sends `Z` gets UTC day boundaries. For a user in UTC+12
  that can be the wrong day — but the information was discarded by the client,
  not by us. This is a documented limit, not a defect to fix.

---

## 3. Endpoints

All responses are JSON. Errors use ASP.NET Core's built-in `ProblemDetails`
(RFC 7807); validation failures use `ValidationProblemDetails` with per-field
messages. No custom error envelope, no extra dependency.

### `POST /api/users`

Registers a user.

```json
{ "firstName": "Ana", "lastName": "Horvat" }
```

- **201 Created** — `{ "id": "<guid>" }`
- **400** — either name missing, blank, whitespace only, or over 100 characters
- **409 Conflict** — a user with the same name already exists

**Name comparison is normalized** before the uniqueness check: surrounding
whitespace trimmed, internal whitespace collapsed to single spaces, compared
case-insensitively. Without this, `"Ana Horvat"` and `"ana  horvat"` both
register and the leaderboard shows the same person twice.

The stored name keeps the user's original spelling; only the comparison is
normalized.

### `GET /api/users`

Lists registered users. Required by the frontend: the dashboard is "a view for a
specific user", so the user has to be selectable.

- **200** — `[{ "id", "firstName", "lastName" }]`

### `POST /api/activities`

Ingests one activity and stores its points.

```json
{
  "userId": "<guid>",
  "datetime": "2026-06-30T10:30:00Z",
  "sport": "running",
  "distance": 42.195
}
```

- **201 Created** — the stored activity, including the awarded `points`
- **400** — any violation of the validation matrix in section 4

Points are computed **once, at ingest**, and stored. The read side never
recomputes them.

### No `Location` header on either creation

Neither a single user nor a single activity has a URL of its own — the contract exposes a user
list and a per-user activity list, nothing addressable per resource. A `Location` header would
have to point at a collection or at nothing, so both creations return 201 with the identifier in
the body instead.

### `GET /api/sports`

Returns the activity rule table from section 1, so the frontend never hardcodes
the list or the field-to-sport mapping.

- **200** — `[{ "sport", "label", "metric", "unit", "pointsPerUnit" }]`

Daily steps appears with `"sport": null`, which is exactly how it is sent on
ingest.

### `GET /api/leaderboard`

- **200** — `[{ "rank", "userId", "firstName", "lastName", "totalPoints",
  "previousRank", "rankDelta" }]`

**Tie-breaking.** Equal totals are ordered by the earlier most recent activity
first (whoever reached the total sooner), then by user id. Without a total order
the ranking is non-deterministic and its tests flicker.

**Rank trend.** `previousRank` is the ranking recomputed over activities with
`OccurredAtUtc` at or before `now − 7 days`. `rankDelta = previousRank −
currentRank`, so a positive number means the user moved up.

A user with no activity before the cutoff gets `previousRank: null` and
`rankDelta: null` — *unranked then*, which is not the same as *unchanged*. The
UI must distinguish the two.

The 7-day window is fixed, not a query parameter. Nothing asked for a
configurable window.

**No snapshot table and no background job.** Every activity stores its instant
and its points, so any past ranking is derivable with one aggregate. At
production scale this would become a materialized daily snapshot; at this scale
recomputation is both cheaper and correct by construction.

### `GET /api/users/{id}/dashboard`

Optional `from` and `to` query parameters (dates, inclusive). Filtering happens
on the server.

- **200** — aggregates only, no activity list:

```json
{
  "userId": "<guid>",
  "totalPoints": 4820,
  "currentStreakDays": 5,
  "perDay": [{ "date": "2026-06-30", "points": 210, "activityCount": 2 }],
  "perSport": [{ "sport": "running", "points": 3100, "share": 0.64 }],
  "fieldAverage": [{ "sport": "running", "share": 0.41 }]
}
```

- **404** — no user with that id

**`share` and `fieldAverage` exist for the radar chart.** They are proportions,
not totals: "64% of my points come from running" against "the field averages
41%". Comparing absolute totals would only show who has been training longer;
comparing proportions compares *profiles*, which is the question the chart is
actually asking. `fieldAverage` is the mean of every user's per-sport share.

**Streak.** Consecutive `LocalDate` values with at least one activity, counted
backwards from today. If today has no activity, counting starts from yesterday,
so a day still in progress does not destroy a live streak; if yesterday is also
empty, the streak is 0.

"Today" is resolved using the offset from that user's most recent activity. This
keeps the rule consistent with section 2 and still invents no stored timezone.

### `GET /api/users/{id}/activities`

The activity history the dashboard view is required to show. Same optional
`from` and `to`. Ordered newest first.

- **200** — `[{ "id", "occurredAt", "localDate", "sport", "distance",
  "duration", "steps", "points" }]`
- **404** — no user with that id

Kept separate from the dashboard aggregates: the aggregates are small and always
needed, the history is a list that would be the first thing to need paging.

### 400 or 404 for an unknown user

Deliberately inconsistent, for a reason worth stating:

- **In a request body** (`POST /api/activities` with an unknown `userId`) → 400.
  The assignment requires 400 for an invalid body, and an unresolvable reference
  makes the body invalid.
- **In the path** (`GET /api/users/{id}/...`) → 404. The addressed resource does
  not exist.

---

## 4. Validation matrix

Every ingest request is checked against this. The matrix is **derived from the
rule table in section 1**, not written as a chain of conditionals — adding an
activity type must not require touching validation.

### Always required

| Field | Rule | On failure |
| --- | --- | --- |
| `userId` | present, a GUID, belongs to an existing user | 400 |
| `datetime` | present, ISO 8601, **explicit offset** | 400 |

### Metric fields by activity type

Exactly one metric field is allowed, and it must be the one the activity type
requires. Any other metric field present is a rejection, which is what makes the
assignment's own invalid example (`swimming` with `distance`) fail.

| `sport` | `distance` | `duration` | `steps` |
| --- | --- | --- | --- |
| `running`, `walking`, `cycling` | **required** | must be absent | must be absent |
| `gym`, `swimming` | must be absent | **required** | must be absent |
| *absent* | must be absent | must be absent | **required** |
| any other value | 400 — unknown sport | | |

### Value rules

| Field | Rule |
| --- | --- |
| `distance` | decimal, greater than 0, stored to 3 decimal places |
| `duration` | string `mm:ss`; minutes ≥ 0 and may exceed 59; seconds 0–59; total greater than 0 |
| `steps` | integer, greater than 0 |

`"90:30"` is valid — 90 minutes 30 seconds. `"5:75"`, `"5"`, `"-1:00"` and `""`
are not.

Distance is stored at 3 decimal places, matching the assignment's own
`42.195`. Finer precision cannot change any point total: a thousandth of a
kilometre is at most 0.1 points, which the flooring discards.

Zero is rejected for every metric. A zero-valued activity scores nothing and
records nothing; accepting it only pollutes the history and the streak.

### Unknown JSON properties are rejected

A body carrying a property outside the schema returns 400
(`JsonUnmappedMemberHandling.Disallow`). The rule that exactly one metric may be
present is meaningless if unknown fields can be smuggled past it.

---

## 5. Non-goals

Deliberately not built. Each entry states what it is, why not now, and what
would trigger it.

- **Authentication and authorization** — not requested. The user is chosen
  explicitly, as the assignment describes. Adding tokens would introduce a layer
  that changes nothing about scoring, ranking or the dashboard. Needed the
  moment users can see or alter anything that is not their own.
- **Editing and deleting activities** — the assignment describes ingest only.
  Mutable history would require deciding how stored points are recomputed and
  how the rank trend treats retroactive change; that is a larger design than the
  feature justifies here.
- **Idempotency keys on ingest** — a real wearable sync delivers duplicates and
  would need them. Nothing here syncs from a device, so there is no duplicate
  source to defend against yet.
- **Leaderboard paging** — the seed has tens of users, not thousands. Needed
  once the ranking outgrows a single screen.
- **A configurable scoring rule table in the database** — rates do not change at
  runtime and no tenant needs different ones. It stays a single table in code.
  If rates ever had to vary, that table becomes a database row: a change in one
  file, because nothing else reads the rates directly.
- **Snapshot tables and background jobs for rank history** — see the trend
  section. Recomputation is correct and cheap at this size.
- **Per-user timezones** — would change the registration contract the assignment
  specified. See section 2.
- **CI** — the README's local setup is the deliverable, and a pipeline changes
  nothing about the code under review.

Docker was originally listed here and has been moved into scope. The project needs
.NET 10 and Node 22; if a reviewer has neither, `dotnet run` fails before any of
this matters. `docker compose up` removes that risk. It is packaging rather than
behaviour, so it is built last, against the finished application.
- **MediatR, AutoMapper, a repository layer over EF Core** — each adds
  indirection without removing any. `DbContext` already is the repository.
- **FluentAssertions** — version 8 moved to a paid licence for commercial use.
  Not a risk worth taking in a repository handed to a company. xUnit's own
  assertions are enough.

---

## 6. Acceptance

This gate is met when:

- every endpoint above has a defined status code for success and for each way it
  can fail;
- the validation matrix covers all six activity types against all three metric
  fields;
- the timezone decision is written down with its consequences, not left to be
  discovered during implementation.
