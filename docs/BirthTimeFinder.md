# Birth Time Rectification — Questionnaire-Driven Design

## Current state

`API/FrontDesk/BirthTimeFinderAPI.cs` and `Console/Program.cs` implement BTR as a "dictionary
attack on time": sweep an hour window of candidate birth times, render each candidate's full
life-span Events Chart, stack them into one image, and let the practitioner manually compare
against known life events. There is no scoring or automated ranking anywhere in this path (see
`docs/vedAstroArchitecture.md`'s "Birth Time Rectification (BirthTimeFinderAPI)" section for the
full implementation walkthrough).

The mockup below this section is the target UX for a questionnaire-driven alternative: instead of
(or in addition to) visually comparing chart stacks, the practitioner answers a life-event
questionnaire and the system returns candidate birth times ranked by confidence.

## Relation to [btr-literature](https://github.com/ashoksainiengineer/btr-literature)

btr-literature is a knowledge base of BTR rules (event-specific house/planet rules for marriage,
career, death, health, etc.), methodologies (KP Cuspal Sub-Lord, Ruling Planets, Pranapada Lagna,
Pancha Tatwa), and classical text references (BPHS, Jataka Parijata) — it has no calculation or
charting code of its own. It is the natural source for the rule logic behind each questionnaire
item below: each yes/no life-event question needs to map to a testable astrological
condition (house lord placement, planetary affliction, dasha timing, etc.) that can be evaluated
against a candidate chart, and btr-literature is where those rules would be sourced from rather
than invented ad hoc.

## Implemented design (backend + frontend built)

