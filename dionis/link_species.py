from pymongo import MongoClient

db = MongoClient("mongodb://localhost:27017")["dionis"]
species = db["species"]
classifications = db["classifications"]

linked = 0
unmatched = 0

for doc in classifications.find():
    for detection in doc["results"]:
        name = detection["scientific_name"]
        match = species.find_one({"canonicalName": name})
        if match:
            detection["species_key"] = match["key"]
            linked += 1
        else:
            detection["species_key"] = None
            unmatched += 1

    classifications.update_one(
        {"_id": doc["_id"]},
        {"$set": {"results": doc["results"]}},
    )

print(f"Linked: {linked}, unmatched: {unmatched}")