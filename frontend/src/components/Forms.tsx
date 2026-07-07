import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Inbox, Pencil, Plus, Trash2 } from 'lucide-react';
import { useFormStore } from '../store/useFormStore';
import { Form } from '../types';
import { FormEditor } from './FormEditor';

export function Forms() {
  const { forms, deleteForm } = useFormStore();
  const navigate = useNavigate();
  const [editingForm, setEditingForm] = useState<Form | 'new' | null>(null);

  const formatDate = (val: string | null) =>
    val ? new Intl.DateTimeFormat('en-US', { dateStyle: 'medium' }).format(new Date(val)) : '—';

  const handleDelete = async (form: Form) => {
    if (!window.confirm(`Delete "${form.name}"? Its submissions will be deleted too.`)) return;
    await deleteForm(form.id);
  };

  return (
    <div className="flex flex-col gap-8">
      <div className="flex items-center justify-between">
        <div className="flex flex-col gap-1">
          <h2 className="text-lg font-medium tracking-tight text-text-primary">Forms</h2>
          <p className="text-xs text-text-secondary">Create and manage the forms your websites submit to.</p>
        </div>
        <button className="btn btn-primary" onClick={() => setEditingForm('new')}>
          <Plus className="w-3.5 h-3.5" /> New form
        </button>
      </div>

      <div className="card p-0 overflow-hidden">
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Slug</th>
              <th>Notifications</th>
              <th>Submissions</th>
              <th>Last submission</th>
              <th className="text-right">Actions</th>
            </tr>
          </thead>
          <tbody>
            {forms.map((form) => (
              <tr key={form.id} className="hover:bg-surface-elevated/20 transition-colors">
                <td className="font-medium">{form.name}</td>
                <td className="font-mono text-xs text-text-secondary">{form.slug}</td>
                <td>
                  <span className={`tag ${form.notificationsEnabled ? 'tag-on' : 'tag-off'}`}>
                    {form.notificationsEnabled ? 'Enabled' : 'Disabled'}
                  </span>
                </td>
                <td>{form.submissionCount}</td>
                <td className="font-mono text-xs text-text-secondary">{formatDate(form.lastSubmissionAt)}</td>
                <td>
                  <div className="flex items-center justify-end gap-1">
                    <button
                      className="btn-icon-muted"
                      title="View submissions"
                      onClick={() => navigate(`/forms/${form.id}/submissions`)}
                    >
                      <Inbox className="w-3.5 h-3.5" />
                    </button>
                    <button className="btn-icon-muted" title="Edit form" onClick={() => setEditingForm(form)}>
                      <Pencil className="w-3.5 h-3.5" />
                    </button>
                    <button className="btn-icon-muted" title="Delete form" onClick={() => handleDelete(form)}>
                      <Trash2 className="w-3.5 h-3.5" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
            {forms.length === 0 && (
              <tr>
                <td colSpan={6} className="p-12 text-center text-text-secondary italic text-xs">
                  No forms yet. Create one to start receiving submissions.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>

      {editingForm !== null && (
        <FormEditor form={editingForm === 'new' ? null : editingForm} onClose={() => setEditingForm(null)} />
      )}
    </div>
  );
}
