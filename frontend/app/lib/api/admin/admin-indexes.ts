import { BaseAPI } from "../base/base-clients";
import type { IndexInfo, IndexSearchRequest, IndexSearchResponse } from "~/lib/api/types/admin";

const prefix = "/api/admin/indexes";

export class AdminIndexesApi extends BaseAPI {
  getAll() {
    return this.requests.get<IndexInfo[]>(prefix);
  }

  getOne(name: string) {
    return this.requests.get<IndexInfo>(`${prefix}/${name}`);
  }

  rebuild(name: string) {
    return this.requests.post<{ detail: string }>(`${prefix}/${name}/rebuild`, {});
  }

  delete(name: string) {
    return this.requests.delete<{ detail: string }>(`${prefix}/${name}`);
  }

  search(name: string, request: IndexSearchRequest) {
    return this.requests.post<IndexSearchResponse>(`${prefix}/${name}/search`, request);
  }
}
