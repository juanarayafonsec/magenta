import React, { createContext, useContext, ReactNode } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { authenticationService } from '../services/authenticationService';

interface User {
  id: string;
  username: string;
  email: string;
}

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (user: User) => void;
  logout: () => void;
  checkAuthStatus: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const queryClient = useQueryClient();

  // Use React Query for auth status - always check on mount
  const { data: authStatus, isLoading, error } = useQuery({
    queryKey: ['authStatus'],
    queryFn: async () => {
      const authStatus = await authenticationService.getAuthStatus();
      return authStatus;
    },
    staleTime: 5 * 60 * 1000, // 5 minutes
    retry: (failureCount, error: any) => {
      // Don't retry on 401 errors (unauthorized)
      if (error?.response?.status === 401) {
        return false;
      }
      // Retry up to 2 times for other errors
      return failureCount < 2;
    },
    retryDelay: 1000, // 1 second delay between retries
  });

  // Derive user and authentication state from React Query data
  const user = authStatus?.isAuthenticated && authStatus.username && authStatus.email 
    ? {
        id: '', // We don't have ID from status endpoint
        username: authStatus.username,
        email: authStatus.email
      }
    : null;

  const isAuthenticated = !!user;

  const login = (userData: User) => {
    // Update React Query cache directly
    queryClient.setQueryData(['authStatus'], {
      isAuthenticated: true,
      username: userData.username,
      email: userData.email,
      authMethod: 'Cookie'
    });
  };

  const logout = async () => {
    // Clear any cookies by making a logout request
    try {
      await authenticationService.logout();
    } catch (error) {
      console.error('Logout error:', error);
    } finally {
      // Clear React Query cache
      queryClient.setQueryData(['authStatus'], {
        isAuthenticated: false
      });
    }
  };

  const checkAuthStatus = async () => {
    // Invalidate and refetch auth status
    await queryClient.invalidateQueries({ queryKey: ['authStatus'] });
  };

  const value: AuthContextType = {
    user,
    isAuthenticated,
    isLoading,
    login,
    logout,
    checkAuthStatus,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};
