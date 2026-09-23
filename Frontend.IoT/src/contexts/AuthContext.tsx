import React, { createContext, useContext, useState, useCallback, useMemo, useEffect } from 'react';
import { authService, LoginCredentials } from '@/services/authService';
import { setUnauthorizedHandler } from '@/services/api';

interface AuthContextType {
  token: string | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  login: (credentials: LoginCredentials) => Promise<boolean>;
  logout: () => void;
  clearError: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

const TOKEN_STORAGE_KEY = 'entrio.token';

/**
 * Reads the stored token. Wrapped in try/catch because localStorage throws in
 * private-browsing modes and when site data is blocked; a failure there must
 * degrade to "not logged in", never crash the app on boot.
 */
function readStoredToken(): string | null {
  try {
    return localStorage.getItem(TOKEN_STORAGE_KEY);
  } catch {
    return null;
  }
}

function writeStoredToken(token: string | null): void {
  try {
    if (token === null) {
      localStorage.removeItem(TOKEN_STORAGE_KEY);
    } else {
      localStorage.setItem(TOKEN_STORAGE_KEY, token);
    }
  } catch {
    // Storage unavailable: the token still works for this tab, it just will
    // not survive a refresh. Not worth failing the login over.
  }
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
  // Initialised from storage so a page refresh does not silently log you out.
  // Without this the token was React state only, and every data hook's
  // `if (!token) return` guard turned the whole app into a no-op after reload.
  const [token, setTokenState] = useState<string | null>(readStoredToken);

  const setToken = useCallback((next: string | null) => {
    writeStoredToken(next);
    setTokenState(next);
  }, []);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const login = useCallback(async (credentials: LoginCredentials): Promise<boolean> => {
    setIsLoading(true);
    setError(null);
    
    try {
      const response = await authService.login(credentials);
      setToken(response.token);
      return true;
    } catch (err) {
      const message = err instanceof Error ? err.message : 'Login failed';
      setError(message);
      return false;
    } finally {
      setIsLoading(false);
    }
  }, [setToken]);

  const logout = useCallback(() => {
    setToken(null);
    setError(null);
  }, [setToken]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  // Any 401 from any endpoint clears the stored token, so an expired token
  // cannot strand the user on a page that only ever errors.
  useEffect(() => {
    setUnauthorizedHandler(logout);
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  const value = useMemo(() => ({
    token,
    isAuthenticated: !!token,
    isLoading,
    error,
    login,
    logout,
    clearError,
  }), [token, isLoading, error, login, logout, clearError]);

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}
