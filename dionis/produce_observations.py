import json
from kafka import KafkaProducer

producer = KafkaProducer(
    bootstrap_servers="localhost:9092",
    value_serializer=lambda v: json.dumps(v).encode("utf-8"),
)

observations = [
    {"species_key": 2473325, "lat": 45.81, "lng": 15.98, "body_size_cm": 25, "migration_status": "resident"},
    {"species_key": 2473421, "lat": 46.31, "lng": 16.34, "body_temp_c": 41.2},
    {"species_key": 2474156, "lat": 45.55, "lng": 18.69, "wingspan_cm": 58, "flight_pattern": "soaring", "habitat": "wetland"},
    {"species_key": 2473325, "lat": 44.87, "lng": 13.85, "body_size_cm": 24},
]

for obs in observations:
    producer.send("observations", obs)
    print(f"Sent: {obs}")

producer.flush()
producer.close()
print(f"\nDone — sent {len(observations)} observations to topic 'observations'")