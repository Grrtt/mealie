import { BaseAPI } from "../base/base-clients";
import type { ApiRequestInstance } from "~/lib/api/types/non-generated";

export interface AiConfigurationResponse {
  id: string;
  name: string;
  providerType: string;
  hasApiKey: boolean;
  maskedApiKey: string | null;
  baseUrl: string | null;
  defaultModel: string | null;
  isActive: boolean;
  enableImageServices: boolean;
  enableTranscriptionServices: boolean;
  createdAt: string | null;
}

export interface CreateAiConfigurationRequest {
  name: string;
  providerType: string;
  apiKey?: string | null;
  baseUrl?: string | null;
  defaultModel?: string | null;
  enableImageServices?: boolean;
  enableTranscriptionServices?: boolean;
}

export interface UpdateAiConfigurationRequest {
  name?: string | null;
  apiKey?: string | null;
  baseUrl?: string | null;
  defaultModel?: string | null;
  enableImageServices?: boolean | null;
  enableTranscriptionServices?: boolean | null;
}

const prefix = "/api/admin/ai-configurations";

export class AdminAiConfigurationsApi extends BaseAPI {
  constructor(requests: ApiRequestInstance) {
    super(requests);
  }

  async getAll() {
    return await this.requests.get<AiConfigurationResponse[]>(prefix);
  }

  async getOne(id: string) {
    return await this.requests.get<AiConfigurationResponse>(`${prefix}/${id}`);
  }

  async create(payload: CreateAiConfigurationRequest) {
    return await this.requests.post<AiConfigurationResponse>(prefix, payload);
  }

  async update(id: string, payload: UpdateAiConfigurationRequest) {
    return await this.requests.put<AiConfigurationResponse>(`${prefix}/${id}`, payload);
  }

  async delete(id: string) {
    return await this.requests.delete(`${prefix}/${id}`);
  }

  async activate(id: string) {
    return await this.requests.put<AiConfigurationResponse>(`${prefix}/${id}/activate`, {});
  }
}
