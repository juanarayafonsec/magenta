import axios from 'axios';
import type { AxiosInstance, AxiosResponse, AxiosError } from 'axios';

// Create axios instance with base configuration
const api: AxiosInstance = axios.create({
  baseURL: 'https://localhost:7017/api', // Registration API base URL (HTTPS)
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Enable cookies for authentication
});

// Request interceptor
api.interceptors.request.use(
  (config) => {
    // Add any auth tokens or other headers here if needed
    console.log(`Making ${config.method?.toUpperCase()} request to: ${config.url}`);
    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor
api.interceptors.response.use(
  (response: AxiosResponse) => {
    console.log(`Response received from ${response.config.url}:`, response.status);
    return response;
  },
  (error: AxiosError) => {
    console.error('Response error:', error);
    
    // Handle different error types
    if (error.response) {
      // Server responded with error status
      const { status, data } = error.response;
      console.error(`Server error ${status}:`, data);
    } else if (error.request) {
      // Request was made but no response received
      console.error('No response received:', error.request);
    } else {
      // Something else happened
      console.error('Error setting up request:', error.message);
    }
    
    return Promise.reject(error);
  }
);

export default api;
