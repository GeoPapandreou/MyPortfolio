import { useState } from "react";
import { ChevronDown } from "lucide-react";
import ThemePreviewMockup, { themePreviewMap } from "../ThemePicker/ThemePreviewMockup";

const exampleCards = [
  { title: "Minimal", text: "Calm layouts with elegant spacing and a softer presence." },
  { title: "Dark Pro", text: "Confident contrast, stronger structure, and a modern tone." },
  { title: "Creative", text: "Expressive colour, layered sections, and more personality." }
];

export default function ExamplesSection() {
  const [expandedTheme, setExpandedTheme] = useState(null);
  const expandedPreview = expandedTheme ? themePreviewMap[expandedTheme] : null;

  const handleThemeToggle = (themeTitle) => {
    setExpandedTheme((currentTheme) => (currentTheme === themeTitle ? null : themeTitle));
  };

  return (
    <section id="examples" className="mx-auto mt-8 max-w-7xl px-4 sm:px-6 lg:px-8">
      <div className="grid gap-6 lg:grid-cols-3">
        {exampleCards.map((item) => {
          const isExpanded = expandedTheme === item.title;

          return (
            <article
              key={item.title}
              className={`glass-card dark-grid-panel overflow-hidden p-6 transition ${
                isExpanded ? "border-violet-400/40 bg-white/[0.05]" : ""
              }`}
            >
              <div className="h-48">
                <ThemePreviewMockup themeId={item.title} compact />
              </div>
              <h3 className="mt-5 text-2xl font-semibold text-white">{item.title}</h3>
              <p className="mt-3 text-sm leading-7 text-white/60">{item.text}</p>
              <button
                type="button"
                onClick={() => handleThemeToggle(item.title)}
                className="mt-5 flex w-full items-center justify-center gap-2 rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-3 text-sm text-white/72 transition hover:border-white/20 hover:bg-white/[0.05]"
                aria-expanded={isExpanded}
                aria-controls={`theme-preview-${item.title}`}
              >
                <span>Preview</span>
                <ChevronDown size={18} className={`transition-transform ${isExpanded ? "rotate-180" : ""}`} />
              </button>
            </article>
          );
        })}
      </div>

      {expandedPreview ? (
        <div
          id={`theme-preview-${expandedTheme}`}
          className="glass-card dark-grid-panel mt-6 overflow-hidden p-4 sm:p-6"
        >
          <div className="mb-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <p className="text-sm text-white/55">Full preview</p>
              <h3 className="mt-1 text-3xl font-semibold text-white">{expandedTheme}</h3>
              <p className="mt-2 text-sm text-white/60">See the complete page before you choose the style that fits you best.</p>
            </div>
            <button
              type="button"
              onClick={() => setExpandedTheme(null)}
              className="rounded-2xl border border-white/10 px-4 py-2 text-sm text-white/72 transition hover:border-white/20 hover:bg-white/[0.05]"
            >
              Close preview
            </button>
          </div>

          <div className={`overflow-hidden rounded-[28px] shadow-[0_30px_90px_rgba(0,0,0,0.35)] ${expandedPreview.containerClass}`}>
            <img
              src={expandedPreview.src}
              alt={expandedPreview.alt}
              className={`w-full object-contain object-top ${expandedPreview.imageClass ?? ""}`}
            />
          </div>
        </div>
      ) : null}
    </section>
  );
}
