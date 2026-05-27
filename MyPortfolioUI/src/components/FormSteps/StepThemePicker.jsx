import { ImagePlus, Trash2 } from "lucide-react";
import ThemeGallery from "../ThemePicker/ThemeGallery";
import {
  createReferenceImagePreviewUrl,
  formatReferenceImageSize
} from "../../utils/referenceImage";

export default function StepThemePicker({
  value,
  onChange,
  referenceImage,
  onReferenceImageSelect,
  onReferenceImageClear,
  onReferenceNotesChange,
  embedded = false
}) {
  const headingClass = embedded ? "text-2xl font-semibold text-white" : "text-4xl font-semibold text-white";
  const previewUrl = createReferenceImagePreviewUrl(referenceImage);
  const fileSize = formatReferenceImageSize(referenceImage?.sizeBytes);

  function handleFileChange(event) {
    const file = event.target.files?.[0];
    if (file) {
      onReferenceImageSelect(file);
    }

    event.target.value = "";
  }

  return (
    <div className="space-y-6">
      <div>
        <h1 className={headingClass}>Choose the overall style</h1>
        <p className="mt-3 max-w-2xl text-sm leading-7 text-white/58">
          Pick the direction that feels closest to your personality. You can always build again later with a different look.
        </p>
      </div>

      <ThemeGallery selectedTheme={value} onSelect={onChange} />

      <section className="space-y-5 rounded-[28px] border border-white/10 bg-white/[0.03] p-5 sm:p-6">
        <div>
          <p className="text-sm font-semibold uppercase tracking-[0.28em] text-white/35">Design reference</p>
          <h2 className="mt-3 text-xl font-medium text-white">Guide the generated look</h2>
          <p className="mt-3 max-w-3xl text-sm leading-7 text-white/58">
            Your selected theme already sends a built-in design reference image to the generator. Upload your own image only if you want to override that default reference.
          </p>
        </div>

        {referenceImage ? (
          <div className="overflow-hidden rounded-[24px] border border-white/10 bg-[#040b19]">
            <div className="flex flex-col items-center gap-4 p-5">
              {previewUrl ? (
                <img
                  src={previewUrl}
                  alt="Reference inspiration preview"
                  className="h-40 w-40 rounded-[22px] border border-white/10 object-cover shadow-[0_16px_40px_rgba(2,6,23,0.35)] sm:h-48 sm:w-48"
                />
              ) : null}

              <div className="text-center">
                <p className="truncate text-sm font-medium text-white">{referenceImage.fileName || "Reference image"}</p>
                {fileSize ? <p className="mt-1 text-xs text-white/45">{fileSize}</p> : null}
              </div>

              <div className="flex flex-wrap justify-center gap-3">
                <label className="secondary-button cursor-pointer">
                  <ImagePlus size={16} className="mr-2" />
                  Upload image again
                  <input
                    type="file"
                    accept="image/png,image/jpeg,image/webp"
                    className="sr-only"
                    onChange={handleFileChange}
                  />
                </label>

                <button
                  type="button"
                  className="inline-flex items-center rounded-2xl border border-rose-400/25 bg-rose-500/10 px-4 py-3 text-sm font-medium text-rose-100 transition hover:bg-rose-500/20"
                  onClick={onReferenceImageClear}
                >
                  <Trash2 size={15} className="mr-2" />
                  Remove image
                </button>
              </div>
            </div>
          </div>
        ) : (
          <label className="flex h-28 w-28 cursor-pointer flex-col items-center justify-center rounded-[24px] border border-dashed border-white/15 bg-white/[0.02] text-center text-white/70 transition hover:border-violet-400/40 hover:bg-white/[0.04] hover:text-white">
            <ImagePlus size={18} className="mb-2" />
            <span className="px-3 text-xs font-medium leading-5">Upload your image</span>
            <input
              type="file"
              accept="image/png,image/jpeg,image/webp"
              className="sr-only"
              onChange={handleFileChange}
            />
          </label>
        )}

        <div>
          <label className="field-label">Reference notes</label>
          <textarea
            className="field-input min-h-[132px] resize-y"
            placeholder={referenceImage
              ? "Share what you want the generator to borrow from your image, like composition, colors, typography, or overall mood."
              : `Describe what you want the generator to borrow from the ${value} look, like composition, colors, typography, or overall mood.`}
            value={referenceImage?.notes ?? ""}
            onChange={(event) => onReferenceNotesChange(event.target.value)}
            disabled={!referenceImage}
            maxLength={1000}
          />
        </div>
      </section>
    </div>
  );
}
