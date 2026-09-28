import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import { authService, LoginCredentials } from "@/services/authService";
import { setSessionExpiredHandler } from "@/services/api";
import { readTokens } from "@/services/tokenStorage";
import { env } from "@/config/env";

interface AuthContextType {
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (credentials: LoginCredentials) => Promise<boolean>;
  logout: () => Promise<void>;
  clearError: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Seeded from storage so a refresh does not log the user out. The tokens themselves
  // live in tokenStorage and are read by the API layer; this only tracks whether a
  // session exists, so components never handle raw tokens.
  // In demo mode there is no API to authenticate against, so the visitor is treated as
  // signed in -- otherwise the route guard would trap them on a login form that can
  // never succeed. This is driven by runtime config, so it cannot affect a real
  // deployment.
  const [isAuthenticated, setIsAuthenticated] = useState(
    () => env.demoMode || readTokens() !== null,
  );
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const login = useCallback(async (credentials: LoginCredentials): Promise<boolean> => {
    setIsLoading(true);
    setError(null);

    try {
      if (!env.demoMode) {
        await authService.login(credentials);
      }

      setIsAuthenticated(true);
      return true;
    } catch (err) {
      setError(err instanceof Error ? err.message : "Login failed");
      return false;
    } finally {
      setIsLoading(false);
    }
  }, []);

  const logout = useCallback(async () => {
    if (!env.demoMode) {
      await authService.logout();
    }

    setIsAuthenticated(false);
    setError(null);
  }, []);

  const clearError = useCallback(() => setError(null), []);

  // The API layer refreshes expired access tokens on its own; it only calls back here
  // when the session cannot be saved, so this fires once, at the real end of a session.
  useEffect(() => {
    setSessionExpiredHandler(() => setIsAuthenticated(false));
    return () => setSessionExpiredHandler(null);
  }, []);

  const value = useMemo(
    () => ({ isAuthenticated, isLoading, error, login, logout, clearError }),
    [isAuthenticated, isLoading, error, login, logout, clearError],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);

  if (context === undefined) {
    throw new Error("useAuth must be used within an AuthProvider");
  }

  return context;
}
