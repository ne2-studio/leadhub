import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, ChevronLeft, ChevronRight } from 'lucide-react';
import { Form, SubmissionStatus } from '../types';
import { useSubmissionStore } from '../store/useSubmissionStore';
import { SubmissionDetail } from './SubmissionDetail';

interface SubmissionsProps {
  forms: Form[];
}

const PAGE_SIZE = 50;

const STATUS_FILTERS: { label: string; value: SubmissionStatus | undefined }[] = [
  { label: 'All', value: undefined },
  { label: 'Ham', value: 'Ham' },
  { label: 'Suspected spam', value: 'SuspectedSpam' },
  { label: 'Spam', value: 'Spam' },
  { label: 'Pending review', value: 'PendingReview' },
];

const STATUS_TAG_CLASS: Record<SubmissionStatus, string> = {
  Ham: 'tag-on',
  SuspectedSpam: 'tag-warning',
  Spam: 'tag-danger',
  PendingReview: 'tag-off',
};

const STATUS_LABEL: Record<SubmissionStatus, string> = {
  Ham: 'Ham',
  SuspectedSpam: 'Suspected spam',
  Spam: 'Spam',
  PendingReview: 'Pending review',
};

export function Submissions({ forms }: SubmissionsProps) {
  const { formId, submissionId } = useParams<{ formId: string; submissionId?: string }>();
  const navigate = useNavigate();
  const {
    submissions, page, pageSize, totalItems, isLoading,
    selectedSubmission, isDetailLoading,
    fetchSubmissions, fetchSubmissionDetail, clearSelectedSubmission,
  } = useSubmissionStore();
  const [statusFilter, setStatusFilter] = useState<SubmissionStatus | undefined>(undefined);

  const form = forms.find((f) => f.id === formId);

  useEffect(() => {
    if (formId) fetchSubmissions(formId, 1, PAGE_SIZE, statusFilter);
  }, [formId, statusFilter]);

  useEffect(() => {
    if (submissionId) fetchSubmissionDetail(submissionId);
  }, [submissionId]);

  const formatDate = (val: string) =>
    new Intl.DateTimeFormat('en-US', { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(val));

  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));

  const goToPage = (nextPage: number) => {
    if (formId) fetchSubmissions(formId, nextPage, PAGE_SIZE, statusFilter);
  };

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-1">
        <button
          className="flex items-center gap-1 text-xs text-text-secondary hover:text-text-primary transition-colors w-fit"
          onClick={() => navigate('/forms')}
        >
          <ArrowLeft className="w-3.5 h-3.5" /> Back to forms
        </button>
        <h2 className="text-lg font-medium tracking-tight text-text-primary">
          {form ? `Submissions — ${form.name}` : 'Submissions'}
        </h2>
        <p className="text-xs text-text-secondary">{totalItems} total submission{totalItems === 1 ? '' : 's'}.</p>
      </div>

      <div className="flex gap-2">
        {STATUS_FILTERS.map(({ label, value }) => (
          <button
            key={label}
            className={`tag cursor-pointer ${value === statusFilter ? (value ? STATUS_TAG_CLASS[value] : 'tag-on') : 'tag-off'}`}
            onClick={() => setStatusFilter(value)}
          >
            {label}
          </button>
        ))}
      </div>

      <div className="card p-0 overflow-hidden">
        <table>
          <thead>
            <tr>
              <th>Received</th>
              <th>IP address</th>
              <th>Preview</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {submissions.map((submission) => (
              <tr
                key={submission.id}
                className="cursor-pointer hover:bg-surface-elevated/40 transition-colors"
                onClick={() => navigate(`/forms/${formId}/submissions/${submission.id}`)}
              >
                <td className="font-mono text-xs text-text-secondary">{formatDate(submission.createdAt)}</td>
                <td className="font-mono text-xs text-text-secondary">{submission.ipAddress ?? '—'}</td>
                <td className="text-text-primary">{submission.preview}</td>
                <td>
                  <span className={`tag ${STATUS_TAG_CLASS[submission.status]}`}>{STATUS_LABEL[submission.status]}</span>
                </td>
              </tr>
            ))}
            {!isLoading && submissions.length === 0 && (
              <tr>
                <td colSpan={4} className="p-12 text-center text-text-secondary italic text-xs">
                  No submissions yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>

        {totalPages > 1 && (
          <div className="flex items-center justify-between p-4 border-t border-border">
            <span className="text-xs text-text-secondary font-mono">Page {page} of {totalPages}</span>
            <div className="flex gap-2">
              <button className="btn-icon-muted" disabled={page <= 1} onClick={() => goToPage(page - 1)}>
                <ChevronLeft className="w-4 h-4" />
              </button>
              <button className="btn-icon-muted" disabled={page >= totalPages} onClick={() => goToPage(page + 1)}>
                <ChevronRight className="w-4 h-4" />
              </button>
            </div>
          </div>
        )}
      </div>

      {submissionId && (
        <SubmissionDetail
          submission={selectedSubmission}
          isLoading={isDetailLoading}
          onClose={() => {
            clearSelectedSubmission();
            navigate(`/forms/${formId}/submissions`);
          }}
        />
      )}
    </div>
  );
}
