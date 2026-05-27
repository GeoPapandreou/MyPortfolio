import { Bot, CheckCircle2, LoaderCircle } from "lucide-react";

export default function LoadingScreen({ title, subtitle }) {
  return (
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#020617]/92 px-6 backdrop-blur-md">
      <div className="glass-card dark-grid-panel w-full max-w-lg p-8 text-center">
        <div className="mx-auto mb-6 grid h-20 w-20 place-items-center rounded-full border border-violet-400/25 bg-violet-500/10 text-violet-300 shadow-[0_20px_60px_rgba(124,58,237,0.25)]">
          <Bot size={34} />
        </div>
        <h2 className="text-4xl font-semibold text-white">{title}</h2>
        <p className="mt-4 text-white/65">{subtitle}</p>
        <div className="mt-8 space-y-3 text-left text-sm text-white/68">
          {[
            ["Planning the layout", true],
            ["Arranging your sections", true],
            ["Preparing the download", true],
            ["Finishing the final touches", false]
          ].map(([item, complete], index) => (
            <div key={item} className="flex items-center gap-3 rounded-2xl border border-white/10 bg-white/[0.04] px-4 py-3">
              {complete ? (
                <CheckCircle2 size={18} className="shrink-0 text-emerald-400" />
              ) : (
                <LoaderCircle size={18} className="shrink-0 animate-spin text-violet-300" />
              )}
              <span>{item}</span>
            </div>
          ))}
        </div>
        <p className="mt-6 text-xs text-white/38">This may take a minute depending on demand.</p>
      </div>
    </div>
  );
}
