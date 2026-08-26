import requests

CLASSIFY_URL = "https://aves.regoch.net/api/classify"

def classify(path):
    with open(path, "rb") as f:
        files = {"file": (path, f, "audio/mpeg")}
        response = requests.post(CLASSIFY_URL, files=files)
    response.raise_for_status()
    data = response.json()
    return data["results"]


if __name__ == "__main__":
    results = classify("audio/test.mp3")
    print(f"Got {len(results)} detections")
    for r in results[:3]:
        print(f"  {r['common_name']} ({r['confidence']:.2f})")