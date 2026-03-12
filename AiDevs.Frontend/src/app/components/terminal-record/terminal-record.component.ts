import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
import { AgentOutput, StreamUpdateType } from '../../models/agent-output.model';

@Component({
  selector: 'app-terminal-record',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe, JsonPipe],
  template: `
    <div class="terminal-record" [class]="getRecordClass()">
      <span class="timestamp">{{ output().timestamp | date:'HH:mm:ss.SSS' }}</span>

      @if (output().streamUpdate) {
        @switch (output().streamUpdate!.type) {
          @case (StreamUpdateType.LLMToken) {
            <div class="llm-token-row">
              <span class="type-badge llm">LLM</span>
              @if (isJsonCodeBlock(output().streamUpdate!.content)) {
                <details class="tool-details" open>
                  <summary>JSON</summary>
                  <pre class="tool-output">{{ extractJsonContent(output().streamUpdate!.content) }}</pre>
                </details>
              } @else {
                <span class="content">{{ output().streamUpdate!.content }}</span>
              }
            </div>
          }
          @case (StreamUpdateType.ToolCall) {
            <div class="tool-call-row">
              <span class="type-badge tool">TOOL CALL</span>
              <span class="tool-name">{{ output().streamUpdate!.toolName }}</span>
              @if (output().streamUpdate!.toolInput) {
                <details class="tool-details">
                  <summary>Input</summary>
                  <pre class="tool-input">{{ output().streamUpdate!.toolInput }}</pre>
                </details>
              }
            </div>
          }
          @case (StreamUpdateType.ToolResult) {
            <div class="tool-result-row">
              <span class="type-badge tool-result">TOOL RESULT</span>
              <span class="tool-name">{{ output().streamUpdate!.toolName }}</span>
              @if (output().streamUpdate!.toolOutput) {
                <details class="tool-details">
                  <summary>Output</summary>
                  <pre class="tool-output">{{ output().streamUpdate!.toolOutput }}</pre>
                </details>
              }
            </div>
          }
          @case (StreamUpdateType.Status) {
            <div class="status-row">
              <span class="type-badge status">STATUS</span>
              <span class="content">{{ output().streamUpdate!.content }}</span>
            </div>
          }
          @case (StreamUpdateType.Complete) {
            <div class="complete-row">
              <span class="type-badge complete">✓ COMPLETE</span>
              @if (output().streamUpdate!.finalResult) {
                <div class="final-result">
                  <div class="result-status" [class.success]="output().streamUpdate!.finalResult!.success" [class.error]="!output().streamUpdate!.finalResult!.success">
                    {{ output().streamUpdate!.finalResult!.success ? 'Success' : 'Failed' }}
                  </div>
                  @if (output().streamUpdate!.finalResult!.output) {
                    <details class="tool-details">
                      <summary>Output</summary>
                      <pre class="tool-output">{{ output().streamUpdate!.finalResult!.output }}</pre>
                    </details>
                  }
                  @if (output().streamUpdate!.finalResult!.error) {
                    <details class="tool-details" open>
                      <summary>Error</summary>
                      <pre class="tool-error">{{ output().streamUpdate!.finalResult!.error }}</pre>
                    </details>
                  }
                  @if (output().streamUpdate!.finalResult!.metadata) {
                    <details class="tool-details">
                      <summary>Metadata</summary>
                      <pre class="tool-metadata">{{ output().streamUpdate!.finalResult!.metadata | json }}</pre>
                    </details>
                  }
                </div>
              } @else {
                <span class="content">{{ output().message }}</span>
              }
            </div>
          }
        }
      } @else {
        <span class="level-badge" [class]="'level-' + output().level">{{ output().level.toUpperCase() }}</span>
        <span class="message">{{ output().message }}</span>
      }
    </div>
  `,
  styles: [`
    .terminal-record {
      display: flex;
      gap: 0.75rem;
      padding: 0.5rem 0;
      border-bottom: 1px solid #0d1117;
      align-items: flex-start;
    }

    .terminal-record:hover {
      background: #161b22;
    }

    .timestamp {
      color: #6b7280;
      flex-shrink: 0;
      font-size: 0.85rem;
      padding-top: 0.125rem;
    }

    .type-badge {
      flex-shrink: 0;
      padding: 0.125rem 0.5rem;
      border-radius: 4px;
      font-size: 0.7rem;
      font-weight: 600;
      text-transform: uppercase;
      letter-spacing: 0.5px;
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

    .type-badge.llm {
      background: #8b5cf620;
      color: #c084fc;
    }

    .type-badge.tool {
      background: #0ea5e920;
      color: #06b6d4;
    }

    .type-badge.tool-result {
      background: #10b98120;
      color: #34d399;
    }

    .type-badge.status {
      background: #f59e0b20;
      color: #fbbf24;
    }

    .type-badge.complete {
      background: #00ff8820;
      color: #00ff88;
    }

    .llm-token-row,
    .tool-call-row,
    .tool-result-row,
    .status-row,
    .complete-row {
      display: flex;
      gap: 0.75rem;
      align-items: flex-start;
      flex: 1;
      flex-wrap: wrap;
    }

    .content {
      color: #c9d1d9;
      flex: 1;
      word-break: break-word;
    }

    .message {
      color: #c9d1d9;
      flex: 1;
      word-break: break-word;
    }

    .tool-name {
      color: #00ff88;
      font-weight: 600;
    }

    .tool-details {
      width: 100%;
      margin-top: 0.5rem;
      padding-left: 1rem;
    }

    .tool-details summary {
      color: #8b949e;
      cursor: pointer;
      user-select: none;
      font-size: 0.85rem;
      padding: 0.25rem 0;
    }

    .tool-details summary:hover {
      color: #c9d1d9;
    }

    .tool-details[open] summary {
      margin-bottom: 0.5rem;
    }

    .tool-input,
    .tool-output {
      background: #161b22;
      border: 1px solid #30363d;
      border-radius: 4px;
      padding: 0.75rem;
      margin: 0;
      font-size: 0.85rem;
      color: #c9d1d9;
      overflow-x: auto;
      white-space: pre-wrap;
      word-break: break-word;
    }

    .tool-input {
      border-left: 3px solid #06b6d4;
    }

    .tool-output {
      border-left: 3px solid #34d399;
    }

    /* Specific record type styles */
    .terminal-record.llm-token {
      background: #8b5cf605;
    }

    .terminal-record.tool-call {
      background: #0ea5e905;
    }

    .terminal-record.tool-result {
      background: #10b98105;
    }

    .terminal-record.complete {
      background: #00ff8805;
      font-weight: 500;
    }

    .final-result {
      flex: 1;
      display: flex;
      flex-direction: column;
      gap: 0.5rem;
    }

    .result-status {
      padding: 0.25rem 0.75rem;
      border-radius: 4px;
      font-size: 0.85rem;
      font-weight: 600;
      align-self: flex-start;
    }

    .result-status.success {
      background: #00ff8820;
      color: #00ff88;
    }

    .result-status.error {
      background: #ef444420;
      color: #ef4444;
    }

    .tool-error {
      background: #161b22;
      border: 1px solid #30363d;
      border-left: 3px solid #ef4444;
      border-radius: 4px;
      padding: 0.75rem;
      margin: 0;
      font-size: 0.85rem;
      color: #ef4444;
      overflow-x: auto;
      white-space: pre-wrap;
      word-break: break-word;
    }

    .tool-metadata {
      background: #161b22;
      border: 1px solid #30363d;
      border-left: 3px solid #fbbf24;
      border-radius: 4px;
      padding: 0.75rem;
      margin: 0;
      font-size: 0.85rem;
      color: #c9d1d9;
      overflow-x: auto;
      white-space: pre-wrap;
      word-break: break-word;
    }
  `]
})
export class TerminalRecordComponent {
  readonly output = input.required<AgentOutput>();

  protected readonly StreamUpdateType = StreamUpdateType;

  protected getRecordClass(): string {
    const streamUpdate = this.output().streamUpdate;
    if (!streamUpdate) {
      return `level-${this.output().level}`;
    }

    switch (streamUpdate.type) {
      case StreamUpdateType.LLMToken:
        return 'llm-token';
      case StreamUpdateType.ToolCall:
        return 'tool-call';
      case StreamUpdateType.ToolResult:
        return 'tool-result';
      case StreamUpdateType.Status:
        return 'status';
      case StreamUpdateType.Complete:
        return 'complete';
      default:
        return '';
    }
  }

  protected isJsonCodeBlock(content: string | undefined): boolean {
    if (!content) return false;
    const trimmed = content.trim();
    return trimmed.startsWith('```json') && trimmed.endsWith('```');
  }

  protected extractJsonContent(content: string | undefined): string {
    if (!content) return '';
    const trimmed = content.trim();
    if (trimmed.startsWith('```json') && trimmed.endsWith('```')) {
      return trimmed.slice(7, -3).trim();
    }
    return content;
  }
}
