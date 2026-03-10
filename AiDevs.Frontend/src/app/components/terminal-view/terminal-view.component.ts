import { Component, ChangeDetectionStrategy, inject, input, computed, effect, viewChild, ElementRef } from '@angular/core';
import { DatePipe } from '@angular/common';
import { AgentOutputService } from '../../services/agent-output.service';
import { TaskService } from '../../services/task.service';

@Component({
  selector: 'app-terminal-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    <div class="terminal-container">
      <div class="terminal-header">
        <div class="terminal-title">
          <span class="terminal-icon">▶</span>
          <span>{{ taskName() }}</span>
        </div>
        <button
          type="button"
          class="clear-button"
          (click)="clearOutput()"
          aria-label="Clear terminal output"
        >
          Clear
        </button>
      </div>

      <div class="terminal-output" #terminalOutput>
        @if (outputs().length === 0) {
          <div class="terminal-empty">
            <p>No output yet. Select a task and run an AI agent to see results here.</p>
          </div>
        } @else {
          @for (output of outputs(); track output.id) {
            <div class="terminal-line" [class]="'level-' + output.level">
              <span class="timestamp">{{ output.timestamp | date:'HH:mm:ss.SSS' }}</span>
              <span class="level-badge" [class]="'level-' + output.level">{{ output.level.toUpperCase() }}</span>
              <span class="message">{{ output.message }}</span>
            </div>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .terminal-container {
      height: 100vh;
      display: flex;
      flex-direction: column;
      background: #0d1117;
      overflow: hidden;
    }

    .terminal-header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 1rem 1.5rem;
      background: #161b22;
      border-bottom: 1px solid #1a1f2e;
    }

    .terminal-title {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-family: 'Courier New', monospace;
      font-size: 1rem;
      color: #00ff88;
      font-weight: 600;
    }

    .terminal-icon {
      color: #fbbf24;
    }

    .clear-button {
      padding: 0.5rem 1rem;
      background: transparent;
      border: 1px solid #30363d;
      color: #8b949e;
      font-size: 0.875rem;
      border-radius: 6px;
      cursor: pointer;
      transition: all 0.2s ease;
      font-family: system-ui, -apple-system, sans-serif;
    }

    .clear-button:hover {
      background: #21262d;
      border-color: #8b949e;
      color: #c9d1d9;
    }

    .terminal-output {
      flex: 1;
      overflow-y: auto;
      padding: 1rem;
      font-family: 'Courier New', 'Monaco', monospace;
      font-size: 0.9rem;
      line-height: 1.6;
    }

    .terminal-empty {
      display: flex;
      align-items: center;
      justify-content: center;
      height: 100%;
      color: #6b7280;
      text-align: center;
      padding: 2rem;
    }

    .terminal-empty p {
      max-width: 400px;
      margin: 0;
    }

    .terminal-line {
      display: flex;
      gap: 0.75rem;
      padding: 0.375rem 0;
      border-bottom: 1px solid #0d1117;
    }

    .terminal-line:hover {
      background: #161b22;
    }

    .timestamp {
      color: #6b7280;
      flex-shrink: 0;
      font-size: 0.85rem;
    }

    .level-badge {
      flex-shrink: 0;
      padding: 0.125rem 0.5rem;
      border-radius: 4px;
      font-size: 0.75rem;
      font-weight: 600;
      text-transform: uppercase;
    }

    .level-badge.level-info {
      background: #1e40af20;
      color: #60a5fa;
    }

    .level-badge.level-success {
      background: #00ff8820;
      color: #00ff88;
    }

    .level-badge.level-warning {
      background: #fbbf2420;
      color: #fbbf24;
    }

    .level-badge.level-error {
      background: #ef444420;
      color: #ef4444;
    }

    .message {
      color: #c9d1d9;
      flex: 1;
      word-break: break-word;
    }

    .terminal-line.level-error .message {
      color: #ff6b6b;
    }

    .terminal-line.level-success .message {
      color: #00ff88;
    }

    .terminal-line.level-warning .message {
      color: #fbbf24;
    }

    /* Scrollbar styling */
    .terminal-output::-webkit-scrollbar {
      width: 8px;
    }

    .terminal-output::-webkit-scrollbar-track {
      background: #0d1117;
    }

    .terminal-output::-webkit-scrollbar-thumb {
      background: #30363d;
      border-radius: 4px;
    }

    .terminal-output::-webkit-scrollbar-thumb:hover {
      background: #484f58;
    }
  `]
})
export class TerminalViewComponent {
  private readonly agentOutputService = inject(AgentOutputService);
  private readonly taskService = inject(TaskService);

  readonly taskId = input<number | null>(null);

  private readonly terminalOutput = viewChild<ElementRef<HTMLDivElement>>('terminalOutput');

  protected readonly outputs = computed(() => {
    const id = this.taskId();
    if (id === null) return [];
    return this.agentOutputService.getOutputForTask(id);
  });

  protected readonly taskName = computed(() => {
    const id = this.taskId();
    if (id === null) return 'Terminal';
    const task = this.taskService.getTaskById(id);
    return task ? task.name : 'Terminal';
  });

  constructor() {
    // Auto-scroll to bottom when new output is added
    effect(() => {
      this.outputs(); // Track changes
      setTimeout(() => this.scrollToBottom(), 0);
    });
  }

  protected clearOutput(): void {
    const id = this.taskId();
    if (id !== null) {
      this.agentOutputService.clearOutputForTask(id);
    }
  }

  private scrollToBottom(): void {
    const element = this.terminalOutput()?.nativeElement;
    if (element) {
      element.scrollTop = element.scrollHeight;
    }
  }
}
