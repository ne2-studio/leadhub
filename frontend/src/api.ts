import { Form, PaginatedSubmissions, Submission } from './types';

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
  const body = await res.json().catch(() => null);
  if (!body || !body.success) {
    throw new Error(body?.error?.message || `API Error: ${res.status}`);
  }
  return body.data;
};

export interface FormInput {
  name: string;
  slug: string;
  description?: string | null;
  notificationsEnabled: boolean;
  notificationEmail?: string | null;
  thankYouUrl?: string | null;
}

export const api = {
  forms: {
    getAll: async (): Promise<Form[]> =>
      fetch(`${API_BASE_URL}/api/admin/forms`, { headers: getHeaders() })
        .then(handleResponse)
        .then(data => data.map((f: any) => new Form(f))),

    get: async (id: string): Promise<Form> =>
      fetch(`${API_BASE_URL}/api/admin/forms/${id}`, { headers: getHeaders() })
        .then(handleResponse)
        .then(data => new Form(data)),

    create: async (input: FormInput): Promise<Form> =>
      fetch(`${API_BASE_URL}/api/admin/forms`, {
        method: 'POST',
        headers: getHeaders(),
        body: JSON.stringify(input),
      }).then(handleResponse).then(data => new Form(data)),

    update: async (id: string, input: FormInput): Promise<Form> =>
      fetch(`${API_BASE_URL}/api/admin/forms/${id}`, {
        method: 'PUT',
        headers: getHeaders(),
        body: JSON.stringify(input),
      }).then(handleResponse).then(data => new Form(data)),

    delete: async (id: string): Promise<void> =>
      fetch(`${API_BASE_URL}/api/admin/forms/${id}`, {
        method: 'DELETE',
        headers: getHeaders(),
      }).then(handleResponse),
  },

  submissions: {
    list: async (formId: string, page = 1, pageSize = 50): Promise<PaginatedSubmissions> =>
      fetch(`${API_BASE_URL}/api/admin/forms/${formId}/submissions?page=${page}&pageSize=${pageSize}`, {
        headers: getHeaders(),
      })
        .then(handleResponse)
        .then(data => new PaginatedSubmissions({
          ...data,
          items: data.items.map((s: any) => new Submission(s)),
        })),

    get: async (id: string): Promise<Submission> =>
      fetch(`${API_BASE_URL}/api/admin/submissions/${id}`, { headers: getHeaders() })
        .then(handleResponse)
        .then(data => new Submission(data)),
  },
};