All 95 questions in the mockup below are mapped and scored — not a curated subset. Rather than one
bespoke condition method per question (494 already exist in `CalculateHoroscope.cs` for other
chart features, but they're keyed to specific classical yogas, not this questionnaire), BTR reuses
a small set of **generic, parameterized evaluators** built on existing `Calculate.*` primitives
(`Library/Logic/Calculate/Core.cs`/`CoreRelationships.cs`), and each question just picks a
House/Planet parameter:

- `Library/Logic/Calculate/BirthTimeRectification/BtrAnswer.cs` — the 5-point answer scale
  (`StronglyNo`..`StronglyYes`) and its weight (Skip=0, plain=1, Strongly=2).
- `BtrCondition.cs` — `BtrEvaluator` (`HouseLordStrong`/`HouseLordAfflicted`/`PlanetWellPlaced`/
  `PlanetAfflicted`) evaluated via `Calculate.LordOfHouse`, `Calculate.HousePlanetOccupiesBasedOnSign`,
  `Calculate.IsPlanetExaltedSign`, `Calculate.IsPlanetConjunctWithMaleficPlanets`/
  `IsPlanetAspectedByMaleficPlanets` — no new astronomical calculation, only composition.
- `BtrQuestionBank.cs` — the 95-question table, each entry a `BtrQuestion(Id, Text, Category,
  Tier, Rules)`. `Tier` is `Classical` when the question maps to a house whose classical
  significations directly cover the life event (marriage→7th, father→9th/Sun, children→5th, etc.,
  informed by [btr-literature](https://github.com/ashoksainiengineer/btr-literature)'s event rule
  categories), or `Heuristic` when no specific event rule exists and a general planetary karaka
  association is used instead (e.g. "enjoy reading/writing poetry?" → Mercury well placed).
- `BtrScoringEngine.cs` — `ScoreCandidate` sums agreement between each answered rule and the
  answer's direction, weighted by the answer's strength; `RankCandidates` min-max normalizes raw
  scores across the candidate set into a 0–100% confidence and a High/Medium/Low tercile bucket
  (the mockup's percentage + bucket labels below).
- `BirthTimeCandidateSweep.cs` — the candidate-time-sweep logic shared between this and the
  existing SVG endpoint (extracted out of `BirthTimeFinderAPI.cs` so neither duplicates it).
- `API/FrontDesk/BirthTimeScoringAPI.cs` — `GET /api/FindBirthTime/Questions` (serves the question
  bank so the frontend doesn't hardcode it) and `POST /api/FindBirthTime/Score/PersonId/{personId}`
  (body: `{answers: [{questionId, answer}], ...same sweep params as BirthTimeFinderAPI}`, returns
  ranked `[{Time, RawScore, ConfidencePercent, Bucket}]`).
- `WebsiteNative/src/app/BirthTimeQuestionnaire.tsx` — the guided-questionnaire screen (paginated
  by category, 5-chip answer selector), linked from `BirthTimeFinder.tsx`. Results render as a
  **sorted list** with a confidence % + bucket badge rather than the mockup's literal radial
  "hover wheel" — same information, no new visualization primitive built yet (possible follow-up
  polish). Tapping a candidate reuses the existing `addPerson` "save as profile" flow.

Tests: `LibraryTests/Logic/Calculate/BirthTimeRectification/BtrScoringEngineTests.cs` (blanket
check that every question-bank rule evaluates without throwing, plus scoring/ranking behavior) and
`API/API.IntegrationTests/BirthTimeScoringEndpointsTests.cs` (both endpoints end-to-end).

**Known limitation carried over from the mockup itself:** questions stay plain Yes/No — no event
dates are collected, so age/date-specific classical rules (e.g. "father died at age 7") are
evaluated as "does this trait appear anywhere in the candidate's life chart" rather than being
timed against dasha periods. Correlating answers with `VimshottariDasa.cs` timing for date-bearing
questions is a natural next step, not yet implemented.

## Questionnaire mockup (UX reference)

date - 
Bhopal, Madhya Pradesh, India

Have you been involved in court proceedings regarding your marriage?
Have you ever had a son?
Do you follow a regular exercise routine?
Do you often find good opportunities by chance?
Does your horoscope estimate your lifespan to be between 32 and 75 years?
Do you often feel mentally worried or unhappy?
Do you enjoy reading or writing poetry?
Have you ever had suicidal thoughts?
Were you adopted by a family that is not your birth family?
Do you feel content with your wife as your partner?
Have you ever betrayed a teacher's trust?
Do you sleep comfortably no matter where you are?
Did you feel happier after your 30th birthday?
Has your brother provided you with financial support?
Do you have two or more diagnosed health conditions?
Do you often argue with your mother?
Has your married partner died?
Do you struggle to afford basic necessities?
Do you engage in sexual behavior that others describe as animalistic?
Have you caused your mother's death?
Do you struggle to afford basic living expenses?
Have you ever been injured in an accident?
Do you feel confident sharing your thoughts?
Do you have two or fewer brothers?
Do you own a house that you consider attractive?
Does your relationship appear stable on the outside?
Does your wife treat you with kindness?
Do  you feel at peace with the idea of death?
Have you never been married?
Have you ever been charged with a crime?
Have you ever paid a penalty that cost you a substantial amount of money?
Do you often stutter or repeat sounds when you speak?
Do you own substantial assets?
Does your wife lie to people repeatedly?
Do you travel to various religious centres?
Have you been diagnosed with an eye disease by a medical professional?
Is your marriage happy?
Do your real estate transactions usually go smoothly?
Have your relatives ever caused problems for you?
Do you own several real estate properties such as homes or land?
Do you follow a balanced diet?
Do you earn a high income?
Do you prefer furniture with artistic designs?
Have you ever lost valuable belongings?
Do you believe you will become a celestial being after death?
Have you ever experienced the death of one of your children?
Have you faced unexpected expenses because of a child's needs?
Is it correct that your father was not present at the time of your birth?
Do you feel satisfied with your physical health?
Do you have a mental health condition?
Do you live on a low income?
Have you ever been widowed?
Do you enjoy engaging in debates?
Do you own more than one house?
Have you ever done something that made your father happy?
Do you have a heavy-set physique?
Do you often feel unhappy in your marriage?
Do you have at least three brothers?
Do you often struggle to focus on tasks?
Do you find it hard to predict when your brothers will prosper financially?
Have you ever been arrested by law enforcement?
Do you often find your thoughts consumed by worries?
Do you struggle to afford your basic living expenses?
Do you have difficulty seeing things clearly?
Have your children ever given you money or other valuable gifts?
Do you spend your income on many different expenses leaving little savings?
Do you feel that luck has been on your side?
Have you ever been subjected to torture?
Do you consider yourself financially well-off?
Do you regularly donate money or goods to help others?
Do you suffer from any serious health problems?
Do you have health issues?
Do you have difficulty seeing objects in low light?
Do you follow your father's instructions?
Do you hold a valid passport?
Do you lack comfortable bedding due to financial constraints?
Have you experienced more than one episode of high fever during your early life?
Have you ever had a severe fever during your childhood?
Do you feel completely free from all desires and suffering?
Has your husband or wife died?
Have you ever lost money because of your spouse?
Do you read materials on metaphysical lore?
Do you earn your income through brokerage?
Have you been diagnosed with a life-limiting medical condition?
Does your country’s law allow you to have more than one wife?
Do events typically turn out in your favor?
Does your mother express love or affection toward you?
Have you ever married more than one woman?
Do you earn money by selling precious stones?
Has your father passed away?
Do you spend time in forests or mountainous regions?
Do you owe money to any creditor?
Do you often break rules?
Do you have people who openly oppose or dislike you?
Do you have difficulty reading small text?

Options are 
- Strongly Yes
- Yes
- Skip / Maybe
- No
- Strongly No


Your birth time is...
Born on 26 January 1975 in Bhopal,Madhya Pradesh,India, possibly at these times.

--
Hover wheel
High
Medium
Low
Possible Birth Times
6:00 AM
10% · Early Morning
9:00 AM
7% · Morning
10:00 AM
7% · Morning
1:00 PM
7% · Midday
7:00 PM
7% · Evening
1:00 AM
6% · Late Night
3:00 AM
5% · Late Night
5:00 AM
5% · Early Morning
8:00 AM
5% · Morning
5:00 PM
5% · Evening
6:00 PM
5% · Evening
8:00 PM
5% · Night
9:00 PM
5% · Night
Click on a time to save it as a profile for astrology analysis.