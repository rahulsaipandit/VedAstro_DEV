# WebScraper

A standalone Python utility project (not part of the .NET solution/build — it's a
separate Visual Studio Python project, [`WebScraper.pyproj`](WebScraper.pyproj)) used to
harvest real birth-data profiles from a third-party astrology site and feed them into
VedAstro as `Person` records, for use as sample/demo/test data (famous people with known,
verified ("Rodden AA" rated) birth times).

## Files

- [`WebScraper.py`](WebScraper.py) — entry point. Points at
  `https://famouspeople.astro-seek.com/`, constructs an `AstroSeekWebScraper`, and kicks
  off the scrape via `load_all_fam_ppl_roden_aa()`.
- [`AstroSeekWebScraper.py`](AstroSeekWebScraper.py) — all the scraping logic, built on
  `requests` + `BeautifulSoup` (with `pandas` and `concurrent.futures` also used).

## What it does

Astro-Seek's "famous people" search lists ~16,000 profiles (rodden AA = highest source
reliability for birth time/date/place) across paginated search-result pages.

1. `load_all_fam_ppl_roden_aa()` walks the paginated listing backwards from page-offset
   15950 down to 8000 in steps of 50, using a `ThreadPoolExecutor` (10 workers) to
   parallelize.
2. For each page offset, `_get_hrefs_for_famous_people_roden_AA` fetches the search
   results page and `_get_hrefs_from_soup` pulls out each profile's link.
3. For each profile link, `get_astro_chart_data_for_famous_person` fetches the profile
   page and scrapes: name, gender, occupation, birth place, country, and birth
   date/time — converting Astro-Seek's date format (`15 March 1475 - 01:45`) into
   VedAstro's expected format (`01:45/15/03/1475`) via `convert_date_format`.
4. Each scraped profile is then POSTed into a running local VedAstro API instance via
   `add_new_person_to_vedastro`, which calls the `Calculate/AddPerson` endpoint at
   `http://localhost:7071` with `FailIfDuplicate=True` — i.e. this script expects the API
   (see the main [CLAUDE.md](../CLAUDE.md) run instructions) to already be running
   locally on port 7071 while it scrapes.

There's also an older/alternate, currently-unused code path,
`load_raw_celebrity_astro_seek_data` (plus its helpers
`_get_hrefs_for_occupation_types` / `_get_hrefs_for_famous_people_by_occupation_type`),
that instead walks the site by occupation category and builds a `pandas.DataFrame` of
results rather than pushing straight into the API. It isn't called from
`WebScraper.py`'s `__main__` and appears to be a leftover from an earlier scraping
approach.

## Current status: broken (Cloudflare-blocked)

As of 2026-09-12, this scraper no longer works. `astro-seek.com` now sits behind
Cloudflare bot-protection, and every request (even a plain single-page fetch via
`requests`) gets back an HTTP 403 "Attention Required! | Cloudflare" challenge page
instead of real content — so `_get_hrefs_for_famous_people_roden_AA` finds 0 profile
links and nothing can be scraped. This was confirmed by running the script (in a local
`.venv` with `requests`, `beautifulsoup4`, `pandas` installed) against the URL it builds
for the last page (`.../calculate-advanced-astrology-search_15950)/?rodden=1`).

Getting past this would require browser-based scraping (e.g. a headless browser with a
real fingerprint/JS challenge solving) rather than plain `requests` — that's a
meaningfully different and riskier approach (anti-bot-detection evasion) and hasn't been
attempted here.

## Notes

- Errors during a single profile scrape are swallowed and replaced with a placeholder
  profile (`person_name: "Empty"`, `location_name: "Singapore"`, etc.) rather than
  aborting the whole run — meant for a long unattended batch job, not for
  correctness-critical use.
- `add_new_person_to_vedastro` hard-passes `owner_id="xxxxxx"` — this is a placeholder
  and would need to be a real user/account ID before importing at scale.
- This is a one-off data-import tool, not part of the deployed product; safe to run
  independently against a local API instance to seed sample person data.
