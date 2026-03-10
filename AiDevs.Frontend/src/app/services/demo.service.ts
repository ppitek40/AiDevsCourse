import { Injectable, inject } from '@angular/core';
import { TaskService } from './task.service';
import { AgentOutputService } from './agent-output.service';
import { TaskStatus } from '../models/task.model';
import { LogLevel } from '../models/agent-output.model';

@Injectable({
  providedIn: 'root'
})
export class DemoService {
  private readonly taskService = inject(TaskService);
  private readonly agentOutputService = inject(AgentOutputService);

  loadDemoData(): void {
    // Update some task statuses
    this.taskService.updateTaskStatus(1, TaskStatus.Completed);
    this.taskService.updateTaskStatus(2, TaskStatus.Running);
    this.taskService.updateTaskStatus(3, TaskStatus.Error);

    // Add demo output for Task 1
    this.agentOutputService.addOutput(1, 'Starting AI agent for Task 01...', LogLevel.Info);
    this.agentOutputService.addOutput(1, 'Loading data from CSV file', LogLevel.Info);
    this.agentOutputService.addOutput(1, 'Parsing 1523 records', LogLevel.Info);
    this.agentOutputService.addOutput(1, 'Applying AI filters...', LogLevel.Info);
    this.agentOutputService.addOutput(1, 'Successfully filtered 847 matching records', LogLevel.Success);
    this.agentOutputService.addOutput(1, 'Task completed successfully!', LogLevel.Success);

    // Add demo output for Task 2
    this.agentOutputService.addOutput(2, 'Initializing Task 02 agent...', LogLevel.Info);
    this.agentOutputService.addOutput(2, 'Connecting to OpenRouter API', LogLevel.Info);
    this.agentOutputService.addOutput(2, 'Using model: anthropic/claude-3.5-sonnet', LogLevel.Info);
    this.agentOutputService.addOutput(2, 'Processing suspect detection...', LogLevel.Info);
    this.agentOutputService.addOutput(2, 'Found 3 potential suspects', LogLevel.Warning);
    this.agentOutputService.addOutput(2, 'Agent is still running...', LogLevel.Info);

    // Add demo output for Task 3
    this.agentOutputService.addOutput(3, 'Starting Task 03 analysis...', LogLevel.Info);
    this.agentOutputService.addOutput(3, 'Loading training data', LogLevel.Info);
    this.agentOutputService.addOutput(3, 'Error: Unable to load model weights', LogLevel.Error);
    this.agentOutputService.addOutput(3, 'Task failed - please check configuration', LogLevel.Error);
  }
}
