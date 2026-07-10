export class Form {
  id: string;
  name: string;
  slug: string;
  description: string | null;
  notificationsEnabled: boolean;
  notificationEmail: string | null;
  thankYouUrl: string | null;
  submissionCount: number;
  lastSubmissionAt: string | null;
  createdAt: string | null;
  updatedAt: string | null;

  constructor(data: {
    id: string;
    name: string;
    slug: string;
    description?: string | null;
    notificationsEnabled: boolean;
    notificationEmail?: string | null;
    thankYouUrl?: string | null;
    submissionCount?: number;
    lastSubmissionAt?: string | null;
    createdAt?: string | null;
    updatedAt?: string | null;
  }) {
    this.id = data.id;
    this.name = data.name;
    this.slug = data.slug;
    this.description = data.description ?? null;
    this.notificationsEnabled = data.notificationsEnabled;
    this.notificationEmail = data.notificationEmail ?? null;
    this.thankYouUrl = data.thankYouUrl ?? null;
    this.submissionCount = data.submissionCount ?? 0;
    this.lastSubmissionAt = data.lastSubmissionAt ?? null;
    this.createdAt = data.createdAt ?? null;
    this.updatedAt = data.updatedAt ?? null;
  }
}

export type SubmissionStatus = 'PendingReview' | 'Ham' | 'SuspectedSpam' | 'Spam';

export class Submission {
  id: string;
  formId: string | null;
  createdAt: string;
  ipAddress: string | null;
  userAgent: string | null;
  preview: string | null;
  payload: Record<string, unknown> | null;
  status: SubmissionStatus;
  spamScore: number;
  spamReasons: string[];

  constructor(data: {
    id: string;
    formId?: string | null;
    createdAt: string;
    ipAddress?: string | null;
    userAgent?: string | null;
    preview?: string | null;
    payload?: Record<string, unknown> | null;
    status?: SubmissionStatus;
    spamScore?: number;
    spamReasons?: string[];
  }) {
    this.id = data.id;
    this.formId = data.formId ?? null;
    this.createdAt = data.createdAt;
    this.ipAddress = data.ipAddress ?? null;
    this.userAgent = data.userAgent ?? null;
    this.preview = data.preview ?? null;
    this.payload = data.payload ?? null;
    this.status = data.status ?? 'PendingReview';
    this.spamScore = data.spamScore ?? 0;
    this.spamReasons = data.spamReasons ?? [];
  }
}

export class PaginatedSubmissions {
  items: Submission[];
  page: number;
  pageSize: number;
  totalItems: number;

  constructor(data: { items: Submission[]; page: number; pageSize: number; totalItems: number }) {
    this.items = data.items;
    this.page = data.page;
    this.pageSize = data.pageSize;
    this.totalItems = data.totalItems;
  }
}
