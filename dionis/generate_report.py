import sys
import pandas as pd
from pymongo import MongoClient
from rapidfuzz import fuzz

CONFIDENCE_THRESHOLD = 0.5

filter_term = sys.argv[1] if len(sys.argv) > 1 else None

collection = MongoClient("mongodb://localhost:27017")["dionis"]["classifications"]

rows = []
for doc in collection.find():
    for r in doc["results"]:
        rows.append({
            "common_name": r["common_name"],
            "scientific_name": r.get("scientific_name"),
            "confidence": r["confidence"],
            "filename": doc["filename"],
            "lat": doc["location"]["lat"],
            "lng": doc["location"]["lng"],
        })

df = pd.DataFrame(rows)
print(f"Total detections (all): {len(df)}")

df = df[df["confidence"] >= CONFIDENCE_THRESHOLD]
print(f"Detections above {CONFIDENCE_THRESHOLD}: {len(df)}")

summary = (
    df.groupby(["common_name", "scientific_name"])
      .agg(sightings=("common_name", "count"),
           avg_confidence=("confidence", "mean"))
      .reset_index()
      .sort_values("sightings", ascending=False)
)

summary["avg_confidence"] = summary["avg_confidence"].round(3)

print("Summary:")
print(summary)

if filter_term:
    def matches(name):
        return fuzz.partial_ratio(filter_term.lower(), name.lower()) >= 70
    summary = summary[summary["common_name"].apply(matches)]
    print(f"\nFiltered to names matching '{filter_term}':")
    print(summary)

summary.to_csv("report.csv", index=False)
print("\nWrote report.csv")