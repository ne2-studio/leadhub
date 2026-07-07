import { Task } from './types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5050';

let _accessToken: string | undefined;

export function setAccessToken(token: string | undefined) {
  _accessToken = token;
}

const getHeaders = (): Record<string, string> => ({
  'Content-Type': 'application/json',
  'Authorization': `Bearer ${_accessToken}`,
});

const handleResponse = async (res: Response) => {
  if (!res.ok) {
    const error = await res.json().catch(() => ({ message: 'Unknown error' }));
    throw new Error(error.error || error.message || `API Error: ${res.status}`);
  }
  if (res.status === 204) return undefined;
  return res.json();
};

export const api = {
  tasks: {
    getAll: async (): Promise<Task[]> =>
      fetch(`${API_BASE_URL}/api/tasks?take=100`, { headers: getHeaders() }).then(handleResponse).then(data => data.map((t: any) => new Task(t))),
    create: async (task: { title: string }): Promise<Task> =>
      fetch(`${API_BASE_URL}/api/tasks`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(task),
      }).then(handleResponse).then(data => new Task(data)),
    delete: async (id: string): Promise<void> =>
      fetch(`${API_BASE_URL}/api/tasks/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      }).then(handleResponse),
  },
};
