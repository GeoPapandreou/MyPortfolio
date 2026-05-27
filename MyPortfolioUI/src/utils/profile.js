import { createClientId } from "./defaults";

/**
 * Adds a stable client-side id to a list item that doesn't yet have one.
 * Used so React list renders have a consistent key even before the item is saved.
 */
export function withClientId(item) {
  return {
    ...item,
    clientId: item?.clientId || createClientId()
  };
}

/**
 * Converts the API contact-info shape into the flat local editing shape.
 */
export function normaliseContactInfo(apiContactInfo) {
  return {
    email: apiContactInfo?.email || "",
    phone: apiContactInfo?.phone || "",
    linkedIn: apiContactInfo?.linkedIn || "",
    instagram: apiContactInfo?.instagram || "",
    facebook: apiContactInfo?.facebook || "",
    github: apiContactInfo?.gitHub || ""
  };
}

/**
 * Converts the flat local editing contact-info shape back to the API shape.
 */
export function toApiContactInfo(contactInfo) {
  return {
    email: contactInfo.email,
    phone: contactInfo.phone,
    linkedIn: contactInfo.linkedIn,
    instagram: contactInfo.instagram,
    facebook: contactInfo.facebook,
    gitHub: contactInfo.github
  };
}

/**
 * Converts the local profile state into the shape expected
 * by PUT /api/portfolio and POST /api/portfolio/generate.
 *
 * startDate / endDate are coerced with `|| null` so that empty strings from
 * the wizard date inputs are sent as null rather than as empty strings.
 */
export function toApiProfile(profile) {
  return {
    theme: profile.theme,
    personalInfo: profile.personalInfo,
    experiences: profile.experiences.map((item) => ({
      organisation: item.organisation,
      role: item.role,
      startDate: item.startDate || null,
      endDate: item.endDate || null,
      isCurrent: item.isCurrent,
      bullets: item.bullets
    })),
    workSamples: profile.workSamples.map((item) => ({
      title: item.title,
      description: item.description,
      tools: item.tools,
      liveUrl: item.liveUrl
    })),
    contactInfo: toApiContactInfo(profile.contactInfo)
  };
}

export function isInlineImageDataUrl(value) {
  return typeof value === "string" && value.startsWith("data:image/");
}

export function toPersistedApiProfile(profile) {
  const apiProfile = toApiProfile(profile);

  if (isInlineImageDataUrl(apiProfile.personalInfo?.photoUrl)) {
    apiProfile.personalInfo.photoUrl = "";
  }

  return apiProfile;
}
