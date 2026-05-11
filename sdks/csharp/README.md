# axon-weave C# SDK

```csharp
using AxonWeave.Sdk;

var client = new AxonWeaveClient("https://your-api.onrender.com");

await client.RegisterAsync(new RegisterRequest("+2348012345678", "Ada"));
var auth = await client.VerifyOtpAsync(new VerifyOtpRequest("+2348012345678", "123456"));

var conversations = await client.ListConversationsAsync();
```

Real-time:

```csharp
var hub = client.CreateHubConnection();
hub.On<MessageDto>("OnMessageReceived", message => Console.WriteLine(message.Id));
await hub.StartAsync();
await hub.InvokeAsync("SendTyping", conversationId, true);
```
