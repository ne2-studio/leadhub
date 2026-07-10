import { X } from 'lucide-react';
import { Submission, SubmissionStatus } from '../types';

interface SubmissionDetailProps {
  submission: Submission | null;
  isLoading: boolean;
  onClose: () => void;
}

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

const REASON_LABEL: Record<string, string> = {
  honeypot_filled: 'Honeypot field was filled',
  message_contains_url: 'Message contains a URL',
  suspicious_keyword: 'Message contains a suspicious keyword',
  suspicious_name_pattern: 'Name looks machine-generated',
  message_too_long: 'Message is excessively long',
};

export function SubmissionDetail({ submission, isLoading, onClose }: SubmissionDetailProps) {
  const formatDate = (val: string) =>
    new Intl.DateTimeFormat('en-US', { dateStyle: 'full', timeStyle: 'medium' }).format(new Date(val));

  return (
    <div className="fixed inset-0 bg-ink/60 flex items-center justify-center p-4 z-50">
      <div className="card w-full max-w-lg max-h-[90vh] overflow-y-auto">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-base font-semibold text-text-primary">Submission</h3>
          <button className="btn-icon-muted" onClick={onClose}>
            <X className="w-4 h-4" />
          </button>
        </div>

        {isLoading || !submission ? (
          <p className="text-sm text-text-secondary">Loading…</p>
        ) : (
          <div className="flex flex-col gap-6">
            <dl className="grid grid-cols-[auto_1fr] gap-x-4 gap-y-2 text-sm">
              <dt className="text-text-secondary font-mono text-xs uppercase tracking-wide">Status</dt>
              <dd>
                <span className={`tag ${STATUS_TAG_CLASS[submission.status]}`}>{STATUS_LABEL[submission.status]}</span>
              </dd>

              <dt className="text-text-secondary font-mono text-xs uppercase tracking-wide">Spam score</dt>
              <dd className="text-text-primary font-mono">{submission.spamScore}</dd>

              <dt className="text-text-secondary font-mono text-xs uppercase tracking-wide">Received</dt>
              <dd className="text-text-primary">{formatDate(submission.createdAt)}</dd>

              <dt className="text-text-secondary font-mono text-xs uppercase tracking-wide">IP address</dt>
              <dd className="text-text-primary font-mono">{submission.ipAddress ?? '—'}</dd>

              <dt className="text-text-secondary font-mono text-xs uppercase tracking-wide">User agent</dt>
              <dd className="text-text-primary break-words">{submission.userAgent ?? '—'}</dd>
            </dl>

            {submission.spamReasons.length > 0 && (
              <div>
                <p className="text-xs uppercase tracking-wide text-text-secondary font-mono mb-2">Reasons</p>
                <ul className="flex flex-col gap-1 text-sm text-text-primary list-disc list-inside">
                  {submission.spamReasons.map((reason) => (
                    <li key={reason}>{REASON_LABEL[reason] ?? reason}</li>
                  ))}
                </ul>
              </div>
            )}

            <div>
              <p className="text-xs uppercase tracking-wide text-text-secondary font-mono mb-2">Payload</p>
              <div className="border border-border bg-surface-subtle">
                <table>
                  <tbody>
                    {Object.entries(submission.payload ?? {}).map(([key, value]) => (
                      <tr key={key}>
                        <td className="font-medium w-1/3 align-top">{key}</td>
                        <td className="break-words align-top">
                          {typeof value === 'object' ? JSON.stringify(value) : String(value ?? '')}
                        </td>
                      </tr>
                    ))}
                    {Object.keys(submission.payload ?? {}).length === 0 && (
                      <tr>
                        <td className="text-text-secondary italic text-xs">No fields submitted.</td>
                      </tr>
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
