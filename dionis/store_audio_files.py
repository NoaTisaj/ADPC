from minio import Minio

client = Minio(
    "localhost:9000",           
    access_key="dionis",
    secret_key="dionis_dev_pass",
    secure=False,             
)

bucket = "bird-audio"
if not client.bucket_exists(bucket):
    client.make_bucket(bucket)
    print(f"Created bucket: {bucket}")
else:
    print(f"Bucket already exists: {bucket}")

local_path = "audio/test.mp3"   
object_key = "test.mp3"        
client.fput_object(bucket, object_key, local_path)
print(f"Uploaded {local_path} -> {bucket}/{object_key}")

print("Objects now in bucket:")
for obj in client.list_objects(bucket):
    print("  ", obj.object_name, f"({obj.size} bytes)")