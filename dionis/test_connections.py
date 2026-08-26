from pymongo import MongoClient
from minio import Minio

print("Testing MongoDB...")
mongo = MongoClient("mongodb://localhost:27017/")
mongo.admin.command("ping")
print("  MongoDB is alive:", mongo.server_info()["version"])

print("Testing MinIO...")
minio = Minio(
    "localhost:9000",
    access_key="dionis",
    secret_key="dionis_dev_pass",
    secure=False,
)
buckets = minio.list_buckets()
print("  MinIO is alive. Buckets so far:", [b.name for b in buckets])

print("Done — both storages reachable.")