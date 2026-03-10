export interface Task {
  id: number;
  name: string;
  status: TaskStatus;
  description?: string;
}

export enum TaskStatus {
  Pending = 'pending',
  Running = 'running',
  Completed = 'completed',
  Error = 'error'
}
