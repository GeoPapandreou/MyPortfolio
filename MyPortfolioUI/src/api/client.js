import axios from "axios";

const authExpiredEventName = "myportfolio:auth-expired";

const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "http://localhost:5000"
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem("myportfolio_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401 && localStorage.getItem("myportfolio_token")) {
      window.dispatchEvent(new Event(authExpiredEventName));
    }

    return Promise.reject(error);
  }
);

export default apiClient;
export { authExpiredEventName };
