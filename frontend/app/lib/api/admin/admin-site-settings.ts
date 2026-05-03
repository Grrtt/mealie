import { BaseAPI } from "../base/base-clients";
import type { ApiRequestInstance } from "~/lib/api/types/non-generated";

export interface SiteSettingsResponse {
  defaultParser: string;
  defaultParserUnavailable: boolean;
  ingredientSystemPrompt: string | null;
  categorySystemPrompt: string | null;
  tagSystemPrompt: string | null;
}

export interface UpdateSiteSettingsRequest {
  defaultParser: string;
  ingredientSystemPrompt?: string | null;
  categorySystemPrompt?: string | null;
  tagSystemPrompt?: string | null;
}

const route = "/api/admin/site-settings";

export class AdminSiteSettingsApi extends BaseAPI {
  constructor(requests: ApiRequestInstance) {
    super(requests);
  }

  async get() {
    return await this.requests.get<SiteSettingsResponse>(route);
  }

  async update(payload: UpdateSiteSettingsRequest) {
    return await this.requests.put<SiteSettingsResponse>(route, payload);
  }
}
