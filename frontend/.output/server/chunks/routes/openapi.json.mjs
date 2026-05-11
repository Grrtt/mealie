import { d as defineEventHandler, u as useRuntimeConfig, j as joinURL, p as proxyRequest } from '../nitro/nitro.mjs';
import 'node:http';
import 'node:https';
import 'node:events';
import 'node:buffer';
import 'node:fs';
import 'node:path';
import 'node:crypto';
import 'node:url';

const openapi_json = defineEventHandler(async (event) => {
  const apiUrl = useRuntimeConfig().apiUrl;
  const target = joinURL(apiUrl, event.path);
  return proxyRequest(event, target);
});

export { openapi_json as default };
//# sourceMappingURL=openapi.json.mjs.map
