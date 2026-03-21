import { Component, ChangeDetectionStrategy, inject, OnInit, signal, output } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ApiService } from '../../services/api.service';
import { LlmActionRequest, OpenRouterModel, ToolType } from '../../models/llm-action.model';

interface LlmActionForm {
  model: FormControl<number>;
  toolTypes: FormControl<string[]>;
  systemMessage: FormControl<string>;
  userMessage: FormControl<string>;
  temperature: FormControl<number>;
  iterations: FormControl<number>;
}

@Component({
  selector: 'app-llm-action-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule],
  template: `
    <div class="llm-action-container">
      <div class="llm-action-header">
        <h2 class="llm-action-title">Custom LLM Action</h2>
      </div>

      <form [formGroup]="form" (ngSubmit)="onSubmit()" class="llm-action-form">
        <div class="form-group">
          <label for="model" class="form-label">Model</label>
          <select id="model" formControlName="model" class="form-select">
            <option value="">Select a model...</option>
            @for (model of availableModels(); track model.id) {
              <option [ngValue]="model.id">{{ model.name }}</option>
            }
          </select>
        </div>

        <div class="form-group">
          <label for="toolTypes" class="form-label">Tool Types (Optional)</label>
          <select id="toolTypes" formControlName="toolTypes" class="form-select" multiple size="5">
            @for (tool of availableToolTypes(); track tool.name) {
              <option [value]="tool.name">{{ tool.name }}</option>
            }
          </select>
          <span class="form-hint">Hold Ctrl/Cmd to select multiple</span>
        </div>

        <div class="form-group">
          <label for="systemMessage" class="form-label">System Message (Optional)</label>
          <textarea
            id="systemMessage"
            formControlName="systemMessage"
            class="form-textarea"
            rows="3"
            placeholder="Enter system message..."
          ></textarea>
        </div>

        <div class="form-group">
          <label for="userMessage" class="form-label">User Message</label>
          <textarea
            id="userMessage"
            formControlName="userMessage"
            class="form-textarea"
            rows="4"
            placeholder="Enter user message..."
          ></textarea>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label for="temperature" class="form-label">
              Temperature: {{ form.controls.temperature.value }}
            </label>
            <input
              type="range"
              id="temperature"
              formControlName="temperature"
              class="form-range"
              min="0"
              max="2"
              step="0.1"
            />
          </div>

          <div class="form-group">
            <label for="iterations" class="form-label">Iterations</label>
            <input
              type="number"
              id="iterations"
              formControlName="iterations"
              class="form-input"
              min="1"
              max="10"
            />
          </div>
        </div>

        <div class="form-actions">
          <button
            type="submit"
            class="submit-button"
            [disabled]="form.invalid || isLoading()"
          >
            {{ isLoading() ? 'Running...' : 'Run Action' }}
          </button>
        </div>
      </form>
    </div>
  `,
  styles: [`
    .llm-action-container {
      height: 100%;
      display: flex;
      flex-direction: column;
      background: #0d1117;
      overflow: hidden;
    }

    .llm-action-header {
      padding: 1rem 1.5rem;
      background: #161b22;
      border-bottom: 1px solid #1a1f2e;
    }

    .llm-action-title {
      margin: 0;
      font-size: 1.25rem;
      font-weight: 600;
      color: #00ff88;
      font-family: 'Courier New', monospace;
    }

    .llm-action-form {
      flex: 1;
      overflow-y: auto;
      padding: 1.5rem;
    }

    .form-group {
      margin-bottom: 1.5rem;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
    }

    .form-label {
      display: block;
      margin-bottom: 0.5rem;
      font-size: 0.875rem;
      font-weight: 500;
      color: #c9d1d9;
    }

    .form-select,
    .form-input,
    .form-textarea {
      width: 100%;
      padding: 0.5rem 0.75rem;
      background: #0d1117;
      border: 1px solid #30363d;
      border-radius: 6px;
      color: #c9d1d9;
      font-size: 0.875rem;
      font-family: 'Courier New', monospace;
      transition: border-color 0.2s ease;
    }

    .form-select:focus,
    .form-input:focus,
    .form-textarea:focus {
      outline: none;
      border-color: #00ff88;
    }

    .form-select[multiple] {
      padding: 0.25rem;
    }

    .form-select[multiple] option {
      padding: 0.375rem 0.5rem;
      border-radius: 4px;
    }

    .form-select[multiple] option:checked {
      background: #00ff8820;
      color: #00ff88;
    }

    .form-textarea {
      resize: vertical;
      min-height: 60px;
    }

    .form-range {
      width: 100%;
      height: 6px;
      background: #30363d;
      border-radius: 3px;
      outline: none;
      -webkit-appearance: none;
    }

    .form-range::-webkit-slider-thumb {
      -webkit-appearance: none;
      appearance: none;
      width: 16px;
      height: 16px;
      background: #00ff88;
      border-radius: 50%;
      cursor: pointer;
    }

    .form-range::-moz-range-thumb {
      width: 16px;
      height: 16px;
      background: #00ff88;
      border-radius: 50%;
      cursor: pointer;
      border: none;
    }

    .form-hint {
      display: block;
      margin-top: 0.25rem;
      font-size: 0.75rem;
      color: #6b7280;
    }

    .form-actions {
      margin-top: 2rem;
      padding-top: 1rem;
      border-top: 1px solid #1a1f2e;
    }

    .submit-button {
      width: 100%;
      padding: 0.75rem 1.5rem;
      background: #00ff88;
      border: none;
      border-radius: 6px;
      color: #0d1117;
      font-size: 0.875rem;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s ease;
      font-family: system-ui, -apple-system, sans-serif;
    }

    .submit-button:hover:not(:disabled) {
      background: #00cc6a;
      transform: translateY(-1px);
    }

    .submit-button:disabled {
      opacity: 0.5;
      cursor: not-allowed;
      transform: none;
    }

    /* Scrollbar styling */
    .llm-action-form::-webkit-scrollbar {
      width: 8px;
    }

    .llm-action-form::-webkit-scrollbar-track {
      background: #0d1117;
    }

    .llm-action-form::-webkit-scrollbar-thumb {
      background: #30363d;
      border-radius: 4px;
    }

    .llm-action-form::-webkit-scrollbar-thumb:hover {
      background: #484f58;
    }
  `]
})
export class LlmActionFormComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly apiService = inject(ApiService);

  readonly actionSubmitted = output<LlmActionRequest>();

  protected readonly availableModels = signal<OpenRouterModel[]>([]);
  protected readonly availableToolTypes = signal<ToolType[]>([]);
  protected readonly isLoading = signal(false);

  protected readonly form: FormGroup<LlmActionForm> = this.fb.group({
    model: this.fb.control(0, { nonNullable: true, validators: [Validators.required] }),
    toolTypes: this.fb.control<string[]>([], { nonNullable: true }),
    systemMessage: this.fb.control('', { nonNullable: true }),
    userMessage: this.fb.control('', { nonNullable: true, validators: [Validators.required] }),
    temperature: this.fb.control(0.7, { nonNullable: true, validators: [Validators.min(0), Validators.max(1)] }),
    iterations: this.fb.control(1, { nonNullable: true, validators: [Validators.min(1), Validators.max(30)] }),
  });

  ngOnInit(): void {
    this.loadMetadata();
  }

  private loadMetadata(): void {
    this.apiService.get<OpenRouterModel[]>('custom-llm/models').subscribe({
      next: (models) => {
        this.availableModels.set(models);
      },
      error: (error) => {
        console.error('Failed to load LLM action metadata:', error);
      }
    });
    this.apiService.get<ToolType[]>('custom-llm/tools').subscribe({
      next: (toolTypes) => {
        this.availableToolTypes.set(toolTypes);
      },
      error: (error) => {
        console.error('Failed to load tool types:', error);
      }
    });
  }

  protected onSubmit(): void {
    if (this.form.invalid) return;

    const formValue = this.form.getRawValue();

    const request: LlmActionRequest = {
      model: formValue.model,
      toolTypes: formValue.toolTypes.length > 0 ? formValue.toolTypes : [],
      systemMessage: formValue.systemMessage || null,
      userMessage: formValue.userMessage,
      temperature: formValue.temperature,
      iterations: formValue.iterations
    };

    this.isLoading.set(true);
    this.actionSubmitted.emit(request);
  }

  resetLoading(): void {
    this.isLoading.set(false);
  }
}
