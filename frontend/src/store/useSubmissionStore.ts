import { create } from 'zustand';
import { Form, Submission } from '../types';
import { api } from '../api';

interface SubmissionStore {
  submissions: Submission[];
  page: number;
  pageSize: number;
  totalItems: number;
  isLoading: boolean;
  error: string | null;

  selectedSubmission: Submission | null;
  isDetailLoading: boolean;

  recentSubmissions: Submission[];
  isRecentLoading: boolean;

  fetchSubmissions: (formId: string, page?: number, pageSize?: number) => Promise<void>;
  fetchSubmissionDetail: (id: string) => Promise<void>;
  clearSelectedSubmission: () => void;
  fetchRecentSubmissions: (forms: Form[], limit?: number) => Promise<void>;
}

export const useSubmissionStore = create<SubmissionStore>((set) => ({
  submissions: [],
  page: 1,
  pageSize: 50,
  totalItems: 0,
  isLoading: false,
  error: null,

  selectedSubmission: null,
  isDetailLoading: false,

  recentSubmissions: [],
  isRecentLoading: false,

  fetchSubmissions: async (formId, page = 1, pageSize = 50) => {
    set({ isLoading: true, error: null });
    try {
      const result = await api.submissions.list(formId, page, pageSize);
      set({
        submissions: result.items,
        page: result.page,
        pageSize: result.pageSize,
        totalItems: result.totalItems,
        isLoading: false,
      });
    } catch (error) {
      set({ error: (error as Error).message, isLoading: false });
    }
  },

  fetchSubmissionDetail: async (id) => {
    set({ isDetailLoading: true });
    try {
      const submission = await api.submissions.get(id);
      set({ selectedSubmission: submission, isDetailLoading: false });
    } catch (error) {
      set({ error: (error as Error).message, isDetailLoading: false });
    }
  },

  clearSelectedSubmission: () => set({ selectedSubmission: null }),

  fetchRecentSubmissions: async (forms, limit = 8) => {
    set({ isRecentLoading: true });
    try {
      const perFormResults = await Promise.all(
        forms.map(async (form) => {
          const result = await api.submissions.list(form.id, 1, limit).catch(() => null);
          return result?.items.map((item) => new Submission({ ...item, formId: form.id })) ?? [];
        }),
      );

      const merged = perFormResults
        .flat()
        .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
        .slice(0, limit);

      set({ recentSubmissions: merged, isRecentLoading: false });
    } catch (error) {
      set({ error: (error as Error).message, isRecentLoading: false });
    }
  },
}));
