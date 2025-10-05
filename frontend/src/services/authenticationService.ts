import authApi from './authApi';
import type { LoginRequest, LoginResponse, AuthStatusResponse, LogoutResponse } from '../types/authentication';

export const authenticationService = {
  async login(credentials: LoginRequest): Promise<LoginResponse> {
    try {
      // Use our custom JSON login endpoint
      const response = await authApi.post('/auth/login', {
        usernameOrEmail: credentials.usernameOrEmail, // Use usernameOrEmail field for both email and username
        password: credentials.password
      });
      
      return {
        success: true,
        expiresIn: 1800, // 30 minutes
        authMethod: 'Cookie',
        user: {
          username: response.data.user.username || '',
          email: response.data.user.email || '',
          createdAt: new Date().toISOString()
        }
      };
    } catch (error: any) {
      console.log('Login failed:', error.message);
      return {
        success: false,
        expiresIn: 0,
        authMethod: 'Cookie',
        errors: [error.response?.data?.message || 'Login failed. Please check your credentials.']
      };
    }
  },

  async getAuthStatus(): Promise<AuthStatusResponse> {
    try {
      const response = await authApi.get<AuthStatusResponse>('/auth/status');
      return response.data;
    } catch (error: any) {
      // Handle 401 errors gracefully - this is expected when user is not authenticated
      if (error?.response?.status === 401) {
        return { isAuthenticated: false };
      }
      // For other errors, still return false but log the error
      console.error('Auth status check failed:', error);
      return { isAuthenticated: false };
    }
  },

  async refresh(): Promise<{ success: boolean; message?: string }> {
    try {
      // For refresh, we can just check auth status
      const authStatus = await this.getAuthStatus();
      if (authStatus.isAuthenticated) {
        return { success: true, message: 'Session is valid' };
      } else {
        return { success: false, message: 'Session expired' };
      }
    } catch (error: any) {
      console.log('Refresh failed:', error.message);
      return { success: false, message: 'Failed to refresh session' };
    }
  },

  async logout(): Promise<LogoutResponse> {
    try {
      const response = await authApi.post<LogoutResponse>('/auth/logout');
      return response.data;
    } catch (error: any) {
      console.log('Logout API call failed, but continuing with local logout:', error.message);
      // Even if the API call fails, we should still return success
      // because the frontend will clear the local state anyway
      return {
        success: true,
        message: 'Logged out successfully'
      };
    }
  }
};
