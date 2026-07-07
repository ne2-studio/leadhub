import { useEffect, useState } from 'react';
import { X } from 'lucide-react';
import { useFormStore } from '../store/useFormStore';
import { Form } from '../types';

interface FormEditorProps {
  form: Form | null;
  onClose: () => void;
}

const slugify = (value: string) =>
  value
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');

export function FormEditor({ form, onClose }: FormEditorProps) {
  const { getForm, createForm, updateForm } = useFormStore();
  const isEditing = form !== null;

  const [isLoading, setIsLoading] = useState(isEditing);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [name, setName] = useState('');
  const [slug, setSlug] = useState('');
  const [slugTouched, setSlugTouched] = useState(isEditing);
  const [description, setDescription] = useState('');
  const [notificationsEnabled, setNotificationsEnabled] = useState(false);
  const [notificationEmail, setNotificationEmail] = useState('');
  const [thankYouUrl, setThankYouUrl] = useState('');

  useEffect(() => {
    if (!form) return;

    setIsLoading(true);
    getForm(form.id)
      .then((full) => {
        setName(full.name);
        setSlug(full.slug);
        setDescription(full.description ?? '');
        setNotificationsEnabled(full.notificationsEnabled);
        setNotificationEmail(full.notificationEmail ?? '');
        setThankYouUrl(full.thankYouUrl ?? '');
      })
      .catch((err) => setError((err as Error).message))
      .finally(() => setIsLoading(false));
  }, [form]);

  const handleNameChange = (value: string) => {
    setName(value);
    if (!slugTouched) setSlug(slugify(value));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!name.trim()) return setError('Name is required.');
    if (!slug.trim()) return setError('Slug is required.');
    if (notificationsEnabled && !notificationEmail.trim()) {
      return setError('Notification email is required when notifications are enabled.');
    }

    const input = {
      name: name.trim(),
      slug: slug.trim(),
      description: description.trim() || null,
      notificationsEnabled,
      notificationEmail: notificationEmail.trim() || null,
      thankYouUrl: thankYouUrl.trim() || null,
    };

    setIsSaving(true);
    try {
      if (form) {
        await updateForm(form.id, input);
      } else {
        await createForm(input);
      }
      onClose();
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <div className="fixed inset-0 bg-ink/60 flex items-center justify-center p-4 z-50">
      <div className="card w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-base font-semibold text-text-primary">{isEditing ? 'Edit form' : 'New form'}</h3>
          <button className="btn-icon-muted" onClick={onClose}>
            <X className="w-4 h-4" />
          </button>
        </div>

        {isLoading ? (
          <p className="text-sm text-text-secondary">Loading…</p>
        ) : (
          <form onSubmit={handleSubmit} className="flex flex-col gap-4">
            {error && (
              <p className="text-xs text-ink bg-surface-subtle border border-border p-3">{error}</p>
            )}

            <label className="flex flex-col gap-1">
              <span className="text-xs uppercase tracking-wide text-text-secondary font-mono">Name</span>
              <input
                type="text"
                value={name}
                onChange={(e) => handleNameChange(e.target.value)}
                placeholder="Contact Form"
                className="p-2 text-sm"
              />
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs uppercase tracking-wide text-text-secondary font-mono">Slug</span>
              <input
                type="text"
                value={slug}
                onChange={(e) => {
                  setSlugTouched(true);
                  setSlug(e.target.value);
                }}
                placeholder="contact"
                className="p-2 text-sm font-mono"
              />
              <span className="text-[11px] text-text-secondary">
                Public endpoint: <code className="font-mono">/api/forms/{slug || '…'}/submit</code>
              </span>
            </label>

            <label className="flex flex-col gap-1">
              <span className="text-xs uppercase tracking-wide text-text-secondary font-mono">Description (optional)</span>
              <textarea
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                rows={2}
                className="p-2 text-sm"
              />
            </label>

            <label className="flex items-center gap-2">
              <input
                type="checkbox"
                checked={notificationsEnabled}
                onChange={(e) => setNotificationsEnabled(e.target.checked)}
                className="w-4 h-4"
              />
              <span className="text-sm text-text-primary">Send email notification on new submission</span>
            </label>

            {notificationsEnabled && (
              <label className="flex flex-col gap-1">
                <span className="text-xs uppercase tracking-wide text-text-secondary font-mono">Notification email</span>
                <input
                  type="email"
                  value={notificationEmail}
                  onChange={(e) => setNotificationEmail(e.target.value)}
                  placeholder="owner@example.com"
                  className="p-2 text-sm"
                />
              </label>
            )}

            <label className="flex flex-col gap-1">
              <span className="text-xs uppercase tracking-wide text-text-secondary font-mono">Thank-you URL (optional)</span>
              <input
                type="url"
                value={thankYouUrl}
                onChange={(e) => setThankYouUrl(e.target.value)}
                placeholder="https://example.com/thank-you"
                className="p-2 text-sm"
              />
            </label>

            <div className="flex items-center justify-end gap-3 mt-2">
              <button type="button" className="btn btn-hollow" onClick={onClose} disabled={isSaving}>
                Cancel
              </button>
              <button type="submit" className="btn btn-accent" disabled={isSaving}>
                {isSaving ? 'Saving…' : 'Save'}
              </button>
            </div>
          </form>
        )}
      </div>
    </div>
  );
}
