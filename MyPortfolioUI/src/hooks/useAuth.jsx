import { createContext, useContext, useEffect, useMemo, useState } from "react";
import { loginAccount, registerAccount } from "../api/auth";
import { authExpiredEventName } from "../api/client";

const AuthContext = createContext(null);

function readStoredUser() {
  const raw = localStorage.getItem("myportfolio_user");
  if (!raw) {
    return null;
  }

  try {
    return JSON.parse(raw);
  } catch {
    localStorage.removeItem("myportfolio_user");
    return null;
  }
}

export function AuthProvider({ children }) {
  const [user, setUser] = useState(readStoredUser);
  const [token, setToken] = useState(() => localStorage.getItem("myportfolio_token"));

  useEffect(() => {
    if (token) {
      localStorage.setItem("myportfolio_token", token);
    } else {
      localStorage.removeItem("myportfolio_token");
    }
  }, [token]);

  useEffect(() => {
    if (user) {
      localStorage.setItem("myportfolio_user", JSON.stringify(user));
    } else {
      localStorage.removeItem("myportfolio_user");
    }
  }, [user]);

  useEffect(() => {
    function handleAuthExpired() {
      setToken(null);
      setUser(null);
    }

    window.addEventListener(authExpiredEventName, handleAuthExpired);
    return () => {
      window.removeEventListener(authExpiredEventName, handleAuthExpired);
    };
  }, []);

  const value = useMemo(
    () => ({
      isAuthenticated: Boolean(token),
      token,
      user,
      async register(payload) {
        const response = await registerAccount(payload);
        setToken(response.token);
        setUser({ email: response.email, displayName: response.displayName });
        return response;
      },
      async login(payload) {
        const response = await loginAccount(payload);
        setToken(response.token);
        setUser({ email: response.email, displayName: response.displayName });
        return response;
      },
      updateUser(nextUser) {
        setUser((current) => ({
          ...(current ?? {}),
          ...nextUser
        }));
      },
      logout() {
        setToken(null);
        setUser(null);
      }
    }),
    [token, user]
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used inside AuthProvider.");
  }

  return context;
}
