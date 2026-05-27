import { FolderKanban, MinusCircle, Plus } from "lucide-react";
import TagInput from "../TagInput";
import { createWorkSample } from "../../utils/defaults";

export default function Step3WorkSamples({ value, onChange, embedded = false }) {
  function updateItem(index, nextItem) {
    onChange(value.map((item, currentIndex) => (currentIndex === index ? nextItem : item)));
  }

  const headingClass = embedded ? "text-2xl font-semibold text-white" : "text-4xl font-semibold text-white";

  return (
    <div className="space-y-6">
      <div className="flex items-start justify-between gap-4">
        <div>
          <h1 className={headingClass}>Show your best work</h1>
          <p className="mt-3 max-w-2xl text-sm leading-7 text-white/58">
            These can be projects, campaigns, client work, research, menus, exhibitions, workshops or anything else that shows your strengths.
          </p>
        </div>
        <button type="button" className="secondary-button" onClick={() => onChange([...value, createWorkSample()])}>
          <Plus size={16} className="mr-2" />
          Add project
        </button>
      </div>

      {value.map((item, index) => (
        <article key={item.clientId} className="rounded-[28px] border border-white/10 bg-white/[0.03] p-5">
          <div className="mb-5 flex items-center justify-between gap-4">
            <div>
              <h2 className="text-xl font-medium text-white">Project</h2>
            </div>
            <button type="button" className="ghost-button" onClick={() => onChange(value.filter((_, currentIndex) => currentIndex !== index))}>
              <MinusCircle size={16} className="mr-2" />
              Remove
            </button>
          </div>

          <div className="grid gap-5 md:grid-cols-2">
            <div>
              <label className="field-label">Title</label>
              <input className="field-input" value={item.title} onChange={(e) => updateItem(index, { ...item, title: e.target.value })} />
            </div>
            <div>
              <label className="field-label">Link</label>
              <input className="field-input" value={item.liveUrl} onChange={(e) => updateItem(index, { ...item, liveUrl: e.target.value })} />
            </div>
          </div>

          <div className="mt-5">
            <label className="field-label">Description</label>
            <textarea
              rows="5"
              className="field-input"
              value={item.description}
              onChange={(e) => updateItem(index, { ...item, description: e.target.value })}
              placeholder="What it was, what you did, and what changed because of it"
            />
          </div>

          <div className="mt-5">
            <TagInput
              label="Tools and methods used"
              placeholder="Add one item at a time"
              value={item.tools}
              onChange={(tools) => updateItem(index, { ...item, tools })}
            />
          </div>
        </article>
      ))}

      {value.length === 0 ? (
        <div className="wizard-empty-state">
          <div className="wizard-empty-icon">
            <FolderKanban size={20} />
          </div>
          <p className="mt-4 text-lg font-medium text-white">Your project cards will appear here</p>
          <p className="mt-2 max-w-xl text-sm leading-7 text-white/55">
            Add two or three standout projects and the finished portfolio will feel much closer to something ready to publish.
          </p>
          <div className="mt-6 grid gap-3 text-left sm:grid-cols-2">
            <div className="wizard-empty-card">
              <div className="wizard-empty-line w-28" />
              <div className="wizard-empty-line mt-4 w-full" />
              <div className="wizard-empty-line mt-3 w-4/5" />
            </div>
            <div className="wizard-empty-card">
              <div className="wizard-empty-line w-24" />
              <div className="wizard-empty-line mt-4 w-5/6" />
              <div className="wizard-empty-line mt-3 w-2/3" />
            </div>
          </div>
        </div>
      ) : null}
    </div>
  );
}
