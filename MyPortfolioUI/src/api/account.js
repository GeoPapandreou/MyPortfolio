import apiClient from "./client";

export async function getAccountSettings() {
  const { data } = await apiClient.get("/api/account");
  return data;
}

export async function saveAccountSettings(payload) {
  const { data } = await apiClient.put("/api/account", payload);
  return data;
}

export async function deleteAccount() {
  await apiClient.delete("/api/account");
}
