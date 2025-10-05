import api from './api';
import type { RegisterUserRequest, RegisterUserResponse } from '../types/registration';

export const registrationService = {
  async registerUser(userData: RegisterUserRequest): Promise<RegisterUserResponse> {
    try {
      const response = await api.post<RegisterUserResponse>('/auth/register', userData);
      return response.data;
    } catch (error: any) {
      // Handle axios error
      if (error.response?.data) {
        return error.response.data;
      }
      
      // Handle network or other errors
      return {
        success: false,
        errors: ['Network error. Please check your connection and try again.']
      };
    }
  },

  async getCurrentUser(): Promise<{ success: boolean; user?: any; error?: string }> {
    try {
      const response = await api.get('/auth/me');
      return { success: true, user: response.data };
    } catch (error: any) {
      return { 
        success: false, 
        error: error.response?.data?.message || 'Failed to get user data' 
      };
    }
  }
};
