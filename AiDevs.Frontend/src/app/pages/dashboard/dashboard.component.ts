import { Component, ChangeDetectionStrategy, signal, inject, OnInit } from '@angular/core';
import { SidebarComponent } from '../../components/sidebar/sidebar.component';
import { TerminalViewComponent } from '../../components/terminal-view/terminal-view.component';

@Component({
  selector: 'app-dashboard',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [SidebarComponent, TerminalViewComponent],
  template: `
    <div class="dashboard-layout">
      <app-sidebar (taskSelected)="onTaskSelected($event)" />
      <main class="dashboard-main">
        <app-terminal-view [taskId]="selectedTaskId()" />
      </main>
    </div>
  `,
  styles: [`
    .dashboard-layout {
      display: flex;
      height: 100vh;
      overflow: hidden;
      background: #0d1117;
    }

    .dashboard-main {
      flex: 1;
      overflow: hidden;
    }
  `]
})
export class DashboardComponent {
  protected readonly selectedTaskId = signal<number | null>(null);

  protected onTaskSelected(taskId: number): void {
    this.selectedTaskId.set(taskId);
  }
}
