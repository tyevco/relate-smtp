import { create } from "zustand";
import { persist, createJSONStorage } from "zustand/middleware";
import AsyncStorage from "@react-native-async-storage/async-storage";
import { deleteApiKey } from "./secure-storage";

export interface Account {
  id: string;
  displayName: string;
  serverUrl: string;
  userEmail: string;
  apiKeyId: string;
  scopes: string[];
  createdAt: string;
  lastUsedAt: string;
  isActive: boolean;
}

interface AccountState {
  accounts: Account[];
  activeAccountId: string | null;

  // Actions
  addAccount: (account: Omit<Account, "isActive">) => void;
  removeAccount: (accountId: string) => Promise<void>;
  setActiveAccount: (accountId: string) => void;
  updateAccount: (accountId: string, updates: Partial<Account>) => void;
  updateLastUsed: (accountId: string) => void;
  getActiveAccount: () => Account | null;
}

export const useAccountStore = create<AccountState>()(
  persist(
    (set, get) => ({
      accounts: [],
      activeAccountId: null,

      addAccount: (account) => {
        set((state) => {
          // Deactivate all other accounts
          const updatedAccounts = state.accounts.map((a) => ({
            ...a,
            isActive: false,
          }));

          return {
            accounts: [
              ...updatedAccounts,
              { ...account, isActive: true },
            ],
            activeAccountId: account.id,
          };
        });
      },

      removeAccount: async (accountId) => {
        // Delete the API key from secure storage
        await deleteApiKey(accountId);

        set((state) => {
          const remainingAccounts = state.accounts.filter(
            (a) => a.id !== accountId
          );

          // If we removed the active account, activate the first remaining one
          let newActiveId = state.activeAccountId;
          if (state.activeAccountId === accountId) {
            newActiveId = remainingAccounts[0]?.id ?? null;
          }

          // Use immutable pattern - create new objects instead of mutating
          const updatedAccounts = remainingAccounts.map((a) => ({
            ...a,
            isActive: a.id === newActiveId,
          }));

          return {
            accounts: updatedAccounts,
            activeAccountId: newActiveId,
          };
        });
      },

      setActiveAccount: (accountId) => {
        set((state) => ({
          accounts: state.accounts.map((a) => ({
            ...a,
            isActive: a.id === accountId,
          })),
          activeAccountId: accountId,
        }));
      },

      updateAccount: (accountId, updates) => {
        set((state) => ({
          accounts: state.accounts.map((a) =>
            a.id === accountId ? { ...a, ...updates } : a
          ),
        }));
      },

      updateLastUsed: (accountId) => {
        set((state) => ({
          accounts: state.accounts.map((a) =>
            a.id === accountId
              ? { ...a, lastUsedAt: new Date().toISOString() }
              : a
          ),
        }));
      },

      getActiveAccount: () => {
        const state = get();
        return (
          state.accounts.find((a) => a.id === state.activeAccountId) ?? null
        );
      },
    }),
    {
      name: "relate-accounts",
      storage: createJSONStorage(() => AsyncStorage),
      partialize: (state) => ({
        accounts: state.accounts,
        activeAccountId: state.activeAccountId,
      }),
    }
  )
);

// Selector hooks for convenience
export function useActiveAccount() {
  return useAccountStore((state) =>
    state.accounts.find((a) => a.id === state.activeAccountId) ?? null
  );
}

export function useAccounts() {
  return useAccountStore((state) => state.accounts);
}

export function useHasAccounts() {
  return useAccountStore((state) => state.accounts.length > 0);
}
