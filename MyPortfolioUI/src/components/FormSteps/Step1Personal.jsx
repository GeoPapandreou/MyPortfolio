import { ImagePlus } from "lucide-react";
import { useId } from "react";

export default function Step1Personal({ value, onChange, onPhotoUpload, embedded = false }) {
  const photoInputId = useId();

  function update(field, nextValue) {
    onChange({ ...value, [field]: nextValue });
  }

  const headingClass = embedded ? "text-2xl font-semibold text-white" : "text-4xl font-semibold text-white";

  return (
    <div className="space-y-6">
      <div>
        <h1 className={headingClass}>Let&apos;s start with you</h1>
        <p className="mt-3 max-w-2xl text-sm leading-7 text-white/58">
          Tell us about yourself so your portfolio can feel personal, clear, and true to the work you do.
        </p>
      </div>

      <div className="grid gap-5 md:grid-cols-2">
        <div>
          <label className="field-label">Full name</label>
          <input className="field-input" value={value.fullName} onChange={(e) => update("fullName", e.target.value)} />
        </div>
        <div>
          <label className="field-label">Job title or profession</label>
          <input className="field-input" value={value.profession} onChange={(e) => update("profession", e.target.value)} />
        </div>
      </div>

      <div>
        <label className="field-label">Short bio</label>
        <textarea
          rows="5"
          className="field-input"
          value={value.bio}
          onChange={(e) => update("bio", e.target.value)}
          placeholder="A short introduction in your own words."
        />
      </div>

      <div>
        <div>
          <label className="field-label">Location</label>
          <input
            className="field-input"
            value={value.location}
            onChange={(e) => update("location", e.target.value)}
            placeholder="City, country"
          />
        </div>
      </div>

      <div>
        <label className="field-label">Profile photo</label>
        <div className="rounded-[24px] border border-white/10 bg-white/[0.03] p-4">
          <div className="flex items-center gap-4">
            <div className="grid h-16 w-16 shrink-0 place-items-center overflow-hidden rounded-[20px] border border-white/10 bg-white/[0.04]">
              {value.photoUrl ? (
                <img src={value.photoUrl} alt="Profile preview" className="h-full w-full object-cover" />
              ) : (
                <ImagePlus size={20} className="text-white/55" />
              )}
            </div>

            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium text-white">{value.photoUrl ? "Photo selected" : "Upload a portrait"}</p>
              <p className="mt-1 text-sm leading-6 text-white/55">
                Use a clear headshot or portrait image. JPG, PNG, and WebP work well here, and local uploads stay available while you build this portfolio.
              </p>
            </div>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-3">
            <label htmlFor={photoInputId} className="secondary-button cursor-pointer">
              <ImagePlus size={16} className="mr-2" />
              {value.photoUrl ? "Replace photo" : "Choose photo"}
            </label>
          </div>

          <input id={photoInputId} className="sr-only" type="file" accept="image/*" onChange={onPhotoUpload} />
        </div>
      </div>

      {value.photoUrl ? (
        <div className="overflow-hidden rounded-[28px] border border-white/10 bg-white/[0.03] p-3">
          <div className="mb-3 px-1">
            <p className="text-xs font-semibold uppercase tracking-[0.28em] text-white/35">Preview</p>
          </div>
          <img src={value.photoUrl} alt="Profile preview" className="h-56 w-full rounded-[22px] object-cover" />
        </div>
      ) : null}
    </div>
  );
}
