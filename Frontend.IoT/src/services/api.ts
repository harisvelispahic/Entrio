import { API_BASE_URL } from "@/config/api";
import {
  StoredTokens,
  isAccessTokenExpiring,
  readTokens,
  writeTokens,
} from "./tokenStorage";

export class ApiError extends Error {
  constructor(
    public status: number,
    message: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

/**
 * Called when the session is beyond saving — no refresh token, or the refresh itself
 * was rejected. AuthContext registers its logout here so the route guard can send the
 * user back to /login.
 */
let onSessionExpired: (() => void) | null = null;

export function setSessionExpiredHandler(handler: (() => void) | null): void {
  onSessionExpired = handler;
}

/**
 * In-flight refresh, shared by every caller.
 *
 * Without this, a dashboard that fires four requests at once would hit four 401s and
 * start four refreshes. Because refresh tokens ROTATE server-side, the first would
 * succeed and the other three would present an already-revoked token, get a 401, and
 * log the user out. Funnelling every refresh through one promise is what makes
 * rotation and concurrency coexist.
 */
let refreshInFlight: Promise<StoredTokens | null> | null = null;

/**
 * Exchanges the stored refresh token for a new pair. Never throws: a failure clears
 * the session and resolves to null, so callers only deal with "have tokens or not".
 */
async function refreshTokens(): Promise<StoredTokens | null> {
  const current = readTokens();

  if (!current?.refreshToken) {
    return null;
  }

  try {
    const response = await fetch(`${API_BASE_URL}/auth/refresh`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: current.refreshToken }),
    });

    if (!response.ok) {
      writeTokens(null);
      return null;
    }

    const tokens = (await response.json()) as StoredTokens;
    writeTokens(tokens);
    return tokens;
  } catch {
    // Network failure: keep the tokens. The backend may simply be down, and
    // discarding a still-valid refresh token would log the user out needlessly.
    return null;
  }
}

function startOrJoinRefresh(): Promise<StoredTokens | null> {
  refreshInFlight ??= refreshTokens().finally(() => {
    refreshInFlight = null;
  });

  return refreshInFlight;
}

class ApiService {
  /**
   * Issues a request, attaching the access token and transparently refreshing it.
   *
   * Two layers of refresh:
   *  - proactive: if the token is about to expire, refresh before sending, which avoids
   *    a guaranteed round-trip failure;
   *  - reactive: if the server still says 401, refresh once and retry, which covers a
   *    token revoked server-side or a clock skew the proactive check missed.
   */
  async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    let tokens = readTokens();

    if (tokens && isAccessTokenExpiring(tokens)) {
      tokens = (await startOrJoinRefresh()) ?? tokens;
    }

    let response = await this.send(endpoint, options, tokens?.accessToken);

    if (response.status === 401 && tokens?.refreshToken) {
      const refreshed = await startOrJoinRefresh();

      if (refreshed) {
        response = await this.send(endpoint, options, refreshed.accessToken);
      }
    }

    if (response.status === 401) {
      // Refresh is impossible or was itself rejected: the session is genuinely over.
      writeTokens(null);
      onSessionExpired?.();
    }

    if (!response.ok) {
      throw new ApiError(response.status, await this.readErrorMessage(response));
    }

    const text = await response.text();
    return text ? (JSON.parse(text) as T) : ({} as T);
  }

  private send(endpoint: string, options: RequestInit, accessToken?: string) {
    const headers: Record<string, string> = {
      "Content-Type": "application/json",
      ...((options.headers as Record<string, string>) ?? {}),
    };

    if (accessToken) {
      headers.Authorization = `Bearer ${accessToken}`;
    }

    return fetch(`${API_BASE_URL}${endpoint}`, { ...options, headers });
  }

  /** Pulls the message out of the API's standard error shape, with sane fallbacks. */
  private async readErrorMessage(response: Response): Promise<string> {
    try {
      const body = await response.json();
      return body?.message ?? `HTTP error ${response.status}`;
    } catch {
      return `HTTP error ${response.status}`;
    }
  }

  get<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: "GET" });
  }

  post<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: "POST",
      body: data === undefined ? undefined : JSON.stringify(data),
    });
  }

  put<T>(endpoint: string, data?: unknown): Promise<T> {
    return this.request<T>(endpoint, {
      method: "PUT",
      body: data === undefined ? undefined : JSON.stringify(data),
    });
  }

  delete<T>(endpoint: string): Promise<T> {
    return this.request<T>(endpoint, { method: "DELETE" });
  }
}

export const api = new ApiService();
