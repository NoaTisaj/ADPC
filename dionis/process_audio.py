import os
from minio import Minio
from pymongo import MongoClient
from classify_audio import classify
from save_log import save_log

AUDIO_DIR = "audio"
BUCKET = "bird-audio"
LOCATION = {"lat": 45.81, "lng": 15.98}

minio_client = Minio(
    "localhost:9000",
    access_key="dionis",
    secret_key="dionis_dev_pass",
    secure=False,
)

collection = MongoClient("mongodb://localhost:27017")["dionis"]["classifications"]

if not minio_client.bucket_exists(BUCKET):
    minio_client.make_bucket(BUCKET)

for filename in os.listdir(AUDIO_DIR):
    if not filename.lower().endswith((".mp3", ".wav")):
        continue

    if collection.find_one({"filename": filename}):
        print(f"Skipping (already done): {filename}")
        continue

    local_path = os.path.join(AUDIO_DIR, filename)
    print(f"Processing {filename} ...")

    minio_client.fput_object(BUCKET, filename, local_path)
    results = classify(local_path)

    document = {
        "filename": filename,
        "minio_key": filename,
        "location": LOCATION,
        "results": results,
    }
    collection.insert_one(document)
    print(f"  saved {len(results)} detections")

    save_log(minio_client, BUCKET, filename, results)
    print("  logged request to MinIO")

print("Done.")