import { create } from 'zustand';
import { Form } from '../types';
import { api, FormInput } from '../api';

interface FormStore {
  forms: Form[];
  isLoading: boolean;
  error: string | null;

  fetchData: () => Promise<void>;

  getForm: (id: string) => Promise<Form>;
  createForm: (input: FormInput) => Promise<Form>;
  updateForm: (id: string, input: FormInput) => Promise<Form>;
  deleteForm: (id: string) => Promise<void>;
}

export const useFormStore = create<FormStore>((set) => ({
  forms: [],
  isLoading: false,
  error: null,

  fetchData: async () => {
    set({ isLoading: true, error: null });
    try {
      const [forms] = await Promise.all([
        api.forms.getAll(),
      ]);

      set({ forms, isLoading: false });
    } catch (error) {
      set({ error: (error as Error).message, isLoading: false });
    }
  },

  getForm: async (id) => api.forms.get(id),

  createForm: async (input) => {
    const form = await api.forms.create(input);
    set((state) => ({ forms: [form, ...state.forms] }));
    return form;
  },

  updateForm: async (id, input) => {
    const form = await api.forms.update(id, input);
    set((state) => ({ forms: state.forms.map((f) => (f.id === id ? form : f)) }));
    return form;
  },

  deleteForm: async (id) => {
    await api.forms.delete(id);
    set((state) => ({ forms: state.forms.filter((f) => f.id !== id) }));
  },
}));
