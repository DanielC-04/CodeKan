export interface AuthUser {
  id: string;
  email: string;
  role: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  accessTokenExpiresAt: string;
  user: AuthUser;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
}
