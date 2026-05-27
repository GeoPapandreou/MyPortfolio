import { BriefcaseBusiness, MinusCircle, Plus } from "lucide-react";
import { createExperience } from "../../utils/defaults";

export default function Step2Experience({ value, onChange, embedded = false }) {
  function updateItem(index, nextItem) {
    onChange(value.map((item, currentIndex) => (currentIndex === index ? nextItem : item)));
  }

  function updateBullet(itemIndex, bulletIndex, nextValue) {
    const item = value[itemIndex];
    const bullets = item.bullets.map((bullet, index) => (index === bulletIndex ? nextValue : bullet));
    updateItem(itemIndex, { ...item, bullets });
  }

  function removeBullet(itemIndex, bulletIndex) {
    const item = value[itemIndex];
    const bullets = item.bullets.filter((_, index) => index !== bulletIndex);
    updateItem(itemIndex, { ...item, bullets: bullets.length > 0 ? bullets : [""] });
  }

  const headingClass = embedded ? "text-2xl font-semibold text-white" : "text-4xl font-semibold text-white";

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className={headingClass}>Tell the story of your work</h1>
          <p className="mt-3 max-w-2xl text-sm leading-7 text-white/58">
            Add as many roles as you like. Use short bullet points to describe what you handled, improved or achieved.
          </p>
        </div>
        <button type="button" className="secondary-button" onClick={() => onChange([...value, createExperience()])}>
          <Plus size={16} className="mr-2" />
          Add role
        </button>
      </div>

      {value.length === 0 ? (
        <div className="wizard-empty-state">
          <div className="wizard-empty-icon">
            <BriefcaseBusiness size={20} />
          </div>
          <p className="mt-4 text-lg font-medium text-white">Your timeline starts here</p>
          <p className="mt-2 max-w-xl text-sm leading-7 text-white/55">
            Add one role to start shaping the experience section. Your answers will be turned into a polished timeline when you build the portfolio.
          </p>
          <div className="mt-6 grid gap-3 text-left sm:grid-cols-2">
            <div className="wizard-empty-card">
              <div className="wizard-empty-line w-24" />
              <div className="wizard-empty-line mt-4 w-3/4" />
              <div className="wizard-empty-line mt-3 w-full" />
            </div>
            <div className="wizard-empty-card">
              <div className="wizard-empty-line w-20" />
              <div className="wizard-empty-line mt-4 w-2/3" />
              <div className="wizard-empty-line mt-3 w-5/6" />
            </div>
          </div>
        </div>
      ) : null}

      <div className="space-y-6">
        {value.map((item, index) => (
          <article key={item.clientId} className="rounded-[28px] border border-white/10 bg-white/[0.03] p-5">
            <div className="mb-5 flex items-center justify-between gap-4">
              <div>
                <p className="text-sm uppercase tracking-[0.22em] text-white/35">Role {index + 1}</p>
                <h2 className="mt-2 text-xl font-medium text-white">Work history</h2>
              </div>
              <button type="button" className="ghost-button" onClick={() => onChange(value.filter((_, currentIndex) => currentIndex !== index))}>
                <MinusCircle size={16} className="mr-2" />
                Remove
              </button>
            </div>

            <div className="grid gap-5 md:grid-cols-2">
              <div>
                <label className="field-label">Employer or organisation</label>
                <input
                  className="field-input"
                  value={item.organisation}
                  onChange={(e) => updateItem(index, { ...item, organisation: e.target.value })}
                />
              </div>
              <div>
                <label className="field-label">Your role</label>
                <input className="field-input" value={item.role} onChange={(e) => updateItem(index, { ...item, role: e.target.value })} />
              </div>
            </div>

            <div className="mt-5 grid gap-5 md:grid-cols-[1fr_1fr_auto]">
              <div>
                <label className="field-label">Start date</label>
                <input
                  type="date"
                  className="field-input"
                  value={item.startDate ?? ""}
                  onChange={(e) => updateItem(index, { ...item, startDate: e.target.value })}
                />
              </div>
              <div>
                <label className="field-label">End date</label>
                <input
                  type="date"
                  disabled={item.isCurrent}
                  className="field-input disabled:opacity-50"
                  value={item.endDate ?? ""}
                  onChange={(e) => updateItem(index, { ...item, endDate: e.target.value })}
                />
              </div>
              <label className="mt-8 flex items-center gap-3 text-sm text-white/68">
                <input
                  type="checkbox"
                  checked={item.isCurrent}
                  onChange={(e) => updateItem(index, { ...item, isCurrent: e.target.checked, endDate: e.target.checked ? "" : item.endDate })}
                />
                Present
              </label>
            </div>

            <div className="mt-5 space-y-3">
              <label className="field-label">Key responsibilities or achievements</label>
              {item.bullets.map((bullet, bulletIndex) => (
                <div key={`bullet-${bulletIndex}`} className="flex items-start gap-3">
                  <input
                    className="field-input"
                    value={bullet}
                    onChange={(e) => updateBullet(index, bulletIndex, e.target.value)}
                    placeholder="Describe a responsibility, result or contribution"
                  />
                  <button type="button" className="ghost-button shrink-0" onClick={() => removeBullet(index, bulletIndex)}>
                    <MinusCircle size={16} className="mr-2" />
                    Remove
                  </button>
                </div>
              ))}
              <button
                type="button"
                className="ghost-button"
                onClick={() => updateItem(index, { ...item, bullets: [...item.bullets, ""] })}
              >
                <Plus size={16} className="mr-2" />
                Add
              </button>
            </div>
          </article>
        ))}
      </div>
    </div>
  );
}
