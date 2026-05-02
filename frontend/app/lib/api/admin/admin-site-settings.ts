import { BaseAPI } from "../base/base-clients";
import type { ApiRequestInstance } from "~/lib/api/types/non-generated";

export interface SiteSettingsResponse {
  defaultParser: string;
  defaultParserUnavailable: boolean;
  legacyEnvVarsDetected: boolean;
}

export interface UpdateSiteSettingsRequest {
  defaultParser: string;
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
