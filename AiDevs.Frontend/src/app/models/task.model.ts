export interface Task {
  id: number;
  name: string;
  status: TaskStatus;
  description?: string;
}

export enum TaskStatus {
  Completed = 'Completed',
  NotCompleted = 'NotCompleted',
  NotPublished = 'NotPublished',
}
