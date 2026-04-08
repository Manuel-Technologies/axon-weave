# axon-weave

`axon-weave` is an open-source chat backend built with .NET 8, ASP.NET Core, SignalR, PostgreSQL, and Redis-compatible presence tracking.

It is ready to deploy on Render and gives you a real public API URL like:

- `https://your-service-name.onrender.com/api/auth/register`
- `https://your-service-name.onrender.com/hubs/chat`
- `https://your-service-name.onrender.com/swagger`

## What this project includes

- REST API for auth, users, conversations, messages, media, and read receipts
- SignalR hub for real-time messaging, typing indicators, online presence, and delivery receipts
- PostgreSQL with EF Core migrations
- Redis-compatible presence tracking
- Background worker for retrying failed message deliveries
- Dockerized deployment
- Render Blueprint file: `render.yaml`

## Open-source-friendly Render setup

This repository is configured so it can be deployed as an open API service on Render with:

- public HTTPS URL
- Swagger docs enabled in production
- health endpoints for uptime checks
- automatic database migration retry on startup
- configurable CORS for public clients
- Render-managed PostgreSQL and Key Value services
- free-tier-friendly deployment files

## Important note about Render free plan

This repository is now configured for Render free instances.

That makes it easier to launch, but Render's official docs say free services have important limitations:

- free web services spin down after 15 minutes without traffic
- wake-up can take around a minute
- local uploaded files are lost on restart, redeploy, or spin-down
- free services are not recommended for production apps

That means `axon-weave` can be deployed for free, but it is best for demos, testing, open-source previews, and early development, not for a serious always-on public chat product.

## Fastest way to deploy on Render

### Option 1: Free Blueprint deploy from this repo

1. Go to Render.
2. Click `New`.
3. Click `Blueprint`.
4. Connect your GitHub account if Render asks.
5. Select this repository: `Manuel-Technologies/axon-weave`
6. Render will detect `render.yaml`
7. Review the services Render is about to create:
   - `axon-weave-api`
   - `axon-weave-db`
   - `axon-weave-redis`
8. Click `Apply`
9. Wait for deployment to finish

When it is done, Render gives you a public URL such as:

```text
https://axon-weave-api.onrender.com
```

Your live API URLs will then be:

- `https://axon-weave-api.onrender.com/swagger`
- `https://axon-weave-api.onrender.com/health`
- `https://axon-weave-api.onrender.com/api/auth/register`
- `https://axon-weave-api.onrender.com/hubs/chat`

## Render resources created by this repo

The `render.yaml` file creates:

- one Docker web service for the API
- one Render PostgreSQL database
- one Render Key Value instance

Important:

- On Render free tier, uploads stored in `/app/uploads` are temporary
- if the service restarts or spins down, those files disappear
- for long-term file storage, you should later move uploads to Cloudinary, Amazon S3, Cloudflare R2, or another object storage service

## Health and docs endpoints

After deployment, these URLs should work:

- `/` basic service metadata
- `/swagger` API docs and testing UI
- `/health` simple liveness endpoint
- `/health/ready` readiness check for database + Redis-compatible cache

Example:

```bash
curl https://your-service-name.onrender.com/health
```

## First API test after deploy

Register a user:

```bash
curl -X POST "https://your-service-name.onrender.com/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+2348012345678",
    "name": "Ada Lovelace"
  }'
```

## Authentication flow

### Development

Local development uses a static OTP code:

```text
123456
```

### Production on Render

Production is configured to disable static OTP by default:

- `Otp__UseStaticOtp=false`

That means you should wire in a real SMS/OTP provider before inviting public users.

For demo/testing on Render, you can temporarily set:

```text
Otp__UseStaticOtp=true
Otp__StaticOtpCode=123456
```

inside the Render dashboard for the API service.

## REST API examples

Set a base URL:

```bash
export BASE_URL=https://your-service-name.onrender.com
```

Register:

```bash
curl -X POST "$BASE_URL/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "phoneNumber": "+2348012345678",
    "name": "Ada Lovelace"
  }'
```

Verify OTP:

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

List conversations:

```bash
curl "$BASE_URL/api/conversations" \
  -H "Authorization: Bearer YOUR_JWT"
```

Get messages:

```bash
curl "$BASE_URL/api/messages?conversationId=CONVERSATION_ID&before=2026-04-08T09:00:00Z&limit=50" \
  -H "Authorization: Bearer YOUR_JWT"
```

Send message over REST:

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

Delete message for everyone:

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

Mark as read:

```bash
curl -X PUT "$BASE_URL/api/messages/MESSAGE_ID/read" \
  -H "Authorization: Bearer YOUR_JWT" \
  -H "Content-Type: application/json" \
  -d '{
    "conversationId": "CONVERSATION_ID"
  }'
```

## JavaScript SignalR client example

```html
<script src="https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.7/signalr.min.js"></script>
<script>
  const token = "YOUR_JWT";
  const baseUrl = "https://your-service-name.onrender.com";

  const connection = new signalR.HubConnectionBuilder()
    .withUrl(`${baseUrl}/hubs/chat`, {
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

## Local development

```bash
docker compose up --build
```

Swagger:

```text
http://localhost:8080/swagger
```

## What you still need before public launch

Even though deployment is ready, these are the last business-level items to decide:

- connect a real SMS provider for OTPs
- set your final frontend domain if you want restricted CORS
- move uploads to Cloudinary, S3, or Cloudflare R2 if you want files to survive redeploys
- choose your moderation, abuse protection, and user support policies

## Files added for deployment

- `render.yaml` Render Blueprint for free Render services
- `.dockerignore` faster cleaner Docker builds
- updated `Program.cs` for Render port binding, health checks, migration retries, and open API behavior
- updated production configuration for safer defaults

## Official Render docs used

- Blueprint YAML reference: https://render.com/docs/blueprint-spec
- Render Blueprints: https://render.com/docs/infrastructure-as-code
- Render environment variables: https://render.com/docs/environment-variables
- Render Key Value: https://render.com/docs/key-value
- Render Postgres: https://render.com/docs/postgresql
