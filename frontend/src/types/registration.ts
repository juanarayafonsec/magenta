export interface RegisterUserRequest {
  username: string;
  email: string;
  password: string;
  confirmPassword: string;
}

export interface RegisterUserResponse {
  success: boolean;
  userId?: string;
  username?: string;
  email?: string;
  errors: string[];
}
