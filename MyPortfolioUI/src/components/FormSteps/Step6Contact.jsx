export default function Step6Contact({ value, onChange, embedded = false }) {
  function update(field, nextValue) {
    onChange({ ...value, [field]: nextValue });
  }

  const headingClass = embedded ? "text-2xl font-semibold text-white" : "text-4xl font-semibold text-white";

  return (
    <div className="space-y-6">
      <div>
        <h1 className={headingClass}>How can people reach you?</h1>
        <p className="mt-3 max-w-2xl text-sm leading-7 text-white/58">
          Choose the details you want to make public. A simple email address is enough if you prefer to keep this short.
        </p>
      </div>

      <div className="grid gap-5 md:grid-cols-2">
        <div>
          <label className="field-label">Email address</label>
          <input className="field-input" type="email" value={value.email} onChange={(e) => update("email", e.target.value)} />
        </div>
        <div>
          <label className="field-label">Phone number</label>
          <input className="field-input" value={value.phone} onChange={(e) => update("phone", e.target.value)} />
        </div>
        <div>
          <label className="field-label">LinkedIn profile</label>
          <input className="field-input" value={value.linkedIn} onChange={(e) => update("linkedIn", e.target.value)} />
        </div>
        <div>
          <label className="field-label">Instagram profile</label>
          <input className="field-input" value={value.instagram} onChange={(e) => update("instagram", e.target.value)} />
        </div>
        <div>
          <label className="field-label">Facebook profile</label>
          <input className="field-input" value={value.facebook} onChange={(e) => update("facebook", e.target.value)} />
        </div>
        <div>
          <label className="field-label">GitHub profile</label>
          <input className="field-input" value={value.github} onChange={(e) => update("github", e.target.value)} />
        </div>
      </div>
    </div>
  );
}
