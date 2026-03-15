export interface  LlmActionRequest {
  model: number;
  toolTypes: string[] | null;
  systemMessage: string | null;
  userMessage: string;
  temperature: number;
  iterations: number;
}

export interface OpenRouterModel {
  id: number;
  name: string;
}

export interface ToolType {
  name: string;
}

export interface LlmActionMetadata {
  models: OpenRouterModel[];
  toolTypes: ToolType[];
}
