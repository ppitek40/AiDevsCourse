import { Component, ChangeDetectionStrategy, inject, output } from '@angular/core';
import { TaskService } from '../../services/task.service';

@Component({
  selector: 'app-sidebar',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <aside class="sidebar">
      <div class="sidebar-header">
        <h1 class="sidebar-title">AiDevs Course</h1>
        <p class="sidebar-subtitle">AI Agent Tasks</p>
      </div>

      <nav class="sidebar-nav" role="navigation" aria-label="Task navigation">
        <ul class="task-list">
          @for (task of taskService.tasks(); track task.id) {
            <li>
              <button
                type="button"
                class="task-item"
                [class.active]="selectedTaskId === task.id"
                [class.pending]="task.status === 'pending'"
                [class.running]="task.status === 'running'"
                [class.completed]="task.status === 'completed'"
                [class.error]="task.status === 'error'"
                (click)="selectTask(task.id)"
                [attr.aria-current]="selectedTaskId === task.id ? 'page' : null"
              >
                <span class="task-number">{{ task.name }}</span>
                <span class="task-status" [attr.aria-label]="task.status"></span>
              </button>
            </li>
          }
        </ul>
      </nav>
    </aside>
  `,
  styles: [`
    .sidebar {
      width: 280px;
      height: 100vh;
      background: #0a0e14;
      border-right: 1px solid #1a1f2e;
      display: flex;
      flex-direction: column;
      overflow: hidden;
    }

    .sidebar-header {
      padding: 2rem 1.5rem;
      border-bottom: 1px solid #1a1f2e;
    }

    .sidebar-title {
      font-size: 1.5rem;
      font-weight: 700;
      color: #00ff88;
      margin: 0;
      font-family: 'Courier New', monospace;
    }

    .sidebar-subtitle {
      font-size: 0.875rem;
      color: #6b7280;
      margin: 0.25rem 0 0;
    }

    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      padding: 1rem 0;
    }

    .task-list {
      list-style: none;
      margin: 0;
      padding: 0;
    }

    .task-item {
      width: 100%;
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.875rem 1.5rem;
      background: transparent;
      border: none;
      color: #9ca3af;
      font-family: 'Courier New', monospace;
      font-size: 0.95rem;
      cursor: pointer;
      transition: all 0.2s ease;
      text-align: left;
    }

    .task-item:hover {
      background: #151923;
      color: #00ff88;
    }

    .task-item.active {
      background: #1a1f2e;
      color: #00ff88;
      border-left: 3px solid #00ff88;
    }

    .task-number {
      font-weight: 500;
    }

    .task-status {
      width: 8px;
      height: 8px;
      border-radius: 50%;
      background: #4b5563;
    }

    .task-item.pending .task-status {
      background: #6b7280;
    }

    .task-item.running .task-status {
      background: #fbbf24;
      animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
    }

    .task-item.completed .task-status {
      background: #00ff88;
    }

    .task-item.error .task-status {
      background: #ef4444;
    }

    @keyframes pulse {
      0%, 100% {
        opacity: 1;
      }
      50% {
        opacity: 0.5;
      }
    }

    /* Scrollbar styling */
    .sidebar-nav::-webkit-scrollbar {
      width: 6px;
    }

    .sidebar-nav::-webkit-scrollbar-track {
      background: #0a0e14;
    }

    .sidebar-nav::-webkit-scrollbar-thumb {
      background: #1a1f2e;
      border-radius: 3px;
    }

    .sidebar-nav::-webkit-scrollbar-thumb:hover {
      background: #2a2f3e;
    }
  `]
})
export class SidebarComponent {
  protected readonly taskService = inject(TaskService);

  readonly taskSelected = output<number>();

  protected selectedTaskId: number | null = null;

  protected selectTask(taskId: number): void {
    this.selectedTaskId = taskId;
    this.taskSelected.emit(taskId);
  }
}
