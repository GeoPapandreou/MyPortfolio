export const wizardSteps = [
  { id: "about", title: "About you" },
  { id: "work", title: "Your work" },
  { id: "details", title: "Contact" },
  { id: "review", title: "Styling" }
];

export const themeOptions = [
  {
    id: "Minimal",
    name: "Minimal",
    description: "Bright, calm and editorial with generous spacing."
  },
  {
    id: "Dark Pro",
    name: "Dark Pro",
    description: "Bold contrast, deep tones and sharp highlights."
  },
  {
    id: "Creative",
    name: "Creative",
    description: "Expressive colour, layered shapes and playful rhythm."
  }
];

export function createEmptyProfile() {
  return {
    theme: "Minimal",
    personalInfo: {
      fullName: "",
      profession: "",
      bio: "",
      photoUrl: "",
      location: ""
    },
    experiences: [],
    workSamples: [],
    contactInfo: {
      email: "",
      phone: "",
      linkedIn: "",
      instagram: "",
      facebook: "",
      github: ""
    },
    versions: []
  };
}

export function createClientId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  return `item-${Date.now()}-${Math.random().toString(36).slice(2, 10)}`;
}

export function createExperience() {
  return {
    clientId: createClientId(),
    organisation: "",
    role: "",
    startDate: "",
    endDate: "",
    isCurrent: false,
    bullets: [""]
  };
}

export function createWorkSample() {
  return {
    clientId: createClientId(),
    title: "",
    description: "",
    tools: [],
    liveUrl: ""
  };
}
