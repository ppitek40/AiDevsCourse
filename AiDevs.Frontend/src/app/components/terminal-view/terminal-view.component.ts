import { Component, ChangeDetectionStrategy, inject, input, computed, effect, viewChild, ElementRef } from '@angular/core';
import { AgentOutputService } from '../../services/agent-output.service';
import { TaskService } from '../../services/task.service';
import { ApiService } from '../../services/api.service';
import { LogLevel } from '../../models/agent-output.model';
import { TerminalRecordComponent } from '../terminal-record/terminal-record.component';

@Component({
  selector: 'app-terminal-view',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TerminalRecordComponent],
  template: `
    <div class="terminal-container">
      <div class="terminal-header">
        <div class="terminal-title">
          <span class="terminal-icon">▶</span>
          <span>{{ taskName() }}</span>
        </div>
        <div class="terminal-actions">
          <button
            type="button"
            class="action-button start-button"
            (click)="startTask()"
            [disabled]="taskId() === null"
            aria-label="Start task execution"
          >
            Start
          </button>
          <button
            type="button"
            class="action-button stop-button"
            (click)="stopTask()"
            [disabled]="taskId() === null || !isStreamActive()"
            aria-label="Stop task execution"
          >
            Stop
          </button>
          <button
            type="button"
            class="action-button clear-button"
            (click)="clearOutput()"
            aria-label="Clear terminal output"
          >
            Clear
          </button>
        </div>
      </div>

      <div class="terminal-output" #terminalOutput>
        @if (outputs().length === 0) {
          <div class="terminal-empty">
            <p>No output yet. Select a task and run an AI agent to see results here.</p>
          </div>
        } @else {
          @for (output of outputs(); track output.id) {
            <app-terminal-record [output]="output" />
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

    .terminal-actions {
      display: flex;
      gap: 0.5rem;
    }

    .action-button {
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

    .action-button:hover:not(:disabled) {
      background: #21262d;
      border-color: #8b949e;
      color: #c9d1d9;
    }

    .action-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
    }

    .start-button:hover:not(:disabled) {
      background: #00ff8820;
      border-color: #00ff88;
      color: #00ff88;
    }

    .stop-button:hover:not(:disabled) {
      background: #ef444420;
      border-color: #ef4444;
      color: #ef4444;
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
  private readonly apiService = inject(ApiService);

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

  protected readonly isStreamActive = computed(() => {
    const id = this.taskId();
    return id !== null && this.agentOutputService.isStreamActive(id);
  });

  constructor() {
    // Auto-scroll to bottom when new output is added
    effect(() => {
      this.outputs(); // Track changes
      setTimeout(() => this.scrollToBottom(), 0);
    });
  }

  protected startTask(): void {
    const id = this.taskId();
    if (id !== null) {
      const streamUrl = this.apiService.getStreamUrl(`solutions/${id}`);
      this.agentOutputService.addOutput(id, 'Connecting to task execution endpoint...', LogLevel.Info);
      this.agentOutputService.startTaskStream(id, streamUrl);
    }
  }

  protected stopTask(): void {
    const id = this.taskId();
    if (id !== null) {
      this.agentOutputService.stopTaskStream(id);
      this.agentOutputService.addOutput(id, 'warning', LogLevel.Warning);
    }
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
