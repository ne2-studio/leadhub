import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { FileText, Inbox } from 'lucide-react';
import { Form } from '../types';
import { useSubmissionStore } from '../store/useSubmissionStore';

interface DashboardProps {
  forms: Form[];
  totalForms: number;
  totalSubmissions: number;
}

export function Dashboard({ forms, totalForms, totalSubmissions }: DashboardProps) {
  const navigate = useNavigate();
  const { recentSubmissions, isRecentLoading, fetchRecentSubmissions } = useSubmissionStore();

  useEffect(() => {
    if (forms.length > 0) {
      fetchRecentSubmissions(forms);
    }
  }, [forms]);

  const formatDate = (val: string) =>
    new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(val));

  const formName = (formId: string | null) => forms.find((f) => f.id === formId)?.name ?? 'Unknown form';

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-medium tracking-tight text-text-primary">Dashboard</h2>
        <p className="text-xs text-text-secondary">Quick visibility into forms and submission activity.</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-6">
        <div className="card flex items-center gap-4">
          <div className="p-3 bg-surface-elevated text-ink">
            <FileText className="w-6 h-6" />
          </div>
          <div>
            <p className="text-xs uppercase tracking-widest text-text-secondary font-mono">Total Forms</p>
            <p className="text-2xl font-semibold text-text-primary">{totalForms}</p>
          </div>
        </div>

        <div className="card flex items-center gap-4">
          <div className="p-3 bg-surface-elevated text-ink">
            <Inbox className="w-6 h-6" />
          </div>
          <div>
            <p className="text-xs uppercase tracking-widest text-text-secondary font-mono">Total Submissions</p>
            <p className="text-2xl font-semibold text-text-primary">{totalSubmissions}</p>
          </div>
        </div>
      </div>

      <div className="card p-0 overflow-hidden">
        <div className="p-4 border-b border-border">
          <h3 className="text-sm font-semibold text-text-primary">Recent submissions</h3>
        </div>
        <table>
          <thead>
            <tr>
              <th>Form</th>
              <th>Preview</th>
              <th>Received</th>
            </tr>
          </thead>
          <tbody>
            {recentSubmissions.map((submission) => (
              <tr
                key={submission.id}
                className="cursor-pointer hover:bg-surface-elevated/40 transition-colors"
                onClick={() => submission.formId && navigate(`/forms/${submission.formId}/submissions/${submission.id}`)}
              >
                <td className="font-medium">{formName(submission.formId)}</td>
                <td className="text-text-secondary">{submission.preview}</td>
                <td className="font-mono text-xs text-text-secondary">{formatDate(submission.createdAt)}</td>
              </tr>
            ))}
            {!isRecentLoading && recentSubmissions.length === 0 && (
              <tr>
                <td colSpan={3} className="p-12 text-center text-text-secondary italic text-xs">
                  No submissions yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
