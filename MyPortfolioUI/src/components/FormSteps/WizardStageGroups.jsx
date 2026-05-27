import Step1Personal from "./Step1Personal";
import Step2Experience from "./Step2Experience";
import Step3WorkSamples from "./Step3WorkSamples";
import Step6Contact from "./Step6Contact";
import StepThemePicker from "./StepThemePicker";

function StageShell({ eyebrow, title, description, children }) {
  return (
    <div className="space-y-6">
      <div>
        <p className="text-xs font-semibold uppercase tracking-[0.32em] text-white/40">{eyebrow}</p>
        <h1 className="mt-4 text-4xl font-semibold text-white">{title}</h1>
        <p className="mt-3 max-w-3xl text-sm leading-7 text-white/58">{description}</p>
      </div>

      <div className="space-y-5">{children}</div>
    </div>
  );
}

function StagePanel({ children }) {
  return <section className="rounded-[30px] border border-white/10 bg-white/[0.03] p-5 sm:p-6">{children}</section>;
}

export function AboutYouStage({ personalInfo, onPersonalChange, onPhotoUpload }) {
  return (
    <StageShell
      eyebrow="Step 1"
      title="Let's start with you"
      description="Share the essentials people should know first. This page shapes the introduction visitors will see when they open your portfolio."
    >
      <StagePanel>
        <Step1Personal value={personalInfo} onChange={onPersonalChange} onPhotoUpload={onPhotoUpload} embedded />
      </StagePanel>
    </StageShell>
  );
}

export function YourWorkStage({
  experiences,
  onExperiencesChange,
  workSamples,
  onWorkSamplesChange
}) {
  return (
    <StageShell
      eyebrow="Step 2"
      title="Show the work behind your portfolio"
      description="This page brings together your experience and your best examples so people can quickly understand the work you have done."
    >
      <StagePanel>
        <Step2Experience value={experiences} onChange={onExperiencesChange} embedded />
      </StagePanel>

      <StagePanel>
        <Step3WorkSamples value={workSamples} onChange={onWorkSamplesChange} embedded />
      </StagePanel>
    </StageShell>
  );
}

export function DetailsStage({ contactInfo, onContactInfoChange }) {
  return (
    <StageShell
      eyebrow="Step 3"
      title="Add the contact details you want to share"
      description="Choose the contact details people should use when they want to reach you from your portfolio."
    >
      <StagePanel>
        <Step6Contact value={contactInfo} onChange={onContactInfoChange} embedded />
      </StagePanel>
    </StageShell>
  );
}

export function ReviewStage({
  profile,
  onThemeChange,
  referenceImage,
  onReferenceImageSelect,
  onReferenceImageClear,
  onReferenceNotesChange
}) {
  return (
    <StageShell
      eyebrow="Step 4"
      title="Choose the styling for your portfolio"
      description="Pick the visual direction that feels right for your portfolio before building it."
    >
      <StagePanel>
        <StepThemePicker
          value={profile.theme}
          onChange={onThemeChange}
          referenceImage={referenceImage}
          onReferenceImageSelect={onReferenceImageSelect}
          onReferenceImageClear={onReferenceImageClear}
          onReferenceNotesChange={onReferenceNotesChange}
          embedded
        />
      </StagePanel>
    </StageShell>
  );
}
