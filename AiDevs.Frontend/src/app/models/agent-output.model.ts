export interface AgentOutput {
  id: string;
  taskId: number;
  timestamp: Date;
  level: LogLevel;
  message: string;
}

export enum LogLevel {
  Info = 'info',
  Warning = 'warning',
  Error = 'error',
  Success = 'success'
}
