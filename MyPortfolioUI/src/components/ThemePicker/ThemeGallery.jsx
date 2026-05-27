import { Check } from "lucide-react";
import { themeOptions } from "../../utils/defaults";
import ThemePreviewMockup from "./ThemePreviewMockup";

export default function ThemeGallery({ selectedTheme, onSelect }) {
  return (
    <div className="grid gap-4 lg:grid-cols-3">
      {themeOptions.map((theme) => {
        const selected = selectedTheme === theme.id;

        return (
          <button
            key={theme.id}
            type="button"
            onClick={() => onSelect(theme.id)}
            className={`overflow-hidden rounded-[28px] border text-left transition ${
              selected ? "border-violet-400/70 bg-white/[0.08]" : "border-white/10 bg-white/[0.03] hover:border-white/20"
            }`}
          >
            <div className="h-48 p-4">
              <ThemePreviewMockup themeId={theme.id} />
            </div>

            <div className="p-5">
              <div className="flex items-center justify-between">
                <h2 className="text-xl font-medium text-white">{theme.name}</h2>
                {selected ? (
                  <span className="grid h-8 w-8 place-items-center rounded-full bg-gradient-to-r from-violet-500 to-fuchsia-500 text-white">
                    <Check size={16} />
                  </span>
                ) : null}
              </div>
              <p className="mt-3 text-sm text-white/60">{theme.description}</p>
            </div>
          </button>
        );
      })}
    </div>
  );
}
