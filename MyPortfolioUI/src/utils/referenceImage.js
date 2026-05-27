import creativeThemeReference from "../assets/creative.png";
import darkThemeReference from "../assets/dark.png";
import minimalThemeReference from "../assets/minimal.png";

const allowedReferenceImageTypes = new Set(["image/png", "image/jpeg", "image/webp"]);
const maxReferenceImageBytes = 4 * 1024 * 1024;
const themeReferenceAssetMap = {
  Minimal: {
    assetUrl: minimalThemeReference,
    fileName: "minimal.png",
    mimeType: "image/png"
  },
  "Dark Pro": {
    assetUrl: darkThemeReference,
    fileName: "dark.png",
    mimeType: "image/png"
  },
  Creative: {
    assetUrl: creativeThemeReference,
    fileName: "creative.png",
    mimeType: "image/png"
  }
};
const themeReferenceCache = new Map();

function readFileAsDataUrl(file) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = () => {
      if (typeof reader.result !== "string") {
        reject(new Error("We couldn't read that image. Please try another file."));
        return;
      }

      resolve(reader.result);
    };

    reader.onerror = () => {
      reject(new Error("We couldn't read that image. Please try another file."));
    };

    reader.readAsDataURL(file);
  });
}

function readBlobAsDataUrl(blob) {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();

    reader.onload = () => {
      if (typeof reader.result !== "string") {
        reject(new Error("We couldn't process that built-in theme reference."));
        return;
      }

      resolve(reader.result);
    };

    reader.onerror = () => {
      reject(new Error("We couldn't process that built-in theme reference."));
    };

    reader.readAsDataURL(blob);
  });
}

export function formatReferenceImageSize(sizeBytes) {
  if (!Number.isFinite(sizeBytes) || sizeBytes <= 0) {
    return "";
  }

  if (sizeBytes < 1024 * 1024) {
    return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
  }

  return `${(sizeBytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getThemeReferencePreview(theme) {
  return themeReferenceAssetMap[theme] ?? themeReferenceAssetMap.Minimal;
}

export async function readReferenceImageFile(file) {
  if (!allowedReferenceImageTypes.has(file.type)) {
    throw new Error("Reference images must be PNG, JPG, or WebP files.");
  }

  if (file.size > maxReferenceImageBytes) {
    throw new Error("Reference images must be 4 MB or smaller.");
  }

  const dataUrl = await readFileAsDataUrl(file);
  const marker = "base64,";
  const markerIndex = dataUrl.indexOf(marker);

  if (markerIndex < 0) {
    throw new Error("We couldn't process that image. Please try another file.");
  }

  return {
    mimeType: file.type,
    base64Data: dataUrl.slice(markerIndex + marker.length),
    fileName: file.name,
    sizeBytes: file.size,
    notes: ""
  };
}

export async function getThemeReferenceImage(theme, notes = "") {
  const preview = getThemeReferencePreview(theme);
  const cacheKey = preview.fileName;

  let cached = themeReferenceCache.get(cacheKey);
  if (!cached) {
    cached = fetch(preview.assetUrl)
      .then(async (response) => {
        if (!response.ok) {
          throw new Error("We couldn't load that built-in theme reference.");
        }

        const blob = await response.blob();
        const dataUrl = await readBlobAsDataUrl(blob);
        const marker = "base64,";
        const markerIndex = dataUrl.indexOf(marker);

        if (markerIndex < 0) {
          throw new Error("We couldn't process that built-in theme reference.");
        }

        return {
          mimeType: preview.mimeType,
          base64Data: dataUrl.slice(markerIndex + marker.length),
          fileName: preview.fileName,
          sizeBytes: blob.size
        };
      })
      .catch((error) => {
        themeReferenceCache.delete(cacheKey);
        throw error;
      });

    themeReferenceCache.set(cacheKey, cached);
  }

  const builtInReference = await cached;
  return {
    ...builtInReference,
    notes: notes?.trim() || ""
  };
}

export function createReferenceImagePreviewUrl(referenceImage) {
  if (!referenceImage?.mimeType || !referenceImage?.base64Data) {
    return "";
  }

  return `data:${referenceImage.mimeType};base64,${referenceImage.base64Data}`;
}

function normaliseReferenceImage(referenceImage) {
  if (!referenceImage?.mimeType || !referenceImage?.base64Data) {
    return null;
  }

  return {
    mimeType: referenceImage.mimeType,
    base64Data: referenceImage.base64Data,
    fileName: referenceImage.fileName || "",
    sizeBytes: Number.isFinite(referenceImage.sizeBytes) ? referenceImage.sizeBytes : 0,
    notes: referenceImage.notes || ""
  };
}

export function toGenerateReferencePayload(referenceImage) {
  if (!referenceImage?.mimeType || !referenceImage?.base64Data) {
    return null;
  }

  return {
    mimeType: referenceImage.mimeType,
    base64Data: referenceImage.base64Data,
    fileName: referenceImage.fileName || "",
    notes: referenceImage.notes?.trim() || ""
  };
}

export async function getEffectiveReferenceImage(theme, customReferenceImage) {
  if (customReferenceImage?.mimeType && customReferenceImage?.base64Data) {
    return normaliseReferenceImage(customReferenceImage);
  }

  return await getThemeReferenceImage(theme);
}
