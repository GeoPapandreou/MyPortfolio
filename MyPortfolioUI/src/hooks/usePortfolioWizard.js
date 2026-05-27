import { useEffect, useRef, useState } from "react";
import { getPortfolio, generatePortfolio, savePortfolio } from "../api/portfolio";
import { getApiErrorMessage } from "../utils/apiErrors";
import { createEmptyProfile, wizardSteps } from "../utils/defaults";
import { withClientId, normaliseContactInfo, toApiProfile, toPersistedApiProfile } from "../utils/profile";
import { readReferenceImageFile } from "../utils/referenceImage";

const allowedProfilePhotoTypes = new Set(["image/png", "image/jpeg", "image/webp"]);
const maxProfilePhotoBytes = 4 * 1024 * 1024;

function normaliseProfile(apiProfile) {
  return {
    theme: apiProfile?.theme || "Minimal",
    personalInfo: {
      fullName: apiProfile?.personalInfo?.fullName || "",
      profession: apiProfile?.personalInfo?.profession || "",
      bio: apiProfile?.personalInfo?.bio || "",
      photoUrl: apiProfile?.personalInfo?.photoUrl || "",
      location: apiProfile?.personalInfo?.location || ""
    },
    experiences: (apiProfile?.experiences || []).map((item) =>
      withClientId({
        organisation: item.organisation || "",
        role: item.role || "",
        startDate: item.startDate ? item.startDate.slice(0, 10) : "",
        endDate: item.endDate ? item.endDate.slice(0, 10) : "",
        isCurrent: Boolean(item.isCurrent),
        bullets: item.bullets?.length ? item.bullets : [""]
      })
    ),
    workSamples: (apiProfile?.workSamples || []).map((item) =>
      withClientId({
        title: item.title || "",
        description: item.description || "",
        tools: item.tools || [],
        liveUrl: item.liveUrl || ""
      })
    ),
    contactInfo: normaliseContactInfo(apiProfile?.contactInfo),
    versions: apiProfile?.versions || []
  };
}

