import io
import json
from datetime import datetime, timezone

def save_log(minio_client, bucket, filename, results):
    log = {
        "filename": filename,
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "request": {"url": "https://aves.regoch.net/api/classify", "file": filename},
        "response": results,
    }
    data = json.dumps(log, indent=2).encode("utf-8")
    minio_client.put_object(
        bucket,
        f"logs/{filename}.json",
        io.BytesIO(data),
        length=len(data),
        content_type="application/json",
    )