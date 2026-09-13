---
language:
- en
license: mit
task_categories:
  - question-answering
pretty_name: all planet data in vedic astrology 
---

This dataset contains vedic astrology planetary data for large spans of time.

Generated using --> [ML Table Generator](https://github.com/user/repo/blob/branch/other_file.md)

## What this folder is

`HuggingFace/` is a dataset-publishing folder, not a scraper or a live tool. It holds
locally generated CSV/JSON datasets plus two small scripts that sync them to/from the
[Hugging Face Hub](https://huggingface.co) dataset repo `vedastro-org/all-planet-data-london`.

- [`push.py`](push.py) — loads `ml-table.csv` and pushes it to the Hub dataset repo via
  the `datasets` library (`dataset.push_to_hub(...)`).
- [`pull.py`](pull.py) — downloads that Hub dataset back down and saves it to a local
  directory (`load_dataset(...)` / `dataset.save_to_disk(...)`).

## Files

- `ml-table.csv` / `ml-table.xlsx` — the main planetary-position dataset pushed to the
  Hub (this is the file `push.py` uploads); this content is what the top-of-file
  "ML Table Generator" note refers to.
- `100-years-vedic-astro-london-1900-2000.csv` — a large precomputed planetary-data
  table for London spanning 1900-2000.
- `PersonList-15k.csv` — a bulk list of ~15k person profiles (name/time/location rows),
  used as sample/training input.
- `BodyInfoDataset.csv`, `MarriageInfoDataset.csv` (+ `MarriageInfoDatasetBackUp.csv`),
  `MarriageTrainingDataset.csv` — labeled datasets used for training/testing
  body-type and marriage-compatibility prediction models.
- `MedicalTagEmbeddings.csv`, `PresetQuestionEmbeddings.csv` — precomputed text
  embeddings, used to power semantic-similarity lookups (e.g. matching a user's
  question against a preset list) without recomputing embeddings at runtime.
- `GeoLocationCache.csv` — a cache of resolved geolocation lookups, to avoid repeat
  geocoding API calls.
- `NCCModels.csv` — data backing NCC (Nakshatra/Chart-Calculation-related) models.
- `alpaca_bvraman_horoscope_data.json` — horoscope data formatted in the Alpaca
  instruction-tuning JSON format, for LLM fine-tuning.
- `metadata.csv` — a small sample/test dataset (Name/Time/Location rows) used for
  local experimentation, distinct from the full production datasets above.

## How this differs from `WebScraper/`

`WebScraper/` and `HuggingFace/` sit at opposite ends of the data pipeline:

- **[`WebScraper/`](../WebScraper/README.md)** *acquires* data — it scrapes birth-data
  profiles from a third-party site (astro-seek.com) and pushes them live into a running
  VedAstro API (`Calculate/AddPerson`), populating VedAstro's own `Person` records.
- **`HuggingFace/`** *publishes* data — it takes locally generated datasets (e.g.
  `ml-table.csv`, produced by the ML Table Generator) and syncs them out to the public
  Hugging Face Hub for ML/training use, via `push.py`/`pull.py`.

In short: WebScraper feeds real-world data *into* VedAstro; this folder ships VedAstro's
generated data *out* to Hugging Face.