export interface AgentOutput {
  id: string;
  taskId: number;
  timestamp: Date;
  level: LogLevel;
  message: string;
  streamUpdate?: StreamUpdate;
}

export enum LogLevel {
  Info = 'info',
  Warning = 'warning',
  Error = 'error',
  Success = 'success'
}

export interface StreamUpdate {
  type: StreamUpdateType;
  content?: string;
  toolName?: string;
  toolInput?: string;
  toolOutput?: string;
  isComplete: boolean;
  finalResult?: FinalResult;
}

export interface FinalResult {
  success: boolean;
  output?: string;
  error?: string;
  metadata?: Record<string, unknown>;
}

export enum StreamUpdateType {
  LLMToken = 0,
  ToolCall = 1,
  ToolResult = 2,
  Status = 3,
  Complete = 4
}
