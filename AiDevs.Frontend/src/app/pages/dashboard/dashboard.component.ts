import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { TerminalViewComponent } from '../../components/terminal-view/terminal-view.component';
import { LlmActionFormComponent } from '../../components/llm-action-form/llm-action-form.component';
import { LlmActionRequest } from '../../models/llm-action.model';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SidebarComponent, TerminalViewComponent, LlmActionFormComponent],
  template: `
    <div class="dashboard-layout">
      <app-sidebar (taskSelected)="onTaskSelected($event)" />
      <div class="dashboard-content">
        @if (isCustomMode()) {
          <div class="llm-action-panel">
            <app-llm-action-form (actionSubmitted)="onLlmActionSubmitted($event)" />
          </div>
        }
        <main class="dashboard-main">
          <app-terminal-view
            [taskId]="selectedTaskId()"
            [llmActionRequest]="llmActionRequest()"
            [isCustomMode]="isCustomMode()"
            (customModeToggled)="onCustomModeToggled()"
          />
        </main>
      </div>
    </div>
  `,
  styles: [`
    .dashboard-layout {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background: #0d1117;
    }

    .dashboard-content {
      flex: 1;
      display: flex;
      overflow: hidden;
    }

    .llm-action-panel {
      width: 400px;
      border-right: 1px solid #1a1f2e;
      overflow: hidden;
    }

    .dashboard-main {
      flex: 1;
      overflow: hidden;
    }
  `]
})
export class DashboardComponent {
  protected readonly selectedTaskId = signal<number | null>(null);
  protected readonly llmActionRequest = signal<LlmActionRequest | null>(null);
  protected readonly isCustomMode = signal(false);

  private lastTaskIdBeforeCustomMode: number | null = null;

  protected onTaskSelected(taskId: number): void {
    this.selectedTaskId.set(taskId);
    this.llmActionRequest.set(null);
    this.isCustomMode.set(false);
  }

  protected onLlmActionSubmitted(request: LlmActionRequest): void {
    this.selectedTaskId.set(null);
    this.llmActionRequest.set(request);
  }

  protected onCustomModeToggled(): void {
    const wasInCustomMode = this.isCustomMode();

    if (!wasInCustomMode) {
      // Entering custom mode - remember current task
      this.lastTaskIdBeforeCustomMode = this.selectedTaskId();
      this.selectedTaskId.set(null);
      this.llmActionRequest.set(null);
    } else {
      // Exiting custom mode - restore previous task
      if (this.lastTaskIdBeforeCustomMode !== null) {
        this.selectedTaskId.set(this.lastTaskIdBeforeCustomMode);
      }
      this.llmActionRequest.set(null);
    }

    this.isCustomMode.update(mode => !mode);
  }
}
