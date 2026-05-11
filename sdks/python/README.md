# axon-weave Python SDK

```python
from axon_weave import AxonWeaveClient

client = AxonWeaveClient("https://your-api.onrender.com")

client.register("+2348012345678", "Ada")
auth = client.verify_otp("+2348012345678", "123456")

print(auth["user"])
print(client.list_conversations())
```

Python SDK coverage is REST-first. Real-time SignalR support can be added later with a dedicated SignalR client dependency.
