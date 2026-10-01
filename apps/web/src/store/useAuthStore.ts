import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  token: string | null;
  email: string | null;
  rol: string | null;
  empresaId: number | null;
  setAuth: (token: string, email: string, rol: string, empresaId?: number) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      token: null,
      email: null,
      rol: null,
      empresaId: null,
      setAuth: (token, email, rol, empresaId) => set({ token, email, rol, empresaId: empresaId ?? null }),
      logout: () => set({ token: null, email: null, rol: null, empresaId: null }),
    }),
    {
      name: 'auth-storage',
    }
  )
);