import { Download, LoaderCircle, LogOut, Save, Settings, Trash2 } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { deleteAccount, getAccountSettings, saveAccountSettings } from "../api/account";
import { deletePortfolioVersion, downloadPortfolioVersion } from "../api/portfolio";
import { useAuth } from "../hooks/useAuth";
import { formatDateTime } from "../utils/formatters";

const initialForm = {
  fullName: "",
  profession: "",
  location: "",
  phoneNumber: "",
  email: "",
  versions: []
};

export default function AccountSettingsPage() {
  const [form, setForm] = useState(initialForm);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [downloadingId, setDownloadingId] = useState("");
  const [deletingVersionId, setDeletingVersionId] = useState("");
  const [deleting, setDeleting] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");
  const { logout, updateUser } = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        const data = await getAccountSettings();
        if (active) {
          setForm({
            fullName: data.fullName || "",
            profession: data.profession || "",
            location: data.location || "",
            phoneNumber: data.phoneNumber || "",
            email: data.email || "",
            versions: data.versions || []
          });
        }
      } catch (requestError) {
        if (active) {
          setError(requestError.response?.data?.message || "We couldn't load your account settings right now.");
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    load();

    return () => {
      active = false;
    };
  }, []);

  function update(field, value) {
    setForm((current) => ({ ...current, [field]: value }));
  }

  async function handleSave(event) {
    event.preventDefault();
    setSaving(true);
    setError("");
    setSuccess("");

    try {
      const data = await saveAccountSettings({
        fullName: form.fullName,
        profession: form.profession,
        location: form.location,
        phoneNumber: form.phoneNumber,
        email: form.email
      });

      setForm((current) => ({
        ...current,
        fullName: data.fullName || "",
        profession: data.profession || "",
        location: data.location || "",
        phoneNumber: data.phoneNumber || "",
        email: data.email || "",
        versions: data.versions || current.versions
      }));
      updateUser({ displayName: data.fullName, email: data.email });
      setSuccess("Your profile settings have been saved.");
    } catch (requestError) {
      setError(requestError.response?.data?.message || "We couldn't save your profile settings right now.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDownload(versionId) {
    setDownloadingId(versionId);
    setError("");
    setSuccess("");

    try {
      const result = await downloadPortfolioVersion(versionId);
      const url = URL.createObjectURL(result.blob);
      const anchor = document.createElement("a");
      anchor.href = url;
      anchor.download = result.fileName;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (requestError) {
      setError(requestError.response?.data?.message || "We couldn't download that portfolio package right now.");
    } finally {
      setDownloadingId("");
    }
  }

  async function handleDeleteVersion(versionId) {
    const confirmed = window.confirm("Remove this saved portfolio package from your account?");
    if (!confirmed) {
      return;
    }

    setDeletingVersionId(versionId);
    setError("");
    setSuccess("");

    try {
      const data = await deletePortfolioVersion(versionId);
      setForm((current) => ({
        ...current,
        versions: data.versions || current.versions.filter((version) => version.id !== versionId)
      }));
      setSuccess("The saved portfolio package has been removed.");
    } catch (requestError) {
      setError(requestError.response?.data?.message || "We couldn't remove that portfolio package right now.");
    } finally {
      setDeletingVersionId("");
    }
  }

  async function handleDeleteAccount() {
    const confirmed = window.confirm("Delete your account and all saved portfolio data? This cannot be undone.");
    if (!confirmed) {
      return;
    }

    setDeleting(true);
    setError("");

    try {
      await deleteAccount();
      logout();
      navigate("/", { replace: true });
    } catch (requestError) {
      setError(requestError.response?.data?.message || "We couldn't delete your account right now.");
      setDeleting(false);
    }
  }

  if (loading) {
    return (
      <div className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="section-shell dark-grid-panel flex items-center justify-center gap-3 py-20 text-white/70">
          <LoaderCircle className="animate-spin" size={20} />
          Loading your account settings...
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl px-4 pb-20 sm:px-6 lg:px-8">
      <div className="grid gap-6 xl:grid-cols-[1.1fr_0.9fr]">
        <section className="glass-card dark-grid-panel p-6 sm:p-8">
          <div className="flex items-start gap-4">
            <div className="grid h-12 w-12 place-items-center rounded-2xl border border-violet-400/30 bg-violet-500/10 text-violet-200">
              <Settings size={20} />
            </div>
            <div>
              <p className="text-sm uppercase tracking-[0.28em] text-white/40">Profile settings</p>
              <h1 className="mt-3 text-4xl font-semibold text-white">Keep your details ready</h1>
              <p className="mt-3 max-w-2xl text-sm leading-7 text-white/58">
                Save your main details here and they will be filled in for you the next time you build a portfolio.
              </p>
            </div>
          </div>

          <form className="mt-8 space-y-5" onSubmit={handleSave}>
            <div className="grid gap-5 md:grid-cols-2">
              <div>
                <label className="field-label">Full name</label>
                <input className="field-input" value={form.fullName} onChange={(event) => update("fullName", event.target.value)} required />
              </div>
              <div>
                <label className="field-label">Job title</label>
                <input className="field-input" value={form.profession} onChange={(event) => update("profession", event.target.value)} />
              </div>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              <div>
                <label className="field-label">Location</label>
                <input className="field-input" value={form.location} onChange={(event) => update("location", event.target.value)} />
              </div>
              <div>
                <label className="field-label">Phone number</label>
                <input className="field-input" value={form.phoneNumber} onChange={(event) => update("phoneNumber", event.target.value)} />
              </div>
            </div>

            <div>
              <label className="field-label">Email address</label>
              <input className="field-input" type="email" value={form.email} onChange={(event) => update("email", event.target.value)} required />
            </div>

            {error ? <div className="rounded-[24px] border border-coral/30 bg-coral/10 px-5 py-4 text-sm text-rose-100">{error}</div> : null}
            {success ? <div className="rounded-[24px] border border-emerald-400/25 bg-emerald-500/10 px-5 py-4 text-sm text-emerald-100">{success}</div> : null}

            <div className="flex justify-end">
              <div className="rounded-[28px] border border-white/10 bg-white/[0.03] p-3">
                <button className="primary-button" type="submit" disabled={saving}>
                  <Save size={16} className="mr-2" />
                  {saving ? "Saving..." : "Save changes"}
                </button>
              </div>
            </div>
          </form>
        </section>

        <div className="space-y-6">
          <section className="glass-card dark-grid-panel p-6 sm:p-8">
            <p className="text-sm uppercase tracking-[0.28em] text-white/40">Saved portfolios</p>
            <h2 className="mt-3 text-3xl font-semibold text-white">Your generated packages</h2>
            <p className="mt-3 text-sm leading-7 text-white/58">
              Every time you build a portfolio, it will appear here so you can come back and download it again.
            </p>

            <div className="mt-6 space-y-4">
              {form.versions.length === 0 ? (
                <div className="rounded-[28px] border border-dashed border-white/10 px-6 py-10 text-center text-white/55">
                  No portfolio packages have been saved yet.
                </div>
              ) : (
                form.versions.map((version, index) => (
                  <article key={version.id} className="rounded-[28px] border border-white/10 bg-white/[0.03] p-5">
                    <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                      <div>
                        <p className="text-xs font-semibold uppercase tracking-[0.28em] text-white/35">Portfolio {form.versions.length - index}</p>
                        <h3 className="mt-2 text-lg font-medium text-white">{formatDateTime(version.generatedAt)}</h3>
                      </div>
                      <div className="flex flex-wrap gap-3">
                        <button
                          type="button"
                          className="secondary-button"
                          onClick={() => handleDownload(version.id)}
                          disabled={downloadingId === version.id || deletingVersionId === version.id}
                        >
                          <Download size={16} className="mr-2" />
                          {downloadingId === version.id ? "Preparing..." : "Download"}
                        </button>
                        <button
                          type="button"
                          className="inline-flex items-center rounded-2xl border border-rose-400/25 bg-rose-500/10 px-4 py-2 text-sm font-medium text-rose-100 transition hover:bg-rose-500/20 disabled:cursor-not-allowed disabled:opacity-60"
                          onClick={() => handleDeleteVersion(version.id)}
                          disabled={deletingVersionId === version.id || downloadingId === version.id}
                        >
                          <Trash2 size={15} className="mr-2" />
                          {deletingVersionId === version.id ? "Removing..." : "Remove"}
                        </button>
                      </div>
                    </div>
                  </article>
                ))
              )}
            </div>
          </section>

          <div className="flex flex-wrap items-center justify-between gap-3 rounded-[24px] border border-white/10 bg-white/[0.03] px-5 py-4">
            <button
              type="button"
              className="inline-flex items-center rounded-2xl border border-rose-400/25 bg-rose-500/10 px-4 py-2 text-sm font-medium text-rose-100 transition hover:bg-rose-500/20"
              onClick={handleDeleteAccount}
              disabled={deleting}
            >
              <Trash2 size={15} className="mr-2" />
              {deleting ? "Deleting..." : "Delete account"}
            </button>
            <button type="button" className="ghost-button" onClick={logout}>
              <LogOut size={16} className="mr-2" />
              Sign out
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
