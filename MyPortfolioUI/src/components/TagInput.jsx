import { Plus, X } from "lucide-react";
import { useState } from "react";

export default function TagInput({ label, placeholder, value, onChange, maxItems }) {
  const [draft, setDraft] = useState("");

  function addTag() {
    const trimmed = draft.trim();
    if (!trimmed) {
      return;
    }

    if (maxItems && value.length >= maxItems) {
      return;
    }

    if (!value.some((item) => item.toLowerCase() === trimmed.toLowerCase())) {
      onChange([...value, trimmed]);
    }

    setDraft("");
  }

  return (
    <div>
      {label ? <label className="field-label">{label}</label> : null}
      <div className="rounded-[26px] border border-white/10 bg-slate/70 p-3">
        <div className="flex flex-wrap gap-2">
          {value.map((item) => (
            <span key={item} className="tag-pill">
              {item}
              <button
                type="button"
                className="ml-2 text-mist/60 hover:text-mist"
                onClick={() => onChange(value.filter((tag) => tag !== item))}
              >
                <X size={14} />
              </button>
            </span>
          ))}
        </div>
        <div className="mt-3 flex flex-col gap-3 sm:flex-row">
          <input
            className="field-input"
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            placeholder={placeholder}
            onKeyDown={(event) => {
              if (event.key === "Enter") {
                event.preventDefault();
                addTag();
              }
            }}
          />
          <button type="button" className="secondary-button sm:w-auto" onClick={addTag}>
            <Plus size={16} className="mr-2" />
            Add
          </button>
        </div>
      </div>
    </div>
  );
}
