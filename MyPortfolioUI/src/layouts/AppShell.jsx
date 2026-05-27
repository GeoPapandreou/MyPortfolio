import { Link } from "react-router-dom";
import { ChevronDown, LogOut, Settings, Sparkles } from "lucide-react";
import { useAuth } from "../hooks/useAuth";
import { getInitials } from "../utils/formatters";

export default function AppShell({ children }) {
  const { isAuthenticated, user, logout } = useAuth();

  return (
    <div className="min-h-screen">
      <header className="mx-auto flex w-full max-w-7xl items-center justify-between px-4 py-5 sm:px-6 lg:px-8">
        <Link to="/" className="flex items-center gap-3 text-white">
          <span className="grid h-10 w-10 place-items-center rounded-2xl border border-white/10 bg-white/[0.04] text-violet-400">
            <Sparkles size={18} />
          </span>
          <div>
            <p className="text-xl font-semibold leading-none">MyPortfolio</p>
            <p className="text-xs text-white/45">Portfolio builder</p>
          </div>
        </Link>

        <nav className="hidden items-center gap-8 text-sm text-white/60 md:flex">
          <Link to="/features">Features</Link>
          <Link to="/how-it-works">How it works</Link>
          <Link to="/examples">Examples</Link>
        </nav>

        <div className="flex items-center gap-3">
          {isAuthenticated ? (
            <>
              <div className="group relative">
                <button
                  type="button"
                  className="flex items-center gap-2 rounded-full border border-white/10 bg-white/[0.04] px-2 py-2 text-white transition hover:bg-white/[0.07]"
                  aria-label="Open account menu"
                >
                  <span className="grid h-9 w-9 place-items-center rounded-full bg-gradient-to-r from-violet-500 to-fuchsia-500 text-xs font-semibold text-white">
                    {getInitials(user?.displayName || user?.email)}
                  </span>
                  <ChevronDown size={16} className="hidden text-white/60 md:block" />
                </button>

                <div className="pointer-events-none invisible absolute right-0 top-full z-20 mt-3 min-w-[220px] translate-y-2 rounded-[28px] border border-white/10 bg-slate-950/95 p-3 opacity-0 shadow-[0_28px_70px_rgba(15,23,42,0.45)] backdrop-blur-xl transition duration-200 group-hover:pointer-events-auto group-hover:visible group-hover:translate-y-0 group-hover:opacity-100 group-focus-within:pointer-events-auto group-focus-within:visible group-focus-within:translate-y-0 group-focus-within:opacity-100">
                  <Link
                    to="/account"
                    className="flex items-center gap-3 rounded-2xl px-4 py-3 text-sm font-medium text-white/82 transition hover:bg-white/[0.05]"
                  >
                    <Settings size={16} />
                    Profile settings
                  </Link>
                  <button
                    type="button"
                    className="mt-1 flex w-full items-center gap-3 rounded-2xl px-4 py-3 text-left text-sm font-medium text-white/82 transition hover:bg-white/[0.05]"
                    onClick={logout}
                  >
                    <LogOut size={16} />
                    Sign out
                  </button>
                </div>
              </div>
            </>
          ) : (
            <>
              <Link to="/login" className="rounded-2xl border border-white/10 bg-white/[0.02] px-5 py-3 text-sm font-medium text-white/80 transition hover:bg-white/[0.05]">
                Sign in
              </Link>
              <Link to="/register" className="primary-button">
                Sign up
              </Link>
            </>
          )}
        </div>
      </header>

      <main>{children}</main>
    </div>
  );
}
