import { Injectable, signal } from '@angular/core';
import { AgentOutput, LogLevel } from '../models/agent-output.model';

@Injectable({
  providedIn: 'root'
})
export class AgentOutputService {
  private readonly outputSignal = signal<AgentOutput[]>([]);

  readonly output = this.outputSignal.asReadonly();

  addOutput(taskId: number, message: string, level: LogLevel = LogLevel.Info): void {
    const newOutput: AgentOutput = {
      id: crypto.randomUUID(),
      taskId,
      timestamp: new Date(),
      level,
      message
    };

    this.outputSignal.update(output => [...output, newOutput]);
  }

  getOutputForTask(taskId: number): AgentOutput[] {
    return this.output().filter(output => output.taskId === taskId);
  }

  clearOutput(): void {
    this.outputSignal.set([]);
  }

  clearOutputForTask(taskId: number): void {
    this.outputSignal.update(output =>
      output.filter(o => o.taskId !== taskId)
    );
  }
}
