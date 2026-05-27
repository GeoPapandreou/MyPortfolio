import { ArrowLeft, ArrowRight, Download, LoaderCircle, Save } from "lucide-react";
import LoadingScreen from "../components/LoadingScreen";
import {
  AboutYouStage,
  DetailsStage,
  ReviewStage,
  YourWorkStage
} from "../components/FormSteps/WizardStageGroups";
import usePortfolioWizard from "../hooks/usePortfolioWizard";
import { wizardSteps } from "../utils/defaults";

export default function WizardPage() {
  const {
    profile,
    referenceImage,
    currentStep,
    loading,
    saving,
    generating,
    readyDownload,
    loadError,
    error,
    success,
    isLastStep,
    loadPortfolio,
    handleSaveDraft,
    handleGenerate,
    handleDownloadReadyPackage,
    handleDismissReadyDownload,
    handleReferenceImageSelect,
    handleReferenceImageClear,
    handleReferenceNotesChange,
    handlePhotoUpload,
    updatePersonalInfo,
    updateExperiences,
    updateWorkSamples,
    updateContactInfo,
    updateTheme,
    goToStep,
    goToPreviousStep,
    goToNextStep
  } = usePortfolioWizard();

  const stepComponents = [
    <AboutYouStage
      key="about"
      personalInfo={profile.personalInfo}
      onPersonalChange={updatePersonalInfo}
      onPhotoUpload={handlePhotoUpload}
    />,
    <YourWorkStage
      key="work"
      experiences={profile.experiences}
      onExperiencesChange={updateExperiences}
      workSamples={profile.workSamples}
      onWorkSamplesChange={updateWorkSamples}
    />,
    <DetailsStage
      key="details"
      contactInfo={profile.contactInfo}
      onContactInfoChange={updateContactInfo}
    />,
    <ReviewStage
      key="review"
      profile={profile}
      onThemeChange={updateTheme}
      referenceImage={referenceImage}
      onReferenceImageSelect={handleReferenceImageSelect}
      onReferenceImageClear={handleReferenceImageClear}
      onReferenceNotesChange={handleReferenceNotesChange}
    />
  ];

  if (loading) {
    return (
      <div className="mx-auto max-w-5xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="section-shell dark-grid-panel flex items-center justify-center gap-3 py-20 text-white/70">
          <LoaderCircle className="animate-spin" size={20} />
          Loading your saved answers...
        </div>
      </div>
    );
  }

  return (
    <>
      {generating ? (
        <LoadingScreen
          title="Your portfolio is being built..."
          subtitle="This can take a moment while your pages and package are prepared."
        />
      ) : null}

      {readyDownload ? (
        <div className="fixed inset-0 z-50 grid place-items-center bg-[#020617]/88 px-6 backdrop-blur-md">
          <div className="glass-card dark-grid-panel w-full max-w-lg rounded-[32px] border border-white/10 p-6 sm:p-8">
            <p className="text-sm uppercase tracking-[0.28em] text-white/40">Portfolio ready</p>
            <h2 className="mt-4 text-3xl font-semibold text-white">Your package is ready to download</h2>
            <p className="mt-3 text-sm leading-7 text-white/58">
              We saved this version to your account. Download it now, or close this window and grab it later from Profile settings.
            </p>

            <div className="mt-6 rounded-[24px] border border-white/10 bg-white/[0.03] px-5 py-4">
              <p className="text-xs font-semibold uppercase tracking-[0.24em] text-white/35">File name</p>
              <p className="mt-2 break-all text-sm text-white/82">{readyDownload.fileName}</p>
            </div>

            <div className="mt-8 flex flex-wrap justify-end gap-3">
              <button type="button" className="secondary-button" onClick={handleDismissReadyDownload}>
                Maybe later
              </button>
              <button type="button" className="primary-button" onClick={handleDownloadReadyPackage}>
                <Download size={16} className="mr-2" />
                Download package
              </button>
            </div>
          </div>
        </div>
      ) : null}

      <div className="mx-auto max-w-5xl px-4 pb-20 sm:px-6 lg:px-8">
        <div className="glass-card dark-grid-panel overflow-hidden p-6 sm:p-8">
          {loadError ? (
            <div className="mb-6 rounded-[24px] border border-amber-300/25 bg-amber-400/10 px-5 py-4 text-sm text-amber-50">
              <div className="flex flex-wrap items-center justify-between gap-3">
                <span>{loadError}</span>
                <button
                  type="button"
                  className="rounded-full border border-amber-200/30 px-4 py-2 text-xs font-semibold uppercase tracking-[0.18em] text-amber-50 transition hover:bg-amber-200/10"
                  onClick={loadPortfolio}
                >
                  Retry load
                </button>
              </div>
            </div>
          ) : null}

          <div className="mb-8 flex flex-wrap items-center gap-4 border-b border-white/10 pb-6">
            {wizardSteps.map((step, index) => {
              const active = index === currentStep;
              const completed = index < currentStep;

              return (
                <div key={step.id} className="flex min-w-0 flex-1 items-center gap-3">
                  <button
                    type="button"
                    onClick={() => goToStep(index)}
                    className={`flex min-w-0 items-center gap-3 rounded-full transition ${
                      active ? "text-white" : "text-white/55 hover:text-white"
                    }`}
                  >
                    <span
                      className={`grid h-9 w-9 shrink-0 place-items-center rounded-full border text-sm ${
                        active
                          ? "border-violet-400 bg-violet-500 text-white"
                          : completed
                            ? "border-violet-400/40 bg-violet-500/20 text-white"
                            : "border-white/12 bg-transparent text-white/65"
                      }`}
                    >
                      {index + 1}
                    </span>
                    <span className="truncate text-sm">{step.title}</span>
                  </button>
                  {index < wizardSteps.length - 1 ? <div className="hidden h-px flex-1 bg-white/10 md:block" /> : null}
                </div>
              );
            })}
          </div>

          {error ? <div className="mb-6 rounded-[24px] border border-coral/30 bg-coral/10 px-5 py-4 text-sm text-rose-100">{error}</div> : null}
          {success ? <div className="mb-6 rounded-[24px] border border-emerald-400/25 bg-emerald-500/10 px-5 py-4 text-sm text-emerald-100">{success}</div> : null}

          {stepComponents[currentStep]}

          <div className="mt-8 flex justify-end">
            <div className="rounded-[28px] border border-white/10 bg-white/[0.03] p-3">
              <div className="flex flex-wrap items-center justify-end gap-3">
                <button type="button" className="secondary-button" onClick={handleSaveDraft} disabled={saving || Boolean(loadError)}>
                  <Save size={16} className="mr-2" />
                  {saving ? "Saving..." : "Save"}
                </button>
                <button type="button" className="secondary-button" onClick={goToPreviousStep} disabled={currentStep === 0}>
                  <ArrowLeft size={16} className="mr-2" />
                  Back
                </button>
                {isLastStep ? (
                  <button type="button" className="primary-button" onClick={handleGenerate} disabled={generating || Boolean(loadError)}>
                    Build my portfolio
                    <ArrowRight size={16} className="ml-2" />
                  </button>
                ) : (
                  <button type="button" className="primary-button" onClick={goToNextStep}>
                    Next
                    <ArrowRight size={16} className="ml-2" />
                  </button>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </>
  );
}
