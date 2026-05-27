import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { Link, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export default function LoginPage() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const destination = location.state?.from || "/wizard";

  async function handleSubmit(event) {
    event.preventDefault();
    setBusy(true);
    setError("");

    try {
      await login({ email, password });
      navigate(destination, { replace: true });
    } catch (requestError) {
      setError(requestError.response?.data?.message || "We couldn't sign you in. Please try again.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto flex max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto w-full max-w-md section-shell">
        <p className="text-sm uppercase tracking-[0.28em] text-mist/50">Welcome back</p>
        <h1 className="mt-4 font-display text-4xl text-mist">Sign in to continue</h1>
        <p className="mt-3 text-mist/70">Pick up where you left off and continue shaping your portfolio.</p>

        <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
          <div>
            <label className="field-label">Email address</label>
            <input className="field-input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
          </div>
          <div>
            <label className="field-label">Password</label>
            <div className="relative">
              <input
                className="field-input pr-12"
                type={showPassword ? "text" : "password"}
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                required
              />
              <button
                type="button"
                onClick={() => setShowPassword((value) => !value)}
                className="absolute inset-y-0 right-0 flex items-center px-4 text-white/45 transition hover:text-white/75"
                aria-label={showPassword ? "Hide password" : "Show password"}
              >
                {showPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>
          {error ? <p className="rounded-2xl border border-coral/30 bg-coral/10 px-4 py-3 text-sm text-rose-100">{error}</p> : null}
          <button className="primary-button w-full" type="submit" disabled={busy}>
            {busy ? "Signing in..." : "Sign in"}
          </button>
        </form>

        <p className="mt-6 text-sm text-mist/60">
          New here?{" "}
          <Link to="/register" className="text-glow">
            Create an account
          </Link>
        </p>
      </div>
    </div>
  );
}
