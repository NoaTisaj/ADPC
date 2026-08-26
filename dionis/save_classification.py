from pymongo import MongoClient

client = MongoClient("mongodb://localhost:27017")
db = client["dionis"]
collection = db["classifications"]

document = {
    "filename": "test.mp3",
    "minio_key": "test.mp3",
    "location": {"lat": 45.81, "lng": 15.98},
    "results": [
        {"common_name": "Undulated Antpitta", "confidence": 0.55},
        {"common_name": "Northern Hawk Owl", "confidence": 0.41},
    ],
}

inserted = collection.insert_one(document)
print("Inserted document id:", inserted.inserted_id)

print("Reading it back:")
found = collection.find_one({"filename": "test.mp3"})
print(found)