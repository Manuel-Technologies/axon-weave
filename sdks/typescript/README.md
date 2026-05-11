# axon-weave TypeScript SDK

```ts
import { AxonWeaveClient } from "@axon-weave/sdk";

const client = new AxonWeaveClient({ baseUrl: "https://your-api.onrender.com" });

await client.register({ phoneNumber: "+2348012345678", name: "Ada" });
const auth = await client.verifyOtp({ phoneNumber: "+2348012345678", code: "123456" });

const conversations = await client.listConversations();
console.log(auth.user, conversations);
```

Real-time:

```ts
const hub = client.createHubConnection();
hub.on("OnMessageReceived", message => console.log(message));
await hub.start();
await hub.invoke("SendTyping", "conversation-id", true);
```
