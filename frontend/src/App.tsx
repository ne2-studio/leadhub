import { useEffect, useMemo } from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useAuth } from 'react-oidc-context';
import { useFormStore } from './store/useFormStore';
import { Layout } from './components/Layout';
import { Dashboard } from './components/Dashboard';
import { Forms } from './components/Forms';
import { Submissions } from './components/Submissions';
import { setAccessToken } from './api';

export default function App() {
  const auth = useAuth();
  const { forms, fetchData, isLoading, error } = useFormStore();

  useEffect(() => {
    setAccessToken(auth.user?.access_token);
  }, [auth.user]);

  useEffect(() => {
    const isCallback = window.location.pathname === '/callback';
    if (!auth.isLoading && !auth.isAuthenticated && !isCallback) {
      auth.signinRedirect();
    }
  }, [auth.isLoading, auth.isAuthenticated]);

  useEffect(() => {
    if (auth.isAuthenticated) {
      fetchData();
    }
  }, [auth.isAuthenticated]);

  const totalForms = useMemo(() => forms.length, [forms]);
  const totalSubmissions = useMemo(() => forms.reduce((sum, f) => sum + f.submissionCount, 0), [forms]);

  const handleLogout = async () => {
    await auth.signoutRedirect();
  };

  if (auth.isLoading || !auth.isAuthenticated) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-text-secondary font-mono text-xs uppercase tracking-widest">Loading...</p>
        </div>
      </div>
    );
  }

  if (isLoading) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary mx-auto"></div>
          <p className="mt-4 text-text-secondary font-mono text-xs uppercase tracking-widest">Loading data...</p>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-background p-6">
        <div className="text-center max-w-md border border-border p-12 bg-surface">
          <h2 className="text-xs font-mono uppercase tracking-[0.2em] text-text-secondary mb-4">Connection error</h2>
          <p className="text-sm text-text-primary mb-8 leading-relaxed">{error}</p>
          <button onClick={() => fetchData()} className="btn btn-primary">
            Retry
          </button>
        </div>
      </div>
    );
  }

  return (
    <BrowserRouter>
      <Layout onLogout={handleLogout}>
        <Routes>
          <Route path="/" element={<Dashboard forms={forms} totalForms={totalForms} totalSubmissions={totalSubmissions} />} />
          <Route path="/forms" element={<Forms />} />
          <Route path="/forms/:formId/submissions" element={<Submissions forms={forms} />} />
          <Route path="/forms/:formId/submissions/:submissionId" element={<Submissions forms={forms} />} />
          <Route path="/callback" element={null} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </BrowserRouter>
  );
}
