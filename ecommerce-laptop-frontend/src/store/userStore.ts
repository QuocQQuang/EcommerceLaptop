import { User } from '@/types/api';
import { create } from 'zustand';

interface UserState {
    user: User | null;
    isLoading: boolean;

    // Actions
    setUser: (user: User | null) => void;
    setLoading: (isLoading: boolean) => void;
    clearUser: () => void;
    updateUser: (updates: Partial<User>) => void;
}

export const useUserStore = create<UserState>((set) => ({
    user: null,
    isLoading: false,

    setUser: (user: User | null) => {
        set({ user });
    },

    setLoading: (isLoading: boolean) => {
        set({ isLoading });
    },

    clearUser: () => {
        set({ user: null });
    },

    updateUser: (updates: Partial<User>) => {
        set(state => ({
            user: state.user ? { ...state.user, ...updates } : null
        }));
    },
}));