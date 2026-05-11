# axon-weave JavaScript SDK

```js
import { AxonWeaveClient } from "axon-weave";

const client = new AxonWeaveClient({ baseUrl: "https://your-api.onrender.com" });

await client.register({ phoneNumber: "+2348012345678", name: "Ada" });
await client.verifyOtp({ phoneNumber: "+2348012345678", code: "123456" });

const users = await client.searchUsers("+234");
console.log(users);
```

```js
const hub = client.createHubConnection();
hub.on("OnMessageReceived", message => console.log(message));
await hub.start();
```
