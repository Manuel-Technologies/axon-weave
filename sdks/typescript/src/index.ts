import * as signalR from "@microsoft/signalr";

export interface AxonWeaveClientOptions {
  baseUrl: string;
  token?: string;
  fetchImpl?: typeof fetch;
}

export interface ApiResponse<T> {
  data: T;
}

export interface RegisterRequest {
  phoneNumber: string;
  name: string;
}

export interface RegisterResponse {
  phoneNumber: string;
  name: string;
  otpExpiresAt: string;
  developmentOtp?: string | null;
}

export interface VerifyOtpRequest {
  phoneNumber: string;
  code: string;
}

export interface User {
  id: string;
  phoneNumber: string;
  name: string;
}

export interface AuthResponse {
  token: string;
  expiresAt: string;
  user: User;
}

export interface CreateConversationRequest {
  title?: string | null;
  isGroup: boolean;
  participantIds: string[];
}

export interface MessagePreview {
  id: string;
  encryptedContent: string;
  createdAt: string;
  senderId: string;
}

export interface Conversation {
  id: string;
  title: string;
  type: string;
  createdAt: string;
  updatedAt: string;
  participants: User[];
  lastMessage?: MessagePreview | null;
}

export interface SendMessageRequest {
  conversationId: string;
  encryptedContent: string;
  mediaUrl?: string | null;
  mediaContentType?: string | null;
}

export interface MessageDelivery {
  userId: string;
  status: string;
  deliveredAt?: string | null;
  readAt?: string | null;
}

export interface Message {
  id: string;
  conversationId: string;
  senderId: string;
  encryptedContent: string;
  mediaUrl?: string | null;
  mediaContentType?: string | null;
  isDeletedForEveryone: boolean;
  createdAt: string;
  readAt?: string | null;
  deliveries: MessageDelivery[];
}

export interface MediaUploadResponse {
  id: string;
  url: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
}

export class AxonWeaveError extends Error {
  constructor(
    message: string,
    public readonly status: number,
    public readonly body: unknown
  ) {
    super(message);
  }
}

export class AxonWeaveClient {
  private readonly baseUrl: string;
  private readonly fetchImpl: typeof fetch;
  private token?: string;

  constructor(options: AxonWeaveClientOptions) {
    this.baseUrl = options.baseUrl.replace(/\/+$/, "");
    this.token = options.token;
    this.fetchImpl = options.fetchImpl ?? fetch;
  }

  setToken(token: string): void {
    this.token = token;
  }

  async register(request: RegisterRequest): Promise<RegisterResponse> {
    return this.request<RegisterResponse>("/api/auth/register", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async verifyOtp(request: VerifyOtpRequest): Promise<AuthResponse> {
    const auth = await this.request<AuthResponse>("/api/auth/verify-otp", {
      method: "POST",
      body: JSON.stringify(request)
    });
    this.token = auth.token;
    return auth;
  }

  async searchUsers(phone?: string): Promise<User[]> {
    const query = phone ? `?phone=${encodeURIComponent(phone)}` : "";
    return this.request<User[]>(`/api/users${query}`);
  }

  async createConversation(request: CreateConversationRequest): Promise<Conversation> {
    return this.request<Conversation>("/api/conversations", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async listConversations(): Promise<Conversation[]> {
    return this.request<Conversation[]>("/api/conversations");
  }

  async getMessages(conversationId: string, options: { before?: string; limit?: number } = {}): Promise<Message[]> {
    const params = new URLSearchParams({ conversationId });
    if (options.before) params.set("before", options.before);
    if (options.limit) params.set("limit", String(options.limit));
    return this.request<Message[]>(`/api/messages?${params.toString()}`);
  }

  async sendMessage(request: SendMessageRequest): Promise<Message> {
    return this.request<Message>("/api/messages", {
      method: "POST",
      body: JSON.stringify(request)
    });
  }

  async deleteMessage(messageId: string): Promise<void> {
    await this.request<void>(`/api/messages/${messageId}`, { method: "DELETE" }, false);
  }

  async markRead(messageId: string, conversationId: string): Promise<Message> {
    return this.request<Message>(`/api/messages/${messageId}/read`, {
      method: "PUT",
      body: JSON.stringify({ conversationId })
    });
  }

  async uploadMedia(file: Blob, fileName = "upload"): Promise<MediaUploadResponse> {
    const form = new FormData();
    form.append("file", file, fileName);
    return this.request<MediaUploadResponse>("/api/media/upload", {
      method: "POST",
      body: form
    });
  }

  createHubConnection(): signalR.HubConnection {
    return new signalR.HubConnectionBuilder()
      .withUrl(`${this.baseUrl}/hubs/chat`, {
        accessTokenFactory: () => this.token ?? ""
      })
      .withAutomaticReconnect()
      .build();
  }

  private async request<T>(path: string, init: RequestInit = {}, unwrap = true): Promise<T> {
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
      return undefined as T;
    }

    const body = await response.json();
    return unwrap ? (body as ApiResponse<T>).data : (body as T);
  }
}

async function readJsonOrText(response: Response): Promise<unknown> {
  const text = await response.text();
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}
