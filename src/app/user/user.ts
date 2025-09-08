export interface User {
  firstName: string;
  lastName: string;
  email: string;
}

export interface LoginResponse {
  token: string;
}

export interface UserCredentials {
  userName: string;
  password: string;
}
