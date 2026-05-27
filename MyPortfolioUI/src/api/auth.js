import apiClient from "./client";

export async function registerAccount(payload) {
  const { data } = await apiClient.post("/api/auth/register", payload);
  return data;
}

export async function loginAccount(payload) {
  const { data } = await apiClient.post("/api/auth/login", payload);
  return data;
}
