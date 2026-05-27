async function readErrorPayload(data) {
  if (!data) {
    return null;
  }

  if (typeof Blob !== "undefined" && data instanceof Blob) {
    const text = await data.text();
    if (!text) {
      return null;
    }

    try {
      return JSON.parse(text);
    } catch {
      return { message: text };
    }
  }

  return data;
}

function collectValidationMessages(errors) {
  if (!errors || typeof errors !== "object") {
    return [];
  }

  return [...new Set(
    Object.values(errors)
      .flatMap((messages) => (Array.isArray(messages) ? messages : []))
      .map((message) => String(message || "").trim())
      .filter(Boolean)
  )];
}

export async function getApiErrorMessage(error, fallbackMessage) {
  const payload = await readErrorPayload(error?.response?.data);
  const validationMessages = collectValidationMessages(payload?.errors);

  if (validationMessages.length > 0) {
    return validationMessages.join(" ");
  }

  if (typeof payload?.message === "string" && payload.message.trim()) {
    return payload.message.trim();
  }

  if (error instanceof Error && error.message.trim()) {
    return error.message.trim();
  }

  return fallbackMessage;
}
