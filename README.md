# axon-weave

`axon-weave` is a .NET 8 chat backend built with Clean Architecture, ASP.NET Core REST APIs, SignalR, PostgreSQL, Redis presence tracking, JWT authentication, and a background worker for retrying failed deliveries.

## Architecture

- `src/AxonWeave.Domain`: entities and enums
- `src/AxonWeave.Application`: DTOs, interfaces, options, shared contracts
- `src/AxonWeave.Infrastructure`: EF Core, repositories, unit of work, JWT, OTP, Redis, file storage
- `src/AxonWeave.API`: controllers, SignalR hub, worker, app bootstrap

## Run With Docker

```bash
docker compose up --build
```

API base URL: `http://localhost:8080`

Swagger UI: `http://localhost:8080/swagger`

## Authentication Flow

The development profile uses a static OTP code of `123456`. `POST /api/auth/register` also returns `developmentOtp` while `Otp:UseStaticOtp` is `true`.

## REST API Examples

Set the base URL:

```bash
export BASE_URL=http://localhost:8080
```

Register a user:

```bash
curl -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+2348012345678",
    "name": "Ada Lovelace"
  }'
```

Verify OTP and get JWT:

```bash
curl -X POST "$BASE_URL/api/auth/verify-otp" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+2348012345678",
    "code": "123456"
  }'
```

Search users by phone:

```bash
curl "$BASE_URL/api/users?phone=%2B234801" \
  -H "Authorization: Bearer YOUR_JWT"
```

Create a direct conversation:

```bash
curl -X POST "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "isGroup": false,
    "participantIds": ["USER_ID_OF_OTHER_PERSON"]
  }'
```

Create a group conversation:

```bash
curl -X POST "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Engineering",
    "isGroup": true,
    "participantIds": [
      "USER_ID_1",
      "USER_ID_2"
    ]
  }'
```

List current user's conversations:

```bash
curl "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer YOUR_JWT"
```

Get messages with pagination:

```bash
curl "$BASE_URL/api/messages?conversationId=CONVERSATION_ID&before=2026-04-08T09:00:00Z&limit=50" \
  -H "Authorization: Bearer YOUR_JWT"
```

Send a message over REST:

```bash
curl -X POST "$BASE_URL/api/messages" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "CONVERSATION_ID",
    "encryptedContent": "BASE64_OR_CIPHERTEXT_HERE",
    "mediaUrl": null,
    "mediaContentType": null
  }'
```

Delete a message for everyone:

```bash
curl -X DELETE "$BASE_URL/api/messages/MESSAGE_ID" \
  -H "Authorization: Bearer YOUR_JWT"
```

Upload media:

```bash
curl -X POST "$BASE_URL/api/media/upload" \
  -H "Authorization: Bearer YOUR_JWT" \
  -F "file=@/path/to/image.png"
```

Mark a message as read:

```bash
curl -X PUT "$BASE_URL/api/messages/MESSAGE_ID/read" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "CONVERSATION_ID"
  }'
```

## SignalR JavaScript Client Example

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js"></script>
<script>
  const token = "YOUR_JWT";
  const connection = new signalR.HubConnectionBuilder()
    .withUrl("http://localhost:8080/hubs/chat", {
      accessTokenFactory: () => token
    })
    .withAutomaticReconnect()
    .build();

  connection.on("OnMessageReceived", message => {
    console.log("message", message);
  });

  connection.on("OnUserOnline", payload => {
    console.log("online", payload.userId);
  });

  connection.on("OnUserOffline", payload => {
    console.log("offline", payload.userId);
  });

  connection.on("OnDeliveryReceipt", payload => {
    console.log("delivery", payload);
  });

  connection.on("OnTyping", payload => {
    console.log("typing", payload);
  });

  async function start() {
    await connection.start();
    await connection.invoke("SendTyping", "CONVERSATION_ID", true);
    await connection.invoke("SendMessage", "CONVERSATION_ID", "ENCRYPTED_PAYLOAD");
    await connection.invoke("MarkDelivered", ["MESSAGE_ID_1", "MESSAGE_ID_2"]);
  }

  start().catch(console.error);
</script>
```

## Notes

- JWT bearer auth is enabled for both HTTP controllers and `/hubs/chat`.
- Redis stores user presence with a TTL so stale connections age out.
- The background worker retries failed deliveries every 15 seconds.
- Message storage is indexed by `(conversation_id, created_at)` through EF Core.
- Uploaded media is stored locally in `/app/uploads` inside the container.
