import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";

export default function RegisterPage() {
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");
  const { register } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(event) {
    event.preventDefault();
    setError("");

    if (!fullName.trim()) {
      setError("Please enter your full name.");
      return;
    }

    if (password !== confirmPassword) {
      setError("Your passwords do not match. Please check them and try again.");
      return;
    }

    setBusy(true);

    try {
      await register({ fullName: fullName.trim(), email, password });
      navigate("/wizard", { replace: true });
    } catch (requestError) {
      setError(requestError.response?.data?.message || "We couldn't create your account. Please try again.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="mx-auto flex max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto w-full max-w-md section-shell">
        <p className="text-sm uppercase tracking-[0.28em] text-mist/50">Create your account</p>
        <h1 className="mt-4 font-display text-4xl text-mist">Start your portfolio</h1>
        <p className="mt-3 text-mist/70">Create an account so your answers stay saved and ready when you come back.</p>

        <form className="mt-8 space-y-5" onSubmit={handleSubmit}>
          <div>
            <label className="field-label">Full name</label>
            <input
              className="field-input"
              type="text"
              value={fullName}
              onChange={(e) => setFullName(e.target.value)}
              required
              maxLength={160}
            />
          </div>
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
                minLength={8}
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
          <div>
            <label className="field-label">Confirm password</label>
            <div className="relative">
              <input
                className="field-input pr-12"
                type={showConfirmPassword ? "text" : "password"}
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                required
                minLength={8}
              />
              <button
                type="button"
                onClick={() => setShowConfirmPassword((value) => !value)}
                className="absolute inset-y-0 right-0 flex items-center px-4 text-white/45 transition hover:text-white/75"
                aria-label={showConfirmPassword ? "Hide password confirmation" : "Show password confirmation"}
              >
                {showConfirmPassword ? <EyeOff size={18} /> : <Eye size={18} />}
              </button>
            </div>
          </div>
          {error ? <p className="rounded-2xl border border-coral/30 bg-coral/10 px-4 py-3 text-sm text-rose-100">{error}</p> : null}
          <button className="primary-button w-full" type="submit" disabled={busy}>
            {busy ? "Creating account..." : "Create account"}
          </button>
        </form>

        <p className="mt-6 text-sm text-mist/60">
          Already have an account?{" "}
          <Link to="/login" className="text-glow">
            Sign in
          </Link>
        </p>
      </div>
    </div>
  );
}
