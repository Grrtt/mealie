import { d as defineEventHandler, u as useRuntimeConfig, j as joinURL, s as sendRedirect, p as proxyRequest } from '../../nitro/nitro.mjs';
import 'node:http';
import 'node:https';
import 'node:events';
import 'node:buffer';
import 'node:fs';
import 'node:path';
import 'node:crypto';
import 'node:url';

const _____ = defineEventHandler(async (event) => {
  const apiUrl = useRuntimeConfig().apiUrl;
  const target = joinURL(apiUrl, event.path);
  if (event.path === "/api/auth/oauth") {
    return sendRedirect(event, target);
  }
  return proxyRequest(event, target);
});

export { _____ as default };
//# sourceMappingURL=_..._.mjs.map
