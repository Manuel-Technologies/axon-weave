# axon-weave SDKs

This folder contains first-party SDKs for the axon-weave API.

- `typescript`: typed TypeScript package with REST and SignalR helpers
- `javascript`: plain ESM JavaScript package with REST and SignalR helpers
- `python`: REST-first Python package
- `csharp`: .NET 8 package with REST and SignalR helpers

All SDKs expect an API base URL such as:

```text
https://axon-weave-api.onrender.com
```

Protected calls require a JWT returned from `verifyOtp` / `verify_otp` / `VerifyOtpAsync`.
