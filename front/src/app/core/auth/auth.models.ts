export interface AuthStatus {
  initialized: boolean;
}

export interface SetupRequest {
  username: string;
  password: string;
  passwordConfirmation: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
}

/** Session conservée dans le sessionStorage : effacée à la fermeture de l'onglet (TS §3.6). */
export interface Session {
  username: string;
  token: string;
  expiresAt: string;
}
