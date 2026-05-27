import apiClient from "./client";
import { getEffectiveReferenceImage, toGenerateReferencePayload } from "../utils/referenceImage";

export async function getPortfolio() {
  const { data } = await apiClient.get("/api/portfolio");
  return data;
}

export async function savePortfolio(profile) {
  const { data } = await apiClient.put("/api/portfolio", profile);
  return data;
}

export async function generatePortfolio(profile, referenceImage = null) {
  const payload = { profile };
  const effectiveReferenceImage = await getEffectiveReferenceImage(profile.theme, referenceImage);
  const referencePayload = toGenerateReferencePayload(effectiveReferenceImage);

  if (referencePayload) {
    payload.referenceImage = referencePayload;
  }

  const response = await apiClient.post(
    "/api/portfolio/generate",
    payload,
    {
      responseType: "blob"
    }
  );

  const disposition = response.headers["content-disposition"] ?? "";
  const fileNameMatch = disposition.match(/filename="?([^"]+)"?/i);

  return {
    blob: response.data,
    fileName: fileNameMatch?.[1] ?? "portfolio-package.zip"
  };
}

export async function downloadPortfolioVersion(versionId) {
  const response = await apiClient.get(`/api/portfolio/versions/${versionId}/download`, {
    responseType: "blob"
  });

  const disposition = response.headers["content-disposition"] ?? "";
  const fileNameMatch = disposition.match(/filename="?([^"]+)"?/i);

  return {
    blob: response.data,
    fileName: fileNameMatch?.[1] ?? "portfolio-package.zip"
  };
}

export async function deletePortfolioVersion(versionId) {
  const { data } = await apiClient.delete(`/api/account/versions/${versionId}`);
  return data;
}
