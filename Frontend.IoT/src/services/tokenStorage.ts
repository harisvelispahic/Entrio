/**
 * Persistence for the access/refresh token pair.
 *
 * localStorage, matching the Detailly project. Every accessor is wrapped in try/catch
 * because localStorage throws in private-browsing modes and when site data is blocked;
 * a failure there must degrade to "not logged in", never crash the app on boot.
 */
export interface StoredTokens {
  accessToken: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}

const STORAGE_KEY = "entrio.auth";

export function readTokens(): StoredTokens | null {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return null;

    const parsed = JSON.parse(raw) as StoredTokens;
    return parsed.accessToken && parsed.refreshToken ? parsed : null;
  } catch {
    return null;
  }
}

export function writeTokens(tokens: StoredTokens | null): void {
  try {
    if (tokens === null) {
      localStorage.removeItem(STORAGE_KEY);
    } else {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(tokens));
    }
  } catch {
    // Storage unavailable: tokens still work for this tab, they just will not
    // survive a refresh. Not worth failing a login over.
  }
}

/** True when the access token is expired or within `skewSeconds` of expiring. */
export function isAccessTokenExpiring(tokens: StoredTokens, skewSeconds = 30): boolean {
  const expiresAt = new Date(tokens.accessTokenExpiresAtUtc).getTime();
  if (Number.isNaN(expiresAt)) return true;

  return expiresAt - Date.now() <= skewSeconds * 1000;
}
