import axios from 'axios';
import type { AxiosInstance, AxiosResponse, AxiosError } from 'axios';

// Create axios instance for Authentication API
const authApi: AxiosInstance = axios.create({
  baseURL: 'https://localhost:7018/api', // Authentication API base URL (HTTPS)
  timeout: 10000,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true, // Enable cookies for authentication
});

// Request interceptor
authApi.interceptors.request.use(
  (config) => {
    console.log(`Making ${config.method?.toUpperCase()} request to: ${config.url}`);
    return config;
  },
  (error) => {
    console.error('Request error:', error);
    return Promise.reject(error);
  }
);

// Response interceptor
authApi.interceptors.response.use(
  (response: AxiosResponse) => {
    console.log(`Response received from ${response.config.url}:`, response.status);
    return response;
  },
  (error: AxiosError) => {
    // Handle different error types
    if (error.response) {
      // Server responded with error status
      const { status, data } = error.response;
      
      // Don't log 401 errors as errors since they're expected when not authenticated
      if (status === 401) {
        console.log(`Auth check: User not authenticated (${status})`);
      } else {
        console.error(`Server error ${status}:`, data);
      }
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

export default authApi;
