export interface LoginRequest {
  usernameOrEmail: string;
  password: string;
  rememberMe: boolean;
}

export interface UserInfo {
  username: string;
  email: string;
  createdAt: string;
}

export interface LoginResponse {
  success: boolean;
  expiresIn: number;
  authMethod: string;
  user?: UserInfo;
  errors: string[];
}

export interface AuthStatusResponse {
  isAuthenticated: boolean;
  username?: string;
  email?: string;
  loginTime?: string;
  authMethod?: string;
}

export interface LogoutResponse {
  success: boolean;
  message: string;
}
