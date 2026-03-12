import { Injectable, signal, inject } from '@angular/core';
import { Task, TaskStatus } from '../models/task.model';
import { ApiService } from './api.service';

interface TaskResponse {
  tasks: Array<{
    taskId: number;
    status: string;
  }>;
}

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly api = inject(ApiService);
  private readonly tasksSignal = signal<Task[]>([]);

  readonly tasks = this.tasksSignal.asReadonly();

  loadTasks(): void {
    this.api.get<TaskResponse>('solutions').subscribe({
      next: (response) => {
        const tasks = response.tasks.map(t => ({
          id: t.taskId,
          name: `Task ${String(t.taskId).padStart(2, '0')}`,
          status: this.mapStatus(t.status),
          description: `AI Devs Course - Task ${t.taskId}`
        }));
        this.tasksSignal.set(tasks);
      },
      error: (error) => {
        console.error('Failed to load tasks:', error);
        this.tasksSignal.set(this.initializeTasks());
      }
    });
  }

  private mapStatus(status: string): TaskStatus {
    switch (status) {
      case 'Completed':
        return TaskStatus.Completed;
      case 'NotCompleted':
        return TaskStatus.NotCompleted;
      case 'NotPublished':
        return TaskStatus.NotPublished;
      default:
        return TaskStatus.NotPublished;
    }
  }

  private initializeTasks(): Task[] {
    return Array.from({ length: 25 }, (_, i) => ({
      id: i + 1,
      name: `Task ${String(i + 1).padStart(2, '0')}`,
      status: TaskStatus.NotCompleted,
      description: `AI Devs Course - Task ${i + 1}`
    }));
  }

  updateTaskStatus(taskId: number, status: TaskStatus): void {
    this.tasksSignal.update(tasks =>
      tasks.map(task =>
        task.id === taskId ? { ...task, status } : task
      )
    );
  }

  getTaskById(taskId: number): Task | undefined {
    return this.tasks().find(task => task.id === taskId);
  }
}
