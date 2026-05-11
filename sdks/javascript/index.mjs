import * as signalR from "@microsoft/signalr";

export class AxonWeaveError extends Error {
  constructor(message, status, body) {
    super(message);
    this.status = status;
    this.body = body;
  }
}

export class AxonWeaveClient {
  constructor({ baseUrl, token, fetchImpl = fetch }) {
    this.baseUrl = baseUrl.replace(/\/+$/, "");
    this.token = token;
    this.fetchImpl = fetchImpl;
  }

  setToken(token) {
    this.token = token;
  }

  register(request) {
    return this.request("/api/auth/register", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async verifyOtp(request) {
    const auth = await this.request("/api/auth/verify-otp", {
      method: "POST",
      body: JSON.stringify(request)
    });
    this.token = auth.token;
    return auth;
  }

  searchUsers(phone) {
    const query = phone ? `?phone=${encodeURIComponent(phone)}` : "";
    return this.request(`/api/users${query}`);
  }

  createConversation(request) {
    return this.request("/api/conversations", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  listConversations() {
    return this.request("/api/conversations");
  }

  getMessages(conversationId, { before, limit } = {}) {
    const params = new URLSearchParams({ conversationId });
    if (before) params.set("before", before);
    if (limit) params.set("limit", String(limit));
    return this.request(`/api/messages?${params.toString()}`);
  }

  sendMessage(request) {
    return this.request("/api/messages", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async deleteMessage(messageId) {
    await this.request(`/api/messages/${messageId}`, { method: "DELETE" });
  }

  markRead(messageId, conversationId) {
    return this.request(`/api/messages/${messageId}/read`, {
      method: "PUT",
      body: JSON.stringify({ conversationId })
    });
  }

  uploadMedia(file, fileName = "upload") {
    const form = new FormData();
    form.append("file", file, fileName);
    return this.request("/api/media/upload", {
      method: "POST",
      body: form
    });
  }

  createHubConnection() {
    return new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/chat`, {
        accessTokenFactory: () => this.token ?? ""
      })
      .withAutomaticReconnect()
      .build();
  }

  async request(path, init = {}) {
    const headers = new Headers(init.headers);
    if (!(init.body instanceof FormData)) {
      headers.set("Content-Type", "application/json");
    }
    if (this.token) {
      headers.set("Authorization", `Bearer ${this.token}`);
    }

    const response = await this.fetchImpl(`${this.baseUrl}${path}`, {
      ...init,
      headers
    });

    if (!response.ok) {
      const body = await readJsonOrText(response);
      throw new AxonWeaveError(`axon-weave request failed with ${response.status}`, response.status, body);
    }

    if (response.status === 204) {
      return undefined;
    }

    const body = await response.json();
    return body.data;
  }
}

async function readJsonOrText(response) {
  const text = await response.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}
