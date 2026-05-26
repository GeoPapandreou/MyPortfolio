import creativePreview from "../../assets/creative.png";
import darkPreview from "../../assets/dark.png";
import minimalPreview from "../../assets/minimal.png";

export const themePreviewMap = {
  Minimal: {
    src: minimalPreview,
    alt: "Minimal portfolio preview",
    containerClass: "border border-slate-200/80 bg-white"
  },
  "Dark Pro": {
    src: darkPreview,
    alt: "Dark portfolio preview",
    containerClass: "border border-white/10 bg-[#050b16]"
  },
  Creative: {
    src: creativePreview,
    alt: "Creative portfolio preview",
    containerClass: "bg-transparent",
    imageClass: "scale-[1.03]"
  }
};

export default function ThemePreviewMockup({ themeId, compact = false }) {
  const preview = themePreviewMap[themeId] ?? themePreviewMap.Minimal;

  return (
    <div
      className={`h-full overflow-hidden rounded-[24px] shadow-[0_18px_50px_rgba(2,6,23,0.28)] ${preview.containerClass} ${
        compact ? "rounded-[22px]" : ""
      }`}
    >
      <img
        src={preview.src}
        alt={preview.alt}
        className={`h-full w-full object-cover object-top ${preview.imageClass ?? ""}`}
      />
    </div>
  );
}
