import { useState } from 'react';
import { useTaskStore } from '../store/useTaskStore';
import { Plus, Trash2 } from 'lucide-react';

export function Tasks() {
  const { tasks, addTask, deleteTask } = useTaskStore();
  const [title, setTitle] = useState('');

  const handleAddTask = () => {
    if (!title.trim()) return;
    addTask({ title: title.trim() });
    setTitle('');
  };

  const formatDate = (val: string) =>
    new Intl.DateTimeFormat('en-US', { dateStyle: 'medium' }).format(new Date(val));

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-1">
        <h2 className="text-lg font-medium tracking-tight text-text-primary">Tasks</h2>
        <p className="text-xs text-text-secondary">Add, list, and delete tasks.</p>
      </div>

      <div className="bg-surface border border-border p-4 rounded-sm flex gap-3">
        <input
          type="text"
          value={title}
          onChange={e => setTitle(e.target.value)}
          onKeyDown={e => e.key === 'Enter' && handleAddTask()}
          placeholder="What needs to be done?"
          className="flex-1 bg-background border border-border p-2 text-sm rounded-sm focus:ring-1 focus:ring-primary outline-none"
        />
        <button
          onClick={handleAddTask}
          className="bg-primary text-white px-4 py-1.5 text-xs font-bold uppercase tracking-widest rounded-sm hover:bg-primary/90 transition-all flex items-center gap-2"
        >
          <Plus className="w-3.5 h-3.5" /> Add task
        </button>
      </div>

      <div className="bg-surface border border-border rounded-sm overflow-hidden">
        <table className="w-full text-left">
          <thead>
            <tr className="bg-surface-elevated/50 border-b border-border">
              <th className="p-4">Title</th>
              <th className="p-4">Created</th>
              <th className="p-4 text-right">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border/30">
            {tasks.map((task) => (
              <tr key={task.id} className="hover:bg-surface-elevated/20 transition-colors">
                <td className="p-4 text-sm font-medium">{task.title}</td>
                <td className="p-4 text-xs font-mono text-text-secondary">{formatDate(task.createdAt)}</td>
                <td className="p-4 text-right">
                  <button onClick={() => deleteTask(task.id)} className="p-1.5 text-text-secondary hover:text-error transition-all">
                    <Trash2 className="w-3.5 h-3.5" />
                  </button>
                </td>
              </tr>
            ))}
            {tasks.length === 0 && (
              <tr>
                <td colSpan={3} className="p-12 text-center text-text-secondary italic text-xs">
                  No tasks yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
