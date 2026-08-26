import requests
from pymongo import MongoClient

AVES_URL = "https://aves.regoch.net/aves.json"

mongo = MongoClient("mongodb://localhost:27017/")
db = mongo["dionis"]
species_collection = db["species"]

species_collection.create_index("key", unique=True)

existing_count = species_collection.count_documents({})
if existing_count > 0:
    print(f"Species already in DB ({existing_count} documents). Skipping scrape.")
else:
    print("Downloading species data...")
    response = requests.get(AVES_URL)
    response.raise_for_status()
    all_species = response.json()
    print(f"Downloaded {len(all_species)} entries.")

    inserted = 0
    skipped = 0
    for bird in all_species:
        document = {
            "key": bird.get("key"),
            "speciesKey": bird.get("speciesKey"),
            "scientificName": bird.get("scientificName"),
            "canonicalName": bird.get("canonicalName"),
            "rank": bird.get("rank"),
            "taxonomicStatus": bird.get("taxonomicStatus"),
            "genus": bird.get("genus"),
            "family": bird.get("family"),
            "order": bird.get("order"),
            "class": bird.get("class"),
        }

        try:
            species_collection.insert_one(document)
            inserted += 1
        except Exception:
            skipped += 1

    print(f"Done. Inserted {inserted}, skipped {skipped} duplicates.")

print(f"Total species in DB now: {species_collection.count_documents({})}")