export default function usePortfolioWizard() {
  const isMountedRef = useRef(true);
  const downloadUrlRef = useRef("");
  const [profile, setProfile] = useState(createEmptyProfile);
  const [referenceImage, setReferenceImage] = useState(null);
  const [currentStep, setCurrentStep] = useState(0);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [generating, setGenerating] = useState(false);
  const [readyDownload, setReadyDownload] = useState(null);
  const [loadError, setLoadError] = useState("");
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  function clearReadyDownload() {
    if (downloadUrlRef.current) {
      URL.revokeObjectURL(downloadUrlRef.current);
      downloadUrlRef.current = "";
    }

    setReadyDownload(null);
  }

  useEffect(() => {
    isMountedRef.current = true;

    return () => {
      isMountedRef.current = false;
      if (downloadUrlRef.current) {
        URL.revokeObjectURL(downloadUrlRef.current);
        downloadUrlRef.current = "";
      }
    };
  }, []);

  async function loadPortfolio() {
    setLoading(true);
    setLoadError("");

    try {
      const data = await getPortfolio();
      if (!isMountedRef.current) {
        return;
      }

      setProfile(normaliseProfile(data));
    } catch (requestError) {
      if (!isMountedRef.current) {
        return;
      }

      setLoadError(await getApiErrorMessage(
        requestError,
        "We couldn't load your saved answers. Retry loading before saving so you don't overwrite existing portfolio data."
      ));
    } finally {
      if (isMountedRef.current) {
        setLoading(false);
      }
    }
  }

  useEffect(() => {
    loadPortfolio();
  }, []);

  async function handleSaveDraft() {
    if (loadError) {
      setError(loadError);
      return;
    }

    setSaving(true);
    setError("");
    setSuccess("");

    try {
      const saved = await savePortfolio(toPersistedApiProfile(profile));
      setProfile((current) => ({ ...current, versions: saved.versions || current.versions }));
      setSuccess("Your answers have been saved.");
    } catch (requestError) {
      setError(await getApiErrorMessage(requestError, "We couldn't save your answers. Please try again."));
    } finally {
      setSaving(false);
    }
  }

  async function handleReferenceImageSelect(file) {
    setError("");

    try {
      const nextImage = await readReferenceImageFile(file);
      setReferenceImage((current) => ({
        ...nextImage,
        notes: current?.notes || ""
      }));
    } catch (fileError) {
      setError(fileError instanceof Error ? fileError.message : "We couldn't process that reference image.");
    }
  }

  function handleReferenceImageClear() {
    setReferenceImage(null);
  }

  function handleReferenceNotesChange(notes) {
    setReferenceImage((current) => {
      if (!current) {
        return current;
      }

      return {
        ...current,
        notes
      };
    });
  }

  async function handleGenerate() {
    if (loadError) {
      setError(loadError);
      return;
    }

    clearReadyDownload();
    setGenerating(true);
    setError("");
    setSuccess("");

    try {
      await savePortfolio(toPersistedApiProfile(profile));
      const result = await generatePortfolio(toApiProfile(profile), referenceImage);
      const downloadUrl = URL.createObjectURL(result.blob);
      downloadUrlRef.current = downloadUrl;
      setReadyDownload({
        url: downloadUrl,
        fileName: result.fileName
      });

      try {
        const refreshed = await getPortfolio();
        setProfile(normaliseProfile(refreshed));
      } catch {}

      setSuccess("Your portfolio package is ready to download.");
    } catch (requestError) {
      setError(await getApiErrorMessage(requestError, "We couldn't build your portfolio right now. Please try again."));
    } finally {
      setGenerating(false);
    }
  }

  function handleDownloadReadyPackage() {
    if (!readyDownload) {
      return;
    }

    const anchor = document.createElement("a");
    anchor.href = readyDownload.url;
    anchor.download = readyDownload.fileName;
    anchor.click();

    clearReadyDownload();
    setSuccess("Your portfolio package has been downloaded.");
  }

  function handleDismissReadyDownload() {
    clearReadyDownload();
    setSuccess("Your portfolio package is saved and can be downloaded later from Profile settings.");
  }

  async function handlePhotoUpload(event) {
    const file = event.target.files?.[0];
    if (!file) {
      return;
    }

    if (!allowedProfilePhotoTypes.has(file.type)) {
      setError("Profile photos must be PNG, JPG, or WebP files.");
      return;
    }

    if (file.size > maxProfilePhotoBytes) {
      setError("Profile photos must be 4 MB or smaller.");
      return;
    }

    const reader = new FileReader();
    reader.onload = () => {
      setProfile((current) => ({
        ...current,
        personalInfo: {
          ...current.personalInfo,
          photoUrl: typeof reader.result === "string" ? reader.result : ""
        }
      }));
      setError("");
    };
    reader.onerror = () => {
      setError("We couldn't read that profile photo. Please try another file.");
    };
    reader.readAsDataURL(file);
  }

  function updatePersonalInfo(personalInfo) {
    setProfile((current) => ({ ...current, personalInfo }));
  }

  function updateExperiences(experiences) {
    setProfile((current) => ({ ...current, experiences }));
  }

  function updateWorkSamples(workSamples) {
    setProfile((current) => ({ ...current, workSamples }));
  }

  function updateContactInfo(contactInfo) {
    setProfile((current) => ({ ...current, contactInfo }));
  }

  function updateTheme(theme) {
    setProfile((current) => ({ ...current, theme }));
  }

  function goToStep(stepIndex) {
    setCurrentStep(stepIndex);
  }

  function goToPreviousStep() {
    setCurrentStep((current) => Math.max(current - 1, 0));
  }

  function goToNextStep() {
    setCurrentStep((current) => Math.min(current + 1, wizardSteps.length - 1));
  }

  return {
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
    isLastStep: currentStep === wizardSteps.length - 1,
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
  };
}
