import json
from kafka import KafkaConsumer
from pymongo import MongoClient

collection = MongoClient("mongodb://localhost:27017")["dionis"]["observations"]

consumer = KafkaConsumer(
    "observations",
    bootstrap_servers="localhost:9092",
    auto_offset_reset="earliest",
    consumer_timeout_ms=5000,
    value_deserializer=lambda v: json.loads(v.decode("utf-8")),
)

count = 0
for message in consumer:
    obs = message.value
    collection.insert_one(obs)
    count += 1
    print(f"Stored: {obs}")

consumer.close()
print(f"\nDone — stored {count} observations in Mongo")