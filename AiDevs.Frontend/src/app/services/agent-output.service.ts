import { Injectable, signal } from '@angular/core';
import { AgentOutput, LogLevel, StreamUpdate, StreamUpdateType } from '../models/agent-output.model';

@Injectable({
  providedIn: 'root',
})
export class AgentOutputService {
  private readonly outputSignal = signal<AgentOutput[]>([]);
  private eventSources = new Map<number, { close: () => Promise<void> }>();
  private lastLLMTokenOutputId = new Map<number, string>();

  readonly output = this.outputSignal.asReadonly();

  addOutput(taskId: number, message: string, level: LogLevel = LogLevel.Info): void {
    const newOutput: AgentOutput = {
      id: crypto.randomUUID(),
      taskId,
      timestamp: new Date(),
      level,
      message,
    };

    this.outputSignal.update((output) => [...output, newOutput]);
  }

  getOutputForTask(taskId: number): AgentOutput[] {
    return this.output().filter((output) => output.taskId === taskId);
  }

  clearOutput(): void {
    this.outputSignal.set([]);
  }

  clearOutputForTask(taskId: number): void {
    this.outputSignal.update((output) => output.filter((o) => o.taskId !== taskId));
    this.lastLLMTokenOutputId.delete(taskId);
  }

  startTaskStream(taskId: number, url: string): void {
    // Close existing stream if any
    this.stopTaskStream(taskId);

    // EventSource only supports GET, so we need to use fetch for POST with SSE
    fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        Accept: 'text/event-stream',
      },
      body: JSON.stringify({}),
    })
      .then((response) => {
        if (!response.ok) {
          throw new Error(`HTTP error! status: ${response.status}`);
        }

        const reader = response.body?.getReader();
        const decoder = new TextDecoder();

        if (!reader) {
          throw new Error('No response body');
        }

        const readStream = () => {
          reader
            .read()
            .then(({ done, value }) => {
              if (done) {
                this.addOutput(taskId, 'Task execution completed', LogLevel.Success);
                this.stopTaskStream(taskId);
                return;
              }

              const chunk = decoder.decode(value, { stream: true });
              const lines = chunk.split('\n');

              for (const line of lines) {
                if (line.startsWith('data: ')) {
                  const data = line.substring(6).trim();
                  if (data) {
                    try {
                      const parsed = JSON.parse(data) as StreamUpdate;

                      // Check if this is a StreamUpdate
                      if (parsed.type !== undefined) {
                        const message = this.getMessageForStreamUpdate(parsed);
                        const level = this.getLevelForStreamUpdate(parsed);
                        this.addOutputWithStreamUpdate(taskId, message, level, parsed);
                      } else {
                        // Legacy format
                        this.addOutput(taskId, parsed.content || '', LogLevel.Info);
                      }
                    } catch {
                      this.addOutput(taskId, data, LogLevel.Info);
                    }
                  }
                }
              }

              readStream();
            })
            .catch(() => {
              this.addOutput(taskId, 'Connection to task stream lost', LogLevel.Error);
              this.stopTaskStream(taskId);
            });
        };

        readStream();

        // Store a reference so we can abort if needed
        this.eventSources.set(taskId, {
          close: async () => {
            try {
              await reader.cancel();
            } catch {
              // Ignore cancellation errors
            }
          }
        });
      })
      .catch(() => {
        this.addOutput(taskId, 'Failed to start task execution', LogLevel.Error);
        this.stopTaskStream(taskId);
      });
  }

  stopTaskStream(taskId: number): void {
    const eventSource = this.eventSources.get(taskId);
    if (eventSource) {
      eventSource.close();
      this.eventSources.delete(taskId);
    }
    this.lastLLMTokenOutputId.delete(taskId);
  }

  isStreamActive(taskId: number): boolean {
    return this.eventSources.has(taskId);
  }

  private addOutputWithStreamUpdate(
    taskId: number,
    message: string,
    level: LogLevel,
    streamUpdate: StreamUpdate
  ): void {
    // For LLMToken type, append to existing output instead of creating new
    if (streamUpdate.type === StreamUpdateType.LLMToken) {
      const lastOutputId = this.lastLLMTokenOutputId.get(taskId);

      if (lastOutputId) {
        this.outputSignal.update((outputs) => {
          const index = outputs.findIndex(o => o.id === lastOutputId);
          if (index !== -1) {
            const updated = [...outputs];
            const existingOutput = updated[index];
            updated[index] = {
              ...existingOutput,
              message: existingOutput.message + (streamUpdate.content || ''),
              streamUpdate: {
                ...existingOutput.streamUpdate!,
                content: (existingOutput.streamUpdate?.content || '') + (streamUpdate.content || '')
              }
            };
            return updated;
          }
          return outputs;
        });
        return;
      }
    }

    // For non-LLMToken types or first LLMToken, create new output
    const newOutput: AgentOutput = {
      id: crypto.randomUUID(),
      taskId,
      timestamp: new Date(),
      level,
      message,
      streamUpdate,
    };

    if (streamUpdate.type === StreamUpdateType.LLMToken) {
      this.lastLLMTokenOutputId.set(taskId, newOutput.id);
    } else {
      // Clear the last LLMToken ID when we get a different type
      this.lastLLMTokenOutputId.delete(taskId);
    }

    this.outputSignal.update((output) => [...output, newOutput]);
  }

  private getMessageForStreamUpdate(update: StreamUpdate): string {
    switch (update.type) {
      case StreamUpdateType.LLMToken:
        return update.content || '';
      case StreamUpdateType.ToolCall:
        return `Tool Call: ${update.toolName || 'Unknown'}`;
      case StreamUpdateType.ToolResult:
        return `Tool Result: ${update.toolName || 'Unknown'}`;
      case StreamUpdateType.Status:
        return update.content || 'Status update';
      case StreamUpdateType.Complete:
        return 'Task completed';
      default:
        return 'Unknown update';
    }
  }

  private getLevelForStreamUpdate(update: StreamUpdate): LogLevel {
    switch (update.type) {
      case StreamUpdateType.Complete:
        return LogLevel.Success;
      case StreamUpdateType.ToolCall:
      case StreamUpdateType.ToolResult:
        return LogLevel.Info;
      case StreamUpdateType.Status:
        return LogLevel.Info;
      case StreamUpdateType.LLMToken:
      default:
        return LogLevel.Info;
    }
  }
}
