import { d as defineEventHandler, u as useRuntimeConfig, j as joinURL, p as proxyRequest } from '../nitro/nitro.mjs';
import 'node:http';
import 'node:https';
import 'node:events';
import 'node:buffer';
import 'node:fs';
import 'node:path';
import 'node:crypto';
import 'node:url';

const docs = defineEventHandler(async (event) => {
  const apiUrl = useRuntimeConfig().apiUrl;
  const target = joinURL(apiUrl, event.path);
  return proxyRequest(event, target);
});

export { docs as default };
//# sourceMappingURL=docs.mjs.map
