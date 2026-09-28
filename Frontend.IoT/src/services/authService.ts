import { api } from "./api";
import { StoredTokens, readTokens, writeTokens } from "./tokenStorage";

export interface LoginCredentials {
  email: string;
  password: string;
}

export const authService = {
  async login(credentials: LoginCredentials): Promise<StoredTokens> {
    const tokens = await api.post<StoredTokens>("/auth/login", credentials);
    writeTokens(tokens);
    return tokens;
  },

  /**
   * Revokes the refresh token server-side, then clears local storage.
   * The server call is best-effort: if it fails the local session must still end,
   * otherwise a network blip would leave the user stuck logged in.
   */
  async logout(): Promise<void> {
    const tokens = readTokens();

    if (tokens?.refreshToken) {
      try {
        await api.post("/auth/logout", { refreshToken: tokens.refreshToken });
      } catch {
        // ignored on purpose, see above
      }
    }

    writeTokens(null);
  },
};
