import { create } from 'zustand';
import { Task } from '../types';
import { api } from '../api';

interface TaskStore {
  tasks: Task[];
  isLoading: boolean;
  error: string | null;

  fetchData: () => Promise<void>;

  addTask: (task: { title: string }) => Promise<Task>;
  deleteTask: (id: string) => Promise<void>;
}

export const useTaskStore = create<TaskStore>((set) => ({
  tasks: [],
  isLoading: false,
  error: null,

  fetchData: async () => {
    set({ isLoading: true, error: null });
    try {
      const [tasks] = await Promise.all([
        api.tasks.getAll(),
      ]);

      set({ tasks, isLoading: false });
    } catch (error) {
      set({ error: (error as Error).message, isLoading: false });
    }
  },

  addTask: async (task) => {
    const newTask = await api.tasks.create(task);
    set((state) => ({ tasks: [newTask, ...state.tasks] }));
    return newTask;
  },

  deleteTask: async (id) => {
    await api.tasks.delete(id);
    set((state) => ({ tasks: state.tasks.filter((t) => t.id !== id) }));
  },
}));
