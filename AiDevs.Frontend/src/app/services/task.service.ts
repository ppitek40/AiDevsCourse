import { Injectable, signal } from '@angular/core';
import { Task, TaskStatus } from '../models/task.model';

@Injectable({
  providedIn: 'root'
})
export class TaskService {
  private readonly tasksSignal = signal<Task[]>(this.initializeTasks());

  readonly tasks = this.tasksSignal.asReadonly();

  private initializeTasks(): Task[] {
    return Array.from({ length: 25 }, (_, i) => ({
      id: i + 1,
      name: `Task ${String(i + 1).padStart(2, '0')}`,
      status: TaskStatus.Pending,
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
