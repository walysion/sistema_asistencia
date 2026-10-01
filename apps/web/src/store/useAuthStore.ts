import { create } from 'zustand';

interface AuthState {
  email: string | null;
  rol: string | null;
  isAuthenticated: boolean;
  setAuth: (email: string, rol: string, accessToken: string, refreshToken: string) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  email: null,
  rol: null,
  isAuthenticated: false,

  setAuth: (email, rol, accessToken, refreshToken) => {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
    set({ email, rol, isAuthenticated: true });
  },

  logout: () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    set({ email: null, rol: null, isAuthenticated: false });
  },
}));