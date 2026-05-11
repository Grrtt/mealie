import process from 'node:process';globalThis._importMeta_=globalThis._importMeta_||{url:"file:///_entry.js",env:process.env};import http, { Server as Server$1 } from 'node:http';
import https, { Server } from 'node:https';
import { EventEmitter } from 'node:events';
import { Buffer as Buffer$1 } from 'node:buffer';
import { promises, existsSync } from 'node:fs';
import { resolve as resolve$1, dirname as dirname$1, join } from 'node:path';
import { createHash } from 'node:crypto';
import { fileURLToPath } from 'node:url';

const suspectProtoRx = /"(?:_|\\u0{2}5[Ff]){2}(?:p|\\u0{2}70)(?:r|\\u0{2}72)(?:o|\\u0{2}6[Ff])(?:t|\\u0{2}74)(?:o|\\u0{2}6[Ff])(?:_|\\u0{2}5[Ff]){2}"\s*:/;
const suspectConstructorRx = /"(?:c|\\u0063)(?:o|\\u006[Ff])(?:n|\\u006[Ee])(?:s|\\u0073)(?:t|\\u0074)(?:r|\\u0072)(?:u|\\u0075)(?:c|\\u0063)(?:t|\\u0074)(?:o|\\u006[Ff])(?:r|\\u0072)"\s*:/;
const JsonSigRx = /^\s*["[{]|^\s*-?\d{1,16}(\.\d{1,17})?([Ee][+-]?\d+)?\s*$/;
function jsonParseTransform(key, value) {
  if (key === "__proto__" || key === "constructor" && value && typeof value === "object" && "prototype" in value) {
    warnKeyDropped(key);
    return;
  }
  return value;
}
function warnKeyDropped(key) {
  console.warn(`[destr] Dropping "${key}" key to prevent prototype pollution.`);
}
function destr(value, options = {}) {
  if (typeof value !== "string") {
    return value;
  }
  if (value[0] === '"' && value[value.length - 1] === '"' && value.indexOf("\\") === -1) {
    return value.slice(1, -1);
  }
  const _value = value.trim();
  if (_value.length <= 9) {
    switch (_value.toLowerCase()) {
      case "true": {
        return true;
      }
      case "false": {
        return false;
      }
      case "undefined": {
        return void 0;
      }
      case "null": {
        return null;
      }
      case "nan": {
        return Number.NaN;
      }
      case "infinity": {
        return Number.POSITIVE_INFINITY;
      }
      case "-infinity": {
        return Number.NEGATIVE_INFINITY;
      }
    }
  }
  if (!JsonSigRx.test(value)) {
    if (options.strict) {
      throw new SyntaxError("[destr] Invalid JSON");
    }
    return value;
  }
  try {
    if (suspectProtoRx.test(value) || suspectConstructorRx.test(value)) {
      if (options.strict) {
        throw new Error("[destr] Possible prototype pollution");
      }
      return JSON.parse(value, jsonParseTransform);
    }
    return JSON.parse(value);
  } catch (error) {
    if (options.strict) {
      throw error;
    }
    return value;
  }
}

const HASH_RE = /#/g;
const AMPERSAND_RE = /&/g;
const SLASH_RE = /\//g;
const EQUAL_RE = /=/g;
const PLUS_RE = /\+/g;
const ENC_CARET_RE = /%5e/gi;
const ENC_BACKTICK_RE = /%60/gi;
const ENC_PIPE_RE = /%7c/gi;
const ENC_SPACE_RE = /%20/gi;
const ENC_SLASH_RE = /%2f/gi;
function encode(text) {
  return encodeURI("" + text).replace(ENC_PIPE_RE, "|");
}
function encodeQueryValue(input) {
  return encode(typeof input === "string" ? input : JSON.stringify(input)).replace(PLUS_RE, "%2B").replace(ENC_SPACE_RE, "+").replace(HASH_RE, "%23").replace(AMPERSAND_RE, "%26").replace(ENC_BACKTICK_RE, "`").replace(ENC_CARET_RE, "^").replace(SLASH_RE, "%2F");
}
function encodeQueryKey(text) {
  return encodeQueryValue(text).replace(EQUAL_RE, "%3D");
}
function decode(text = "") {
  try {
    return decodeURIComponent("" + text);
  } catch {
    return "" + text;
  }
}
function decodePath(text) {
  return decode(text.replace(ENC_SLASH_RE, "%252F"));
}
function decodeQueryKey(text) {
  return decode(text.replace(PLUS_RE, " "));
}
function decodeQueryValue(text) {
  return decode(text.replace(PLUS_RE, " "));
}

function parseQuery(parametersString = "") {
  const object = /* @__PURE__ */ Object.create(null);
  if (parametersString[0] === "?") {
    parametersString = parametersString.slice(1);
  }
  for (const parameter of parametersString.split("&")) {
    const s = parameter.match(/([^=]+)=?(.*)/) || [];
    if (s.length < 2) {
      continue;
    }
    const key = decodeQueryKey(s[1]);
    if (key === "__proto__" || key === "constructor") {
      continue;
    }
    const value = decodeQueryValue(s[2] || "");
    if (object[key] === void 0) {
      object[key] = value;
    } else if (Array.isArray(object[key])) {
      object[key].push(value);
    } else {
      object[key] = [object[key], value];
    }
  }
  return object;
}
function encodeQueryItem(key, value) {
  if (typeof value === "number" || typeof value === "boolean") {
    value = String(value);
  }
  if (!value) {
    return encodeQueryKey(key);
  }
  if (Array.isArray(value)) {
    return value.map(
      (_value) => `${encodeQueryKey(key)}=${encodeQueryValue(_value)}`
    ).join("&");
  }
  return `${encodeQueryKey(key)}=${encodeQueryValue(value)}`;
}
function stringifyQuery(query) {
  return Object.keys(query).filter((k) => query[k] !== void 0).map((k) => encodeQueryItem(k, query[k])).filter(Boolean).join("&");
}

const PROTOCOL_STRICT_REGEX = /^[\s\w\0+.-]{2,}:([/\\]{1,2})/;
const PROTOCOL_REGEX = /^[\s\w\0+.-]{2,}:([/\\]{2})?/;
const PROTOCOL_RELATIVE_REGEX = /^([/\\]\s*){2,}[^/\\]/;
const JOIN_LEADING_SLASH_RE = /^\.?\//;
function hasProtocol(inputString, opts = {}) {
  if (typeof opts === "boolean") {
    opts = { acceptRelative: opts };
  }
  if (opts.strict) {
    return PROTOCOL_STRICT_REGEX.test(inputString);
  }
  return PROTOCOL_REGEX.test(inputString) || (opts.acceptRelative ? PROTOCOL_RELATIVE_REGEX.test(inputString) : false);
}
function hasTrailingSlash(input = "", respectQueryAndFragment) {
  {
    return input.endsWith("/");
  }
}
function withoutTrailingSlash(input = "", respectQueryAndFragment) {
  {
    return (hasTrailingSlash(input) ? input.slice(0, -1) : input) || "/";
  }
}
function withTrailingSlash(input = "", respectQueryAndFragment) {
  {
    return input.endsWith("/") ? input : input + "/";
  }
}
function hasLeadingSlash(input = "") {
  return input.startsWith("/");
}
function withLeadingSlash(input = "") {
  return hasLeadingSlash(input) ? input : "/" + input;
}
function withBase(input, base) {
  if (isEmptyURL(base) || hasProtocol(input)) {
    return input;
  }
  const _base = withoutTrailingSlash(base);
  if (input.startsWith(_base)) {
    const nextChar = input[_base.length];
    if (!nextChar || nextChar === "/" || nextChar === "?") {
      return input;
    }
  }
  return joinURL(_base, input);
}
function withoutBase(input, base) {
  if (isEmptyURL(base)) {
    return input;
  }
  const _base = withoutTrailingSlash(base);
  if (!input.startsWith(_base)) {
    return input;
  }
  const nextChar = input[_base.length];
  if (nextChar && nextChar !== "/" && nextChar !== "?") {
    return input;
  }
  const trimmed = input.slice(_base.length);
  return trimmed[0] === "/" ? trimmed : "/" + trimmed;
}
function withQuery(input, query) {
  const parsed = parseURL(input);
  const mergedQuery = { ...parseQuery(parsed.search), ...query };
  parsed.search = stringifyQuery(mergedQuery);
  return stringifyParsedURL(parsed);
}
function getQuery$1(input) {
  return parseQuery(parseURL(input).search);
}
function isEmptyURL(url) {
  return !url || url === "/";
}
function isNonEmptyURL(url) {
  return url && url !== "/";
}
function joinURL(base, ...input) {
  let url = base || "";
  for (const segment of input.filter((url2) => isNonEmptyURL(url2))) {
    if (url) {
      const _segment = segment.replace(JOIN_LEADING_SLASH_RE, "");
      url = withTrailingSlash(url) + _segment;
    } else {
      url = segment;
    }
  }
  return url;
}
function joinRelativeURL(..._input) {
  const JOIN_SEGMENT_SPLIT_RE = /\/(?!\/)/;
  const input = _input.filter(Boolean);
  const segments = [];
  let segmentsDepth = 0;
  for (const i of input) {
    if (!i || i === "/") {
      continue;
    }
    for (const [sindex, s] of i.split(JOIN_SEGMENT_SPLIT_RE).entries()) {
      if (!s || s === ".") {
        continue;
      }
      if (s === "..") {
        if (segments.length === 1 && hasProtocol(segments[0])) {
          continue;
        }
        segments.pop();
        segmentsDepth--;
        continue;
      }
      if (sindex === 1 && segments[segments.length - 1]?.endsWith(":/")) {
        segments[segments.length - 1] += "/" + s;
        continue;
      }
      segments.push(s);
      segmentsDepth++;
    }
  }
  let url = segments.join("/");
  if (segmentsDepth >= 0) {
    if (input[0]?.startsWith("/") && !url.startsWith("/")) {
      url = "/" + url;
    } else if (input[0]?.startsWith("./") && !url.startsWith("./")) {
      url = "./" + url;
    }
  } else {
    url = "../".repeat(-1 * segmentsDepth) + url;
  }
  if (input[input.length - 1]?.endsWith("/") && !url.endsWith("/")) {
    url += "/";
  }
  return url;
}

const protocolRelative = Symbol.for("ufo:protocolRelative");
function parseURL(input = "", defaultProto) {
  const _specialProtoMatch = input.match(
    /^[\s\0]*(blob:|data:|javascript:|vbscript:)(.*)/i
  );
  if (_specialProtoMatch) {
    const [, _proto, _pathname = ""] = _specialProtoMatch;
    return {
      protocol: _proto.toLowerCase(),
      pathname: _pathname,
      href: _proto + _pathname,
      auth: "",
      host: "",
      search: "",
      hash: ""
    };
  }
  if (!hasProtocol(input, { acceptRelative: true })) {
    return parsePath(input);
  }
  const [, protocol = "", auth, hostAndPath = ""] = input.replace(/\\/g, "/").match(/^[\s\0]*([\w+.-]{2,}:)?\/\/([^/@]+@)?(.*)/) || [];
  let [, host = "", path = ""] = hostAndPath.match(/([^#/?]*)(.*)?/) || [];
  if (protocol === "file:") {
    path = path.replace(/\/(?=[A-Za-z]:)/, "");
  }
  const { pathname, search, hash } = parsePath(path);
  return {
    protocol: protocol.toLowerCase(),
    auth: auth ? auth.slice(0, Math.max(0, auth.length - 1)) : "",
    host,
    pathname,
    search,
    hash,
    [protocolRelative]: !protocol
  };
}
function parsePath(input = "") {
  const [pathname = "", search = "", hash = ""] = (input.match(/([^#?]*)(\?[^#]*)?(#.*)?/) || []).splice(1);
  return {
    pathname,
    search,
    hash
  };
}
function stringifyParsedURL(parsed) {
  const pathname = parsed.pathname || "";
  const search = parsed.search ? (parsed.search.startsWith("?") ? "" : "?") + parsed.search : "";
  const hash = parsed.hash || "";
  const auth = parsed.auth ? parsed.auth + "@" : "";
  const host = parsed.host || "";
  const proto = parsed.protocol || parsed[protocolRelative] ? (parsed.protocol || "") + "//" : "";
  return proto + auth + host + pathname + search + hash;
}

const NODE_TYPES = {
  NORMAL: 0,
  WILDCARD: 1,
  PLACEHOLDER: 2
};

function createRouter$1(options = {}) {
  const ctx = {
    options,
    rootNode: createRadixNode(),
    staticRoutesMap: {}
  };
  const normalizeTrailingSlash = (p) => options.strictTrailingSlash ? p : p.replace(/\/$/, "") || "/";
  if (options.routes) {
    for (const path in options.routes) {
      insert(ctx, normalizeTrailingSlash(path), options.routes[path]);
    }
  }
  return {
    ctx,
    lookup: (path) => lookup(ctx, normalizeTrailingSlash(path)),
    insert: (path, data) => insert(ctx, normalizeTrailingSlash(path), data),
    remove: (path) => remove(ctx, normalizeTrailingSlash(path))
  };
}
function lookup(ctx, path) {
  const staticPathNode = ctx.staticRoutesMap[path];
  if (staticPathNode) {
    return staticPathNode.data;
  }
  const sections = path.split("/");
  const params = {};
  let paramsFound = false;
  let wildcardNode = null;
  let node = ctx.rootNode;
  let wildCardParam = null;
  for (let i = 0; i < sections.length; i++) {
    const section = sections[i];
    if (node.wildcardChildNode !== null) {
      wildcardNode = node.wildcardChildNode;
      wildCardParam = sections.slice(i).join("/");
    }
    const nextNode = node.children.get(section);
    if (nextNode === void 0) {
      if (node && node.placeholderChildren.length > 1) {
        const remaining = sections.length - i;
        node = node.placeholderChildren.find((c) => c.maxDepth === remaining) || null;
      } else {
        node = node.placeholderChildren[0] || null;
      }
      if (!node) {
        break;
      }
      if (node.paramName) {
        params[node.paramName] = section;
      }
      paramsFound = true;
    } else {
      node = nextNode;
    }
  }
  if ((node === null || node.data === null) && wildcardNode !== null) {
    node = wildcardNode;
    params[node.paramName || "_"] = wildCardParam;
    paramsFound = true;
  }
  if (!node) {
    return null;
  }
  if (paramsFound) {
    return {
      ...node.data,
      params: paramsFound ? params : void 0
    };
  }
  return node.data;
}
function insert(ctx, path, data) {
  let isStaticRoute = true;
  const sections = path.split("/");
  let node = ctx.rootNode;
  let _unnamedPlaceholderCtr = 0;
  const matchedNodes = [node];
  for (const section of sections) {
    let childNode;
    if (childNode = node.children.get(section)) {
      node = childNode;
    } else {
      const type = getNodeType(section);
      childNode = createRadixNode({ type, parent: node });
      node.children.set(section, childNode);
      if (type === NODE_TYPES.PLACEHOLDER) {
        childNode.paramName = section === "*" ? `_${_unnamedPlaceholderCtr++}` : section.slice(1);
        node.placeholderChildren.push(childNode);
        isStaticRoute = false;
      } else if (type === NODE_TYPES.WILDCARD) {
        node.wildcardChildNode = childNode;
        childNode.paramName = section.slice(
          3
          /* "**:" */
        ) || "_";
        isStaticRoute = false;
      }
      matchedNodes.push(childNode);
      node = childNode;
    }
  }
  for (const [depth, node2] of matchedNodes.entries()) {
    node2.maxDepth = Math.max(matchedNodes.length - depth, node2.maxDepth || 0);
  }
  node.data = data;
  if (isStaticRoute === true) {
    ctx.staticRoutesMap[path] = node;
  }
  return node;
}
function remove(ctx, path) {
  let success = false;
  const sections = path.split("/");
  let node = ctx.rootNode;
  for (const section of sections) {
    node = node.children.get(section);
    if (!node) {
      return success;
    }
  }
  if (node.data) {
    const lastSection = sections.at(-1) || "";
    node.data = null;
    if (Object.keys(node.children).length === 0 && node.parent) {
      node.parent.children.delete(lastSection);
      node.parent.wildcardChildNode = null;
      node.parent.placeholderChildren = [];
    }
    success = true;
  }
  return success;
}
function createRadixNode(options = {}) {
  return {
    type: options.type || NODE_TYPES.NORMAL,
    maxDepth: 0,
    parent: options.parent || null,
    children: /* @__PURE__ */ new Map(),
    data: options.data || null,
    paramName: options.paramName || null,
    wildcardChildNode: null,
    placeholderChildren: []
  };
}
function getNodeType(str) {
  if (str.startsWith("**")) {
    return NODE_TYPES.WILDCARD;
  }
  if (str[0] === ":" || str === "*") {
    return NODE_TYPES.PLACEHOLDER;
  }
  return NODE_TYPES.NORMAL;
}

function toRouteMatcher(router) {
  const table = _routerNodeToTable("", router.ctx.rootNode);
  return _createMatcher(table, router.ctx.options.strictTrailingSlash);
}
function _createMatcher(table, strictTrailingSlash) {
  return {
    ctx: { table },
    matchAll: (path) => _matchRoutes(path, table, strictTrailingSlash)
  };
}
function _createRouteTable() {
  return {
    static: /* @__PURE__ */ new Map(),
    wildcard: /* @__PURE__ */ new Map(),
    dynamic: /* @__PURE__ */ new Map()
  };
}
function _matchRoutes(path, table, strictTrailingSlash) {
  if (strictTrailingSlash !== true && path.endsWith("/")) {
    path = path.slice(0, -1) || "/";
  }
  const matches = [];
  for (const [key, value] of _sortRoutesMap(table.wildcard)) {
    if (path === key || path.startsWith(key + "/")) {
      matches.push(value);
    }
  }
  for (const [key, value] of _sortRoutesMap(table.dynamic)) {
    if (path.startsWith(key + "/")) {
      const subPath = "/" + path.slice(key.length).split("/").splice(2).join("/");
      matches.push(..._matchRoutes(subPath, value));
    }
  }
  const staticMatch = table.static.get(path);
  if (staticMatch) {
    matches.push(staticMatch);
  }
  return matches.filter(Boolean);
}
function _sortRoutesMap(m) {
  return [...m.entries()].sort((a, b) => a[0].length - b[0].length);
}
function _routerNodeToTable(initialPath, initialNode) {
  const table = _createRouteTable();
  function _addNode(path, node) {
    if (path) {
      if (node.type === NODE_TYPES.NORMAL && !(path.includes("*") || path.includes(":"))) {
        if (node.data) {
          table.static.set(path, node.data);
        }
      } else if (node.type === NODE_TYPES.WILDCARD) {
        table.wildcard.set(path.replace("/**", ""), node.data);
      } else if (node.type === NODE_TYPES.PLACEHOLDER) {
        const subTable = _routerNodeToTable("", node);
        if (node.data) {
          subTable.static.set("/", node.data);
        }
        table.dynamic.set(path.replace(/\/\*|\/:\w+/, ""), subTable);
        return;
      }
    }
    for (const [childPath, child] of node.children.entries()) {
      _addNode(`${path}/${childPath}`.replace("//", "/"), child);
    }
  }
  _addNode(initialPath, initialNode);
  return table;
}

function isPlainObject(value) {
  if (value === null || typeof value !== "object") {
    return false;
  }
  const prototype = Object.getPrototypeOf(value);
  if (prototype !== null && prototype !== Object.prototype && Object.getPrototypeOf(prototype) !== null) {
    return false;
  }
  if (Symbol.iterator in value) {
    return false;
  }
  if (Symbol.toStringTag in value) {
    return Object.prototype.toString.call(value) === "[object Module]";
  }
  return true;
}

function _defu(baseObject, defaults, namespace = ".", merger) {
  if (!isPlainObject(defaults)) {
    return _defu(baseObject, {}, namespace, merger);
  }
  const object = { ...defaults };
  for (const key of Object.keys(baseObject)) {
    if (key === "__proto__" || key === "constructor") {
      continue;
    }
    const value = baseObject[key];
    if (value === null || value === void 0) {
      continue;
    }
    if (merger && merger(object, key, value, namespace)) {
      continue;
    }
    if (Array.isArray(value) && Array.isArray(object[key])) {
      object[key] = [...value, ...object[key]];
    } else if (isPlainObject(value) && isPlainObject(object[key])) {
      object[key] = _defu(
        value,
        object[key],
        (namespace ? `${namespace}.` : "") + key.toString(),
        merger
      );
    } else {
      object[key] = value;
    }
  }
  return object;
}
function createDefu(merger) {
  return (...arguments_) => (
    // eslint-disable-next-line unicorn/no-array-reduce
    arguments_.reduce((p, c) => _defu(p, c, "", merger), {})
  );
}
const defu = createDefu();
const defuFn = createDefu((object, key, currentValue) => {
  if (object[key] !== void 0 && typeof currentValue === "function") {
    object[key] = currentValue(object[key]);
    return true;
  }
});

function o(n){throw new Error(`${n} is not implemented yet!`)}let i$1 = class i extends EventEmitter{__unenv__={};readableEncoding=null;readableEnded=true;readableFlowing=false;readableHighWaterMark=0;readableLength=0;readableObjectMode=false;readableAborted=false;readableDidRead=false;closed=false;errored=null;readable=false;destroyed=false;static from(e,t){return new i(t)}constructor(e){super();}_read(e){}read(e){}setEncoding(e){return this}pause(){return this}resume(){return this}isPaused(){return  true}unpipe(e){return this}unshift(e,t){}wrap(e){return this}push(e,t){return  false}_destroy(e,t){this.removeAllListeners();}destroy(e){return this.destroyed=true,this._destroy(e),this}pipe(e,t){return {}}compose(e,t){throw new Error("Method not implemented.")}[Symbol.asyncDispose](){return this.destroy(),Promise.resolve()}async*[Symbol.asyncIterator](){throw o("Readable.asyncIterator")}iterator(e){throw o("Readable.iterator")}map(e,t){throw o("Readable.map")}filter(e,t){throw o("Readable.filter")}forEach(e,t){throw o("Readable.forEach")}reduce(e,t,r){throw o("Readable.reduce")}find(e,t){throw o("Readable.find")}findIndex(e,t){throw o("Readable.findIndex")}some(e,t){throw o("Readable.some")}toArray(e){throw o("Readable.toArray")}every(e,t){throw o("Readable.every")}flatMap(e,t){throw o("Readable.flatMap")}drop(e,t){throw o("Readable.drop")}take(e,t){throw o("Readable.take")}asIndexedPairs(e){throw o("Readable.asIndexedPairs")}};let l$1 = class l extends EventEmitter{__unenv__={};writable=true;writableEnded=false;writableFinished=false;writableHighWaterMark=0;writableLength=0;writableObjectMode=false;writableCorked=0;closed=false;errored=null;writableNeedDrain=false;writableAborted=false;destroyed=false;_data;_encoding="utf8";constructor(e){super();}pipe(e,t){return {}}_write(e,t,r){if(this.writableEnded){r&&r();return}if(this._data===void 0)this._data=e;else {const s=typeof this._data=="string"?Buffer$1.from(this._data,this._encoding||t||"utf8"):this._data,a=typeof e=="string"?Buffer$1.from(e,t||this._encoding||"utf8"):e;this._data=Buffer$1.concat([s,a]);}this._encoding=t,r&&r();}_writev(e,t){}_destroy(e,t){}_final(e){}write(e,t,r){const s=typeof t=="string"?this._encoding:"utf8",a=typeof t=="function"?t:typeof r=="function"?r:void 0;return this._write(e,s,a),true}setDefaultEncoding(e){return this}end(e,t,r){const s=typeof e=="function"?e:typeof t=="function"?t:typeof r=="function"?r:void 0;if(this.writableEnded)return s&&s(),this;const a=e===s?void 0:e;if(a){const u=t===s?void 0:t;this.write(a,u,s);}return this.writableEnded=true,this.writableFinished=true,this.emit("close"),this.emit("finish"),this}cork(){}uncork(){}destroy(e){return this.destroyed=true,delete this._data,this.removeAllListeners(),this}compose(e,t){throw new Error("Method not implemented.")}[Symbol.asyncDispose](){return Promise.resolve()}};const c=class{allowHalfOpen=true;_destroy;constructor(e=new i$1,t=new l$1){Object.assign(this,e),Object.assign(this,t),this._destroy=m(e._destroy,t._destroy);}};function _(){return Object.assign(c.prototype,i$1.prototype),Object.assign(c.prototype,l$1.prototype),c}function m(...n){return function(...e){for(const t of n)t(...e);}}const g=_();class A extends g{__unenv__={};bufferSize=0;bytesRead=0;bytesWritten=0;connecting=false;destroyed=false;pending=false;localAddress="";localPort=0;remoteAddress="";remoteFamily="";remotePort=0;autoSelectFamilyAttemptedAddresses=[];readyState="readOnly";constructor(e){super();}write(e,t,r){return  false}connect(e,t,r){return this}end(e,t,r){return this}setEncoding(e){return this}pause(){return this}resume(){return this}setTimeout(e,t){return this}setNoDelay(e){return this}setKeepAlive(e,t){return this}address(){return {}}unref(){return this}ref(){return this}destroySoon(){this.destroy();}resetAndDestroy(){const e=new Error("ERR_SOCKET_CLOSED");return e.code="ERR_SOCKET_CLOSED",this.destroy(e),this}}class y extends i$1{aborted=false;httpVersion="1.1";httpVersionMajor=1;httpVersionMinor=1;complete=true;connection;socket;headers={};trailers={};method="GET";url="/";statusCode=200;statusMessage="";closed=false;errored=null;readable=false;constructor(e){super(),this.socket=this.connection=e||new A;}get rawHeaders(){const e=this.headers,t=[];for(const r in e)if(Array.isArray(e[r]))for(const s of e[r])t.push(r,s);else t.push(r,e[r]);return t}get rawTrailers(){return []}setTimeout(e,t){return this}get headersDistinct(){return p(this.headers)}get trailersDistinct(){return p(this.trailers)}}function p(n){const e={};for(const[t,r]of Object.entries(n))t&&(e[t]=(Array.isArray(r)?r:[r]).filter(Boolean));return e}class w extends l$1{statusCode=200;statusMessage="";upgrading=false;chunkedEncoding=false;shouldKeepAlive=false;useChunkedEncodingByDefault=false;sendDate=false;finished=false;headersSent=false;strictContentLength=false;connection=null;socket=null;req;_headers={};constructor(e){super(),this.req=e;}assignSocket(e){e._httpMessage=this,this.socket=e,this.connection=e,this.emit("socket",e),this._flush();}_flush(){this.flushHeaders();}detachSocket(e){}writeContinue(e){}writeHead(e,t,r){e&&(this.statusCode=e),typeof t=="string"&&(this.statusMessage=t,t=void 0);const s=r||t;if(s&&!Array.isArray(s))for(const a in s)this.setHeader(a,s[a]);return this.headersSent=true,this}writeProcessing(){}setTimeout(e,t){return this}appendHeader(e,t){e=e.toLowerCase();const r=this._headers[e],s=[...Array.isArray(r)?r:[r],...Array.isArray(t)?t:[t]].filter(Boolean);return this._headers[e]=s.length>1?s:s[0],this}setHeader(e,t){return this._headers[e.toLowerCase()]=t,this}setHeaders(e){for(const[t,r]of Object.entries(e))this.setHeader(t,r);return this}getHeader(e){return this._headers[e.toLowerCase()]}getHeaders(){return this._headers}getHeaderNames(){return Object.keys(this._headers)}hasHeader(e){return e.toLowerCase()in this._headers}removeHeader(e){delete this._headers[e.toLowerCase()];}addTrailers(e){}flushHeaders(){}writeEarlyHints(e,t){typeof t=="function"&&t();}}const E=(()=>{const n=function(){};return n.prototype=Object.create(null),n})();function R(n={}){const e=new E,t=Array.isArray(n)||H(n)?n:Object.entries(n);for(const[r,s]of t)if(s){if(e[r]===void 0){e[r]=s;continue}e[r]=[...Array.isArray(e[r])?e[r]:[e[r]],...Array.isArray(s)?s:[s]];}return e}function H(n){return typeof n?.entries=="function"}function v(n={}){if(n instanceof Headers)return n;const e=new Headers;for(const[t,r]of Object.entries(n))if(r!==void 0){if(Array.isArray(r)){for(const s of r)e.append(t,String(s));continue}e.set(t,String(r));}return e}const S=new Set([101,204,205,304]);async function b(n,e){const t=new y,r=new w(t);t.url=e.url?.toString()||"/";let s;if(!t.url.startsWith("/")){const d=new URL(t.url);s=d.host,t.url=d.pathname+d.search+d.hash;}t.method=e.method||"GET",t.headers=R(e.headers||{}),t.headers.host||(t.headers.host=e.host||s||"localhost"),t.connection.encrypted=t.connection.encrypted||e.protocol==="https",t.body=e.body||null,t.__unenv__=e.context,await n(t,r);let a=r._data;(S.has(r.statusCode)||t.method.toUpperCase()==="HEAD")&&(a=null,delete r._headers["content-length"]);const u={status:r.statusCode,statusText:r.statusMessage,headers:r._headers,body:a};return t.destroy(),r.destroy(),u}async function C(n,e,t={}){try{const r=await b(n,{url:e,...t});return new Response(r.body,{status:r.status,statusText:r.statusText,headers:v(r.headers)})}catch(r){return new Response(r.toString(),{status:Number.parseInt(r.statusCode||r.code)||500,statusText:r.statusText})}}

function hasProp(obj, prop) {
  try {
    return prop in obj;
  } catch {
    return false;
  }
}

class H3Error extends Error {
  static __h3_error__ = true;
  statusCode = 500;
  fatal = false;
  unhandled = false;
  statusMessage;
  data;
  cause;
  constructor(message, opts = {}) {
    super(message, opts);
    if (opts.cause && !this.cause) {
      this.cause = opts.cause;
    }
  }
  toJSON() {
    const obj = {
      message: this.message,
      statusCode: sanitizeStatusCode(this.statusCode, 500)
    };
    if (this.statusMessage) {
      obj.statusMessage = sanitizeStatusMessage(this.statusMessage);
    }
    if (this.data !== void 0) {
      obj.data = this.data;
    }
    return obj;
  }
}
function createError$1(input) {
  if (typeof input === "string") {
    return new H3Error(input);
  }
  if (isError(input)) {
    return input;
  }
  const err = new H3Error(input.message ?? input.statusMessage ?? "", {
    cause: input.cause || input
  });
  if (hasProp(input, "stack")) {
    try {
      Object.defineProperty(err, "stack", {
        get() {
          return input.stack;
        }
      });
    } catch {
      try {
        err.stack = input.stack;
      } catch {
      }
    }
  }
  if (input.data) {
    err.data = input.data;
  }
  if (input.statusCode) {
    err.statusCode = sanitizeStatusCode(input.statusCode, err.statusCode);
  } else if (input.status) {
    err.statusCode = sanitizeStatusCode(input.status, err.statusCode);
  }
  if (input.statusMessage) {
    err.statusMessage = input.statusMessage;
  } else if (input.statusText) {
    err.statusMessage = input.statusText;
  }
  if (err.statusMessage) {
    const originalMessage = err.statusMessage;
    const sanitizedMessage = sanitizeStatusMessage(err.statusMessage);
    if (sanitizedMessage !== originalMessage) {
      console.warn(
        "[h3] Please prefer using `message` for longer error messages instead of `statusMessage`. In the future, `statusMessage` will be sanitized by default."
      );
    }
  }
  if (input.fatal !== void 0) {
    err.fatal = input.fatal;
  }
  if (input.unhandled !== void 0) {
    err.unhandled = input.unhandled;
  }
  return err;
}
function sendError(event, error, debug) {
  if (event.handled) {
    return;
  }
  const h3Error = isError(error) ? error : createError$1(error);
  const responseBody = {
    statusCode: h3Error.statusCode,
    statusMessage: h3Error.statusMessage,
    stack: [],
    data: h3Error.data
  };
  if (debug) {
    responseBody.stack = (h3Error.stack || "").split("\n").map((l) => l.trim());
  }
  if (event.handled) {
    return;
  }
  const _code = Number.parseInt(h3Error.statusCode);
  setResponseStatus(event, _code, h3Error.statusMessage);
  event.node.res.setHeader("content-type", MIMES.json);
  event.node.res.end(JSON.stringify(responseBody, void 0, 2));
}
function isError(input) {
  return input?.constructor?.__h3_error__ === true;
}

function getQuery(event) {
  return getQuery$1(event.path || "");
}
function isMethod(event, expected, allowHead) {
  if (typeof expected === "string") {
    if (event.method === expected) {
      return true;
    }
  } else if (expected.includes(event.method)) {
    return true;
  }
  return false;
}
function assertMethod(event, expected, allowHead) {
  if (!isMethod(event, expected)) {
    throw createError$1({
      statusCode: 405,
      statusMessage: "HTTP method is not allowed."
    });
  }
}
function getRequestHeaders(event) {
  const _headers = {};
  for (const key in event.node.req.headers) {
    const val = event.node.req.headers[key];
    _headers[key] = Array.isArray(val) ? val.filter(Boolean).join(", ") : val;
  }
  return _headers;
}
function getRequestHeader(event, name) {
  const headers = getRequestHeaders(event);
  const value = headers[name.toLowerCase()];
  return value;
}
function getRequestHost(event, opts = {}) {
  if (opts.xForwardedHost) {
    const _header = event.node.req.headers["x-forwarded-host"];
    const xForwardedHost = (_header || "").split(",").shift()?.trim();
    if (xForwardedHost) {
      return xForwardedHost;
    }
  }
  return event.node.req.headers.host || "localhost";
}
function getRequestProtocol(event, opts = {}) {
  if (opts.xForwardedProto !== false && event.node.req.headers["x-forwarded-proto"] === "https") {
    return "https";
  }
  return event.node.req.connection?.encrypted ? "https" : "http";
}
function getRequestURL(event, opts = {}) {
  const host = getRequestHost(event, opts);
  const protocol = getRequestProtocol(event, opts);
  const path = (event.node.req.originalUrl || event.path).replace(
    /^[/\\]+/g,
    "/"
  );
  return new URL(path, `${protocol}://${host}`);
}

const RawBodySymbol = Symbol.for("h3RawBody");
const PayloadMethods$1 = ["PATCH", "POST", "PUT", "DELETE"];
function readRawBody(event, encoding = "utf8") {
  assertMethod(event, PayloadMethods$1);
  const _rawBody = event._requestBody || event.web?.request?.body || event.node.req[RawBodySymbol] || event.node.req.rawBody || event.node.req.body;
  if (_rawBody) {
    const promise2 = Promise.resolve(_rawBody).then((_resolved) => {
      if (Buffer.isBuffer(_resolved)) {
        return _resolved;
      }
      if (typeof _resolved.pipeTo === "function") {
        return new Promise((resolve, reject) => {
          const chunks = [];
          _resolved.pipeTo(
            new WritableStream({
              write(chunk) {
                chunks.push(chunk);
              },
              close() {
                resolve(Buffer.concat(chunks));
              },
              abort(reason) {
                reject(reason);
              }
            })
          ).catch(reject);
        });
      } else if (typeof _resolved.pipe === "function") {
        return new Promise((resolve, reject) => {
          const chunks = [];
          _resolved.on("data", (chunk) => {
            chunks.push(chunk);
          }).on("end", () => {
            resolve(Buffer.concat(chunks));
          }).on("error", reject);
        });
      }
      if (_resolved.constructor === Object) {
        return Buffer.from(JSON.stringify(_resolved));
      }
      if (_resolved instanceof URLSearchParams) {
        return Buffer.from(_resolved.toString());
      }
      if (_resolved instanceof FormData) {
        return new Response(_resolved).bytes().then((uint8arr) => Buffer.from(uint8arr));
      }
      return Buffer.from(_resolved);
    });
    return encoding ? promise2.then((buff) => buff.toString(encoding)) : promise2;
  }
  if (!Number.parseInt(event.node.req.headers["content-length"] || "") && !/\bchunked\b/i.test(
    String(event.node.req.headers["transfer-encoding"] ?? "")
  )) {
    return Promise.resolve(void 0);
  }
  const promise = event.node.req[RawBodySymbol] = new Promise(
    (resolve, reject) => {
      const bodyData = [];
      event.node.req.on("error", (err) => {
        reject(err);
      }).on("data", (chunk) => {
        bodyData.push(chunk);
      }).on("end", () => {
        resolve(Buffer.concat(bodyData));
      });
    }
  );
  const result = encoding ? promise.then((buff) => buff.toString(encoding)) : promise;
  return result;
}
function getRequestWebStream(event) {
  if (!PayloadMethods$1.includes(event.method)) {
    return;
  }
  const bodyStream = event.web?.request?.body || event._requestBody;
  if (bodyStream) {
    return bodyStream;
  }
  const _hasRawBody = RawBodySymbol in event.node.req || "rawBody" in event.node.req || "body" in event.node.req || "__unenv__" in event.node.req;
  if (_hasRawBody) {
    return new ReadableStream({
      async start(controller) {
        const _rawBody = await readRawBody(event, false);
        if (_rawBody) {
          controller.enqueue(_rawBody);
        }
        controller.close();
      }
    });
  }
  return new ReadableStream({
    start: (controller) => {
      event.node.req.on("data", (chunk) => {
        controller.enqueue(chunk);
      });
      event.node.req.on("end", () => {
        controller.close();
      });
      event.node.req.on("error", (err) => {
        controller.error(err);
      });
    }
  });
}

function handleCacheHeaders(event, opts) {
  const cacheControls = ["public", ...opts.cacheControls || []];
  let cacheMatched = false;
  if (opts.maxAge !== void 0) {
    cacheControls.push(`max-age=${+opts.maxAge}`, `s-maxage=${+opts.maxAge}`);
  }
  if (opts.modifiedTime) {
    const modifiedTime = new Date(opts.modifiedTime);
    const ifModifiedSince = event.node.req.headers["if-modified-since"];
    event.node.res.setHeader("last-modified", modifiedTime.toUTCString());
    if (ifModifiedSince && new Date(ifModifiedSince) >= modifiedTime) {
      cacheMatched = true;
    }
  }
  if (opts.etag) {
    event.node.res.setHeader("etag", opts.etag);
    const ifNonMatch = event.node.req.headers["if-none-match"];
    if (ifNonMatch === opts.etag) {
      cacheMatched = true;
    }
  }
  event.node.res.setHeader("cache-control", cacheControls.join(", "));
  if (cacheMatched) {
    event.node.res.statusCode = 304;
    if (!event.handled) {
      event.node.res.end();
    }
    return true;
  }
  return false;
}

const MIMES = {
  html: "text/html",
  json: "application/json"
};

const DISALLOWED_STATUS_CHARS = /[^\u0009\u0020-\u007E]/g;
function sanitizeStatusMessage(statusMessage = "") {
  return statusMessage.replace(DISALLOWED_STATUS_CHARS, "");
}
function sanitizeStatusCode(statusCode, defaultStatusCode = 200) {
  if (!statusCode) {
    return defaultStatusCode;
  }
  if (typeof statusCode === "string") {
    statusCode = Number.parseInt(statusCode, 10);
  }
  if (statusCode < 100 || statusCode > 999) {
    return defaultStatusCode;
  }
  return statusCode;
}
function splitCookiesString(cookiesString) {
  if (Array.isArray(cookiesString)) {
    return cookiesString.flatMap((c) => splitCookiesString(c));
  }
  if (typeof cookiesString !== "string") {
    return [];
  }
  const cookiesStrings = [];
  let pos = 0;
  let start;
  let ch;
  let lastComma;
  let nextStart;
  let cookiesSeparatorFound;
  const skipWhitespace = () => {
    while (pos < cookiesString.length && /\s/.test(cookiesString.charAt(pos))) {
      pos += 1;
    }
    return pos < cookiesString.length;
  };
  const notSpecialChar = () => {
    ch = cookiesString.charAt(pos);
    return ch !== "=" && ch !== ";" && ch !== ",";
  };
  while (pos < cookiesString.length) {
    start = pos;
    cookiesSeparatorFound = false;
    while (skipWhitespace()) {
      ch = cookiesString.charAt(pos);
      if (ch === ",") {
        lastComma = pos;
        pos += 1;
        skipWhitespace();
        nextStart = pos;
        while (pos < cookiesString.length && notSpecialChar()) {
          pos += 1;
        }
        if (pos < cookiesString.length && cookiesString.charAt(pos) === "=") {
          cookiesSeparatorFound = true;
          pos = nextStart;
          cookiesStrings.push(cookiesString.slice(start, lastComma));
          start = pos;
        } else {
          pos = lastComma + 1;
        }
      } else {
        pos += 1;
      }
    }
    if (!cookiesSeparatorFound || pos >= cookiesString.length) {
      cookiesStrings.push(cookiesString.slice(start));
    }
  }
  return cookiesStrings;
}

const defer = typeof setImmediate === "undefined" ? (fn) => fn() : setImmediate;
function send(event, data, type) {
  if (type) {
    defaultContentType(event, type);
  }
  return new Promise((resolve) => {
    defer(() => {
      if (!event.handled) {
        event.node.res.end(data);
      }
      resolve();
    });
  });
}
function sendNoContent(event, code) {
  if (event.handled) {
    return;
  }
  if (!code && event.node.res.statusCode !== 200) {
    code = event.node.res.statusCode;
  }
  const _code = sanitizeStatusCode(code, 204);
  if (_code === 204) {
    event.node.res.removeHeader("content-length");
  }
  event.node.res.writeHead(_code);
  event.node.res.end();
}
function setResponseStatus(event, code, text) {
  if (code) {
    event.node.res.statusCode = sanitizeStatusCode(
      code,
      event.node.res.statusCode
    );
  }
  if (text) {
    event.node.res.statusMessage = sanitizeStatusMessage(text);
  }
}
function getResponseStatus(event) {
  return event.node.res.statusCode;
}
function getResponseStatusText(event) {
  return event.node.res.statusMessage;
}
function defaultContentType(event, type) {
  if (type && event.node.res.statusCode !== 304 && !event.node.res.getHeader("content-type")) {
    event.node.res.setHeader("content-type", type);
  }
}
function sendRedirect(event, location, code = 302) {
  event.node.res.statusCode = sanitizeStatusCode(
    code,
    event.node.res.statusCode
  );
  event.node.res.setHeader("location", location);
  const encodedLoc = location.replace(/"/g, "%22");
  const html = `<!DOCTYPE html><html><head><meta http-equiv="refresh" content="0; url=${encodedLoc}"></head></html>`;
  return send(event, html, MIMES.html);
}
function getResponseHeader(event, name) {
  return event.node.res.getHeader(name);
}
function setResponseHeaders(event, headers) {
  for (const [name, value] of Object.entries(headers)) {
    event.node.res.setHeader(
      name,
      value
    );
  }
}
const setHeaders = setResponseHeaders;
function setResponseHeader(event, name, value) {
  event.node.res.setHeader(name, value);
}
function appendResponseHeader(event, name, value) {
  let current = event.node.res.getHeader(name);
  if (!current) {
    event.node.res.setHeader(name, value);
    return;
  }
  if (!Array.isArray(current)) {
    current = [current.toString()];
  }
  event.node.res.setHeader(name, [...current, value]);
}
function removeResponseHeader(event, name) {
  return event.node.res.removeHeader(name);
}
function isStream(data) {
  if (!data || typeof data !== "object") {
    return false;
  }
  if (typeof data.pipe === "function") {
    if (typeof data._read === "function") {
      return true;
    }
    if (typeof data.abort === "function") {
      return true;
    }
  }
  if (typeof data.pipeTo === "function") {
    return true;
  }
  return false;
}
function isWebResponse(data) {
  return typeof Response !== "undefined" && data instanceof Response;
}
function sendStream(event, stream) {
  if (!stream || typeof stream !== "object") {
    throw new Error("[h3] Invalid stream provided.");
  }
  event.node.res._data = stream;
  if (!event.node.res.socket) {
    event._handled = true;
    return Promise.resolve();
  }
  if (hasProp(stream, "pipeTo") && typeof stream.pipeTo === "function") {
    return stream.pipeTo(
      new WritableStream({
        write(chunk) {
          event.node.res.write(chunk);
        }
      })
    ).then(() => {
      event.node.res.end();
    });
  }
  if (hasProp(stream, "pipe") && typeof stream.pipe === "function") {
    return new Promise((resolve, reject) => {
      stream.pipe(event.node.res);
      if (stream.on) {
        stream.on("end", () => {
          event.node.res.end();
          resolve();
        });
        stream.on("error", (error) => {
          reject(error);
        });
      }
      event.node.res.on("close", () => {
        if (stream.abort) {
          stream.abort();
        }
      });
    });
  }
  throw new Error("[h3] Invalid or incompatible stream provided.");
}
function sendWebResponse(event, response) {
  for (const [key, value] of response.headers) {
    if (key === "set-cookie") {
      event.node.res.appendHeader(key, splitCookiesString(value));
    } else {
      event.node.res.setHeader(key, value);
    }
  }
  if (response.status) {
    event.node.res.statusCode = sanitizeStatusCode(
      response.status,
      event.node.res.statusCode
    );
  }
  if (response.statusText) {
    event.node.res.statusMessage = sanitizeStatusMessage(response.statusText);
  }
  if (response.redirected) {
    event.node.res.setHeader("location", response.url);
  }
  if (!response.body) {
    event.node.res.end();
    return;
  }
  return sendStream(event, response.body);
}

const PayloadMethods = /* @__PURE__ */ new Set(["PATCH", "POST", "PUT", "DELETE"]);
const ignoredHeaders = /* @__PURE__ */ new Set([
  "transfer-encoding",
  "accept-encoding",
  "connection",
  "keep-alive",
  "upgrade",
  "expect",
  "host",
  "accept"
]);
async function proxyRequest(event, target, opts = {}) {
  let body;
  let duplex;
  if (PayloadMethods.has(event.method)) {
    if (opts.streamRequest) {
      body = getRequestWebStream(event);
      duplex = "half";
    } else {
      body = await readRawBody(event, false).catch(() => void 0);
    }
  }
  const method = opts.fetchOptions?.method || event.method;
  const fetchHeaders = mergeHeaders$1(
    getProxyRequestHeaders(event, { host: target.startsWith("/") }),
    opts.fetchOptions?.headers,
    opts.headers
  );
  return sendProxy(event, target, {
    ...opts,
    fetchOptions: {
      method,
      body,
      duplex,
      ...opts.fetchOptions,
      headers: fetchHeaders
    }
  });
}
async function sendProxy(event, target, opts = {}) {
  let response;
  try {
    response = await _getFetch(opts.fetch)(target, {
      headers: opts.headers,
      ignoreResponseError: true,
      // make $ofetch.raw transparent
      ...opts.fetchOptions
    });
  } catch (error) {
    throw createError$1({
      status: 502,
      statusMessage: "Bad Gateway",
      cause: error
    });
  }
  event.node.res.statusCode = sanitizeStatusCode(
    response.status,
    event.node.res.statusCode
  );
  event.node.res.statusMessage = sanitizeStatusMessage(response.statusText);
  const cookies = [];
  for (const [key, value] of response.headers.entries()) {
    if (key === "content-encoding") {
      continue;
    }
    if (key === "content-length") {
      continue;
    }
    if (key === "set-cookie") {
      cookies.push(...splitCookiesString(value));
      continue;
    }
    event.node.res.setHeader(key, value);
  }
  if (cookies.length > 0) {
    event.node.res.setHeader(
      "set-cookie",
      cookies.map((cookie) => {
        if (opts.cookieDomainRewrite) {
          cookie = rewriteCookieProperty(
            cookie,
            opts.cookieDomainRewrite,
            "domain"
          );
        }
        if (opts.cookiePathRewrite) {
          cookie = rewriteCookieProperty(
            cookie,
            opts.cookiePathRewrite,
            "path"
          );
        }
        return cookie;
      })
    );
  }
  if (opts.onResponse) {
    await opts.onResponse(event, response);
  }
  if (response._data !== void 0) {
    return response._data;
  }
  if (event.handled) {
    return;
  }
  if (opts.sendStream === false) {
    const data = new Uint8Array(await response.arrayBuffer());
    return event.node.res.end(data);
  }
  if (response.body) {
    for await (const chunk of response.body) {
      event.node.res.write(chunk);
    }
  }
  return event.node.res.end();
}
function getProxyRequestHeaders(event, opts) {
  const headers = /* @__PURE__ */ Object.create(null);
  const reqHeaders = getRequestHeaders(event);
  for (const name in reqHeaders) {
    if (!ignoredHeaders.has(name) || name === "host" && opts?.host) {
      headers[name] = reqHeaders[name];
    }
  }
  return headers;
}
function fetchWithEvent(event, req, init, options) {
  return _getFetch(options?.fetch)(req, {
    ...init,
    context: init?.context || event.context,
    headers: {
      ...getProxyRequestHeaders(event, {
        host: typeof req === "string" && req.startsWith("/")
      }),
      ...init?.headers
    }
  });
}
function _getFetch(_fetch) {
  if (_fetch) {
    return _fetch;
  }
  if (globalThis.fetch) {
    return globalThis.fetch;
  }
  throw new Error(
    "fetch is not available. Try importing `node-fetch-native/polyfill` for Node.js."
  );
}
function rewriteCookieProperty(header, map, property) {
  const _map = typeof map === "string" ? { "*": map } : map;
  return header.replace(
    new RegExp(`(;\\s*${property}=)([^;]+)`, "gi"),
    (match, prefix, previousValue) => {
      let newValue;
      if (previousValue in _map) {
        newValue = _map[previousValue];
      } else if ("*" in _map) {
        newValue = _map["*"];
      } else {
        return match;
      }
      return newValue ? prefix + newValue : "";
    }
  );
}
function mergeHeaders$1(defaults, ...inputs) {
  const _inputs = inputs.filter(Boolean);
  if (_inputs.length === 0) {
    return defaults;
  }
  const merged = new Headers(defaults);
  for (const input of _inputs) {
    const entries = Array.isArray(input) ? input : typeof input.entries === "function" ? input.entries() : Object.entries(input);
    for (const [key, value] of entries) {
      if (value !== void 0) {
        merged.set(key, value);
      }
    }
  }
  return merged;
}

class H3Event {
  "__is_event__" = true;
  // Context
  node;
  // Node
  web;
  // Web
  context = {};
  // Shared
  // Request
  _method;
  _path;
  _headers;
  _requestBody;
  // Response
  _handled = false;
  // Hooks
  _onBeforeResponseCalled;
  _onAfterResponseCalled;
  constructor(req, res) {
    this.node = { req, res };
  }
  // --- Request ---
  get method() {
    if (!this._method) {
      this._method = (this.node.req.method || "GET").toUpperCase();
    }
    return this._method;
  }
  get path() {
    return this._path || this.node.req.url || "/";
  }
  get headers() {
    if (!this._headers) {
      this._headers = _normalizeNodeHeaders(this.node.req.headers);
    }
    return this._headers;
  }
  // --- Respoonse ---
  get handled() {
    return this._handled || this.node.res.writableEnded || this.node.res.headersSent;
  }
  respondWith(response) {
    return Promise.resolve(response).then(
      (_response) => sendWebResponse(this, _response)
    );
  }
  // --- Utils ---
  toString() {
    return `[${this.method}] ${this.path}`;
  }
  toJSON() {
    return this.toString();
  }
  // --- Deprecated ---
  /** @deprecated Please use `event.node.req` instead. */
  get req() {
    return this.node.req;
  }
  /** @deprecated Please use `event.node.res` instead. */
  get res() {
    return this.node.res;
  }
}
function isEvent(input) {
  return hasProp(input, "__is_event__");
}
function createEvent(req, res) {
  return new H3Event(req, res);
}
function _normalizeNodeHeaders(nodeHeaders) {
  const headers = new Headers();
  for (const [name, value] of Object.entries(nodeHeaders)) {
    if (Array.isArray(value)) {
      for (const item of value) {
        headers.append(name, item);
      }
    } else if (value) {
      headers.set(name, value);
    }
  }
  return headers;
}

function defineEventHandler(handler) {
  if (typeof handler === "function") {
    handler.__is_handler__ = true;
    return handler;
  }
  const _hooks = {
    onRequest: _normalizeArray(handler.onRequest),
    onBeforeResponse: _normalizeArray(handler.onBeforeResponse)
  };
  const _handler = (event) => {
    return _callHandler(event, handler.handler, _hooks);
  };
  _handler.__is_handler__ = true;
  _handler.__resolve__ = handler.handler.__resolve__;
  _handler.__websocket__ = handler.websocket;
  return _handler;
}
function _normalizeArray(input) {
  return input ? Array.isArray(input) ? input : [input] : void 0;
}
async function _callHandler(event, handler, hooks) {
  if (hooks.onRequest) {
    for (const hook of hooks.onRequest) {
      await hook(event);
      if (event.handled) {
        return;
      }
    }
  }
  const body = await handler(event);
  const response = { body };
  if (hooks.onBeforeResponse) {
    for (const hook of hooks.onBeforeResponse) {
      await hook(event, response);
    }
  }
  return response.body;
}
const eventHandler = defineEventHandler;
function isEventHandler(input) {
  return hasProp(input, "__is_handler__");
}
function toEventHandler(input, _, _route) {
  return input;
}
function defineLazyEventHandler(factory) {
  let _promise;
  let _resolved;
  const resolveHandler = () => {
    if (_resolved) {
      return Promise.resolve(_resolved);
    }
    if (!_promise) {
      _promise = Promise.resolve(factory()).then((r) => {
        const handler2 = r.default || r;
        if (typeof handler2 !== "function") {
          throw new TypeError(
            "Invalid lazy handler result. It should be a function:",
            handler2
          );
        }
        _resolved = { handler: toEventHandler(r.default || r) };
        return _resolved;
      });
    }
    return _promise;
  };
  const handler = eventHandler((event) => {
    if (_resolved) {
      return _resolved.handler(event);
    }
    return resolveHandler().then((r) => r.handler(event));
  });
  handler.__resolve__ = resolveHandler;
  return handler;
}
const lazyEventHandler = defineLazyEventHandler;

function createApp(options = {}) {
  const stack = [];
  const handler = createAppEventHandler(stack, options);
  const resolve = createResolver(stack);
  handler.__resolve__ = resolve;
  const getWebsocket = cachedFn(() => websocketOptions(resolve, options));
  const app = {
    // @ts-expect-error
    use: (arg1, arg2, arg3) => use(app, arg1, arg2, arg3),
    resolve,
    handler,
    stack,
    options,
    get websocket() {
      return getWebsocket();
    }
  };
  return app;
}
function use(app, arg1, arg2, arg3) {
  if (Array.isArray(arg1)) {
    for (const i of arg1) {
      use(app, i, arg2, arg3);
    }
  } else if (Array.isArray(arg2)) {
    for (const i of arg2) {
      use(app, arg1, i, arg3);
    }
  } else if (typeof arg1 === "string") {
    app.stack.push(
      normalizeLayer({ ...arg3, route: arg1, handler: arg2 })
    );
  } else if (typeof arg1 === "function") {
    app.stack.push(normalizeLayer({ ...arg2, handler: arg1 }));
  } else {
    app.stack.push(normalizeLayer({ ...arg1 }));
  }
  return app;
}
function createAppEventHandler(stack, options) {
  const spacing = options.debug ? 2 : void 0;
  return eventHandler(async (event) => {
    event.node.req.originalUrl = event.node.req.originalUrl || event.node.req.url || "/";
    const _rawReqUrl = event.node.req.url || "/";
    const _reqPath = _decodePath(event._path || _rawReqUrl);
    event._path = _reqPath;
    const _needsRawUrl = _reqPath !== _rawReqUrl;
    let _layerPath;
    if (options.onRequest) {
      await options.onRequest(event);
    }
    for (const layer of stack) {
      if (layer.route.length > 1) {
        if (!_reqPath.startsWith(layer.route)) {
          continue;
        }
        _layerPath = _reqPath.slice(layer.route.length) || "/";
      } else {
        _layerPath = _reqPath;
      }
      if (layer.match && !layer.match(_layerPath, event)) {
        continue;
      }
      event._path = _layerPath;
      event.node.req.url = _needsRawUrl ? layer.route.length > 1 ? _rawReqUrl.slice(layer.route.length) || "/" : _rawReqUrl : _layerPath;
      const val = await layer.handler(event);
      const _body = val === void 0 ? void 0 : await val;
      if (_body !== void 0) {
        const _response = { body: _body };
        if (options.onBeforeResponse) {
          event._onBeforeResponseCalled = true;
          await options.onBeforeResponse(event, _response);
        }
        await handleHandlerResponse(event, _response.body, spacing);
        if (options.onAfterResponse) {
          event._onAfterResponseCalled = true;
          await options.onAfterResponse(event, _response);
        }
        return;
      }
      if (event.handled) {
        if (options.onAfterResponse) {
          event._onAfterResponseCalled = true;
          await options.onAfterResponse(event, void 0);
        }
        return;
      }
    }
    if (!event.handled) {
      throw createError$1({
        statusCode: 404,
        statusMessage: `Cannot find any path matching ${event.path || "/"}.`
      });
    }
    if (options.onAfterResponse) {
      event._onAfterResponseCalled = true;
      await options.onAfterResponse(event, void 0);
    }
  });
}
function createResolver(stack) {
  return async (path) => {
    let _layerPath;
    for (const layer of stack) {
      if (layer.route === "/" && !layer.handler.__resolve__) {
        continue;
      }
      if (!path.startsWith(layer.route)) {
        continue;
      }
      _layerPath = path.slice(layer.route.length) || "/";
      if (layer.match && !layer.match(_layerPath, void 0)) {
        continue;
      }
      let res = { route: layer.route, handler: layer.handler };
      if (res.handler.__resolve__) {
        const _res = await res.handler.__resolve__(_layerPath);
        if (!_res) {
          continue;
        }
        res = {
          ...res,
          ..._res,
          route: joinURL(res.route || "/", _res.route || "/")
        };
      }
      return res;
    }
  };
}
function normalizeLayer(input) {
  let handler = input.handler;
  if (handler.handler) {
    handler = handler.handler;
  }
  if (input.lazy) {
    handler = lazyEventHandler(handler);
  } else if (!isEventHandler(handler)) {
    handler = toEventHandler(handler, void 0, input.route);
  }
  return {
    route: withoutTrailingSlash(input.route),
    match: input.match,
    handler
  };
}
function handleHandlerResponse(event, val, jsonSpace) {
  if (val === null) {
    return sendNoContent(event);
  }
  if (val) {
    if (isWebResponse(val)) {
      return sendWebResponse(event, val);
    }
    if (isStream(val)) {
      return sendStream(event, val);
    }
    if (val.buffer) {
      return send(event, val);
    }
    if (val.arrayBuffer && typeof val.arrayBuffer === "function") {
      return val.arrayBuffer().then((arrayBuffer) => {
        return send(event, Buffer.from(arrayBuffer), val.type);
      });
    }
    if (val instanceof Error) {
      throw createError$1(val);
    }
    if (typeof val.end === "function") {
      return true;
    }
  }
  const valType = typeof val;
  if (valType === "string") {
    return send(event, val, MIMES.html);
  }
  if (valType === "object" || valType === "boolean" || valType === "number") {
    return send(event, JSON.stringify(val, void 0, jsonSpace), MIMES.json);
  }
  if (valType === "bigint") {
    return send(event, val.toString(), MIMES.json);
  }
  throw createError$1({
    statusCode: 500,
    statusMessage: `[h3] Cannot send ${valType} as response.`
  });
}
function cachedFn(fn) {
  let cache;
  return () => {
    if (!cache) {
      cache = fn();
    }
    return cache;
  };
}
function _decodePath(url) {
  const qIndex = url.indexOf("?");
  const path = qIndex === -1 ? url : url.slice(0, qIndex);
  const query = qIndex === -1 ? "" : url.slice(qIndex);
  const decodedPath = path.includes("%25") ? decodePath(path.replace(/%25/g, "%2525")) : decodePath(path);
  return decodedPath + query;
}
function websocketOptions(evResolver, appOptions) {
  return {
    ...appOptions.websocket,
    async resolve(info) {
      const url = info.request?.url || info.url || "/";
      const { pathname } = typeof url === "string" ? parseURL(url) : url;
      const resolved = await evResolver(pathname);
      return resolved?.handler?.__websocket__ || {};
    }
  };
}

const RouterMethods = [
  "connect",
  "delete",
  "get",
  "head",
  "options",
  "post",
  "put",
  "trace",
  "patch"
];
function createRouter(opts = {}) {
  const _router = createRouter$1({});
  const routes = {};
  let _matcher;
  const router = {};
  const addRoute = (path, handler, method) => {
    let route = routes[path];
    if (!route) {
      routes[path] = route = { path, handlers: {} };
      _router.insert(path, route);
    }
    if (Array.isArray(method)) {
      for (const m of method) {
        addRoute(path, handler, m);
      }
    } else {
      route.handlers[method] = toEventHandler(handler);
    }
    return router;
  };
  router.use = router.add = (path, handler, method) => addRoute(path, handler, method || "all");
  for (const method of RouterMethods) {
    router[method] = (path, handle) => router.add(path, handle, method);
  }
  const matchHandler = (path = "/", method = "get") => {
    const qIndex = path.indexOf("?");
    if (qIndex !== -1) {
      path = path.slice(0, Math.max(0, qIndex));
    }
    const matched = _router.lookup(path);
    if (!matched || !matched.handlers) {
      return {
        error: createError$1({
          statusCode: 404,
          name: "Not Found",
          statusMessage: `Cannot find any route matching ${path || "/"}.`
        })
      };
    }
    let handler = matched.handlers[method] || matched.handlers.all;
    if (!handler) {
      if (!_matcher) {
        _matcher = toRouteMatcher(_router);
      }
      const _matches = _matcher.matchAll(path).reverse();
      for (const _match of _matches) {
        if (_match.handlers[method]) {
          handler = _match.handlers[method];
          matched.handlers[method] = matched.handlers[method] || handler;
          break;
        }
        if (_match.handlers.all) {
          handler = _match.handlers.all;
          matched.handlers.all = matched.handlers.all || handler;
          break;
        }
      }
    }
    if (!handler) {
      return {
        error: createError$1({
          statusCode: 405,
          name: "Method Not Allowed",
          statusMessage: `Method ${method} is not allowed on this route.`
        })
      };
    }
    return { matched, handler };
  };
  const isPreemptive = opts.preemptive || opts.preemtive;
  router.handler = eventHandler((event) => {
    const match = matchHandler(
      event.path,
      event.method.toLowerCase()
    );
    if ("error" in match) {
      if (isPreemptive) {
        throw match.error;
      } else {
        return;
      }
    }
    event.context.matchedRoute = match.matched;
    const params = match.matched.params || {};
    event.context.params = params;
    return Promise.resolve(match.handler(event)).then((res) => {
      if (res === void 0 && isPreemptive) {
        return null;
      }
      return res;
    });
  });
  router.handler.__resolve__ = async (path) => {
    path = withLeadingSlash(path);
    const match = matchHandler(path);
    if ("error" in match) {
      return;
    }
    let res = {
      route: match.matched.path,
      handler: match.handler
    };
    if (match.handler.__resolve__) {
      const _res = await match.handler.__resolve__(path);
      if (!_res) {
        return;
      }
      res = { ...res, ..._res };
    }
    return res;
  };
  return router;
}
function toNodeListener(app) {
  const toNodeHandle = async function(req, res) {
    const event = createEvent(req, res);
    try {
      await app.handler(event);
    } catch (_error) {
      const error = createError$1(_error);
      if (!isError(_error)) {
        error.unhandled = true;
      }
      setResponseStatus(event, error.statusCode, error.statusMessage);
      if (app.options.onError) {
        await app.options.onError(error, event);
      }
      if (event.handled) {
        return;
      }
      if (error.unhandled || error.fatal) {
        console.error("[h3]", error.fatal ? "[fatal]" : "[unhandled]", error);
      }
      if (app.options.onBeforeResponse && !event._onBeforeResponseCalled) {
        await app.options.onBeforeResponse(event, { body: error });
      }
      await sendError(event, error, !!app.options.debug);
      if (app.options.onAfterResponse && !event._onAfterResponseCalled) {
        await app.options.onAfterResponse(event, { body: error });
      }
    }
  };
  return toNodeHandle;
}

function flatHooks(configHooks, hooks = {}, parentName) {
  for (const key in configHooks) {
    const subHook = configHooks[key];
    const name = parentName ? `${parentName}:${key}` : key;
    if (typeof subHook === "object" && subHook !== null) {
      flatHooks(subHook, hooks, name);
    } else if (typeof subHook === "function") {
      hooks[name] = subHook;
    }
  }
  return hooks;
}
const defaultTask = { run: (function_) => function_() };
const _createTask = () => defaultTask;
const createTask = typeof console.createTask !== "undefined" ? console.createTask : _createTask;
function serialTaskCaller(hooks, args) {
  const name = args.shift();
  const task = createTask(name);
  return hooks.reduce(
    (promise, hookFunction) => promise.then(() => task.run(() => hookFunction(...args))),
    Promise.resolve()
  );
}
function parallelTaskCaller(hooks, args) {
  const name = args.shift();
  const task = createTask(name);
  return Promise.all(hooks.map((hook) => task.run(() => hook(...args))));
}
function callEachWith(callbacks, arg0) {
  for (const callback of [...callbacks]) {
    callback(arg0);
  }
}

class Hookable {
  constructor() {
    this._hooks = {};
    this._before = void 0;
    this._after = void 0;
    this._deprecatedMessages = void 0;
    this._deprecatedHooks = {};
    this.hook = this.hook.bind(this);
    this.callHook = this.callHook.bind(this);
    this.callHookWith = this.callHookWith.bind(this);
  }
  hook(name, function_, options = {}) {
    if (!name || typeof function_ !== "function") {
      return () => {
      };
    }
    const originalName = name;
    let dep;
    while (this._deprecatedHooks[name]) {
      dep = this._deprecatedHooks[name];
      name = dep.to;
    }
    if (dep && !options.allowDeprecated) {
      let message = dep.message;
      if (!message) {
        message = `${originalName} hook has been deprecated` + (dep.to ? `, please use ${dep.to}` : "");
      }
      if (!this._deprecatedMessages) {
        this._deprecatedMessages = /* @__PURE__ */ new Set();
      }
      if (!this._deprecatedMessages.has(message)) {
        console.warn(message);
        this._deprecatedMessages.add(message);
      }
    }
    if (!function_.name) {
      try {
        Object.defineProperty(function_, "name", {
          get: () => "_" + name.replace(/\W+/g, "_") + "_hook_cb",
          configurable: true
        });
      } catch {
      }
    }
    this._hooks[name] = this._hooks[name] || [];
    this._hooks[name].push(function_);
    return () => {
      if (function_) {
        this.removeHook(name, function_);
        function_ = void 0;
      }
    };
  }
  hookOnce(name, function_) {
    let _unreg;
    let _function = (...arguments_) => {
      if (typeof _unreg === "function") {
        _unreg();
      }
      _unreg = void 0;
      _function = void 0;
      return function_(...arguments_);
    };
    _unreg = this.hook(name, _function);
    return _unreg;
  }
  removeHook(name, function_) {
    if (this._hooks[name]) {
      const index = this._hooks[name].indexOf(function_);
      if (index !== -1) {
        this._hooks[name].splice(index, 1);
      }
      if (this._hooks[name].length === 0) {
        delete this._hooks[name];
      }
    }
  }
  deprecateHook(name, deprecated) {
    this._deprecatedHooks[name] = typeof deprecated === "string" ? { to: deprecated } : deprecated;
    const _hooks = this._hooks[name] || [];
    delete this._hooks[name];
    for (const hook of _hooks) {
      this.hook(name, hook);
    }
  }
  deprecateHooks(deprecatedHooks) {
    Object.assign(this._deprecatedHooks, deprecatedHooks);
    for (const name in deprecatedHooks) {
      this.deprecateHook(name, deprecatedHooks[name]);
    }
  }
  addHooks(configHooks) {
    const hooks = flatHooks(configHooks);
    const removeFns = Object.keys(hooks).map(
      (key) => this.hook(key, hooks[key])
    );
    return () => {
      for (const unreg of removeFns.splice(0, removeFns.length)) {
        unreg();
      }
    };
  }
  removeHooks(configHooks) {
    const hooks = flatHooks(configHooks);
    for (const key in hooks) {
      this.removeHook(key, hooks[key]);
    }
  }
  removeAllHooks() {
    for (const key in this._hooks) {
      delete this._hooks[key];
    }
  }
  callHook(name, ...arguments_) {
    arguments_.unshift(name);
    return this.callHookWith(serialTaskCaller, name, ...arguments_);
  }
  callHookParallel(name, ...arguments_) {
    arguments_.unshift(name);
    return this.callHookWith(parallelTaskCaller, name, ...arguments_);
  }
  callHookWith(caller, name, ...arguments_) {
    const event = this._before || this._after ? { name, args: arguments_, context: {} } : void 0;
    if (this._before) {
      callEachWith(this._before, event);
    }
    const result = caller(
      name in this._hooks ? [...this._hooks[name]] : [],
      arguments_
    );
    if (result instanceof Promise) {
      return result.finally(() => {
        if (this._after && event) {
          callEachWith(this._after, event);
        }
      });
    }
    if (this._after && event) {
      callEachWith(this._after, event);
    }
    return result;
  }
  beforeEach(function_) {
    this._before = this._before || [];
    this._before.push(function_);
    return () => {
      if (this._before !== void 0) {
        const index = this._before.indexOf(function_);
        if (index !== -1) {
          this._before.splice(index, 1);
        }
      }
    };
  }
  afterEach(function_) {
    this._after = this._after || [];
    this._after.push(function_);
    return () => {
      if (this._after !== void 0) {
        const index = this._after.indexOf(function_);
        if (index !== -1) {
          this._after.splice(index, 1);
        }
      }
    };
  }
}
function createHooks() {
  return new Hookable();
}

const s$1=globalThis.Headers,i=globalThis.AbortController,l=globalThis.fetch||(()=>{throw new Error("[node-fetch-native] Failed to fetch: `globalThis.fetch` is not available!")});

class FetchError extends Error {
  constructor(message, opts) {
    super(message, opts);
    this.name = "FetchError";
    if (opts?.cause && !this.cause) {
      this.cause = opts.cause;
    }
  }
}
function createFetchError(ctx) {
  const errorMessage = ctx.error?.message || ctx.error?.toString() || "";
  const method = ctx.request?.method || ctx.options?.method || "GET";
  const url = ctx.request?.url || String(ctx.request) || "/";
  const requestStr = `[${method}] ${JSON.stringify(url)}`;
  const statusStr = ctx.response ? `${ctx.response.status} ${ctx.response.statusText}` : "<no response>";
  const message = `${requestStr}: ${statusStr}${errorMessage ? ` ${errorMessage}` : ""}`;
  const fetchError = new FetchError(
    message,
    ctx.error ? { cause: ctx.error } : void 0
  );
  for (const key of ["request", "options", "response"]) {
    Object.defineProperty(fetchError, key, {
      get() {
        return ctx[key];
      }
    });
  }
  for (const [key, refKey] of [
    ["data", "_data"],
    ["status", "status"],
    ["statusCode", "status"],
    ["statusText", "statusText"],
    ["statusMessage", "statusText"]
  ]) {
    Object.defineProperty(fetchError, key, {
      get() {
        return ctx.response && ctx.response[refKey];
      }
    });
  }
  return fetchError;
}

const payloadMethods = new Set(
  Object.freeze(["PATCH", "POST", "PUT", "DELETE"])
);
function isPayloadMethod(method = "GET") {
  return payloadMethods.has(method.toUpperCase());
}
function isJSONSerializable(value) {
  if (value === void 0) {
    return false;
  }
  const t = typeof value;
  if (t === "string" || t === "number" || t === "boolean" || t === null) {
    return true;
  }
  if (t !== "object") {
    return false;
  }
  if (Array.isArray(value)) {
    return true;
  }
  if (value.buffer) {
    return false;
  }
  if (value instanceof FormData || value instanceof URLSearchParams) {
    return false;
  }
  return value.constructor && value.constructor.name === "Object" || typeof value.toJSON === "function";
}
const textTypes = /* @__PURE__ */ new Set([
  "image/svg",
  "application/xml",
  "application/xhtml",
  "application/html"
]);
const JSON_RE = /^application\/(?:[\w!#$%&*.^`~-]*\+)?json(;.+)?$/i;
function detectResponseType(_contentType = "") {
  if (!_contentType) {
    return "json";
  }
  const contentType = _contentType.split(";").shift() || "";
  if (JSON_RE.test(contentType)) {
    return "json";
  }
  if (contentType === "text/event-stream") {
    return "stream";
  }
  if (textTypes.has(contentType) || contentType.startsWith("text/")) {
    return "text";
  }
  return "blob";
}
function resolveFetchOptions(request, input, defaults, Headers) {
  const headers = mergeHeaders(
    input?.headers ?? request?.headers,
    defaults?.headers,
    Headers
  );
  let query;
  if (defaults?.query || defaults?.params || input?.params || input?.query) {
    query = {
      ...defaults?.params,
      ...defaults?.query,
      ...input?.params,
      ...input?.query
    };
  }
  return {
    ...defaults,
    ...input,
    query,
    params: query,
    headers
  };
}
function mergeHeaders(input, defaults, Headers) {
  if (!defaults) {
    return new Headers(input);
  }
  const headers = new Headers(defaults);
  if (input) {
    for (const [key, value] of Symbol.iterator in input || Array.isArray(input) ? input : new Headers(input)) {
      headers.set(key, value);
    }
  }
  return headers;
}
async function callHooks(context, hooks) {
  if (hooks) {
    if (Array.isArray(hooks)) {
      for (const hook of hooks) {
        await hook(context);
      }
    } else {
      await hooks(context);
    }
  }
}

const retryStatusCodes = /* @__PURE__ */ new Set([
  408,
  // Request Timeout
  409,
  // Conflict
  425,
  // Too Early (Experimental)
  429,
  // Too Many Requests
  500,
  // Internal Server Error
  502,
  // Bad Gateway
  503,
  // Service Unavailable
  504
  // Gateway Timeout
]);
const nullBodyResponses = /* @__PURE__ */ new Set([101, 204, 205, 304]);
function createFetch(globalOptions = {}) {
  const {
    fetch = globalThis.fetch,
    Headers = globalThis.Headers,
    AbortController = globalThis.AbortController
  } = globalOptions;
  async function onError(context) {
    const isAbort = context.error && context.error.name === "AbortError" && !context.options.timeout || false;
    if (context.options.retry !== false && !isAbort) {
      let retries;
      if (typeof context.options.retry === "number") {
        retries = context.options.retry;
      } else {
        retries = isPayloadMethod(context.options.method) ? 0 : 1;
      }
      const responseCode = context.response && context.response.status || 500;
      if (retries > 0 && (Array.isArray(context.options.retryStatusCodes) ? context.options.retryStatusCodes.includes(responseCode) : retryStatusCodes.has(responseCode))) {
        const retryDelay = typeof context.options.retryDelay === "function" ? context.options.retryDelay(context) : context.options.retryDelay || 0;
        if (retryDelay > 0) {
          await new Promise((resolve) => setTimeout(resolve, retryDelay));
        }
        return $fetchRaw(context.request, {
          ...context.options,
          retry: retries - 1
        });
      }
    }
    const error = createFetchError(context);
    if (Error.captureStackTrace) {
      Error.captureStackTrace(error, $fetchRaw);
    }
    throw error;
  }
  const $fetchRaw = async function $fetchRaw2(_request, _options = {}) {
    const context = {
      request: _request,
      options: resolveFetchOptions(
        _request,
        _options,
        globalOptions.defaults,
        Headers
      ),
      response: void 0,
      error: void 0
    };
    if (context.options.method) {
      context.options.method = context.options.method.toUpperCase();
    }
    if (context.options.onRequest) {
      await callHooks(context, context.options.onRequest);
      if (!(context.options.headers instanceof Headers)) {
        context.options.headers = new Headers(
          context.options.headers || {}
          /* compat */
        );
      }
    }
    if (typeof context.request === "string") {
      if (context.options.baseURL) {
        context.request = withBase(context.request, context.options.baseURL);
      }
      if (context.options.query) {
        context.request = withQuery(context.request, context.options.query);
        delete context.options.query;
      }
      if ("query" in context.options) {
        delete context.options.query;
      }
      if ("params" in context.options) {
        delete context.options.params;
      }
    }
    if (context.options.body && isPayloadMethod(context.options.method)) {
      if (isJSONSerializable(context.options.body)) {
        const contentType = context.options.headers.get("content-type");
        if (typeof context.options.body !== "string") {
          context.options.body = contentType === "application/x-www-form-urlencoded" ? new URLSearchParams(
            context.options.body
          ).toString() : JSON.stringify(context.options.body);
        }
        if (!contentType) {
          context.options.headers.set("content-type", "application/json");
        }
        if (!context.options.headers.has("accept")) {
          context.options.headers.set("accept", "application/json");
        }
      } else if (
        // ReadableStream Body
        "pipeTo" in context.options.body && typeof context.options.body.pipeTo === "function" || // Node.js Stream Body
        typeof context.options.body.pipe === "function"
      ) {
        if (!("duplex" in context.options)) {
          context.options.duplex = "half";
        }
      }
    }
    let abortTimeout;
    if (!context.options.signal && context.options.timeout) {
      const controller = new AbortController();
      abortTimeout = setTimeout(() => {
        const error = new Error(
          "[TimeoutError]: The operation was aborted due to timeout"
        );
        error.name = "TimeoutError";
        error.code = 23;
        controller.abort(error);
      }, context.options.timeout);
      context.options.signal = controller.signal;
    }
    try {
      context.response = await fetch(
        context.request,
        context.options
      );
    } catch (error) {
      context.error = error;
      if (context.options.onRequestError) {
        await callHooks(
          context,
          context.options.onRequestError
        );
      }
      return await onError(context);
    } finally {
      if (abortTimeout) {
        clearTimeout(abortTimeout);
      }
    }
    const hasBody = (context.response.body || // https://github.com/unjs/ofetch/issues/324
    // https://github.com/unjs/ofetch/issues/294
    // https://github.com/JakeChampion/fetch/issues/1454
    context.response._bodyInit) && !nullBodyResponses.has(context.response.status) && context.options.method !== "HEAD";
    if (hasBody) {
      const responseType = (context.options.parseResponse ? "json" : context.options.responseType) || detectResponseType(context.response.headers.get("content-type") || "");
      switch (responseType) {
        case "json": {
          const data = await context.response.text();
          const parseFunction = context.options.parseResponse || destr;
          context.response._data = parseFunction(data);
          break;
        }
        case "stream": {
          context.response._data = context.response.body || context.response._bodyInit;
          break;
        }
        default: {
          context.response._data = await context.response[responseType]();
        }
      }
    }
    if (context.options.onResponse) {
      await callHooks(
        context,
        context.options.onResponse
      );
    }
    if (!context.options.ignoreResponseError && context.response.status >= 400 && context.response.status < 600) {
      if (context.options.onResponseError) {
        await callHooks(
          context,
          context.options.onResponseError
        );
      }
      return await onError(context);
    }
    return context.response;
  };
  const $fetch = async function $fetch2(request, options) {
    const r = await $fetchRaw(request, options);
    return r._data;
  };
  $fetch.raw = $fetchRaw;
  $fetch.native = (...args) => fetch(...args);
  $fetch.create = (defaultOptions = {}, customGlobalOptions = {}) => createFetch({
    ...globalOptions,
    ...customGlobalOptions,
    defaults: {
      ...globalOptions.defaults,
      ...customGlobalOptions.defaults,
      ...defaultOptions
    }
  });
  return $fetch;
}

function createNodeFetch() {
  const useKeepAlive = JSON.parse(process.env.FETCH_KEEP_ALIVE || "false");
  if (!useKeepAlive) {
    return l;
  }
  const agentOptions = { keepAlive: true };
  const httpAgent = new http.Agent(agentOptions);
  const httpsAgent = new https.Agent(agentOptions);
  const nodeFetchOptions = {
    agent(parsedURL) {
      return parsedURL.protocol === "http:" ? httpAgent : httpsAgent;
    }
  };
  return function nodeFetchWithKeepAlive(input, init) {
    return l(input, { ...nodeFetchOptions, ...init });
  };
}
const fetch = globalThis.fetch ? (...args) => globalThis.fetch(...args) : createNodeFetch();
const Headers$1 = globalThis.Headers || s$1;
const AbortController = globalThis.AbortController || i;
createFetch({ fetch, Headers: Headers$1, AbortController });

function wrapToPromise(value) {
  if (!value || typeof value.then !== "function") {
    return Promise.resolve(value);
  }
  return value;
}
function asyncCall(function_, ...arguments_) {
  try {
    return wrapToPromise(function_(...arguments_));
  } catch (error) {
    return Promise.reject(error);
  }
}
function isPrimitive(value) {
  const type = typeof value;
  return value === null || type !== "object" && type !== "function";
}
function isPureObject(value) {
  const proto = Object.getPrototypeOf(value);
  return !proto || proto.isPrototypeOf(Object);
}
function stringify(value) {
  if (isPrimitive(value)) {
    return String(value);
  }
  if (isPureObject(value) || Array.isArray(value)) {
    return JSON.stringify(value);
  }
  if (typeof value.toJSON === "function") {
    return stringify(value.toJSON());
  }
  throw new Error("[unstorage] Cannot stringify value!");
}
const BASE64_PREFIX = "base64:";
function serializeRaw(value) {
  if (typeof value === "string") {
    return value;
  }
  return BASE64_PREFIX + base64Encode(value);
}
function deserializeRaw(value) {
  if (typeof value !== "string") {
    return value;
  }
  if (!value.startsWith(BASE64_PREFIX)) {
    return value;
  }
  return base64Decode(value.slice(BASE64_PREFIX.length));
}
function base64Decode(input) {
  if (globalThis.Buffer) {
    return Buffer.from(input, "base64");
  }
  return Uint8Array.from(
    globalThis.atob(input),
    (c) => c.codePointAt(0)
  );
}
function base64Encode(input) {
  if (globalThis.Buffer) {
    return Buffer.from(input).toString("base64");
  }
  return globalThis.btoa(String.fromCodePoint(...input));
}

const storageKeyProperties = [
  "has",
  "hasItem",
  "get",
  "getItem",
  "getItemRaw",
  "set",
  "setItem",
  "setItemRaw",
  "del",
  "remove",
  "removeItem",
  "getMeta",
  "setMeta",
  "removeMeta",
  "getKeys",
  "clear",
  "mount",
  "unmount"
];
function prefixStorage(storage, base) {
  base = normalizeBaseKey(base);
  if (!base) {
    return storage;
  }
  const nsStorage = { ...storage };
  for (const property of storageKeyProperties) {
    nsStorage[property] = (key = "", ...args) => (
      // @ts-ignore
      storage[property](base + key, ...args)
    );
  }
  nsStorage.getKeys = (key = "", ...arguments_) => storage.getKeys(base + key, ...arguments_).then((keys) => keys.map((key2) => key2.slice(base.length)));
  nsStorage.keys = nsStorage.getKeys;
  nsStorage.getItems = async (items, commonOptions) => {
    const prefixedItems = items.map(
      (item) => typeof item === "string" ? base + item : { ...item, key: base + item.key }
    );
    const results = await storage.getItems(prefixedItems, commonOptions);
    return results.map((entry) => ({
      key: entry.key.slice(base.length),
      value: entry.value
    }));
  };
  nsStorage.setItems = async (items, commonOptions) => {
    const prefixedItems = items.map((item) => ({
      key: base + item.key,
      value: item.value,
      options: item.options
    }));
    return storage.setItems(prefixedItems, commonOptions);
  };
  return nsStorage;
}
function normalizeKey$1(key) {
  if (!key) {
    return "";
  }
  return key.split("?")[0]?.replace(/[/\\]/g, ":").replace(/:+/g, ":").replace(/^:|:$/g, "") || "";
}
function joinKeys(...keys) {
  return normalizeKey$1(keys.join(":"));
}
function normalizeBaseKey(base) {
  base = normalizeKey$1(base);
  return base ? base + ":" : "";
}
function filterKeyByDepth(key, depth) {
  if (depth === void 0) {
    return true;
  }
  let substrCount = 0;
  let index = key.indexOf(":");
  while (index > -1) {
    substrCount++;
    index = key.indexOf(":", index + 1);
  }
  return substrCount <= depth;
}
function filterKeyByBase(key, base) {
  if (base) {
    return key.startsWith(base) && key[key.length - 1] !== "$";
  }
  return key[key.length - 1] !== "$";
}

function defineDriver$1(factory) {
  return factory;
}

const DRIVER_NAME$1 = "memory";
const memory = defineDriver$1(() => {
  const data = /* @__PURE__ */ new Map();
  return {
    name: DRIVER_NAME$1,
    getInstance: () => data,
    hasItem(key) {
      return data.has(key);
    },
    getItem(key) {
      return data.get(key) ?? null;
    },
    getItemRaw(key) {
      return data.get(key) ?? null;
    },
    setItem(key, value) {
      data.set(key, value);
    },
    setItemRaw(key, value) {
      data.set(key, value);
    },
    removeItem(key) {
      data.delete(key);
    },
    getKeys() {
      return [...data.keys()];
    },
    clear() {
      data.clear();
    },
    dispose() {
      data.clear();
    }
  };
});

function createStorage(options = {}) {
  const context = {
    mounts: { "": options.driver || memory() },
    mountpoints: [""],
    watching: false,
    watchListeners: [],
    unwatch: {}
  };
  const getMount = (key) => {
    for (const base of context.mountpoints) {
      if (key.startsWith(base)) {
        return {
          base,
          relativeKey: key.slice(base.length),
          driver: context.mounts[base]
        };
      }
    }
    return {
      base: "",
      relativeKey: key,
      driver: context.mounts[""]
    };
  };
  const getMounts = (base, includeParent) => {
    return context.mountpoints.filter(
      (mountpoint) => mountpoint.startsWith(base) || includeParent && base.startsWith(mountpoint)
    ).map((mountpoint) => ({
      relativeBase: base.length > mountpoint.length ? base.slice(mountpoint.length) : void 0,
      mountpoint,
      driver: context.mounts[mountpoint]
    }));
  };
  const onChange = (event, key) => {
    if (!context.watching) {
      return;
    }
    key = normalizeKey$1(key);
    for (const listener of context.watchListeners) {
      listener(event, key);
    }
  };
  const startWatch = async () => {
    if (context.watching) {
      return;
    }
    context.watching = true;
    for (const mountpoint in context.mounts) {
      context.unwatch[mountpoint] = await watch(
        context.mounts[mountpoint],
        onChange,
        mountpoint
      );
    }
  };
  const stopWatch = async () => {
    if (!context.watching) {
      return;
    }
    for (const mountpoint in context.unwatch) {
      await context.unwatch[mountpoint]();
    }
    context.unwatch = {};
    context.watching = false;
  };
  const runBatch = (items, commonOptions, cb) => {
    const batches = /* @__PURE__ */ new Map();
    const getBatch = (mount) => {
      let batch = batches.get(mount.base);
      if (!batch) {
        batch = {
          driver: mount.driver,
          base: mount.base,
          items: []
        };
        batches.set(mount.base, batch);
      }
      return batch;
    };
    for (const item of items) {
      const isStringItem = typeof item === "string";
      const key = normalizeKey$1(isStringItem ? item : item.key);
      const value = isStringItem ? void 0 : item.value;
      const options2 = isStringItem || !item.options ? commonOptions : { ...commonOptions, ...item.options };
      const mount = getMount(key);
      getBatch(mount).items.push({
        key,
        value,
        relativeKey: mount.relativeKey,
        options: options2
      });
    }
    return Promise.all([...batches.values()].map((batch) => cb(batch))).then(
      (r) => r.flat()
    );
  };
  const storage = {
    // Item
    hasItem(key, opts = {}) {
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      return asyncCall(driver.hasItem, relativeKey, opts);
    },
    getItem(key, opts = {}) {
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      return asyncCall(driver.getItem, relativeKey, opts).then(
        (value) => destr(value)
      );
    },
    getItems(items, commonOptions = {}) {
      return runBatch(items, commonOptions, (batch) => {
        if (batch.driver.getItems) {
          return asyncCall(
            batch.driver.getItems,
            batch.items.map((item) => ({
              key: item.relativeKey,
              options: item.options
            })),
            commonOptions
          ).then(
            (r) => r.map((item) => ({
              key: joinKeys(batch.base, item.key),
              value: destr(item.value)
            }))
          );
        }
        return Promise.all(
          batch.items.map((item) => {
            return asyncCall(
              batch.driver.getItem,
              item.relativeKey,
              item.options
            ).then((value) => ({
              key: item.key,
              value: destr(value)
            }));
          })
        );
      });
    },
    getItemRaw(key, opts = {}) {
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      if (driver.getItemRaw) {
        return asyncCall(driver.getItemRaw, relativeKey, opts);
      }
      return asyncCall(driver.getItem, relativeKey, opts).then(
        (value) => deserializeRaw(value)
      );
    },
    async setItem(key, value, opts = {}) {
      if (value === void 0) {
        return storage.removeItem(key);
      }
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      if (!driver.setItem) {
        return;
      }
      await asyncCall(driver.setItem, relativeKey, stringify(value), opts);
      if (!driver.watch) {
        onChange("update", key);
      }
    },
    async setItems(items, commonOptions) {
      await runBatch(items, commonOptions, async (batch) => {
        if (batch.driver.setItems) {
          return asyncCall(
            batch.driver.setItems,
            batch.items.map((item) => ({
              key: item.relativeKey,
              value: stringify(item.value),
              options: item.options
            })),
            commonOptions
          );
        }
        if (!batch.driver.setItem) {
          return;
        }
        await Promise.all(
          batch.items.map((item) => {
            return asyncCall(
              batch.driver.setItem,
              item.relativeKey,
              stringify(item.value),
              item.options
            );
          })
        );
      });
    },
    async setItemRaw(key, value, opts = {}) {
      if (value === void 0) {
        return storage.removeItem(key, opts);
      }
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      if (driver.setItemRaw) {
        await asyncCall(driver.setItemRaw, relativeKey, value, opts);
      } else if (driver.setItem) {
        await asyncCall(driver.setItem, relativeKey, serializeRaw(value), opts);
      } else {
        return;
      }
      if (!driver.watch) {
        onChange("update", key);
      }
    },
    async removeItem(key, opts = {}) {
      if (typeof opts === "boolean") {
        opts = { removeMeta: opts };
      }
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      if (!driver.removeItem) {
        return;
      }
      await asyncCall(driver.removeItem, relativeKey, opts);
      if (opts.removeMeta || opts.removeMata) {
        await asyncCall(driver.removeItem, relativeKey + "$", opts);
      }
      if (!driver.watch) {
        onChange("remove", key);
      }
    },
    // Meta
    async getMeta(key, opts = {}) {
      if (typeof opts === "boolean") {
        opts = { nativeOnly: opts };
      }
      key = normalizeKey$1(key);
      const { relativeKey, driver } = getMount(key);
      const meta = /* @__PURE__ */ Object.create(null);
      if (driver.getMeta) {
        Object.assign(meta, await asyncCall(driver.getMeta, relativeKey, opts));
      }
      if (!opts.nativeOnly) {
        const value = await asyncCall(
          driver.getItem,
          relativeKey + "$",
          opts
        ).then((value_) => destr(value_));
        if (value && typeof value === "object") {
          if (typeof value.atime === "string") {
            value.atime = new Date(value.atime);
          }
          if (typeof value.mtime === "string") {
            value.mtime = new Date(value.mtime);
          }
          Object.assign(meta, value);
        }
      }
      return meta;
    },
    setMeta(key, value, opts = {}) {
      return this.setItem(key + "$", value, opts);
    },
    removeMeta(key, opts = {}) {
      return this.removeItem(key + "$", opts);
    },
    // Keys
    async getKeys(base, opts = {}) {
      base = normalizeBaseKey(base);
      const mounts = getMounts(base, true);
      let maskedMounts = [];
      const allKeys = [];
      let allMountsSupportMaxDepth = true;
      for (const mount of mounts) {
        if (!mount.driver.flags?.maxDepth) {
          allMountsSupportMaxDepth = false;
        }
        const rawKeys = await asyncCall(
          mount.driver.getKeys,
          mount.relativeBase,
          opts
        );
        for (const key of rawKeys) {
          const fullKey = mount.mountpoint + normalizeKey$1(key);
          if (!maskedMounts.some((p) => fullKey.startsWith(p))) {
            allKeys.push(fullKey);
          }
        }
        maskedMounts = [
          mount.mountpoint,
          ...maskedMounts.filter((p) => !p.startsWith(mount.mountpoint))
        ];
      }
      const shouldFilterByDepth = opts.maxDepth !== void 0 && !allMountsSupportMaxDepth;
      return allKeys.filter(
        (key) => (!shouldFilterByDepth || filterKeyByDepth(key, opts.maxDepth)) && filterKeyByBase(key, base)
      );
    },
    // Utils
    async clear(base, opts = {}) {
      base = normalizeBaseKey(base);
      await Promise.all(
        getMounts(base, false).map(async (m) => {
          if (m.driver.clear) {
            return asyncCall(m.driver.clear, m.relativeBase, opts);
          }
          if (m.driver.removeItem) {
            const keys = await m.driver.getKeys(m.relativeBase || "", opts);
            return Promise.all(
              keys.map((key) => m.driver.removeItem(key, opts))
            );
          }
        })
      );
    },
    async dispose() {
      await Promise.all(
        Object.values(context.mounts).map((driver) => dispose(driver))
      );
    },
    async watch(callback) {
      await startWatch();
      context.watchListeners.push(callback);
      return async () => {
        context.watchListeners = context.watchListeners.filter(
          (listener) => listener !== callback
        );
        if (context.watchListeners.length === 0) {
          await stopWatch();
        }
      };
    },
    async unwatch() {
      context.watchListeners = [];
      await stopWatch();
    },
    // Mount
    mount(base, driver) {
      base = normalizeBaseKey(base);
      if (base && context.mounts[base]) {
        throw new Error(`already mounted at ${base}`);
      }
      if (base) {
        context.mountpoints.push(base);
        context.mountpoints.sort((a, b) => b.length - a.length);
      }
      context.mounts[base] = driver;
      if (context.watching) {
        Promise.resolve(watch(driver, onChange, base)).then((unwatcher) => {
          context.unwatch[base] = unwatcher;
        }).catch(console.error);
      }
      return storage;
    },
    async unmount(base, _dispose = true) {
      base = normalizeBaseKey(base);
      if (!base || !context.mounts[base]) {
        return;
      }
      if (context.watching && base in context.unwatch) {
        context.unwatch[base]?.();
        delete context.unwatch[base];
      }
      if (_dispose) {
        await dispose(context.mounts[base]);
      }
      context.mountpoints = context.mountpoints.filter((key) => key !== base);
      delete context.mounts[base];
    },
    getMount(key = "") {
      key = normalizeKey$1(key) + ":";
      const m = getMount(key);
      return {
        driver: m.driver,
        base: m.base
      };
    },
    getMounts(base = "", opts = {}) {
      base = normalizeKey$1(base);
      const mounts = getMounts(base, opts.parents);
      return mounts.map((m) => ({
        driver: m.driver,
        base: m.mountpoint
      }));
    },
    // Aliases
    keys: (base, opts = {}) => storage.getKeys(base, opts),
    get: (key, opts = {}) => storage.getItem(key, opts),
    set: (key, value, opts = {}) => storage.setItem(key, value, opts),
    has: (key, opts = {}) => storage.hasItem(key, opts),
    del: (key, opts = {}) => storage.removeItem(key, opts),
    remove: (key, opts = {}) => storage.removeItem(key, opts)
  };
  return storage;
}
function watch(driver, onChange, base) {
  return driver.watch ? driver.watch((event, key) => onChange(event, base + key)) : () => {
  };
}
async function dispose(driver) {
  if (typeof driver.dispose === "function") {
    await asyncCall(driver.dispose);
  }
}

const _assets = {

};

const normalizeKey = function normalizeKey(key) {
  if (!key) {
    return "";
  }
  return key.split("?")[0]?.replace(/[/\\]/g, ":").replace(/:+/g, ":").replace(/^:|:$/g, "") || "";
};

const assets$1 = {
  getKeys() {
    return Promise.resolve(Object.keys(_assets))
  },
  hasItem (id) {
    id = normalizeKey(id);
    return Promise.resolve(id in _assets)
  },
  getItem (id) {
    id = normalizeKey(id);
    return Promise.resolve(_assets[id] ? _assets[id].import() : null)
  },
  getMeta (id) {
    id = normalizeKey(id);
    return Promise.resolve(_assets[id] ? _assets[id].meta : {})
  }
};

function defineDriver(factory) {
  return factory;
}
function createError(driver, message, opts) {
  const err = new Error(`[unstorage] [${driver}] ${message}`, opts);
  if (Error.captureStackTrace) {
    Error.captureStackTrace(err, createError);
  }
  return err;
}
function createRequiredError(driver, name) {
  if (Array.isArray(name)) {
    return createError(
      driver,
      `Missing some of the required options ${name.map((n) => "`" + n + "`").join(", ")}`
    );
  }
  return createError(driver, `Missing required option \`${name}\`.`);
}

function ignoreNotfound(err) {
  return err.code === "ENOENT" || err.code === "EISDIR" ? null : err;
}
function ignoreExists(err) {
  return err.code === "EEXIST" ? null : err;
}
async function writeFile(path, data, encoding) {
  await ensuredir(dirname$1(path));
  return promises.writeFile(path, data, encoding);
}
function readFile(path, encoding) {
  return promises.readFile(path, encoding).catch(ignoreNotfound);
}
function unlink(path) {
  return promises.unlink(path).catch(ignoreNotfound);
}
function readdir(dir) {
  return promises.readdir(dir, { withFileTypes: true }).catch(ignoreNotfound).then((r) => r || []);
}
async function ensuredir(dir) {
  if (existsSync(dir)) {
    return;
  }
  await ensuredir(dirname$1(dir)).catch(ignoreExists);
  await promises.mkdir(dir).catch(ignoreExists);
}
async function readdirRecursive(dir, ignore, maxDepth) {
  if (ignore && ignore(dir)) {
    return [];
  }
  const entries = await readdir(dir);
  const files = [];
  await Promise.all(
    entries.map(async (entry) => {
      const entryPath = resolve$1(dir, entry.name);
      if (entry.isDirectory()) {
        if (maxDepth === void 0 || maxDepth > 0) {
          const dirFiles = await readdirRecursive(
            entryPath,
            ignore,
            maxDepth === void 0 ? void 0 : maxDepth - 1
          );
          files.push(...dirFiles.map((f) => entry.name + "/" + f));
        }
      } else {
        if (!(ignore && ignore(entry.name))) {
          files.push(entry.name);
        }
      }
    })
  );
  return files;
}
async function rmRecursive(dir) {
  const entries = await readdir(dir);
  await Promise.all(
    entries.map((entry) => {
      const entryPath = resolve$1(dir, entry.name);
      if (entry.isDirectory()) {
        return rmRecursive(entryPath).then(() => promises.rmdir(entryPath));
      } else {
        return promises.unlink(entryPath);
      }
    })
  );
}

const PATH_TRAVERSE_RE = /\.\.:|\.\.$/;
const DRIVER_NAME = "fs-lite";
const unstorage_47drivers_47fs_45lite = defineDriver((opts = {}) => {
  if (!opts.base) {
    throw createRequiredError(DRIVER_NAME, "base");
  }
  opts.base = resolve$1(opts.base);
  const r = (key) => {
    if (PATH_TRAVERSE_RE.test(key)) {
      throw createError(
        DRIVER_NAME,
        `Invalid key: ${JSON.stringify(key)}. It should not contain .. segments`
      );
    }
    const resolved = join(opts.base, key.replace(/:/g, "/"));
    return resolved;
  };
  return {
    name: DRIVER_NAME,
    options: opts,
    flags: {
      maxDepth: true
    },
    hasItem(key) {
      return existsSync(r(key));
    },
    getItem(key) {
      return readFile(r(key), "utf8");
    },
    getItemRaw(key) {
      return readFile(r(key));
    },
    async getMeta(key) {
      const { atime, mtime, size, birthtime, ctime } = await promises.stat(r(key)).catch(() => ({}));
      return { atime, mtime, size, birthtime, ctime };
    },
    setItem(key, value) {
      if (opts.readOnly) {
        return;
      }
      return writeFile(r(key), value, "utf8");
    },
    setItemRaw(key, value) {
      if (opts.readOnly) {
        return;
      }
      return writeFile(r(key), value);
    },
    removeItem(key) {
      if (opts.readOnly) {
        return;
      }
      return unlink(r(key));
    },
    getKeys(_base, topts) {
      return readdirRecursive(r("."), opts.ignore, topts?.maxDepth);
    },
    async clear() {
      if (opts.readOnly || opts.noClear) {
        return;
      }
      await rmRecursive(r("."));
    }
  };
});

const storage = createStorage({});

storage.mount('/assets', assets$1);

storage.mount('data', unstorage_47drivers_47fs_45lite({"driver":"fsLite","base":"./.data/kv"}));

function useStorage(base = "") {
  return base ? prefixStorage(storage, base) : storage;
}

const e=globalThis.process?.getBuiltinModule?.("crypto")?.hash,r="sha256",s="base64url";function digest(t){if(e)return e(r,t,s);const o=createHash(r).update(t);return globalThis.process?.versions?.webcontainer?o.digest().toString(s):o.digest(s)}

const Hasher = /* @__PURE__ */ (() => {
  class Hasher2 {
    buff = "";
    #context = /* @__PURE__ */ new Map();
    write(str) {
      this.buff += str;
    }
    dispatch(value) {
      const type = value === null ? "null" : typeof value;
      return this[type](value);
    }
    object(object) {
      if (object && typeof object.toJSON === "function") {
        return this.object(object.toJSON());
      }
      const objString = Object.prototype.toString.call(object);
      let objType = "";
      const objectLength = objString.length;
      objType = objectLength < 10 ? "unknown:[" + objString + "]" : objString.slice(8, objectLength - 1);
      objType = objType.toLowerCase();
      let objectNumber = null;
      if ((objectNumber = this.#context.get(object)) === void 0) {
        this.#context.set(object, this.#context.size);
      } else {
        return this.dispatch("[CIRCULAR:" + objectNumber + "]");
      }
      if (typeof Buffer !== "undefined" && Buffer.isBuffer && Buffer.isBuffer(object)) {
        this.write("buffer:");
        return this.write(object.toString("utf8"));
      }
      if (objType !== "object" && objType !== "function" && objType !== "asyncfunction") {
        if (this[objType]) {
          this[objType](object);
        } else {
          this.unknown(object, objType);
        }
      } else {
        const keys = Object.keys(object).sort();
        const extraKeys = [];
        this.write("object:" + (keys.length + extraKeys.length) + ":");
        const dispatchForKey = (key) => {
          this.dispatch(key);
          this.write(":");
          this.dispatch(object[key]);
          this.write(",");
        };
        for (const key of keys) {
          dispatchForKey(key);
        }
        for (const key of extraKeys) {
          dispatchForKey(key);
        }
      }
    }
    array(arr, unordered) {
      unordered = unordered === void 0 ? false : unordered;
      this.write("array:" + arr.length + ":");
      if (!unordered || arr.length <= 1) {
        for (const entry of arr) {
          this.dispatch(entry);
        }
        return;
      }
      const contextAdditions = /* @__PURE__ */ new Map();
      const entries = arr.map((entry) => {
        const hasher = new Hasher2();
        hasher.dispatch(entry);
        for (const [key, value] of hasher.#context) {
          contextAdditions.set(key, value);
        }
        return hasher.toString();
      });
      this.#context = contextAdditions;
      entries.sort();
      return this.array(entries, false);
    }
    date(date) {
      return this.write("date:" + date.toJSON());
    }
    symbol(sym) {
      return this.write("symbol:" + sym.toString());
    }
    unknown(value, type) {
      this.write(type);
      if (!value) {
        return;
      }
      this.write(":");
      if (value && typeof value.entries === "function") {
        return this.array(
          [...value.entries()],
          true
          /* ordered */
        );
      }
    }
    error(err) {
      return this.write("error:" + err.toString());
    }
    boolean(bool) {
      return this.write("bool:" + bool);
    }
    string(string) {
      this.write("string:" + string.length + ":");
      this.write(string);
    }
    function(fn) {
      this.write("fn:");
      if (isNativeFunction(fn)) {
        this.dispatch("[native]");
      } else {
        this.dispatch(fn.toString());
      }
    }
    number(number) {
      return this.write("number:" + number);
    }
    null() {
      return this.write("Null");
    }
    undefined() {
      return this.write("Undefined");
    }
    regexp(regex) {
      return this.write("regex:" + regex.toString());
    }
    arraybuffer(arr) {
      this.write("arraybuffer:");
      return this.dispatch(new Uint8Array(arr));
    }
    url(url) {
      return this.write("url:" + url.toString());
    }
    map(map) {
      this.write("map:");
      const arr = [...map];
      return this.array(arr, false);
    }
    set(set) {
      this.write("set:");
      const arr = [...set];
      return this.array(arr, false);
    }
    bigint(number) {
      return this.write("bigint:" + number.toString());
    }
  }
  for (const type of [
    "uint8array",
    "uint8clampedarray",
    "unt8array",
    "uint16array",
    "unt16array",
    "uint32array",
    "unt32array",
    "float32array",
    "float64array"
  ]) {
    Hasher2.prototype[type] = function(arr) {
      this.write(type + ":");
      return this.array([...arr], false);
    };
  }
  function isNativeFunction(f) {
    if (typeof f !== "function") {
      return false;
    }
    return Function.prototype.toString.call(f).slice(
      -15
      /* "[native code] }".length */
    ) === "[native code] }";
  }
  return Hasher2;
})();
function serialize(object) {
  const hasher = new Hasher();
  hasher.dispatch(object);
  return hasher.buff;
}
function hash(value) {
  return digest(typeof value === "string" ? value : serialize(value)).replace(/[-_]/g, "").slice(0, 10);
}

function defaultCacheOptions() {
  return {
    name: "_",
    base: "/cache",
    swr: true,
    maxAge: 1
  };
}
function defineCachedFunction(fn, opts = {}) {
  opts = { ...defaultCacheOptions(), ...opts };
  const pending = {};
  const group = opts.group || "nitro/functions";
  const name = opts.name || fn.name || "_";
  const integrity = opts.integrity || hash([fn, opts]);
  const validate = opts.validate || ((entry) => entry.value !== void 0);
  async function get(key, resolver, shouldInvalidateCache, event) {
    const cacheKey = [opts.base, group, name, key + ".json"].filter(Boolean).join(":").replace(/:\/$/, ":index");
    let entry = await useStorage().getItem(cacheKey).catch((error) => {
      console.error(`[cache] Cache read error.`, error);
      useNitroApp().captureError(error, { event, tags: ["cache"] });
    }) || {};
    if (typeof entry !== "object") {
      entry = {};
      const error = new Error("Malformed data read from cache.");
      console.error("[cache]", error);
      useNitroApp().captureError(error, { event, tags: ["cache"] });
    }
    const ttl = (opts.maxAge ?? 0) * 1e3;
    if (ttl) {
      entry.expires = Date.now() + ttl;
    }
    const expired = shouldInvalidateCache || entry.integrity !== integrity || ttl && Date.now() - (entry.mtime || 0) > ttl || validate(entry) === false;
    const _resolve = async () => {
      const isPending = pending[key];
      if (!isPending) {
        if (entry.value !== void 0 && (opts.staleMaxAge || 0) >= 0 && opts.swr === false) {
          entry.value = void 0;
          entry.integrity = void 0;
          entry.mtime = void 0;
          entry.expires = void 0;
        }
        pending[key] = Promise.resolve(resolver());
      }
      try {
        entry.value = await pending[key];
      } catch (error) {
        if (!isPending) {
          delete pending[key];
        }
        throw error;
      }
      if (!isPending) {
        entry.mtime = Date.now();
        entry.integrity = integrity;
        delete pending[key];
        if (validate(entry) !== false) {
          let setOpts;
          if (opts.maxAge && !opts.swr) {
            setOpts = { ttl: opts.maxAge };
          }
          const promise = useStorage().setItem(cacheKey, entry, setOpts).catch((error) => {
            console.error(`[cache] Cache write error.`, error);
            useNitroApp().captureError(error, { event, tags: ["cache"] });
          });
          if (event?.waitUntil) {
            event.waitUntil(promise);
          }
        }
      }
    };
    const _resolvePromise = expired ? _resolve() : Promise.resolve();
    if (entry.value === void 0) {
      await _resolvePromise;
    } else if (expired && event && event.waitUntil) {
      event.waitUntil(_resolvePromise);
    }
    if (opts.swr && validate(entry) !== false) {
      _resolvePromise.catch((error) => {
        console.error(`[cache] SWR handler error.`, error);
        useNitroApp().captureError(error, { event, tags: ["cache"] });
      });
      return entry;
    }
    return _resolvePromise.then(() => entry);
  }
  return async (...args) => {
    const shouldBypassCache = await opts.shouldBypassCache?.(...args);
    if (shouldBypassCache) {
      return fn(...args);
    }
    const key = await (opts.getKey || getKey)(...args);
    const shouldInvalidateCache = await opts.shouldInvalidateCache?.(...args);
    const entry = await get(
      key,
      () => fn(...args),
      shouldInvalidateCache,
      args[0] && isEvent(args[0]) ? args[0] : void 0
    );
    let value = entry.value;
    if (opts.transform) {
      value = await opts.transform(entry, ...args) || value;
    }
    return value;
  };
}
function cachedFunction(fn, opts = {}) {
  return defineCachedFunction(fn, opts);
}
function getKey(...args) {
  return args.length > 0 ? hash(args) : "";
}
function escapeKey(key) {
  return String(key).replace(/\W/g, "");
}
function defineCachedEventHandler(handler, opts = defaultCacheOptions()) {
  const variableHeaderNames = (opts.varies || []).filter(Boolean).map((h) => h.toLowerCase()).sort();
  const _opts = {
    ...opts,
    getKey: async (event) => {
      const customKey = await opts.getKey?.(event);
      if (customKey) {
        return escapeKey(customKey);
      }
      const _path = event.node.req.originalUrl || event.node.req.url || event.path;
      let _pathname;
      try {
        _pathname = escapeKey(decodeURI(parseURL(_path).pathname)).slice(0, 16) || "index";
      } catch {
        _pathname = "-";
      }
      const _hashedPath = `${_pathname}.${hash(_path)}`;
      const _headers = variableHeaderNames.map((header) => [header, event.node.req.headers[header]]).map(([name, value]) => `${escapeKey(name)}.${hash(value)}`);
      return [_hashedPath, ..._headers].join(":");
    },
    validate: (entry) => {
      if (!entry.value) {
        return false;
      }
      if (entry.value.code >= 400) {
        return false;
      }
      if (entry.value.body === void 0) {
        return false;
      }
      if (entry.value.headers.etag === "undefined" || entry.value.headers["last-modified"] === "undefined") {
        return false;
      }
      return true;
    },
    group: opts.group || "nitro/handlers",
    integrity: opts.integrity || hash([handler, opts])
  };
  const _cachedHandler = cachedFunction(
    async (incomingEvent) => {
      const variableHeaders = {};
      for (const header of variableHeaderNames) {
        const value = incomingEvent.node.req.headers[header];
        if (value !== void 0) {
          variableHeaders[header] = value;
        }
      }
      const reqProxy = cloneWithProxy(incomingEvent.node.req, {
        headers: variableHeaders
      });
      const resHeaders = {};
      let _resSendBody;
      const resProxy = cloneWithProxy(incomingEvent.node.res, {
        statusCode: 200,
        writableEnded: false,
        writableFinished: false,
        headersSent: false,
        closed: false,
        getHeader(name) {
          return resHeaders[name];
        },
        setHeader(name, value) {
          resHeaders[name] = value;
          return this;
        },
        getHeaderNames() {
          return Object.keys(resHeaders);
        },
        hasHeader(name) {
          return name in resHeaders;
        },
        removeHeader(name) {
          delete resHeaders[name];
        },
        getHeaders() {
          return resHeaders;
        },
        end(chunk, arg2, arg3) {
          if (typeof chunk === "string") {
            _resSendBody = chunk;
          }
          if (typeof arg2 === "function") {
            arg2();
          }
          if (typeof arg3 === "function") {
            arg3();
          }
          return this;
        },
        write(chunk, arg2, arg3) {
          if (typeof chunk === "string") {
            _resSendBody = chunk;
          }
          if (typeof arg2 === "function") {
            arg2(void 0);
          }
          if (typeof arg3 === "function") {
            arg3();
          }
          return true;
        },
        writeHead(statusCode, headers2) {
          this.statusCode = statusCode;
          if (headers2) {
            if (Array.isArray(headers2) || typeof headers2 === "string") {
              throw new TypeError("Raw headers  is not supported.");
            }
            for (const header in headers2) {
              const value = headers2[header];
              if (value !== void 0) {
                this.setHeader(
                  header,
                  value
                );
              }
            }
          }
          return this;
        }
      });
      const event = createEvent(reqProxy, resProxy);
      event.fetch = (url, fetchOptions) => fetchWithEvent(event, url, fetchOptions, {
        fetch: useNitroApp().localFetch
      });
      event.$fetch = (url, fetchOptions) => fetchWithEvent(event, url, fetchOptions, {
        fetch: globalThis.$fetch
      });
      event.waitUntil = incomingEvent.waitUntil;
      event.context = incomingEvent.context;
      event.context.cache = {
        options: _opts
      };
      const body = await handler(event) || _resSendBody;
      const headers = event.node.res.getHeaders();
      headers.etag = String(
        headers.Etag || headers.etag || `W/"${hash(body)}"`
      );
      headers["last-modified"] = String(
        headers["Last-Modified"] || headers["last-modified"] || (/* @__PURE__ */ new Date()).toUTCString()
      );
      const cacheControl = [];
      if (opts.swr) {
        if (opts.maxAge) {
          cacheControl.push(`s-maxage=${opts.maxAge}`);
        }
        if (opts.staleMaxAge) {
          cacheControl.push(`stale-while-revalidate=${opts.staleMaxAge}`);
        } else {
          cacheControl.push("stale-while-revalidate");
        }
      } else if (opts.maxAge) {
        cacheControl.push(`max-age=${opts.maxAge}`);
      }
      if (cacheControl.length > 0) {
        headers["cache-control"] = cacheControl.join(", ");
      }
      const cacheEntry = {
        code: event.node.res.statusCode,
        headers,
        body
      };
      return cacheEntry;
    },
    _opts
  );
  return defineEventHandler(async (event) => {
    if (opts.headersOnly) {
      if (handleCacheHeaders(event, { maxAge: opts.maxAge })) {
        return;
      }
      return handler(event);
    }
    const response = await _cachedHandler(
      event
    );
    if (event.node.res.headersSent || event.node.res.writableEnded) {
      return response.body;
    }
    if (handleCacheHeaders(event, {
      modifiedTime: new Date(response.headers["last-modified"]),
      etag: response.headers.etag,
      maxAge: opts.maxAge
    })) {
      return;
    }
    event.node.res.statusCode = response.code;
    for (const name in response.headers) {
      const value = response.headers[name];
      if (name === "set-cookie") {
        event.node.res.appendHeader(
          name,
          splitCookiesString(value)
        );
      } else {
        if (value !== void 0) {
          event.node.res.setHeader(name, value);
        }
      }
    }
    return response.body;
  });
}
function cloneWithProxy(obj, overrides) {
  return new Proxy(obj, {
    get(target, property, receiver) {
      if (property in overrides) {
        return overrides[property];
      }
      return Reflect.get(target, property, receiver);
    },
    set(target, property, value, receiver) {
      if (property in overrides) {
        overrides[property] = value;
        return true;
      }
      return Reflect.set(target, property, value, receiver);
    }
  });
}
const cachedEventHandler = defineCachedEventHandler;

function klona(x) {
	if (typeof x !== 'object') return x;

	var k, tmp, str=Object.prototype.toString.call(x);

	if (str === '[object Object]') {
		if (x.constructor !== Object && typeof x.constructor === 'function') {
			tmp = new x.constructor();
			for (k in x) {
				if (x.hasOwnProperty(k) && tmp[k] !== x[k]) {
					tmp[k] = klona(x[k]);
				}
			}
		} else {
			tmp = {}; // null
			for (k in x) {
				if (k === '__proto__') {
					Object.defineProperty(tmp, k, {
						value: klona(x[k]),
						configurable: true,
						enumerable: true,
						writable: true,
					});
				} else {
					tmp[k] = klona(x[k]);
				}
			}
		}
		return tmp;
	}

	if (str === '[object Array]') {
		k = x.length;
		for (tmp=Array(k); k--;) {
			tmp[k] = klona(x[k]);
		}
		return tmp;
	}

	if (str === '[object Set]') {
		tmp = new Set;
		x.forEach(function (val) {
			tmp.add(klona(val));
		});
		return tmp;
	}

	if (str === '[object Map]') {
		tmp = new Map;
		x.forEach(function (val, key) {
			tmp.set(klona(key), klona(val));
		});
		return tmp;
	}

	if (str === '[object Date]') {
		return new Date(+x);
	}

	if (str === '[object RegExp]') {
		tmp = new RegExp(x.source, x.flags);
		tmp.lastIndex = x.lastIndex;
		return tmp;
	}

	if (str === '[object DataView]') {
		return new x.constructor( klona(x.buffer) );
	}

	if (str === '[object ArrayBuffer]') {
		return x.slice(0);
	}

	// ArrayBuffer.isView(x)
	// ~> `new` bcuz `Buffer.slice` => ref
	if (str.slice(-6) === 'Array]') {
		return new x.constructor(x);
	}

	return x;
}

const inlineAppConfig = {
  "nuxt": {}
};



const appConfig = defuFn(inlineAppConfig);

const NUMBER_CHAR_RE = /\d/;
const STR_SPLITTERS = ["-", "_", "/", "."];
function isUppercase(char = "") {
  if (NUMBER_CHAR_RE.test(char)) {
    return void 0;
  }
  return char !== char.toLowerCase();
}
function splitByCase(str, separators) {
  const splitters = STR_SPLITTERS;
  const parts = [];
  if (!str || typeof str !== "string") {
    return parts;
  }
  let buff = "";
  let previousUpper;
  let previousSplitter;
  for (const char of str) {
    const isSplitter = splitters.includes(char);
    if (isSplitter === true) {
      parts.push(buff);
      buff = "";
      previousUpper = void 0;
      continue;
    }
    const isUpper = isUppercase(char);
    if (previousSplitter === false) {
      if (previousUpper === false && isUpper === true) {
        parts.push(buff);
        buff = char;
        previousUpper = isUpper;
        continue;
      }
      if (previousUpper === true && isUpper === false && buff.length > 1) {
        const lastChar = buff.at(-1);
        parts.push(buff.slice(0, Math.max(0, buff.length - 1)));
        buff = lastChar + char;
        previousUpper = isUpper;
        continue;
      }
    }
    buff += char;
    previousUpper = isUpper;
    previousSplitter = isSplitter;
  }
  parts.push(buff);
  return parts;
}
function kebabCase(str, joiner) {
  return str ? (Array.isArray(str) ? str : splitByCase(str)).map((p) => p.toLowerCase()).join(joiner) : "";
}
function snakeCase(str) {
  return kebabCase(str || "", "_");
}

function getEnv(key, opts) {
  const envKey = snakeCase(key).toUpperCase();
  return destr(
    process.env[opts.prefix + envKey] ?? process.env[opts.altPrefix + envKey]
  );
}
function _isObject(input) {
  return typeof input === "object" && !Array.isArray(input);
}
function applyEnv(obj, opts, parentKey = "") {
  for (const key in obj) {
    const subKey = parentKey ? `${parentKey}_${key}` : key;
    const envValue = getEnv(subKey, opts);
    if (_isObject(obj[key])) {
      if (_isObject(envValue)) {
        obj[key] = { ...obj[key], ...envValue };
        applyEnv(obj[key], opts, subKey);
      } else if (envValue === void 0) {
        applyEnv(obj[key], opts, subKey);
      } else {
        obj[key] = envValue ?? obj[key];
      }
    } else {
      obj[key] = envValue ?? obj[key];
    }
    if (opts.envExpansion && typeof obj[key] === "string") {
      obj[key] = _expandFromEnv(obj[key]);
    }
  }
  return obj;
}
const envExpandRx = /\{\{([^{}]*)\}\}/g;
function _expandFromEnv(value) {
  return value.replace(envExpandRx, (match, key) => {
    return process.env[key] || match;
  });
}

const _inlineRuntimeConfig = {
  "app": {
    "baseURL": "/",
    "buildId": "161e7434-9d92-4cb9-ab30-77244548e8d9",
    "buildAssetsDir": "/_nuxt/",
    "cdnURL": ""
  },
  "nitro": {
    "envPrefix": "NUXT_",
    "routeRules": {
      "/__nuxt_error": {
        "cache": false
      },
      "/_nuxt/builds/meta/**": {
        "headers": {
          "cache-control": "public, max-age=31536000, immutable"
        }
      },
      "/_nuxt/builds/**": {
        "headers": {
          "cache-control": "public, max-age=1, immutable"
        }
      },
      "/_fonts/**": {
        "headers": {
          "cache-control": "public, max-age=31536000, immutable"
        }
      },
      "/_nuxt/**": {
        "headers": {
          "cache-control": "public, max-age=31536000, immutable"
        }
      }
    }
  },
  "public": {
    "AUTH_TOKEN": "mealie.access_token",
    "GLOBAL_MIDDLEWARE": "",
    "SUB_PATH": "",
    "useDark": false,
    "themes": {
      "dark": {
        "primary": "#E58325",
        "accent": "#007A99",
        "secondary": "#973542",
        "success": "#43A047",
        "info": "#1976d2",
        "warning": "#FF6D00",
        "error": "#EF5350",
        "background": "#1E1E1E"
      },
      "light": {
        "primary": "#E58325",
        "accent": "#007A99",
        "secondary": "#973542",
        "success": "#43A047",
        "info": "#1976d2",
        "warning": "#FF6D00",
        "error": "#EF5350"
      }
    },
    "i18n": {
      "baseUrl": "",
      "defaultLocale": "en-US",
      "defaultDirection": "ltr",
      "strategy": "no_prefix",
      "lazy": true,
      "rootRedirect": "",
      "routesNameSeparator": "___",
      "defaultLocaleRouteNameSuffix": "default",
      "skipSettingLocaleOnNavigate": false,
      "differentDomains": false,
      "trailingSlash": false,
      "locales": [
        {
          "code": "af-ZA",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/af-ZA.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "ar-SA",
          "dir": "rtl",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/ar-SA.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "bg-BG",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/bg-BG.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "ca-ES",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/ca-ES.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "cs-CZ",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/cs-CZ.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "da-DK",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/da-DK.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "de-DE",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/de-DE.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "el-GR",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/el-GR.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "en-GB",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/en-GB.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "en-US",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/en-US.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "es-ES",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/es-ES.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "et-EE",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/et-EE.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "fi-FI",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/fi-FI.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "fr-BE",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/fr-BE.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "fr-CA",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/fr-CA.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "fr-FR",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/fr-FR.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "gl-ES",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/gl-ES.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "he-IL",
          "dir": "rtl",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/he-IL.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "hr-HR",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/hr-HR.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "hu-HU",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/hu-HU.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "is-IS",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/is-IS.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "it-IT",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/it-IT.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "ja-JP",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/ja-JP.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "ko-KR",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/ko-KR.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "lt-LT",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/lt-LT.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "lv-LV",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/lv-LV.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "nl-NL",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/nl-NL.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "no-NO",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/no-NO.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "pl-PL",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/pl-PL.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "pt-BR",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/pt-BR.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "pt-PT",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/pt-PT.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "ro-RO",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/ro-RO.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "ru-RU",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/ru-RU.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "sk-SK",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/sk-SK.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "sl-SI",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/sl-SI.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "sr-SP",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/sr-SP.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "sv-SE",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/sv-SE.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "tr-TR",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/tr-TR.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "uk-UA",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/uk-UA.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "vi-VN",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/vi-VN.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "zh-CN",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/zh-CN.ts",
              "cache": ""
            }
          ]
        },
        {
          "code": "zh-TW",
          "dir": "ltr",
          "files": [
            {
              "path": "/home/grrtt/dev/mealie/frontend/app/lang/locales/zh-TW.ts",
              "cache": ""
            }
          ]
        }
      ],
      "detectBrowserLanguage": {
        "alwaysRedirect": true,
        "cookieCrossOrigin": false,
        "cookieDomain": "",
        "cookieKey": "i18n_redirected",
        "cookieSecure": false,
        "fallbackLocale": "en-US",
        "redirectOn": "root",
        "useCookie": true
      },
      "experimental": {
        "localeDetector": "",
        "switchLocalePathLinkSSR": false,
        "autoImportTranslationFunctions": false,
        "typedPages": true,
        "typedOptionsAndMessages": false,
        "generatedLocaleFilePathFormat": "absolute",
        "alternateLinkCanonicalQueries": false,
        "hmr": true
      },
      "multiDomainLocales": false,
      "domainLocales": {
        "af-ZA": {
          "domain": ""
        },
        "ar-SA": {
          "domain": ""
        },
        "bg-BG": {
          "domain": ""
        },
        "ca-ES": {
          "domain": ""
        },
        "cs-CZ": {
          "domain": ""
        },
        "da-DK": {
          "domain": ""
        },
        "de-DE": {
          "domain": ""
        },
        "el-GR": {
          "domain": ""
        },
        "en-GB": {
          "domain": ""
        },
        "en-US": {
          "domain": ""
        },
        "es-ES": {
          "domain": ""
        },
        "et-EE": {
          "domain": ""
        },
        "fi-FI": {
          "domain": ""
        },
        "fr-BE": {
          "domain": ""
        },
        "fr-CA": {
          "domain": ""
        },
        "fr-FR": {
          "domain": ""
        },
        "gl-ES": {
          "domain": ""
        },
        "he-IL": {
          "domain": ""
        },
        "hr-HR": {
          "domain": ""
        },
        "hu-HU": {
          "domain": ""
        },
        "is-IS": {
          "domain": ""
        },
        "it-IT": {
          "domain": ""
        },
        "ja-JP": {
          "domain": ""
        },
        "ko-KR": {
          "domain": ""
        },
        "lt-LT": {
          "domain": ""
        },
        "lv-LV": {
          "domain": ""
        },
        "nl-NL": {
          "domain": ""
        },
        "no-NO": {
          "domain": ""
        },
        "pl-PL": {
          "domain": ""
        },
        "pt-BR": {
          "domain": ""
        },
        "pt-PT": {
          "domain": ""
        },
        "ro-RO": {
          "domain": ""
        },
        "ru-RU": {
          "domain": ""
        },
        "sk-SK": {
          "domain": ""
        },
        "sl-SI": {
          "domain": ""
        },
        "sr-SP": {
          "domain": ""
        },
        "sv-SE": {
          "domain": ""
        },
        "tr-TR": {
          "domain": ""
        },
        "uk-UA": {
          "domain": ""
        },
        "vi-VN": {
          "domain": ""
        },
        "zh-CN": {
          "domain": ""
        },
        "zh-TW": {
          "domain": ""
        }
      }
    }
  },
  "sessionPassword": "password-with-at-least-32-characters",
  "apiUrl": "http://localhost:9000"
};
const envOptions = {
  prefix: "NITRO_",
  altPrefix: _inlineRuntimeConfig.nitro.envPrefix ?? process.env.NITRO_ENV_PREFIX ?? "_",
  envExpansion: _inlineRuntimeConfig.nitro.envExpansion ?? process.env.NITRO_ENV_EXPANSION ?? false
};
const _sharedRuntimeConfig = _deepFreeze(
  applyEnv(klona(_inlineRuntimeConfig), envOptions)
);
function useRuntimeConfig(event) {
  if (!event) {
    return _sharedRuntimeConfig;
  }
  if (event.context.nitro.runtimeConfig) {
    return event.context.nitro.runtimeConfig;
  }
  const runtimeConfig = klona(_inlineRuntimeConfig);
  applyEnv(runtimeConfig, envOptions);
  event.context.nitro.runtimeConfig = runtimeConfig;
  return runtimeConfig;
}
_deepFreeze(klona(appConfig));
function _deepFreeze(object) {
  const propNames = Object.getOwnPropertyNames(object);
  for (const name of propNames) {
    const value = object[name];
    if (value && typeof value === "object") {
      _deepFreeze(value);
    }
  }
  return Object.freeze(object);
}
new Proxy(/* @__PURE__ */ Object.create(null), {
  get: (_, prop) => {
    console.warn(
      "Please use `useRuntimeConfig()` instead of accessing config directly."
    );
    const runtimeConfig = useRuntimeConfig();
    if (prop in runtimeConfig) {
      return runtimeConfig[prop];
    }
    return void 0;
  }
});

function createContext(opts = {}) {
  let currentInstance;
  let isSingleton = false;
  const checkConflict = (instance) => {
    if (currentInstance && currentInstance !== instance) {
      throw new Error("Context conflict");
    }
  };
  let als;
  if (opts.asyncContext) {
    const _AsyncLocalStorage = opts.AsyncLocalStorage || globalThis.AsyncLocalStorage;
    if (_AsyncLocalStorage) {
      als = new _AsyncLocalStorage();
    } else {
      console.warn("[unctx] `AsyncLocalStorage` is not provided.");
    }
  }
  const _getCurrentInstance = () => {
    if (als) {
      const instance = als.getStore();
      if (instance !== void 0) {
        return instance;
      }
    }
    return currentInstance;
  };
  return {
    use: () => {
      const _instance = _getCurrentInstance();
      if (_instance === void 0) {
        throw new Error("Context is not available");
      }
      return _instance;
    },
    tryUse: () => {
      return _getCurrentInstance();
    },
    set: (instance, replace) => {
      if (!replace) {
        checkConflict(instance);
      }
      currentInstance = instance;
      isSingleton = true;
    },
    unset: () => {
      currentInstance = void 0;
      isSingleton = false;
    },
    call: (instance, callback) => {
      checkConflict(instance);
      currentInstance = instance;
      try {
        return als ? als.run(instance, callback) : callback();
      } finally {
        if (!isSingleton) {
          currentInstance = void 0;
        }
      }
    },
    async callAsync(instance, callback) {
      currentInstance = instance;
      const onRestore = () => {
        currentInstance = instance;
      };
      const onLeave = () => currentInstance === instance ? onRestore : void 0;
      asyncHandlers.add(onLeave);
      try {
        const r = als ? als.run(instance, callback) : callback();
        if (!isSingleton) {
          currentInstance = void 0;
        }
        return await r;
      } finally {
        asyncHandlers.delete(onLeave);
      }
    }
  };
}
function createNamespace(defaultOpts = {}) {
  const contexts = {};
  return {
    get(key, opts = {}) {
      if (!contexts[key]) {
        contexts[key] = createContext({ ...defaultOpts, ...opts });
      }
      return contexts[key];
    }
  };
}
const _globalThis = typeof globalThis !== "undefined" ? globalThis : typeof self !== "undefined" ? self : typeof global !== "undefined" ? global : {};
const globalKey = "__unctx__";
const defaultNamespace = _globalThis[globalKey] || (_globalThis[globalKey] = createNamespace());
const getContext = (key, opts = {}) => defaultNamespace.get(key, opts);
const asyncHandlersKey = "__unctx_async_handlers__";
const asyncHandlers = _globalThis[asyncHandlersKey] || (_globalThis[asyncHandlersKey] = /* @__PURE__ */ new Set());

getContext("nitro-app", {
  asyncContext: false,
  AsyncLocalStorage: void 0
});

const config = useRuntimeConfig();
const _routeRulesMatcher = toRouteMatcher(
  createRouter$1({ routes: config.nitro.routeRules })
);
function createRouteRulesHandler(ctx) {
  return eventHandler((event) => {
    const routeRules = getRouteRules(event);
    if (routeRules.headers) {
      setHeaders(event, routeRules.headers);
    }
    if (routeRules.redirect) {
      let target = routeRules.redirect.to;
      if (target.endsWith("/**")) {
        let targetPath = event.path;
        const strpBase = routeRules.redirect._redirectStripBase;
        if (strpBase) {
          targetPath = withoutBase(targetPath, strpBase);
        }
        target = joinURL(target.slice(0, -3), targetPath);
      } else if (event.path.includes("?")) {
        const query = getQuery$1(event.path);
        target = withQuery(target, query);
      }
      return sendRedirect(event, target, routeRules.redirect.statusCode);
    }
    if (routeRules.proxy) {
      let target = routeRules.proxy.to;
      if (target.endsWith("/**")) {
        let targetPath = event.path;
        const strpBase = routeRules.proxy._proxyStripBase;
        if (strpBase) {
          targetPath = withoutBase(targetPath, strpBase);
        }
        target = joinURL(target.slice(0, -3), targetPath);
      } else if (event.path.includes("?")) {
        const query = getQuery$1(event.path);
        target = withQuery(target, query);
      }
      return proxyRequest(event, target, {
        fetch: ctx.localFetch,
        ...routeRules.proxy
      });
    }
  });
}
function getRouteRules(event) {
  event.context._nitro = event.context._nitro || {};
  if (!event.context._nitro.routeRules) {
    event.context._nitro.routeRules = getRouteRulesForPath(
      withoutBase(event.path.split("?")[0], useRuntimeConfig().app.baseURL)
    );
  }
  return event.context._nitro.routeRules;
}
function getRouteRulesForPath(path) {
  return defu({}, ..._routeRulesMatcher.matchAll(path).reverse());
}

function _captureError(error, type) {
  console.error(`[${type}]`, error);
  useNitroApp().captureError(error, { tags: [type] });
}
function trapUnhandledNodeErrors() {
  process.on(
    "unhandledRejection",
    (error) => _captureError(error, "unhandledRejection")
  );
  process.on(
    "uncaughtException",
    (error) => _captureError(error, "uncaughtException")
  );
}
function joinHeaders(value) {
  return Array.isArray(value) ? value.join(", ") : String(value);
}
function normalizeFetchResponse(response) {
  if (!response.headers.has("set-cookie")) {
    return response;
  }
  return new Response(response.body, {
    status: response.status,
    statusText: response.statusText,
    headers: normalizeCookieHeaders(response.headers)
  });
}
function normalizeCookieHeader(header = "") {
  return splitCookiesString(joinHeaders(header));
}
function normalizeCookieHeaders(headers) {
  const outgoingHeaders = new Headers();
  for (const [name, header] of headers) {
    if (name === "set-cookie") {
      for (const cookie of normalizeCookieHeader(header)) {
        outgoingHeaders.append("set-cookie", cookie);
      }
    } else {
      outgoingHeaders.set(name, joinHeaders(header));
    }
  }
  return outgoingHeaders;
}

/**
* Nitro internal functions extracted from https://github.com/nitrojs/nitro/blob/v2/src/runtime/internal/utils.ts
*/
function isJsonRequest(event) {
	// If the client specifically requests HTML, then avoid classifying as JSON.
	if (hasReqHeader(event, "accept", "text/html")) {
		return false;
	}
	return hasReqHeader(event, "accept", "application/json") || hasReqHeader(event, "user-agent", "curl/") || hasReqHeader(event, "user-agent", "httpie/") || hasReqHeader(event, "sec-fetch-mode", "cors") || event.path.startsWith("/api/") || event.path.endsWith(".json");
}
function hasReqHeader(event, name, includes) {
	const value = getRequestHeader(event, name);
	return !!(value && typeof value === "string" && value.toLowerCase().includes(includes));
}

const errorHandler$0 = (async function errorhandler(error, event, { defaultHandler }) {
	if (event.handled || isJsonRequest(event)) {
		// let Nitro handle JSON errors
		return;
	}
	// invoke default Nitro error handler (which will log appropriately if required)
	const defaultRes = await defaultHandler(error, event, { json: true });
	// let Nitro handle redirect if appropriate
	const status = error.status || error.statusCode || 500;
	if (status === 404 && defaultRes.status === 302) {
		setResponseHeaders(event, defaultRes.headers);
		setResponseStatus(event, defaultRes.status, defaultRes.statusText);
		return send(event, JSON.stringify(defaultRes.body, null, 2));
	}
	const errorObject = defaultRes.body;
	// remove proto/hostname/port from URL
	const url = new URL(errorObject.url);
	errorObject.url = withoutBase(url.pathname, useRuntimeConfig(event).app.baseURL) + url.search + url.hash;
	// add default server message (keep sanitized for unhandled errors)
	errorObject.message = error.unhandled ? errorObject.message || "Server Error" : error.message || errorObject.message || "Server Error";
	// we will be rendering this error internally so we can pass along the error.data safely
	errorObject.data ||= error.data;
	errorObject.statusText ||= error.statusText || error.statusMessage;
	delete defaultRes.headers["content-type"];
	delete defaultRes.headers["content-security-policy"];
	setResponseHeaders(event, defaultRes.headers);
	// Access request headers
	const reqHeaders = getRequestHeaders(event);
	// Detect to avoid recursion in SSR rendering of errors
	const isRenderingError = event.path.startsWith("/__nuxt_error") || !!reqHeaders["x-nuxt-error"];
	// HTML response (via SSR)
	const res = isRenderingError ? null : await useNitroApp().localFetch(withQuery(joinURL(useRuntimeConfig(event).app.baseURL, "/__nuxt_error"), errorObject), {
		headers: {
			...reqHeaders,
			"x-nuxt-error": "true"
		},
		redirect: "manual"
	}).catch(() => null);
	if (event.handled) {
		return;
	}
	// Fallback to static rendered error page
	if (!res) {
		const { template } = await import('../_/error-500.mjs');
		setResponseHeader(event, "Content-Type", "text/html;charset=UTF-8");
		return send(event, template(errorObject));
	}
	const html = await res.text();
	for (const [header, value] of res.headers.entries()) {
		if (header === "set-cookie") {
			appendResponseHeader(event, header, value);
			continue;
		}
		setResponseHeader(event, header, value);
	}
	setResponseStatus(event, res.status && res.status !== 200 ? res.status : defaultRes.status, res.statusText || defaultRes.statusText);
	return send(event, html);
});

function defineNitroErrorHandler(handler) {
  return handler;
}

const errorHandler$1 = defineNitroErrorHandler(
  function defaultNitroErrorHandler(error, event) {
    const res = defaultHandler(error, event);
    setResponseHeaders(event, res.headers);
    setResponseStatus(event, res.status, res.statusText);
    return send(event, JSON.stringify(res.body, null, 2));
  }
);
function defaultHandler(error, event, opts) {
  const isSensitive = error.unhandled || error.fatal;
  const statusCode = error.statusCode || 500;
  const statusMessage = error.statusMessage || "Server Error";
  const url = getRequestURL(event, { xForwardedHost: true, xForwardedProto: true });
  if (statusCode === 404) {
    const baseURL = "/";
    if (/^\/[^/]/.test(baseURL) && !url.pathname.startsWith(baseURL)) {
      const redirectTo = `${baseURL}${url.pathname.slice(1)}${url.search}`;
      return {
        status: 302,
        statusText: "Found",
        headers: { location: redirectTo },
        body: `Redirecting...`
      };
    }
  }
  if (isSensitive && !opts?.silent) {
    const tags = [error.unhandled && "[unhandled]", error.fatal && "[fatal]"].filter(Boolean).join(" ");
    console.error(`[request error] ${tags} [${event.method}] ${url}
`, error);
  }
  const headers = {
    "content-type": "application/json",
    // Prevent browser from guessing the MIME types of resources.
    "x-content-type-options": "nosniff",
    // Prevent error page from being embedded in an iframe
    "x-frame-options": "DENY",
    // Prevent browsers from sending the Referer header
    "referrer-policy": "no-referrer",
    // Disable the execution of any js
    "content-security-policy": "script-src 'none'; frame-ancestors 'none';"
  };
  setResponseStatus(event, statusCode, statusMessage);
  if (statusCode === 404 || !getResponseHeader(event, "cache-control")) {
    headers["cache-control"] = "no-cache";
  }
  const body = {
    error: true,
    url: url.href,
    statusCode,
    statusMessage,
    message: isSensitive ? "Server Error" : error.message,
    data: isSensitive ? void 0 : error.data
  };
  return {
    status: statusCode,
    statusText: statusMessage,
    headers,
    body
  };
}

const errorHandlers = [errorHandler$0, errorHandler$1];

async function errorHandler(error, event) {
  for (const handler of errorHandlers) {
    try {
      await handler(error, event, { defaultHandler });
      if (event.handled) {
        return; // Response handled
      }
    } catch(error) {
      // Handler itself thrown, log and continue
      console.error(error);
    }
  }
  // H3 will handle fallback
}

const plugins = [
  
];

const assets = {
  "/account.png": {
    "type": "image/png",
    "etag": "\"2a3-ztafiQrzVGWVtXeJXi1Az7Blsqc\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 675,
    "path": "../public/account.png"
  },
  "/discord.svg": {
    "type": "image/svg+xml",
    "etag": "\"453-KGzy9TB+KHT45zEhjqboFQxH+m0\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 1107,
    "path": "../public/discord.svg"
  },
  "/fallback-profile.webp": {
    "type": "image/webp",
    "etag": "\"46a8-mxkJNRY8adi0xmC70wDn4E3ZCWs\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 18088,
    "path": "../public/fallback-profile.webp"
  },
  "/favicon.ico": {
    "type": "image/vnd.microsoft.icon",
    "etag": "\"877-Ves/YRtZhRrcklOt1gjDPGu9XYU\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 2167,
    "path": "../public/favicon.ico"
  },
  "/home-assistant.png": {
    "type": "image/png",
    "etag": "\"42fb-SozvL/xZqVCi9EEAwOng4ZZPRlw\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 17147,
    "path": "../public/home-assistant.png"
  },
  "/gotify.png": {
    "type": "image/png",
    "etag": "\"c411-oahDMztCoPV4ojKsRAM3JgCD7iw\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 50193,
    "path": "../public/gotify.png"
  },
  "/icon.png": {
    "type": "image/png",
    "etag": "\"68d4-VMHWQqaE7VpUgtJaZE18n4XJJmE\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 26836,
    "path": "../public/icon.png"
  },
  "/matrix.png": {
    "type": "image/png",
    "etag": "\"d86-EwE7XXB7cqgaKpRMcG2l+KKCAdM\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 3462,
    "path": "../public/matrix.png"
  },
  "/pushover.svg": {
    "type": "image/svg+xml",
    "etag": "\"d37-ppAmZcmGtEgPtgVSyGrkvDF4u1Y\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 3383,
    "path": "../public/pushover.svg"
  },
  "/mealie-email-banner.png": {
    "type": "image/png",
    "etag": "\"10413-i6sQAs5fuIiZOmXPmNYyMPTyoUM\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 66579,
    "path": "../public/mealie-email-banner.png"
  },
  "/robots.txt": {
    "type": "text/plain; charset=utf-8",
    "etag": "\"18-xHzPGknCTMWEJDCqdccu9JEpJBI\"",
    "mtime": "2026-05-11T21:58:34.995Z",
    "size": 24,
    "path": "../public/robots.txt"
  },
  "/sw.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"51fe-spXW+QRjjAfA1CCR8Vy/mLER490\"",
    "mtime": "2026-05-11T21:58:35.623Z",
    "size": 20990,
    "path": "../public/sw.js"
  },
  "/workbox-8c29f6e4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3b13-Wh1UnUyyWpyRKjt8VAzqPv3W/FM\"",
    "mtime": "2026-05-11T21:58:35.623Z",
    "size": 15123,
    "path": "../public/workbox-8c29f6e4.js"
  },
  "/icons/android-chrome-192x192.png": {
    "type": "image/png",
    "etag": "\"14d5-t9iGKh0I1QUmtMFqqkLG/eHLyRs\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 5333,
    "path": "../public/icons/android-chrome-192x192.png"
  },
  "/icons/android-chrome-512x512.png": {
    "type": "image/png",
    "etag": "\"39b4-zLinsTusDhi32csuB4ViP2pvoEw\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 14772,
    "path": "../public/icons/android-chrome-512x512.png"
  },
  "/icons/android-chrome-maskable-192x192.png": {
    "type": "image/png",
    "etag": "\"120c-IyG6/AUvLF4Wh/tAODk76kdR10o\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 4620,
    "path": "../public/icons/android-chrome-maskable-192x192.png"
  },
  "/icons/android-chrome-maskable-512x512.png": {
    "type": "image/png",
    "etag": "\"2834-dEhqLJxVDg9VZuDV1ZOHB9krHRA\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 10292,
    "path": "../public/icons/android-chrome-maskable-512x512.png"
  },
  "/icons/apple-touch-icon.png": {
    "type": "image/png",
    "etag": "\"a52-2OBfpaTnwcDWOdh0vhBzZuGPt1k\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 2642,
    "path": "../public/icons/apple-touch-icon.png"
  },
  "/icons/favicon-16x16.png": {
    "type": "image/png",
    "etag": "\"373-hdNeN8If3YgKHK/+4x0qJ5Nbko4\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 883,
    "path": "../public/icons/favicon-16x16.png"
  },
  "/icons/favicon-32x32.png": {
    "type": "image/png",
    "etag": "\"469-9FE3DdZNxOYdVBqi8DbuzNM9xnI\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 1129,
    "path": "../public/icons/favicon-32x32.png"
  },
  "/icons/icon-x64.png": {
    "type": "image/png",
    "etag": "\"2a51-yu6p0XcoYpnZ+XI27Jzz1ybzBlE\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 10833,
    "path": "../public/icons/icon-x64.png"
  },
  "/icons/mdiCalendarMultiselect-192x192.png": {
    "type": "image/png",
    "etag": "\"5d7-leiC+h+GRLENIq3SrxWomZap/pU\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 1495,
    "path": "../public/icons/mdiCalendarMultiselect-192x192.png"
  },
  "/icons/mdiCalendarMultiselect-96x96.png": {
    "type": "image/png",
    "etag": "\"303-Tx5Y2w24VhJACDBhjN5hhF2pbNY\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 771,
    "path": "../public/icons/mdiCalendarMultiselect-96x96.png"
  },
  "/icons/mdiFormatListChecks-192x192.png": {
    "type": "image/png",
    "etag": "\"532-c8CjAhBHCA7ygjA5NStLzcCiM/o\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 1330,
    "path": "../public/icons/mdiFormatListChecks-192x192.png"
  },
  "/icons/mdiFormatListChecks-96x96.png": {
    "type": "image/png",
    "etag": "\"2f8-S+AK0KoqZWjkuli5H5bRy/SkBe8\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 760,
    "path": "../public/icons/mdiFormatListChecks-96x96.png"
  },
  "/icons/mstile-150x150.png": {
    "type": "image/png",
    "etag": "\"9e2-ap7mku3xtkY24xfhXS0wbcej2UI\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 2530,
    "path": "../public/icons/mstile-150x150.png"
  },
  "/icons/safari-pinned-tab.svg": {
    "type": "image/svg+xml",
    "etag": "\"629-98b2NJUT+31agEsu+7iLYCgTi+0\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 1577,
    "path": "../public/icons/safari-pinned-tab.svg"
  },
  "/screenshots/editor-narrow.png": {
    "type": "image/png",
    "etag": "\"35c91-iAu399/Gk5RHjJZJT0C7T0NF5s4\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 220305,
    "path": "../public/screenshots/editor-narrow.png"
  },
  "/screenshots/editor-wide.png": {
    "type": "image/png",
    "etag": "\"20842-k+5fw1ergy5mkCGL0GtTYxdN2BU\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 133186,
    "path": "../public/screenshots/editor-wide.png"
  },
  "/screenshots/parser-narrow.png": {
    "type": "image/png",
    "etag": "\"2347e-NIFhuXzRP00fofBcojFFEbgcuv0\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 144510,
    "path": "../public/screenshots/parser-narrow.png"
  },
  "/screenshots/parser-wide.png": {
    "type": "image/png",
    "etag": "\"2c27b-oPrc03dDbzCLWP0cdsyo1B9dIGs\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 180859,
    "path": "../public/screenshots/parser-wide.png"
  },
  "/svgs/admin-site-settings.svg": {
    "type": "image/svg+xml",
    "etag": "\"5473-6SpycM1L9XzUeVq5yzjyTdXzDzQ\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 21619,
    "path": "../public/svgs/admin-site-settings.svg"
  },
  "/svgs/data-reports.svg": {
    "type": "image/svg+xml",
    "etag": "\"1ede-5jHjutmqnA3WHiNlR/grOzBySvw\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 7902,
    "path": "../public/svgs/data-reports.svg"
  },
  "/svgs/manage-api-tokens.svg": {
    "type": "image/svg+xml",
    "etag": "\"56d4-uM0yZU77joLftBWH5G47xl2qwQc\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 22228,
    "path": "../public/svgs/manage-api-tokens.svg"
  },
  "/svgs/manage-cookbooks.svg": {
    "type": "image/svg+xml",
    "etag": "\"4158-IktiK7h9P0aHo9TsO42jm1dSYC4\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 16728,
    "path": "../public/svgs/manage-cookbooks.svg"
  },
  "/svgs/manage-data-migrations.svg": {
    "type": "image/svg+xml",
    "etag": "\"4014-jgr6dnwXKnZvk5mmZNrnvwZDeng\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 16404,
    "path": "../public/svgs/manage-data-migrations.svg"
  },
  "/svgs/manage-group-settings.svg": {
    "type": "image/svg+xml",
    "etag": "\"2b14-GpuZBbxMuqCNpBHnZZ7c4YGFHS0\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 11028,
    "path": "../public/svgs/manage-group-settings.svg"
  },
  "/svgs/manage-members.svg": {
    "type": "image/svg+xml",
    "etag": "\"909b-a5o8bVKCn/CSS2O0gCgqLsY9PF0\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 37019,
    "path": "../public/svgs/manage-members.svg"
  },
  "/svgs/manage-notifiers.svg": {
    "type": "image/svg+xml",
    "etag": "\"24bf-fLe+w5xHiC9NZnYWtD2jKoxJ/NE\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 9407,
    "path": "../public/svgs/manage-notifiers.svg"
  },
  "/svgs/manage-profile.svg": {
    "type": "image/svg+xml",
    "etag": "\"3411-dHGCl1/FUxuiNHlouX8tUwAwi78\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 13329,
    "path": "../public/svgs/manage-profile.svg"
  },
  "/svgs/manage-recipes.svg": {
    "type": "image/svg+xml",
    "etag": "\"2541-UQd+h20w8vT29NrNtPC82CWAnuQ\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 9537,
    "path": "../public/svgs/manage-recipes.svg"
  },
  "/svgs/manage-tasks.svg": {
    "type": "image/svg+xml",
    "etag": "\"269f-vFyDh33fMskSoHgdMaU16TKTxHQ\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 9887,
    "path": "../public/svgs/manage-tasks.svg"
  },
  "/svgs/manage-webhooks.svg": {
    "type": "image/svg+xml",
    "etag": "\"1007-HMa7LpPH62fD7UoIIgjMqcvK8LQ\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 4103,
    "path": "../public/svgs/manage-webhooks.svg"
  },
  "/svgs/recipes-create.svg": {
    "type": "image/svg+xml",
    "etag": "\"210c-Rn5w3c9SUPrWuBtvzH1Lzxo0unk\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 8460,
    "path": "../public/svgs/recipes-create.svg"
  },
  "/svgs/shopping-cart.svg": {
    "type": "image/svg+xml",
    "etag": "\"23ba-YBk+CNxcqxWkVncbuCT0ISpXZTU\"",
    "mtime": "2026-05-11T21:58:34.994Z",
    "size": 9146,
    "path": "../public/svgs/shopping-cart.svg"
  },
  "/_nuxt/08Gi2SHA.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-zPJGGoxgrOqwnWUIvEhyRya16NY\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 477,
    "path": "../public/_nuxt/08Gi2SHA.js"
  },
  "/screenshots/recipe-wide.png": {
    "type": "image/png",
    "etag": "\"ebb60-dO5nw8nWb9NkXixy9cygl5VVDt4\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 965472,
    "path": "../public/screenshots/recipe-wide.png"
  },
  "/screenshots/recipe-narrow.png": {
    "type": "image/png",
    "etag": "\"c080d-jWJ3zgXAYo/it9FC2tbNmONgTqs\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 788493,
    "path": "../public/screenshots/recipe-narrow.png"
  },
  "/screenshots/home-narrow.png": {
    "type": "image/png",
    "etag": "\"11f25c-eNxHhk49LYpC7BDYXzioTyuiPb4\"",
    "mtime": "2026-05-11T21:58:34.987Z",
    "size": 1176156,
    "path": "../public/screenshots/home-narrow.png"
  },
  "/screenshots/home-wide.png": {
    "type": "image/png",
    "etag": "\"1dafb7-IVgHLOejvQ5gX4QlAASid7pAtlg\"",
    "mtime": "2026-05-11T21:58:34.988Z",
    "size": 1945527,
    "path": "../public/screenshots/home-wide.png"
  },
  "/_nuxt/1p5D3XR4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-hF4FK6Hswdr4689qKbOr1fXX48I\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 477,
    "path": "../public/_nuxt/1p5D3XR4.js"
  },
  "/_nuxt/1rFIU-5g.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"417-9sqxIfTFJSOo8cOuQcfRCSWtA8g\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 1047,
    "path": "../public/_nuxt/1rFIU-5g.js"
  },
  "/_nuxt/1x4OaIg4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"16cf-oErIJm9wrONzyS7NMsvBEYoV3Pg\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 5839,
    "path": "../public/_nuxt/1x4OaIg4.js"
  },
  "/_nuxt/2-M5lWXf.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"d8-5WOz7eAxYOkGUvrFPyxbys+Pnss\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 216,
    "path": "../public/_nuxt/2-M5lWXf.js"
  },
  "/_nuxt/3dh0wUzh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"37bd-3waaWCBIzI2qVn9pP+f4JHWZ3XA\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 14269,
    "path": "../public/_nuxt/3dh0wUzh.js"
  },
  "/_nuxt/5JU3oUf-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"481a-JLLo2pGicQmPEXUijQHxu+rRW6s\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 18458,
    "path": "../public/_nuxt/5JU3oUf-.js"
  },
  "/_nuxt/5X2Fd2c4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1bf-rFD3mxt9a0OdeQ/ienZZtm91lUk\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 447,
    "path": "../public/_nuxt/5X2Fd2c4.js"
  },
  "/_nuxt/5-Nlk7O7.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1c665-Tp16PFdwxfvE2J1PbsMf/b93IZU\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 116325,
    "path": "../public/_nuxt/5-Nlk7O7.js"
  },
  "/_nuxt/5iZ40fX-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4924-Yt9VQEPOshilMIMurNNQnGqbg4w\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 18724,
    "path": "../public/_nuxt/5iZ40fX-.js"
  },
  "/_nuxt/5sq1VElT.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5a-xY3cLpNgXu7jLk5IwxxZXaXqnsM\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 90,
    "path": "../public/_nuxt/5sq1VElT.js"
  },
  "/_nuxt/6--iqVZy.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"67-QKYf6rBv97Z1FMhcdwQxQm1zUkg\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 103,
    "path": "../public/_nuxt/6--iqVZy.js"
  },
  "/_nuxt/664PMCwv.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c7-bNY9xzPXl6ROn3bskUTpVhb65eA\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 199,
    "path": "../public/_nuxt/664PMCwv.js"
  },
  "/_nuxt/7qcklD4u.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12a6-ZQA1JjfGxHn/5B0k+6KaF9x2Uc0\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 4774,
    "path": "../public/_nuxt/7qcklD4u.js"
  },
  "/_nuxt/7_QXmskr.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"120e4-ax2yyBiOTU7pZHZn8scXcEk4qck\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 73956,
    "path": "../public/_nuxt/7_QXmskr.js"
  },
  "/_nuxt/8XaFDZeE.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12a02-fKIBPS/7k5tUv49iHQA5ZUD3M0M\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 76290,
    "path": "../public/_nuxt/8XaFDZeE.js"
  },
  "/_nuxt/AppHeader.D_FmVP3F.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"29d-9V5aDVFZtBVl7eWp+N/wiU3yjq0\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 669,
    "path": "../public/_nuxt/AppHeader.D_FmVP3F.css"
  },
  "/_nuxt/8wchoERW.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"14920-zprGSKUaJ1lc6AHnGJvWwN7VZQ0\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 84256,
    "path": "../public/_nuxt/8wchoERW.js"
  },
  "/_nuxt/AppLogo.EwDlndIc.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"117-5DU6AaQKjr4wsyHhdTKoG38zPCU\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 279,
    "path": "../public/_nuxt/AppLogo.EwDlndIc.css"
  },
  "/_nuxt/AppSidebar.CtIbz10Q.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"a0b-YmWIvaxsq7aasD/FJyxpRPy+1oY\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 2571,
    "path": "../public/_nuxt/AppSidebar.CtIbz10Q.css"
  },
  "/_nuxt/AutoForm.Bhz5dwj0.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"401c-/05tlcl2iVPPkh7lUyOuVvIyjlY\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 16412,
    "path": "../public/_nuxt/AutoForm.Bhz5dwj0.css"
  },
  "/_nuxt/B-C9BZhG.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"158a-Whw0Ii2vWws+a8WcxuChjaNl5Eo\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 5514,
    "path": "../public/_nuxt/B-C9BZhG.js"
  },
  "/_nuxt/B-nAyJfk.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"8e3-WrifKVYpJ/zQOsrHZ6fHxiphd70\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 2275,
    "path": "../public/_nuxt/B-nAyJfk.js"
  },
  "/_nuxt/B0YlXxKn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3df-F2EWmrkAz/NjydawLuEXgv31OMw\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 991,
    "path": "../public/_nuxt/B0YlXxKn.js"
  },
  "/_nuxt/B1vrJEjS.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-4TX+xfbXJ+BZ+9/3R7ad7l1ttPQ\"",
    "mtime": "2026-05-11T21:58:34.966Z",
    "size": 477,
    "path": "../public/_nuxt/B1vrJEjS.js"
  },
  "/_nuxt/B3LgXoKV.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"48a3-2H7u8mE4375xxbjU2HojfVSR61s\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 18595,
    "path": "../public/_nuxt/B3LgXoKV.js"
  },
  "/_nuxt/B4VRyijr.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"852-9WZLid0q6wJVHQ5ToiGulU6wpeI\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 2130,
    "path": "../public/_nuxt/B4VRyijr.js"
  },
  "/_nuxt/B4c_1J1g.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-qlsNmOAAB7joeWCrGdkMAoQEMdA\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 477,
    "path": "../public/_nuxt/B4c_1J1g.js"
  },
  "/_nuxt/B4wfpnvP.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"107e-EtIpFrW77xqUSCao3D1v7mnCDxk\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 4222,
    "path": "../public/_nuxt/B4wfpnvP.js"
  },
  "/_nuxt/B4leTdX6.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2124b-XFpp8rJRmcq5QGDFEa57yuGNYfI\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 135755,
    "path": "../public/_nuxt/B4leTdX6.js"
  },
  "/_nuxt/B58TzoZH.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1f8-bRDhGPELM5oMw1aNRq0fQy+8rZo\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 504,
    "path": "../public/_nuxt/B58TzoZH.js"
  },
  "/_nuxt/B5Itw95w.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-K2x2xNEnXN7xuIN9hRuvq1HfiIQ\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 477,
    "path": "../public/_nuxt/B5Itw95w.js"
  },
  "/_nuxt/B6-nkIoj.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5f-Ii0ooNd1OYQncQGO4EizIyNFi6w\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 95,
    "path": "../public/_nuxt/B6-nkIoj.js"
  },
  "/_nuxt/B6rQLnGx.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"be7-nHHFK6fP+hlljfCkIDk/9ardQZ0\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 3047,
    "path": "../public/_nuxt/B6rQLnGx.js"
  },
  "/_nuxt/B7aO1kdQ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"30a-PFxbnEVM1v2MFEMx5Bf7kPZvXHI\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 778,
    "path": "../public/_nuxt/B7aO1kdQ.js"
  },
  "/_nuxt/BAGBNNmg.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3ce-sUgLvbuW1vQk6YAVr0Uz1AfoM/k\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 974,
    "path": "../public/_nuxt/BAGBNNmg.js"
  },
  "/_nuxt/BBHGKdwO.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1b499-0KgnzIu1v5/b1v7Bgx53iwWTxDY\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 111769,
    "path": "../public/_nuxt/BBHGKdwO.js"
  },
  "/_nuxt/BCErUI21.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"95ba-WVycWyVWFnKbByePuveFD80uFpA\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 38330,
    "path": "../public/_nuxt/BCErUI21.js"
  },
  "/_nuxt/BCgjHzJ8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"552-hvyrNAYR+kyNV8RFgOa4iLdmcKg\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 1362,
    "path": "../public/_nuxt/BCgjHzJ8.js"
  },
  "/_nuxt/BCPtXB1v.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"11c6a-YVMcsQMK7NOFb5BIVeEfjKFX0PA\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 72810,
    "path": "../public/_nuxt/BCPtXB1v.js"
  },
  "/_nuxt/BD4KXYlV.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"78c-NEsgKZC7pqEob922RgIeo/4XQkQ\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 1932,
    "path": "../public/_nuxt/BD4KXYlV.js"
  },
  "/_nuxt/BDxysj2v.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2f0-vTK8aW8fNgIYLkqCHGLVuGlY8UA\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 752,
    "path": "../public/_nuxt/BDxysj2v.js"
  },
  "/_nuxt/BE-rkVve.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"43be-ybDFgRTlR5d6MZPwXjagVZk0ikM\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 17342,
    "path": "../public/_nuxt/BE-rkVve.js"
  },
  "/_nuxt/BEOT_YiR.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13ea-jXdGX+mbGF8IHZJj7yxMgH1l7cU\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 5098,
    "path": "../public/_nuxt/BEOT_YiR.js"
  },
  "/_nuxt/BG2P73uy.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3f7-0pVIRoLitRRZjdA8U77brwUWoTA\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 1015,
    "path": "../public/_nuxt/BG2P73uy.js"
  },
  "/_nuxt/BGXb3sce.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5dd-atEv6bx79vcIvNjBPA1ny8q/y+k\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 1501,
    "path": "../public/_nuxt/BGXb3sce.js"
  },
  "/_nuxt/BGYBFyyT.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c9a-Fc/keg0TddneiB1pPrOghxSAB7o\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 3226,
    "path": "../public/_nuxt/BGYBFyyT.js"
  },
  "/_nuxt/BGkFWiwE.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1a848-Cg0dxektySLKabFSrITN8leVkQo\"",
    "mtime": "2026-05-11T21:58:34.967Z",
    "size": 108616,
    "path": "../public/_nuxt/BGkFWiwE.js"
  },
  "/_nuxt/BHAZ5JTv.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2237-zHV+EDNIvBLQ5tsiismW+CeeMc4\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 8759,
    "path": "../public/_nuxt/BHAZ5JTv.js"
  },
  "/_nuxt/BIl4cyR9.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1681-8OY0+UaG+gUiKJSGiH59s2B32fM\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 5761,
    "path": "../public/_nuxt/BIl4cyR9.js"
  },
  "/_nuxt/BJY10GIc.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1ea3-g9QiMEmfc7NR4ApM+zqGU11Bf5s\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 7843,
    "path": "../public/_nuxt/BJY10GIc.js"
  },
  "/_nuxt/BLAr5WRl.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"725-ys2MgvNoPuZJwSIEezM57xol/g0\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 1829,
    "path": "../public/_nuxt/BLAr5WRl.js"
  },
  "/_nuxt/BLVr6lGH.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"15aa-Xl3td70DPa3UtgRqO/gfsfwL+oQ\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 5546,
    "path": "../public/_nuxt/BLVr6lGH.js"
  },
  "/_nuxt/BMMnncbJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"131e8-oiCg8TQXMtwZ1VJh6KbxFBQDcfY\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 78312,
    "path": "../public/_nuxt/BMMnncbJ.js"
  },
  "/_nuxt/BMOCeuHl.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4264-PSd33uPasnx4sa7cr3mP4OtviV0\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 16996,
    "path": "../public/_nuxt/BMOCeuHl.js"
  },
  "/_nuxt/BMZONqPX.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"bd7-J4OGm/R6WGPov9Z4wuO828QermM\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 3031,
    "path": "../public/_nuxt/BMZONqPX.js"
  },
  "/_nuxt/BMnVbJQy.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"a3a7-COkWG+fx0ESze9kuO4pJHhk2xRs\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 41895,
    "path": "../public/_nuxt/BMnVbJQy.js"
  },
  "/_nuxt/BNuA_ywh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"e7b-EfRut2Kaa8HTDGOn74nUDRzD37s\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 3707,
    "path": "../public/_nuxt/BNuA_ywh.js"
  },
  "/_nuxt/BPVNdbJt.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-dsbGRusQyIULpmN6FmKcxE6JOe0\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 477,
    "path": "../public/_nuxt/BPVNdbJt.js"
  },
  "/_nuxt/BNvWBybs.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"7b66-Fn3dH2Hkd2eXYhNL3eaTYpLrEIQ\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 31590,
    "path": "../public/_nuxt/BNvWBybs.js"
  },
  "/_nuxt/BPVaTnTV.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"24ae-SEo2ZTztZ6+HAZfC4bUQdfyEicY\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 9390,
    "path": "../public/_nuxt/BPVaTnTV.js"
  },
  "/_nuxt/BQpv8DN6.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"82-MkZkjmxC4i74R1nQmc7+HLRvmYs\"",
    "mtime": "2026-05-11T21:58:34.968Z",
    "size": 130,
    "path": "../public/_nuxt/BQpv8DN6.js"
  },
  "/_nuxt/BR10fTN-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"258-b41C6Hr4PTvq+lX9wcwRQIpzhCU\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 600,
    "path": "../public/_nuxt/BR10fTN-.js"
  },
  "/_nuxt/BRtKyAeb.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"59bf-57XuDXDPPZoNldTJRue4foRUvZI\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 22975,
    "path": "../public/_nuxt/BRtKyAeb.js"
  },
  "/_nuxt/BRzSBHF_.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"330-Mf4UH4I67tXbDJDpgnbonEJdJGU\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 816,
    "path": "../public/_nuxt/BRzSBHF_.js"
  },
  "/_nuxt/BSKD5ePh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-txIQLh8WMByX+7jmp6Cquj5Nlfs\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 477,
    "path": "../public/_nuxt/BSKD5ePh.js"
  },
  "/_nuxt/BSzZI3Io.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"8fe-P+rKXHXewI7AJvthWzvRJr7E2Kw\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 2302,
    "path": "../public/_nuxt/BSzZI3Io.js"
  },
  "/_nuxt/BT4Sdehq.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1d77-D1ZQicj432xe2lCawgIGYAO+Xd0\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 7543,
    "path": "../public/_nuxt/BT4Sdehq.js"
  },
  "/_nuxt/BS_mDjw5.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13d48-xwhzgpO1u6NFMYPm4Bw22C5kioc\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 81224,
    "path": "../public/_nuxt/BS_mDjw5.js"
  },
  "/_nuxt/BTZ7DjV-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13dd1-qQzF+2fZboR+EIiWT1fT3c/b984\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 81361,
    "path": "../public/_nuxt/BTZ7DjV-.js"
  },
  "/_nuxt/BUFEHwfK.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"804-MN1ROQf1gajCin2bmAhMiD7gPw0\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 2052,
    "path": "../public/_nuxt/BUFEHwfK.js"
  },
  "/_nuxt/BUw-a05R.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-UcIFOjTHKBKtNqKxkF00fTvt1Fw\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 477,
    "path": "../public/_nuxt/BUw-a05R.js"
  },
  "/_nuxt/BW7Y5P_R.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"ad0-jHBm+bsyJRZgcly953kwx7uENA4\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 2768,
    "path": "../public/_nuxt/BW7Y5P_R.js"
  },
  "/_nuxt/BYrS86dR.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1a69c-f7h6zfd4PzWqGYxQQZQfLtAMNwc\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 108188,
    "path": "../public/_nuxt/BYrS86dR.js"
  },
  "/_nuxt/BYtiRF7f.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"15eb-re2wbp0bRHm1YX4L0ptuLxPACoo\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 5611,
    "path": "../public/_nuxt/BYtiRF7f.js"
  },
  "/_nuxt/BZmiGpdZ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"711-3yOIxmMRgwDOrbVKUhhJh+YKkF8\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 1809,
    "path": "../public/_nuxt/BZmiGpdZ.js"
  },
  "/_nuxt/BZnCyfI3.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3721-6gIuvgAxdT2wCQVzP1lQTroQ4DU\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 14113,
    "path": "../public/_nuxt/BZnCyfI3.js"
  },
  "/_nuxt/B_AY8yub.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"20d4-A9Iw1YtaiY3Saqe2VLASJsv1pqQ\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 8404,
    "path": "../public/_nuxt/B_AY8yub.js"
  },
  "/_nuxt/B_mDSMcs.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"360-KOBi9Jo59VIEGEQfNTFv4uTvUog\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 864,
    "path": "../public/_nuxt/B_mDSMcs.js"
  },
  "/_nuxt/BaseButtonGroup.DQQjfeyl.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"80-6TdTRAOGMRe6EVaZJqzl91iY6i4\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 128,
    "path": "../public/_nuxt/BaseButtonGroup.DQQjfeyl.css"
  },
  "/_nuxt/BaseDialog.CAWfojTc.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"22-4rISm1HWEwhfKT6iArv/r0ZaCmQ\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 34,
    "path": "../public/_nuxt/BaseDialog.CAWfojTc.css"
  },
  "/_nuxt/BasePageTitle.U29T7HGJ.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"59-sV9TsDaYTn+tSvUEnG6ne37uN3E\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 89,
    "path": "../public/_nuxt/BasePageTitle.U29T7HGJ.css"
  },
  "/_nuxt/BcJJxRn8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10b9-zf4ZgLS/OHDbtShAaLU7mS6J2ao\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 4281,
    "path": "../public/_nuxt/BcJJxRn8.js"
  },
  "/_nuxt/BgZadWQp.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1c77-bU2fO642LoekwGmkq/yh+FqrCRs\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 7287,
    "path": "../public/_nuxt/BgZadWQp.js"
  },
  "/_nuxt/Bh_EXXPn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10f6f-7xFZ1eL1Su4ouGMZ6PMPgGjJcsw\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 69487,
    "path": "../public/_nuxt/Bh_EXXPn.js"
  },
  "/_nuxt/BiapMOoK.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c70-47iK+U6xpCdyXOQCntjb6mjaiaQ\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 3184,
    "path": "../public/_nuxt/BiapMOoK.js"
  },
  "/_nuxt/BifIOTiJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1346-THjcEMR8SGrPyksq+tQYDipHR5M\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 4934,
    "path": "../public/_nuxt/BifIOTiJ.js"
  },
  "/_nuxt/Bjzm3L-f.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"993-NuDbXSxTKvCZvsYrvvrZYH85gXk\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 2451,
    "path": "../public/_nuxt/Bjzm3L-f.js"
  },
  "/_nuxt/BkP_9oBU.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"9a-P7qp/baRjUhrnvcgyGxrO0Iw5bs\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 154,
    "path": "../public/_nuxt/BkP_9oBU.js"
  },
  "/_nuxt/Bj0IsOiL.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"137f2-eiiMZCS2MRegVhAs/8+YVbRaxtk\"",
    "mtime": "2026-05-11T21:58:34.969Z",
    "size": 79858,
    "path": "../public/_nuxt/Bj0IsOiL.js"
  },
  "/_nuxt/BlMtpda4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"864-XeZr1hBINXMaJo0363bsX6huWi0\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 2148,
    "path": "../public/_nuxt/BlMtpda4.js"
  },
  "/_nuxt/Bmr5BguW.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"15d-oCY5NW8796FjMRz2ibsYbHFgJ2I\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 349,
    "path": "../public/_nuxt/Bmr5BguW.js"
  },
  "/_nuxt/BmvKDsBR.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"234-WAsBiuN2WtU62kTY/R8xYAWSaVw\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 564,
    "path": "../public/_nuxt/BmvKDsBR.js"
  },
  "/_nuxt/Bo3bKcRy.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1339-3BCCneAV1CiGbubhKXYSdWvvsEw\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 4921,
    "path": "../public/_nuxt/Bo3bKcRy.js"
  },
  "/_nuxt/Bocek-xg.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"28a1-6SBWKBsItTdOglgK3zr4OIKXX4M\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 10401,
    "path": "../public/_nuxt/Bocek-xg.js"
  },
  "/_nuxt/BoewrS_7.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3ce-zuqXOrGdA/U6BdbtwCqhR6UtE/I\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 974,
    "path": "../public/_nuxt/BoewrS_7.js"
  },
  "/_nuxt/BqCd4_sy.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"164b-IJoI0GEuTPAtyEkKOpKVRYN5xrE\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 5707,
    "path": "../public/_nuxt/BqCd4_sy.js"
  },
  "/_nuxt/BqG-X_td.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"d7b-Jf5fGoChZzNyfv5Y0u3OJTYl6nM\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 3451,
    "path": "../public/_nuxt/BqG-X_td.js"
  },
  "/_nuxt/Bqb3pK9f.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1107-zhdedmMEJUbSjOHOjxeDm1ft20s\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 4359,
    "path": "../public/_nuxt/Bqb3pK9f.js"
  },
  "/_nuxt/Br9fUWav.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-mXZ4/7JatLPpqb9PN0etwffDu/E\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 477,
    "path": "../public/_nuxt/Br9fUWav.js"
  },
  "/_nuxt/BrijVzbw.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"19a7-UC/5PYe4hnBmrkLuvZbkj6uxRJM\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 6567,
    "path": "../public/_nuxt/BrijVzbw.js"
  },
  "/_nuxt/BsoOi1s7.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"9b6-7T5kz007OxkGyJCNCFbI21bUbBg\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 2486,
    "path": "../public/_nuxt/BsoOi1s7.js"
  },
  "/_nuxt/BssDRXED.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1179-rhNmjlzPsIs/Q+DFXwxfrTUsUP0\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 4473,
    "path": "../public/_nuxt/BssDRXED.js"
  },
  "/_nuxt/Bt-SjrtF.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"6d3-KsBHJeoB4Ftxcpy1gBiVAMGitqs\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 1747,
    "path": "../public/_nuxt/Bt-SjrtF.js"
  },
  "/_nuxt/Bu01R9wh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-/FfGqqMns7F5qKNO+krKWnLavJU\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 477,
    "path": "../public/_nuxt/Bu01R9wh.js"
  },
  "/_nuxt/BuBqirkA.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"aea-VCYE3MAGBd065nh6xKRnlNPWr6c\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 2794,
    "path": "../public/_nuxt/BuBqirkA.js"
  },
  "/_nuxt/Bw5MvWEz.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2664-p70ax5rRGM73SSgAw6bBg+4o1vY\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 9828,
    "path": "../public/_nuxt/Bw5MvWEz.js"
  },
  "/_nuxt/ByH1cxAp.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"309-kuFNoco1Vj/bmk/++ltOK958JAE\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 777,
    "path": "../public/_nuxt/ByH1cxAp.js"
  },
  "/_nuxt/BxnSKzgw.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1de8-PEHl1iB1nuswGDfhZfzU9qUen1U\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 7656,
    "path": "../public/_nuxt/BxnSKzgw.js"
  },
  "/_nuxt/Byk6uuv8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"82c-kXVtUt3a5MvUFsrImjJKRrK1bpk\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 2092,
    "path": "../public/_nuxt/Byk6uuv8.js"
  },
  "/_nuxt/C-BWxKFP.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"18f1-f46jM3oAQ/RSPWBJ3YUm5VIMx8c\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 6385,
    "path": "../public/_nuxt/C-BWxKFP.js"
  },
  "/_nuxt/C0B40Hx0.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"43e-NMKdvaLf9L62HYCpsfst4bR28yw\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 1086,
    "path": "../public/_nuxt/C0B40Hx0.js"
  },
  "/_nuxt/C2KJkdSh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2195-sOZdHZb9Xbk4j9YFyFi1ZV/XltE\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 8597,
    "path": "../public/_nuxt/C2KJkdSh.js"
  },
  "/_nuxt/C2gHkgTP.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"bb7-9QdvZVxGi0iFLEyaltT5xS/vFh8\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 2999,
    "path": "../public/_nuxt/C2gHkgTP.js"
  },
  "/_nuxt/C2PtNRBJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12dc4-9O1OuNHxsQg2KIaJmBgea7Mgs10\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 77252,
    "path": "../public/_nuxt/C2PtNRBJ.js"
  },
  "/_nuxt/C2q5c54j.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1ca-mWwY005LK02pAMQoIuxMLjKc+DI\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 458,
    "path": "../public/_nuxt/C2q5c54j.js"
  },
  "/_nuxt/C4FZAFz4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2f6-KdLaf8TA+B3AAdm8UCXkojw3lpU\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 758,
    "path": "../public/_nuxt/C4FZAFz4.js"
  },
  "/_nuxt/C3OOBwHh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1290f-kJ5ybRs4Md8EJAGCoFtbDtr6aDU\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 76047,
    "path": "../public/_nuxt/C3OOBwHh.js"
  },
  "/_nuxt/C61EalX-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"27cd-02ZgbwwAShFCa5oGQulQ8sE1Als\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 10189,
    "path": "../public/_nuxt/C61EalX-.js"
  },
  "/_nuxt/C6Wk5Ayd.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"72-PDd7W4y8Wj/c5VoooIgFM18MIRo\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 114,
    "path": "../public/_nuxt/C6Wk5Ayd.js"
  },
  "/_nuxt/C75PGHGp.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4114-CvjTs0Ww/PYKzyipiv1NwRLqWkI\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 16660,
    "path": "../public/_nuxt/C75PGHGp.js"
  },
  "/_nuxt/C7COsrzC.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-ZTnDub8yFRqOfspSBYF3j/VnkHI\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 477,
    "path": "../public/_nuxt/C7COsrzC.js"
  },
  "/_nuxt/C7wVpybj.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"945-iMsNOLZuYcVoV76pcMawvXxC3fw\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 2373,
    "path": "../public/_nuxt/C7wVpybj.js"
  },
  "/_nuxt/BvTdO2I-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"100158-w6uvmF3Cyk0b/vkd14UJwRqKRjQ\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 1048920,
    "path": "../public/_nuxt/BvTdO2I-.js"
  },
  "/_nuxt/C7xdETuD.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"91-6S/b2hyQbCmR3fsxcVL/FYfkEFk\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 145,
    "path": "../public/_nuxt/C7xdETuD.js"
  },
  "/_nuxt/C8NNVf-0.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10f7-TagwjhknV61IPmccbqw3MCP4lpM\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 4343,
    "path": "../public/_nuxt/C8NNVf-0.js"
  },
  "/_nuxt/C9ROHDGT.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12c0f-iHiA1XQnaXAGJpKE/G5NH3ChWaI\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 76815,
    "path": "../public/_nuxt/C9ROHDGT.js"
  },
  "/_nuxt/CBFarrKC.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"308-ixnQS86F14gC+LMrudNs8DvfJcg\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 776,
    "path": "../public/_nuxt/CBFarrKC.js"
  },
  "/_nuxt/CC36lCGk.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1580-YiADvPBaZ59d5TkKdhbR35UeqNA\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 5504,
    "path": "../public/_nuxt/CC36lCGk.js"
  },
  "/_nuxt/CDCwEbzN.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"20b6-m/na+Qy6ZmMvfF0d3zawumlwGgg\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 8374,
    "path": "../public/_nuxt/CDCwEbzN.js"
  },
  "/_nuxt/CCkhWwPL.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"11e34-m7lxuu7QbTnPrmw2x958qqSNbVU\"",
    "mtime": "2026-05-11T21:58:34.970Z",
    "size": 73268,
    "path": "../public/_nuxt/CCkhWwPL.js"
  },
  "/_nuxt/CEDXJl5L.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-5knr5+FpzcdQCh8+fjNzfAIo8LI\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 477,
    "path": "../public/_nuxt/CEDXJl5L.js"
  },
  "/_nuxt/CG98-WHR.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"a6c-ZhvPhHZqZI5DiUYwli9simC0eO0\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 2668,
    "path": "../public/_nuxt/CG98-WHR.js"
  },
  "/_nuxt/CGolDEwD.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12aac-RUg7lD/3q8IraDLw3oydFA/Yt50\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 76460,
    "path": "../public/_nuxt/CGolDEwD.js"
  },
  "/_nuxt/CH7M_bSn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"150d-AaM3lSVf4HP3HfV8uBrkci9k9qo\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 5389,
    "path": "../public/_nuxt/CH7M_bSn.js"
  },
  "/_nuxt/CJMIOzFv.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"630-hXEqnaxrRmrH2R1zW533BVJFde0\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 1584,
    "path": "../public/_nuxt/CJMIOzFv.js"
  },
  "/_nuxt/CJWbHyC8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-ulo6dlb4ibxIx25uf985natGEU4\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 477,
    "path": "../public/_nuxt/CJWbHyC8.js"
  },
  "/_nuxt/CKqUbfcN.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-pc918LO4ZVjBw8AV5wP7EdOX3ko\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 477,
    "path": "../public/_nuxt/CKqUbfcN.js"
  },
  "/_nuxt/CKvf_BQn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13f4-3zlEHr9tcmm1UP/FBtgDsVnacRI\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 5108,
    "path": "../public/_nuxt/CKvf_BQn.js"
  },
  "/_nuxt/CMLop-UK.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5f3b-3v14leDvmWZDH2StmSw1QeQ1qEY\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 24379,
    "path": "../public/_nuxt/CMLop-UK.js"
  },
  "/_nuxt/CN9sfrp9.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"f6f-tDxFosCgopDu5rtwehI4s3YPeMM\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 3951,
    "path": "../public/_nuxt/CN9sfrp9.js"
  },
  "/_nuxt/CONh1DRW.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1ca-a4Jj0dFtOL3M0j5c8bByWHlHDnU\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 458,
    "path": "../public/_nuxt/CONh1DRW.js"
  },
  "/_nuxt/CP4DyQH1.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"100ae-KRV4T5A6uRLvFGloS1BWS1mLs0c\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 65710,
    "path": "../public/_nuxt/CP4DyQH1.js"
  },
  "/_nuxt/CPzcsGxn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1e9-YYnxCNXU8dvfVUxLRhaQJf0+ZeQ\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 489,
    "path": "../public/_nuxt/CPzcsGxn.js"
  },
  "/_nuxt/CP7fuWon.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"58-dD0MEvNAN8wityEw63fkNVCmehY\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 88,
    "path": "../public/_nuxt/CP7fuWon.js"
  },
  "/_nuxt/CQ1vglix.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12cef-2xjIE5Tvrt3uEyb3Np6dbELpwMw\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 77039,
    "path": "../public/_nuxt/CQ1vglix.js"
  },
  "/_nuxt/CQ6JBTw3.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"f5-lBPXl8BUc9+64cofDt9JrGkLScE\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 245,
    "path": "../public/_nuxt/CQ6JBTw3.js"
  },
  "/_nuxt/CQSjmIHO.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-wDk8z0TUGDiCP011q3n1gEWytHY\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 477,
    "path": "../public/_nuxt/CQSjmIHO.js"
  },
  "/_nuxt/CRZfbOWO.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10459-fZWKKs4Gdx4rdfMH8Gq+28vkYRA\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 66649,
    "path": "../public/_nuxt/CRZfbOWO.js"
  },
  "/_nuxt/CT82wkVp.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"95f-WZcXMHB4s73/cnDQxptq/53OTIE\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 2399,
    "path": "../public/_nuxt/CT82wkVp.js"
  },
  "/_nuxt/CUnpfX-r.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13fe-qkdVtb4Wruxy9AQhWgPD0T8Lcx8\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 5118,
    "path": "../public/_nuxt/CUnpfX-r.js"
  },
  "/_nuxt/CVsq99uS.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"195e-Z+gOkWi4CXBxV6NyUz1Vf3qiVKU\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 6494,
    "path": "../public/_nuxt/CVsq99uS.js"
  },
  "/_nuxt/CWSnCnNN.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-0cAKX7Z5iUVrEeXEAtQzw+gzgaI\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 477,
    "path": "../public/_nuxt/CWSnCnNN.js"
  },
  "/_nuxt/CWgdMCa2.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-cF7tYe9pM0lljoBluCxUWucZ1ug\"",
    "mtime": "2026-05-11T21:58:34.973Z",
    "size": 477,
    "path": "../public/_nuxt/CWgdMCa2.js"
  },
  "/_nuxt/CWi5EEl4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"86f-UPHNC8+FZefJrcn68aSxL97VWjc\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 2159,
    "path": "../public/_nuxt/CWi5EEl4.js"
  },
  "/_nuxt/CXDYpYYP.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c58-udDXkYMdOGgJzchBUXwlH4aYrck\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 3160,
    "path": "../public/_nuxt/CXDYpYYP.js"
  },
  "/_nuxt/CX4ADzgq.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"962-8OLgbg5g4mragl2Y3IODkr2M2wE\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 2402,
    "path": "../public/_nuxt/CX4ADzgq.js"
  },
  "/_nuxt/CXE9aF61.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"8f5-RywVpNM2lGRfMcyVVVs0OHo8X+Q\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 2293,
    "path": "../public/_nuxt/CXE9aF61.js"
  },
  "/_nuxt/CY3x1O-Y.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"14f8-c4bl2gIOyxli5ckJ33aZzLOWzSI\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 5368,
    "path": "../public/_nuxt/CY3x1O-Y.js"
  },
  "/_nuxt/CZMHtttm.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"86-K0kQ/kzisAV0O9Vxe5VxybuXkn0\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 134,
    "path": "../public/_nuxt/CZMHtttm.js"
  },
  "/_nuxt/CZxnVFoT.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"17c0-3Dc8t2DYm/dbZkTNqfPvtxBpcYI\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 6080,
    "path": "../public/_nuxt/CZxnVFoT.js"
  },
  "/_nuxt/Ca1dOGtW.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"bf-2EjEu9yWVFMg1ZHbKUkjHlzRxj0\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 191,
    "path": "../public/_nuxt/Ca1dOGtW.js"
  },
  "/_nuxt/CantN5LY.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-vZSQNuPRMCAQV9hJ+zdCQF86nLk\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 477,
    "path": "../public/_nuxt/CantN5LY.js"
  },
  "/_nuxt/Ca6N7nKb.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"118a2-c90eCrOo8R1DT0BeaDtAMdXQ+/Q\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 71842,
    "path": "../public/_nuxt/Ca6N7nKb.js"
  },
  "/_nuxt/Cb7kZSuv.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c5-8msxSWIeWE7otOeTJ43KVw8WAXM\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 197,
    "path": "../public/_nuxt/Cb7kZSuv.js"
  },
  "/_nuxt/CaxvrVyb.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"9ce8-mYJTuACYoERLsjOUPtnshxRV3Z8\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 40168,
    "path": "../public/_nuxt/CaxvrVyb.js"
  },
  "/_nuxt/CeqCeBqB.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c7-J91rvpDDat9lMGM62TYx7wzXr9A\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 199,
    "path": "../public/_nuxt/CeqCeBqB.js"
  },
  "/_nuxt/CfxKJf9L.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"261b-39/4Ccbs+TadchCSTVMi0gffLyA\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 9755,
    "path": "../public/_nuxt/CfxKJf9L.js"
  },
  "/_nuxt/Ci5aWdzQ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"9c-MyDiz44/7nYZ3kuvXSwNjD+HZ8E\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 156,
    "path": "../public/_nuxt/Ci5aWdzQ.js"
  },
  "/_nuxt/ChlXZuy4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"124f6-dwyd1p5Ks7msmPVuEC0u4wRXn7c\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 74998,
    "path": "../public/_nuxt/ChlXZuy4.js"
  },
  "/_nuxt/Ci6qYH_H.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"272-5kUEIJumnbCf6v8W7xyv8aqjlEc\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 626,
    "path": "../public/_nuxt/Ci6qYH_H.js"
  },
  "/_nuxt/Ci_IE3nd.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"680-kvplUtVTQUfpnFJk7pwGXk3eafI\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 1664,
    "path": "../public/_nuxt/Ci_IE3nd.js"
  },
  "/_nuxt/CjeRYFSe.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"85c-cnWLsYTypLhb8dg/T6qgIwk0+vw\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 2140,
    "path": "../public/_nuxt/CjeRYFSe.js"
  },
  "/_nuxt/CjyBVroM.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"574-RDtQ+Tt23OlcunI/YGCZ99U1siU\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 1396,
    "path": "../public/_nuxt/CjyBVroM.js"
  },
  "/_nuxt/CkPx-LxE.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c8-ks2rnA2237D2v1k8Fagj6MAPjnQ\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 200,
    "path": "../public/_nuxt/CkPx-LxE.js"
  },
  "/_nuxt/CkyDPvMs.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"343a-o6hshv84yJ+0l1d7bbct8BnXyWo\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 13370,
    "path": "../public/_nuxt/CkyDPvMs.js"
  },
  "/_nuxt/ClB7w50o.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"a5a-G7T+tfzGykZoXi9xKLtF4tzwNDc\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 2650,
    "path": "../public/_nuxt/ClB7w50o.js"
  },
  "/_nuxt/CmMjPU2u.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5e1-uaDwKxf0r3f8Ss0zdhZoPCEHg6w\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 1505,
    "path": "../public/_nuxt/CmMjPU2u.js"
  },
  "/_nuxt/Cnftxm7Q.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-JqzKqqe1BoKaOq3X+uGq+U7A6iY\"",
    "mtime": "2026-05-11T21:58:34.974Z",
    "size": 477,
    "path": "../public/_nuxt/Cnftxm7Q.js"
  },
  "/_nuxt/CqBX-pF8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"e54-Z+Emzw5lDyZhXa5TndQ0Wzk3Nq4\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 3668,
    "path": "../public/_nuxt/CqBX-pF8.js"
  },
  "/_nuxt/CqB_oSxJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c6f-Mzx/ZaDM+UYZRfrQVu6MA2km46o\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 3183,
    "path": "../public/_nuxt/CqB_oSxJ.js"
  },
  "/_nuxt/CqWUpIgW.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3a9-D44ZLyRJ7facxF18mMTYOR2+w7s\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 937,
    "path": "../public/_nuxt/CqWUpIgW.js"
  },
  "/_nuxt/CsEb4KnD.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"417-ScYvkbeuadT0z+zLxDicswvVsUI\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 1047,
    "path": "../public/_nuxt/CsEb4KnD.js"
  },
  "/_nuxt/CtDxqSsn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"109b-OMbsKem2pjP43VD6l+++yVXLknA\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 4251,
    "path": "../public/_nuxt/CtDxqSsn.js"
  },
  "/_nuxt/CsuQkr0U.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13005-VAxT45Pojq1tC15GV/bGkhtFsJA\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 77829,
    "path": "../public/_nuxt/CsuQkr0U.js"
  },
  "/_nuxt/Ctsk8Shx.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-Fj/1xUWmDlWM1rHZZf2tRbocouY\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 477,
    "path": "../public/_nuxt/Ctsk8Shx.js"
  },
  "/_nuxt/CyRfm4hh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-Ub6CAziy9jqFn3JmT70UPsxZO3Q\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 477,
    "path": "../public/_nuxt/CyRfm4hh.js"
  },
  "/_nuxt/CuZk8yDk.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12d6e-sQlGpgPjU5H3UW5WCs4p8MUOs2I\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 77166,
    "path": "../public/_nuxt/CuZk8yDk.js"
  },
  "/_nuxt/CynmbZws.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4b0-LYPOqt+S7gY32lAruNR8Z9IA3s0\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 1200,
    "path": "../public/_nuxt/CynmbZws.js"
  },
  "/_nuxt/CyyuwgbX.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"722-SC/UlHNa3BkKSCPQ0eaSNGZxOsc\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 1826,
    "path": "../public/_nuxt/CyyuwgbX.js"
  },
  "/_nuxt/Cz3i3bph.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"11ef8-TKQ56+rKsfOVfXP9cSfnpR1Q/Co\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 73464,
    "path": "../public/_nuxt/Cz3i3bph.js"
  },
  "/_nuxt/CzxsjifX.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"26ef-ofFUSYRn4D6VXyFpa2xQM6LRam8\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 9967,
    "path": "../public/_nuxt/CzxsjifX.js"
  },
  "/_nuxt/D2vuKzU8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2656-npWF4AlAbi5bBOxa52hyM7QP63E\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 9814,
    "path": "../public/_nuxt/D2vuKzU8.js"
  },
  "/_nuxt/D5dPWUcd.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"557-MM/Jnw1WPEZAKIjTegIiiiRZqQk\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 1367,
    "path": "../public/_nuxt/D5dPWUcd.js"
  },
  "/_nuxt/D635--U8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4f30-Ci3rhiEKR12ZJky2VequGla7pLM\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 20272,
    "path": "../public/_nuxt/D635--U8.js"
  },
  "/_nuxt/D2r1MBJ5.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"7f41d-1T6Oi4Ptnuo3nUooI4+dnnOnP5c\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 521245,
    "path": "../public/_nuxt/D2r1MBJ5.js"
  },
  "/_nuxt/D7wEFlwY.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1038-STv6PF0NO1vAyPRy5/rdk64E13s\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 4152,
    "path": "../public/_nuxt/D7wEFlwY.js"
  },
  "/_nuxt/D8j1tCOi.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"7d0-MlQYBdfJiwkXpgUUhqG2jP2wk40\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 2000,
    "path": "../public/_nuxt/D8j1tCOi.js"
  },
  "/_nuxt/D9_qOLj4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3e6f-QBjA3ebJmLNGQvcdKuXFvhdkl54\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 15983,
    "path": "../public/_nuxt/D9_qOLj4.js"
  },
  "/_nuxt/D9hCU7kp.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"647-dY0lrFUyBmhJAeRw0O1T+WByOyo\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 1607,
    "path": "../public/_nuxt/D9hCU7kp.js"
  },
  "/_nuxt/DBQUI1dI.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3f81-VrbRveXVjvtCdFwj46J7QnpiGgE\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 16257,
    "path": "../public/_nuxt/DBQUI1dI.js"
  },
  "/_nuxt/DBTpbaTF.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-K2STIcAVpKV9Dqd/EZYsO2kE+Xo\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 477,
    "path": "../public/_nuxt/DBTpbaTF.js"
  },
  "/_nuxt/DBWMs2dU.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12cbc-Ll0pkdODBGT527As3l2mcT99g8E\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 76988,
    "path": "../public/_nuxt/DBWMs2dU.js"
  },
  "/_nuxt/DDVxXosY.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"58-9u9N0KUsXZ0G2eW6IDkDTWfe9SY\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 88,
    "path": "../public/_nuxt/DDVxXosY.js"
  },
  "/_nuxt/DDJY-XWz.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"125ef-FpTRMidJyJXebhvqXjm0SX3/+CY\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 75247,
    "path": "../public/_nuxt/DDJY-XWz.js"
  },
  "/_nuxt/DDqjnG60.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5dc-jvWsAR3np/nmtu1+DvntTwGECFI\"",
    "mtime": "2026-05-11T21:58:34.975Z",
    "size": 1500,
    "path": "../public/_nuxt/DDqjnG60.js"
  },
  "/_nuxt/DEGpWy5r.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"57-nWQdHCJHkWStMXfyIkn+UvjvdQc\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 87,
    "path": "../public/_nuxt/DEGpWy5r.js"
  },
  "/_nuxt/DEMlSSgw.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-s+lTaRFEkksXhWU64dER2FAO82c\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 477,
    "path": "../public/_nuxt/DEMlSSgw.js"
  },
  "/_nuxt/DEeKAbwB.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"132e-enPX+Z2LHnv45oRRVTNreYTw1UQ\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 4910,
    "path": "../public/_nuxt/DEeKAbwB.js"
  },
  "/_nuxt/DGJw4GoH.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"89e8-be6BylBnPxcdZIxLiFYIks9lK+Y\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 35304,
    "path": "../public/_nuxt/DGJw4GoH.js"
  },
  "/_nuxt/DH-dNTB5.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c99-rgP/cfzf9qYlJ+PNDaZSZcRnSTc\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 3225,
    "path": "../public/_nuxt/DH-dNTB5.js"
  },
  "/_nuxt/DHnYJX7F.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4c8-u5M39Ol9W/zkGDHH++rNi5C50vg\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 1224,
    "path": "../public/_nuxt/DHnYJX7F.js"
  },
  "/_nuxt/DI3-Oj8z.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1af3-m6GFNpnoEYIjHbr0XLkXYCCVCaY\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 6899,
    "path": "../public/_nuxt/DI3-Oj8z.js"
  },
  "/_nuxt/DIbYRSxN.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4c7-IuU5SnBQEf8swZ6IdZYlXDEiHw8\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 1223,
    "path": "../public/_nuxt/DIbYRSxN.js"
  },
  "/_nuxt/DIh9uT6j.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-x6F7UqxOc95+2BXmE+pjAdj+aBw\"",
    "mtime": "2026-05-11T21:58:34.976Z",
    "size": 477,
    "path": "../public/_nuxt/DIh9uT6j.js"
  },
  "/_nuxt/DJWbbZ_2.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"17c9-L3OwMGl0aT2L++tVaebp3KIqp1s\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 6089,
    "path": "../public/_nuxt/DJWbbZ_2.js"
  },
  "/_nuxt/DKSMJmJS.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"40-8WOgpEHiInlj5PI2WGOhzusIjAk\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 64,
    "path": "../public/_nuxt/DKSMJmJS.js"
  },
  "/_nuxt/DKh9knZX.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"e9e-1DGoBWV0KF5rBYmoA1WYWLLJYFY\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 3742,
    "path": "../public/_nuxt/DKh9knZX.js"
  },
  "/_nuxt/DL-TWf04.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"141-zMEl337o6GKDxQAFFotpPGZ/VgY\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 321,
    "path": "../public/_nuxt/DL-TWf04.js"
  },
  "/_nuxt/DMVNcP4A.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1a1-RR8Zh3ggcl4zYd9o65YJQstOkGM\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 417,
    "path": "../public/_nuxt/DMVNcP4A.js"
  },
  "/_nuxt/DOTl894U.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13b02-8ceu5WF/rSAW1rkauhdfMNuQE88\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 80642,
    "path": "../public/_nuxt/DOTl894U.js"
  },
  "/_nuxt/DOTmEOgr.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"427-8GC8gW9RAkv5Npbg5pYbfuPIG34\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 1063,
    "path": "../public/_nuxt/DOTmEOgr.js"
  },
  "/_nuxt/DQPxgJa8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2131-6/8sLfrw7OFvNaCM1j1F4QMCnvU\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 8497,
    "path": "../public/_nuxt/DQPxgJa8.js"
  },
  "/_nuxt/DQfXZm77.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"309-uGvRdPtDbbCb9s9xsrS49cr04d8\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 777,
    "path": "../public/_nuxt/DQfXZm77.js"
  },
  "/_nuxt/DTUwEAcl.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"87e1-PxNMwZEPjy46oUjNEVJ2r3VMmsc\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 34785,
    "path": "../public/_nuxt/DTUwEAcl.js"
  },
  "/_nuxt/DU4cSdSh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"a76-uR//ZX62bUgPu7gon9WAUFwmlNs\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 2678,
    "path": "../public/_nuxt/DU4cSdSh.js"
  },
  "/_nuxt/DXNb7qVb.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1ca9-AEV8A11+NServEutPviDcrxNPoM\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 7337,
    "path": "../public/_nuxt/DXNb7qVb.js"
  },
  "/_nuxt/DZ6s2oMu.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-3aekUMxM9tHZKmegtxv6ErnZhWQ\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 477,
    "path": "../public/_nuxt/DZ6s2oMu.js"
  },
  "/_nuxt/DZPI8hr3.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"946-dRw3TxT/FKalGZXXGJfvhRdIjow\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 2374,
    "path": "../public/_nuxt/DZPI8hr3.js"
  },
  "/_nuxt/D_b4WV_E.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5263-y1mo1pdeNo16hULLIDkH47qBVIw\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 21091,
    "path": "../public/_nuxt/D_b4WV_E.js"
  },
  "/_nuxt/Da716wWv.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"991-PcOAyNKWTsrZQ38X439yg+D7ZZQ\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 2449,
    "path": "../public/_nuxt/Da716wWv.js"
  },
  "/_nuxt/DaRGwpLh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"89-rmZQj9mq9Blhd08/gEHM3RbSrjU\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 137,
    "path": "../public/_nuxt/DaRGwpLh.js"
  },
  "/_nuxt/DcjHt_hs.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5bb-HrlU7/glYY0TugTcgSqLhX+2Pw8\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 1467,
    "path": "../public/_nuxt/DcjHt_hs.js"
  },
  "/_nuxt/DjwFAMsh.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10dd-dVWUjn9M8vbvFTE8sTUcr2yU7ME\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 4317,
    "path": "../public/_nuxt/DjwFAMsh.js"
  },
  "/_nuxt/DdIfkOm0.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"18731-j6NRBjYvukd+s5pQ1HCFEo8NeMo\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 100145,
    "path": "../public/_nuxt/DdIfkOm0.js"
  },
  "/_nuxt/DlIec4SD.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"104-iANJ6Nj2pBognWeJJAX2Uehe0uY\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 260,
    "path": "../public/_nuxt/DlIec4SD.js"
  },
  "/_nuxt/DluCfres.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"e07-VvaM4tZzNjErivYYXOi7WMnTzLE\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 3591,
    "path": "../public/_nuxt/DluCfres.js"
  },
  "/_nuxt/Dn4zBUtB.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"47e-r9C1RSgixI+73ka5kKBQb755KaA\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 1150,
    "path": "../public/_nuxt/Dn4zBUtB.js"
  },
  "/_nuxt/DlmZ1OB8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1e06d-Os3BA1Q9UoOv4FSZokc9ZdyI/rM\"",
    "mtime": "2026-05-11T21:58:34.977Z",
    "size": 122989,
    "path": "../public/_nuxt/DlmZ1OB8.js"
  },
  "/_nuxt/DpmnXTtn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-WAmx9qAUMmC8ElaprqrVrr3OYCU\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 477,
    "path": "../public/_nuxt/DpmnXTtn.js"
  },
  "/_nuxt/Dqq7TmoL.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3a1-9hYHdTb7LqTf2tmktLiw0mWjL44\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 929,
    "path": "../public/_nuxt/Dqq7TmoL.js"
  },
  "/_nuxt/DshFsbm9.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1489-QNW5BwbfgEELA0mFj89EutbAnmg\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 5257,
    "path": "../public/_nuxt/DshFsbm9.js"
  },
  "/_nuxt/DstIz5m8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"c79-oqUFRLogz6fLETvSyBE+ZHeK5TE\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 3193,
    "path": "../public/_nuxt/DstIz5m8.js"
  },
  "/_nuxt/Dt53Leg8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-C1Xr5sgQNWeWWk6b7RzQ7ofpIPI\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 477,
    "path": "../public/_nuxt/Dt53Leg8.js"
  },
  "/_nuxt/DtZxOyvS.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-P1vwYoJbBvDS6nRZXZRGvZ8BEl0\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 477,
    "path": "../public/_nuxt/DtZxOyvS.js"
  },
  "/_nuxt/DuEV7R7F.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10db1-l5p7NRITZWDELuOftWLrmnYoQnM\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 69041,
    "path": "../public/_nuxt/DuEV7R7F.js"
  },
  "/_nuxt/DvRDbPc5.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-IZLR4g6JwZqakD5Udvznx6mHPH0\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 477,
    "path": "../public/_nuxt/DvRDbPc5.js"
  },
  "/_nuxt/Dxi9HCI7.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13bf4-cv5S89KiEVakLHHmL5aw41Z+mHc\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 80884,
    "path": "../public/_nuxt/Dxi9HCI7.js"
  },
  "/_nuxt/Dz5JoTv2.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3fcd-kz79qWmoptTuMbEc0pfXBWf26mI\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 16333,
    "path": "../public/_nuxt/Dz5JoTv2.js"
  },
  "/_nuxt/Eq_X2-eJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"5c1f-je9HHin2ezYUNiHw/BuTrvlR0og\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 23583,
    "path": "../public/_nuxt/Eq_X2-eJ.js"
  },
  "/_nuxt/F2QUr-Qt.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"bf6-uilNjLCEnx1+AMhVZ/clH89cqYQ\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 3062,
    "path": "../public/_nuxt/F2QUr-Qt.js"
  },
  "/_nuxt/FBDyFo3O.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-jU0v74fFMNUoRt6FH5pn0lG3eq4\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 477,
    "path": "../public/_nuxt/FBDyFo3O.js"
  },
  "/_nuxt/G29ieYoI.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-kWVKm9Io50oBbcf8XM4a3S0N9iQ\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 477,
    "path": "../public/_nuxt/G29ieYoI.js"
  },
  "/_nuxt/Ge9gJQuc.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1157d-DpYFNkKlQNf7iihjI2mHwzMBcE4\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 71037,
    "path": "../public/_nuxt/Ge9gJQuc.js"
  },
  "/_nuxt/GiHtOPY-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"16dd-0W/fNaKXpibqTSz6pUtuVbB4qUY\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 5853,
    "path": "../public/_nuxt/GiHtOPY-.js"
  },
  "/_nuxt/GyFWzZVl.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2e8c-Vw9BKRAITJmaaSZHWFNgoWmBv24\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 11916,
    "path": "../public/_nuxt/GyFWzZVl.js"
  },
  "/_nuxt/HYrXj7RL.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"940-L9A1rzpOZIbo/SVcQyhq1+Y6Z9c\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 2368,
    "path": "../public/_nuxt/HYrXj7RL.js"
  },
  "/_nuxt/HouseholdPreferencesEditor.D12lkyFm.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"54-bW2VTv9iotthvEGYXqBN8hf6uPU\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 84,
    "path": "../public/_nuxt/HouseholdPreferencesEditor.D12lkyFm.css"
  },
  "/_nuxt/ImageCropper.Dw47-jjr.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"10b7-5ze7F4JM7TtCZ9PTzrCB64ahZ8I\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 4279,
    "path": "../public/_nuxt/ImageCropper.Dw47-jjr.css"
  },
  "/_nuxt/LE-36SuN.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1707-sRwUPEhKW1xKBQWW1+qcPJGTeBg\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 5895,
    "path": "../public/_nuxt/LE-36SuN.js"
  },
  "/_nuxt/LJDJy7wU.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1e9-9i+uVnKWOm3/ICUDQLCz2Zi1J/Y\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 489,
    "path": "../public/_nuxt/LJDJy7wU.js"
  },
  "/_nuxt/M-8gzU_0.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1628-q3loft/KvJPwTUdq45Aj4hFosRA\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 5672,
    "path": "../public/_nuxt/M-8gzU_0.js"
  },
  "/_nuxt/MW9Yjrnn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"2ad7-WR0PqkAybOxFi7RTpFkW2DjVPzU\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 10967,
    "path": "../public/_nuxt/MW9Yjrnn.js"
  },
  "/_nuxt/M2UwJY8V.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"12f1a-iMgZMn2sxfKDOHRoUyeO17a82Ws\"",
    "mtime": "2026-05-11T21:58:34.978Z",
    "size": 77594,
    "path": "../public/_nuxt/M2UwJY8V.js"
  },
  "/_nuxt/Nu9ttRpH.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"15074-0GESWGJTSoGm+LnMtXyMmM9HLI4\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 86132,
    "path": "../public/_nuxt/Nu9ttRpH.js"
  },
  "/_nuxt/OQjRFX1W.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"130aa-2Bd75OP1Xsisnhjv+OV7UlWLvbk\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 77994,
    "path": "../public/_nuxt/OQjRFX1W.js"
  },
  "/_nuxt/PcgH_zeJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"116b-kQhSm7JktZtlgbGJK/zhd/9+Qgo\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 4459,
    "path": "../public/_nuxt/PcgH_zeJ.js"
  },
  "/_nuxt/QueryFilterBuilder.CXozbGhX.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"18f-FEYZq3Hw95Oylb7s0yuzZ/pLqgE\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 399,
    "path": "../public/_nuxt/QueryFilterBuilder.CXozbGhX.css"
  },
  "/_nuxt/Q3XCFuJi.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"122c0-oqyQFqMwP2ydcNIuHi1i3fNx2V4\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 74432,
    "path": "../public/_nuxt/Q3XCFuJi.js"
  },
  "/_nuxt/RWfgHrzH.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"b20-nbCz+LI6+Z1ZiszSLwfiayYDplc\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 2848,
    "path": "../public/_nuxt/RWfgHrzH.js"
  },
  "/_nuxt/RecipeCardImage.D7wEjns0.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"fc-v/UYgLBx/QA/CPI7cAdD3xTJtUw\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 252,
    "path": "../public/_nuxt/RecipeCardImage.D7wEjns0.css"
  },
  "/_nuxt/RecipeCardMobile.D1kN81p4.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"417-iHvycR5IhlGaFu0ODGaFq1qJ4h4\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 1047,
    "path": "../public/_nuxt/RecipeCardMobile.D1kN81p4.css"
  },
  "/_nuxt/RecipeCardSection.CWGu24M7.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"179-s07oKrldb0u3p2Vlz0BaW1h4ZwI\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 377,
    "path": "../public/_nuxt/RecipeCardSection.CWGu24M7.css"
  },
  "/_nuxt/RecipeDialogAddToShoppingList.By7mhLB_.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"73-xUE39cERzqvwh3D8GKWjwk760qk\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 115,
    "path": "../public/_nuxt/RecipeDialogAddToShoppingList.By7mhLB_.css"
  },
  "/_nuxt/RecipeIngredientListItem.D-0BxQ9s.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"25f-p2CU5Rp5+Jt3984bL73ro/G8Vxs\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 607,
    "path": "../public/_nuxt/RecipeIngredientListItem.D-0BxQ9s.css"
  },
  "/_nuxt/RecipeOrganizerSelector.Dw7EfpF0.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"31-BwmOdrIIWlJcX1AcFIXQAoyuaPE\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 49,
    "path": "../public/_nuxt/RecipeOrganizerSelector.Dw7EfpF0.css"
  },
  "/_nuxt/RecipePage.BK5k4bLG.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"24a2-5XmmFuf4+jz9Alj6DIharS7oprA\"",
    "mtime": "2026-05-11T21:58:34.979Z",
    "size": 9378,
    "path": "../public/_nuxt/RecipePage.BK5k4bLG.css"
  },
  "/_nuxt/RecipePrintView.B-W6gVrY.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"5d5-ZI8AqGtdS4HjtmW/K/hrYP9G85o\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 1493,
    "path": "../public/_nuxt/RecipePrintView.B-W6gVrY.css"
  },
  "/_nuxt/RecipeTimeline.BsD_diYh.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"3e90-Mz2gQNLgOOI1b7nVzcYOPPIK2gs\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 16016,
    "path": "../public/_nuxt/RecipeTimeline.BsD_diYh.css"
  },
  "/_nuxt/Rp1WfPWj.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-O4U2zngAUj6JeW5OQTaxy4dalp0\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 477,
    "path": "../public/_nuxt/Rp1WfPWj.js"
  },
  "/_nuxt/SafeMarkdown.v6CBXYCK.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"102-ifg6awZPZIl54oHmYSrwaMQbsLs\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 258,
    "path": "../public/_nuxt/SafeMarkdown.v6CBXYCK.css"
  },
  "/_nuxt/SearchFilter.C9f4tbih.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"103-YMZIW500LuIUEM9Tl79G0ZN4/ss\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 259,
    "path": "../public/_nuxt/SearchFilter.C9f4tbih.css"
  },
  "/_nuxt/UGMSI9jZ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"27c2-kqkgY5JruQ/SuW+dza2fstsD15A\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 10178,
    "path": "../public/_nuxt/UGMSI9jZ.js"
  },
  "/_nuxt/U7CY8tNW.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"16c-AjZWyZbCM9abvXmK0X1D4eamNdY\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 364,
    "path": "../public/_nuxt/U7CY8tNW.js"
  },
  "/_nuxt/UmMVKukP.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"ce8-3UHF2Cg1jcGBYiRjiMfL3/ODLAE\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 3304,
    "path": "../public/_nuxt/UmMVKukP.js"
  },
  "/_nuxt/UserRegistrationForm.CajeLClf.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"1b1-9XFkvoBafSdfZ3JyVx7L3l9iNkc\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 433,
    "path": "../public/_nuxt/UserRegistrationForm.CajeLClf.css"
  },
  "/_nuxt/UtK6fZUo.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3609-v9IVD02+ba7aiNz2sOCPp9c4KS0\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 13833,
    "path": "../public/_nuxt/UtK6fZUo.js"
  },
  "/_nuxt/VAlert.HJOs4aB9.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"1313-fDSfzYzRmGEusy5WNs3C3V2n0Ls\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 4883,
    "path": "../public/_nuxt/VAlert.HJOs4aB9.css"
  },
  "/_nuxt/VApp.BfkcucfP.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"178-EygproE+yHSu5yTKmxfi6JzID5M\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 376,
    "path": "../public/_nuxt/VApp.BfkcucfP.css"
  },
  "/_nuxt/VAutocomplete.C7PWn7hl.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"a68-4dGupb1UkBLksZJYrIupmZtAb+o\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2664,
    "path": "../public/_nuxt/VAutocomplete.C7PWn7hl.css"
  },
  "/_nuxt/VAvatar.G5Z_onZ8.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"14c9-1GwWPS+i2bNSI6DYHsWkvAQrX8k\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 5321,
    "path": "../public/_nuxt/VAvatar.G5Z_onZ8.css"
  },
  "/_nuxt/VBtn.aabZj4qq.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"56a1-bjzLKxzWVPLndENA4bvOufztBGs\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 22177,
    "path": "../public/_nuxt/VBtn.aabZj4qq.css"
  },
  "/_nuxt/VCard.CHO6100k.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"1b43-W6t9OZ1+Cl1+Sl+QDBZEwlC8ftg\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 6979,
    "path": "../public/_nuxt/VCard.CHO6100k.css"
  },
  "/_nuxt/VCheckbox.DXrFno_w.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"88-qONARyG6anahglh1AbzDrtk+hJ8\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 136,
    "path": "../public/_nuxt/VCheckbox.DXrFno_w.css"
  },
  "/_nuxt/VChip.Cb5-ZmBP.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"2c7f-QX9jgijcgNc3dvI2/fBFJ0L1Snc\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 11391,
    "path": "../public/_nuxt/VChip.Cb5-ZmBP.css"
  },
  "/_nuxt/VContainer.BexUYEwW.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"165-7mZQkc5bG2AuyT/4d0HECPxX5lA\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 357,
    "path": "../public/_nuxt/VContainer.BexUYEwW.css"
  },
  "/_nuxt/VDataTable.M4O_GTtx.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"330d-7NgXrMUiU2nAX1Sdl4FpwNjNUH8\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 13069,
    "path": "../public/_nuxt/VDataTable.M4O_GTtx.css"
  },
  "/_nuxt/VDatePicker.DwFY_mxd.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"16f1-tjLLTbsUEc5Js4IqA5+3TI8OkvM\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 5873,
    "path": "../public/_nuxt/VDatePicker.DwFY_mxd.css"
  },
  "/_nuxt/VDialog.x_jsX66Z.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"a1e-UGRKJICWMFW55J6OSnRo4+tP6u0\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2590,
    "path": "../public/_nuxt/VDialog.x_jsX66Z.css"
  },
  "/_nuxt/VDivider.DVWKP2HQ.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"622-GhZRfP/d4Pi2q214n9/+2dXDZNI\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 1570,
    "path": "../public/_nuxt/VDivider.DVWKP2HQ.css"
  },
  "/_nuxt/VExpansionPanels.BChy_WkI.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"1a0d-/FbOs+ORwARoXuWiCtqyt+I2v+A\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 6669,
    "path": "../public/_nuxt/VExpansionPanels.BChy_WkI.css"
  },
  "/_nuxt/VField.Hm2CaF0n.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"4770-2m7iY6GzeTj4RSM7iSKHJHJzlnM\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 18288,
    "path": "../public/_nuxt/VField.Hm2CaF0n.css"
  },
  "/_nuxt/VInput.B-YWOfax.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"11b7-xdtaJ5yZj7Yqe8zDFc1HAeVZtuU\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 4535,
    "path": "../public/_nuxt/VInput.B-YWOfax.css"
  },
  "/_nuxt/VList.DIHH2oHL.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"8ef-Xl3hbmN+Q2hZkaL3SPH7xlAdUmU\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2287,
    "path": "../public/_nuxt/VList.DIHH2oHL.css"
  },
  "/_nuxt/VListItem.DBoeI59q.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"36ee-2XHkvsr1nZQcBJKHdP/ghSsyzyo\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 14062,
    "path": "../public/_nuxt/VListItem.DBoeI59q.css"
  },
  "/_nuxt/VMain.B8fBNEjs.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"225-w4+28yIjPOWCgQ0uqHL/BzT9kPg\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 549,
    "path": "../public/_nuxt/VMain.B8fBNEjs.css"
  },
  "/_nuxt/VMenu.DKMSbytd.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"228-PluU8ColHiUenc0yyR1ZVFfMTYc\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 552,
    "path": "../public/_nuxt/VMenu.DKMSbytd.css"
  },
  "/_nuxt/VNumberInput.M1RS63hx.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"b18-E7TiKtZaLwAqta6CsBvE/pwPfsc\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2840,
    "path": "../public/_nuxt/VNumberInput.M1RS63hx.css"
  },
  "/_nuxt/VOverlay.C8A6QkUH.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"409-N+8ZrJhG7AsdTSvGnH9pzVKOLZQ\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 1033,
    "path": "../public/_nuxt/VOverlay.C8A6QkUH.css"
  },
  "/_nuxt/VPicker.Dg3cVXqY.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"575-VkQVPXtCoywH0LpbUjrmhzJ8Nmo\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 1397,
    "path": "../public/_nuxt/VPicker.Dg3cVXqY.css"
  },
  "/_nuxt/VRow.B9A3ZRbC.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"547e-TqRjJYE8kLR94W7eaZ3pftKJYxU\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 21630,
    "path": "../public/_nuxt/VRow.B9A3ZRbC.css"
  },
  "/_nuxt/VSelect.BeEjmbrN.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"732-sNXY6rkDApGbdN7EeeYwCVPZxBE\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 1842,
    "path": "../public/_nuxt/VSelect.BeEjmbrN.css"
  },
  "/_nuxt/VSelectionControl.D5434LIa.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"8f3-r+rKQMX9+r8ro83CENFF26UhtNM\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2291,
    "path": "../public/_nuxt/VSelectionControl.D5434LIa.css"
  },
  "/_nuxt/VSheet.CJWPiaVh.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"315-P+tTis5BW5VteUvc+XGKUjRr5tk\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 789,
    "path": "../public/_nuxt/VSheet.CJWPiaVh.css"
  },
  "/_nuxt/VSlideGroup.DS_rTcTa.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"3df-HYf0EByar9hLGsDvMAyW5E7xKQg\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 991,
    "path": "../public/_nuxt/VSlideGroup.DS_rTcTa.css"
  },
  "/_nuxt/VSnackbar.C1EMa-7w.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"142c-vimV9AySDWzS4SXpYjoMf+vDdhI\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 5164,
    "path": "../public/_nuxt/VSnackbar.C1EMa-7w.css"
  },
  "/_nuxt/VSpacer.DfbUir7X.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"32-iNjQ0F7fOu7Ze+/aqaDl/JIe+Ac\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 50,
    "path": "../public/_nuxt/VSpacer.DfbUir7X.css"
  },
  "/_nuxt/VSwitch.0g6nLhzi.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"13b8-M/7FwiNduW+w7E2TfqnTSBnRoFA\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 5048,
    "path": "../public/_nuxt/VSwitch.0g6nLhzi.css"
  },
  "/_nuxt/VTabs.CBaT8_Ea.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"e0e-et9C5uFLeSQVW+gLGA2aDeqTSfQ\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 3598,
    "path": "../public/_nuxt/VTabs.CBaT8_Ea.css"
  },
  "/_nuxt/VTextField.Dk2JkJDg.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"82d-GBVuIPMYq1HmGQFhGLpmJvjA7Xg\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2093,
    "path": "../public/_nuxt/VTextField.Dk2JkJDg.css"
  },
  "/_nuxt/VTextarea.ZwfEfzKp.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"6fd-4euio21srsyHoSKrMKEgbC83Z+s\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 1789,
    "path": "../public/_nuxt/VTextarea.ZwfEfzKp.css"
  },
  "/_nuxt/VToolbar.Q_gI-Rxl.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"b58-4jn0OrdRTdXpH+e9m1O3dK+htaQ\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 2904,
    "path": "../public/_nuxt/VToolbar.Q_gI-Rxl.css"
  },
  "/_nuxt/VTooltip.D_imBfhL.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"2c1-ufXWOZDH9FqoxNMj9Z/WBs8OhoM\"",
    "mtime": "2026-05-11T21:58:34.980Z",
    "size": 705,
    "path": "../public/_nuxt/VTooltip.D_imBfhL.css"
  },
  "/_nuxt/VVirtualScroll.OP95r7Sh.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"a4-SNZd7ZkJXqLAOGC1zqlrYhamkoU\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 164,
    "path": "../public/_nuxt/VVirtualScroll.OP95r7Sh.css"
  },
  "/_nuxt/VWindowItem.DH4EhKNt.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"db3-OlDPhe+KW5+Vrm5TyDzAGDHBZmo\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 3507,
    "path": "../public/_nuxt/VWindowItem.DH4EhKNt.css"
  },
  "/_nuxt/X4hHIrqQ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"327-dpUlrZwDnumTWiUROBsTMrmXR0o\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 807,
    "path": "../public/_nuxt/X4hHIrqQ.js"
  },
  "/_nuxt/YBeKfHM9.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"132e-nyJS7fV7rReJj0PZAhLCTVEGMKk\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 4910,
    "path": "../public/_nuxt/YBeKfHM9.js"
  },
  "/_nuxt/ZDS8jD7w.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"a4b-OH0+QUCyWx6zdzk0V9z/i+sK5WU\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 2635,
    "path": "../public/_nuxt/ZDS8jD7w.js"
  },
  "/_nuxt/Zb18iu6m.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"598b-TriwIkhCaehRHXPq/YdIivmhREw\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 22923,
    "path": "../public/_nuxt/Zb18iu6m.js"
  },
  "/_nuxt/_id_.BxiDPU01.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"f0-7IHS78Irplm3ezDKCIE8gP6u1Cg\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 240,
    "path": "../public/_nuxt/_id_.BxiDPU01.css"
  },
  "/_nuxt/_nKO7aNQ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"10d5-bI9lc1CfTpUA45GbH0rjwdW35GU\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 4309,
    "path": "../public/_nuxt/_nKO7aNQ.js"
  },
  "/_nuxt/ajE6Ow3Y.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"251f-9rBq30DOYKjNNkR3fps9xHT+OpY\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 9503,
    "path": "../public/_nuxt/ajE6Ow3Y.js"
  },
  "/_nuxt/bP8JGzzi.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1f23-LvSnSpgNQPsSaFGDxYiWUQ0KsxQ\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 7971,
    "path": "../public/_nuxt/bP8JGzzi.js"
  },
  "/_nuxt/backups.eqfSDEIq.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"38-VK+U0Jy+9ISPQZzR3/i/noqBZPM\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 56,
    "path": "../public/_nuxt/backups.eqfSDEIq.css"
  },
  "/_nuxt/blank.C7FXkJd5.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"f25-Ag7ikqFc3iBLrvTg7NFenbgLFAM\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 3877,
    "path": "../public/_nuxt/blank.C7FXkJd5.css"
  },
  "/_nuxt/c9Y-nnYi.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"eb4-roI1O3IJfD9Zk6LL/5fDghqvOQ4\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 3764,
    "path": "../public/_nuxt/c9Y-nnYi.js"
  },
  "/_nuxt/cv-iLVA3.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1603-VDnsfN7aWtxj6EXH40+DBYp0gAg\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 5635,
    "path": "../public/_nuxt/cv-iLVA3.js"
  },
  "/_nuxt/d7RwADoK.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"11bf-rLiaHx/lmS8pZZpYbmZHx7tpZKk\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 4543,
    "path": "../public/_nuxt/d7RwADoK.js"
  },
  "/_nuxt/dKjxHFJN.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1c0f-ZpuPbUDkNoMjYujXD1nCIwIEcXA\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 7183,
    "path": "../public/_nuxt/dKjxHFJN.js"
  },
  "/_nuxt/dDH_z5Qc.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"42ba-B89Wrr9LtnQ7FLeEN2GiZgxkEwY\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 17082,
    "path": "../public/_nuxt/dDH_z5Qc.js"
  },
  "/_nuxt/dR32aK-_.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"93a-3Rri/639ODI+bvGt1SU2qLNFXok\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 2362,
    "path": "../public/_nuxt/dR32aK-_.js"
  },
  "/_nuxt/dYcMBbP7.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1286f-KP/1aNi0Z1W7pzMNj7lhXhkMIfg\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 75887,
    "path": "../public/_nuxt/dYcMBbP7.js"
  },
  "/_nuxt/eFgvidhC.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"134a-ADwJcMBPgCJeICxzkQE6EdAjH9k\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 4938,
    "path": "../public/_nuxt/eFgvidhC.js"
  },
  "/_nuxt/edit.xlzyKxIM.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"9cc-Z/bSh+eVO6Th+v7yoStwxZcrKTU\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 2508,
    "path": "../public/_nuxt/edit.xlzyKxIM.css"
  },
  "/_nuxt/eh4ChG3z.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-EHDRuwqBD+zMAW+kAA1TDi1Xfjs\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 477,
    "path": "../public/_nuxt/eh4ChG3z.js"
  },
  "/_nuxt/entry.M2tfMt5x.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"2e8fd-VpX05kp7rFn3C9T7if+JJAgfgIE\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 190717,
    "path": "../public/_nuxt/entry.M2tfMt5x.css"
  },
  "/_nuxt/error-404.C-Ezrlz-.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"97e-YLcQ2HBNLea0KJoUeqSqSCendIU\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 2430,
    "path": "../public/_nuxt/error-404.C-Ezrlz-.css"
  },
  "/_nuxt/error-500.DBWf9FGj.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"773-9MNIE+ztUss3x7HN62QKMFz0rhs\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 1907,
    "path": "../public/_nuxt/error-500.DBWf9FGj.css"
  },
  "/_nuxt/error.BJhBfqqF.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"7c-ExiOpuRKc4W1Nyke2PvxYvdN0c8\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 124,
    "path": "../public/_nuxt/error.BJhBfqqF.css"
  },
  "/_nuxt/f2KRPTBR.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-xQx5p7IyXmoBlpH8s+7bU19rVBM\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 477,
    "path": "../public/_nuxt/f2KRPTBR.js"
  },
  "/_nuxt/fE9ytK04.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"22c-RuiLaBrHPY7E/p6+xhx91c4HMsc\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 556,
    "path": "../public/_nuxt/fE9ytK04.js"
  },
  "/_nuxt/fIv1eRha.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"483-Kt8ff7/w6IOb/qGXC7mulV3NjfI\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 1155,
    "path": "../public/_nuxt/fIv1eRha.js"
  },
  "/_nuxt/forgot-password.DdcLZU4q.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"19-+38hV5m6nYCQQ9SY5n3U/VAzjus\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 25,
    "path": "../public/_nuxt/forgot-password.DdcLZU4q.css"
  },
  "/_nuxt/hFWusrC-.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"133e7-Y5f8wZs0FrFkQgOf2N0AUOmsq6Q\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 78823,
    "path": "../public/_nuxt/hFWusrC-.js"
  },
  "/_nuxt/i20eZjcT.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1210e-z8BgGebmzg4uoBNyOPAc17GQKRs\"",
    "mtime": "2026-05-11T21:58:34.981Z",
    "size": 73998,
    "path": "../public/_nuxt/i20eZjcT.js"
  },
  "/_nuxt/index.CmSuTdd6.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"112-UdMc88zrqRgQxyWhBnyu8VXoaJM\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 274,
    "path": "../public/_nuxt/index.CmSuTdd6.css"
  },
  "/_nuxt/index.sp21DjuW.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"45-R0EBYvcqMjEEA7twydiQb+euUMM\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 69,
    "path": "../public/_nuxt/index.sp21DjuW.css"
  },
  "/_nuxt/iXnrlxiu.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"175db-Z01B3EmQ9DNZGnqhPUT7qZTQejM\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 95707,
    "path": "../public/_nuxt/iXnrlxiu.js"
  },
  "/_nuxt/index.ubD-TmW1.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"177-c988YxFLo1mdYz1IZdUgodppMCw\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 375,
    "path": "../public/_nuxt/index.ubD-TmW1.css"
  },
  "/_nuxt/kDeaHuOt.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-A71iZLuFY1qEVG4CnixgwZ8ZaAY\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 477,
    "path": "../public/_nuxt/kDeaHuOt.js"
  },
  "/_nuxt/kUGSj9tD.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-4VdvcDbw9Dc//Y+lcnqL/RxaADA\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 477,
    "path": "../public/_nuxt/kUGSj9tD.js"
  },
  "/_nuxt/kkUZZ_LO.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"db6-l11GwwkfD/rg7g4a6UFM7tc79uk\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 3510,
    "path": "../public/_nuxt/kkUZZ_LO.js"
  },
  "/_nuxt/kpaFYBz4.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"86f-yXnrxcQg8KyIsLZJMCzaS7HjuiQ\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 2159,
    "path": "../public/_nuxt/kpaFYBz4.js"
  },
  "/_nuxt/lRLpJxJj.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"4c-gMJNp/wyKS2cKKk5kQMjy59cmdQ\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 76,
    "path": "../public/_nuxt/lRLpJxJj.js"
  },
  "/_nuxt/login.DSEb5a3X.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"197-Vowt/88CLSf8JfO82AK1Co5BRlk\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 407,
    "path": "../public/_nuxt/login.DSEb5a3X.css"
  },
  "/_nuxt/m39OZokJ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"6f7-sdaqukCKU4+x8NF+TV/EWQFZz6E\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 1783,
    "path": "../public/_nuxt/m39OZokJ.js"
  },
  "/_nuxt/mCeS63uV.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"7d0-6tkJWkvlI52/48JRPTZphU4X7tY\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 2000,
    "path": "../public/_nuxt/mCeS63uV.js"
  },
  "/_nuxt/migrations.BurFWd8A.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"a5a-KA2mlLxOX8EUlVQeW78OeKJhlyw\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 2650,
    "path": "../public/_nuxt/migrations.BurFWd8A.css"
  },
  "/_nuxt/notifiers.rhj3JQX1.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"41-OTJLzO878PDqnsh1nA+f2tPS/rM\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 65,
    "path": "../public/_nuxt/notifiers.rhj3JQX1.css"
  },
  "/_nuxt/planner.BpH1bqju.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"97-okRO8LCE9WpHLH2Tzy9VjDbm7X0\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 151,
    "path": "../public/_nuxt/planner.BpH1bqju.css"
  },
  "/_nuxt/rKaPdzVf.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"aed-yw0WpCs8+L1dwrqFM4uHSxcf5L0\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 2797,
    "path": "../public/_nuxt/rKaPdzVf.js"
  },
  "/_nuxt/recipes.CeOIUZSo.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"43-OoeUuoyHru7GGDalBmLEt9VAn/8\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 67,
    "path": "../public/_nuxt/recipes.CeOIUZSo.css"
  },
  "/_nuxt/s-cIFmzX.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"51f-OZh85ow0wa4P5AmtQtRirZCMfJs\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 1311,
    "path": "../public/_nuxt/s-cIFmzX.js"
  },
  "/_nuxt/setup.M_9tYmR1.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"167c-pNf5umny/Sps/3B3dS2fOJud5KY\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 5756,
    "path": "../public/_nuxt/setup.M_9tYmR1.css"
  },
  "/_nuxt/site-settings.G6eZ-puo.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"45-V5c8NOE2UWIcjhB8BG3SwbreGKo\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 69,
    "path": "../public/_nuxt/site-settings.G6eZ-puo.css"
  },
  "/_nuxt/tt1x4p7h.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"129c7-GjRyPtpdZ24YhkN/QfTDXSWizEM\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 76231,
    "path": "../public/_nuxt/tt1x4p7h.js"
  },
  "/_nuxt/url.Dy-QvbNa.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"3a-g0okBCW/K/DRrid/5fyTXZeqK+0\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 58,
    "path": "../public/_nuxt/url.Dy-QvbNa.css"
  },
  "/_nuxt/vGe1j6GK.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"b16-8k0+SCHOiGYk4D5pPBQ60OfNtYQ\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 2838,
    "path": "../public/_nuxt/vGe1j6GK.js"
  },
  "/_nuxt/vx7f58_W.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-nnY6HZhy9ZZOiok6iWaXL6HYvoo\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 477,
    "path": "../public/_nuxt/vx7f58_W.js"
  },
  "/_nuxt/wW-1wdm8.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-KAwrF65Ykw1L/e5Xztoo3UqlVL4\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 477,
    "path": "../public/_nuxt/wW-1wdm8.js"
  },
  "/_nuxt/x9rbaJ0U.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"3c8-AYeRMwIjaAifd3L5Fv/B05ElI40\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 968,
    "path": "../public/_nuxt/x9rbaJ0U.js"
  },
  "/_nuxt/xCS_X55R.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"a0b-YyQr5Wgsf1yKsoxX9ZMs5PNiIas\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 2571,
    "path": "../public/_nuxt/xCS_X55R.js"
  },
  "/_nuxt/xJ0KywJ0.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1460-6RspQMqMb2U+Q2zcMShGG/Wffkc\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 5216,
    "path": "../public/_nuxt/xJ0KywJ0.js"
  },
  "/_nuxt/xQe6ZZmZ.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-EG5PpzY8B9xYsGOcXMYgmG/3BcA\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 477,
    "path": "../public/_nuxt/xQe6ZZmZ.js"
  },
  "/_nuxt/yFoWn368.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"17b6-jylAP0YiYqU2V4ohZRUTGvETZXs\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 6070,
    "path": "../public/_nuxt/yFoWn368.js"
  },
  "/_nuxt/xL0Ds2sn.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"13dff-PuuJM0I1B9v+48/VAlG3AKqIUdc\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 81407,
    "path": "../public/_nuxt/xL0Ds2sn.js"
  },
  "/_nuxt/yU-Z8ksI.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"476-ox1F9mowoJZLeH26HWv2lRs4+dw\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 1142,
    "path": "../public/_nuxt/yU-Z8ksI.js"
  },
  "/_nuxt/zip.DyA2MXND.css": {
    "type": "text/css; charset=utf-8",
    "etag": "\"378-wGgD7YIjaBQBqa2TGjY+rk/8TCY\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 888,
    "path": "../public/_nuxt/zip.DyA2MXND.css"
  },
  "/_nuxt/zsCx89Ks.js": {
    "type": "text/javascript; charset=utf-8",
    "etag": "\"1dd-Zc3l3NDZUGoCEwo5xNVKEDj4gnI\"",
    "mtime": "2026-05-11T21:58:34.982Z",
    "size": 477,
    "path": "../public/_nuxt/zsCx89Ks.js"
  },
  "/_nuxt/builds/latest.json": {
    "type": "application/json",
    "etag": "\"47-sZsOnW5u4F3FzJWZmd8OlAejt1c\"",
    "mtime": "2026-05-11T21:58:34.924Z",
    "size": 71,
    "path": "../public/_nuxt/builds/latest.json"
  },
  "/_nuxt/builds/meta/161e7434-9d92-4cb9-ab30-77244548e8d9.json": {
    "type": "application/json",
    "etag": "\"58-FrCdBsBQN0BtEiD70Kr/IfbMDBI\"",
    "mtime": "2026-05-11T21:58:34.922Z",
    "size": 88,
    "path": "../public/_nuxt/builds/meta/161e7434-9d92-4cb9-ab30-77244548e8d9.json"
  }
};

const _DRIVE_LETTER_START_RE = /^[A-Za-z]:\//;
function normalizeWindowsPath(input = "") {
  if (!input) {
    return input;
  }
  return input.replace(/\\/g, "/").replace(_DRIVE_LETTER_START_RE, (r) => r.toUpperCase());
}
const _IS_ABSOLUTE_RE = /^[/\\](?![/\\])|^[/\\]{2}(?!\.)|^[A-Za-z]:[/\\]/;
const _DRIVE_LETTER_RE = /^[A-Za-z]:$/;
function cwd() {
  if (typeof process !== "undefined" && typeof process.cwd === "function") {
    return process.cwd().replace(/\\/g, "/");
  }
  return "/";
}
const resolve = function(...arguments_) {
  arguments_ = arguments_.map((argument) => normalizeWindowsPath(argument));
  let resolvedPath = "";
  let resolvedAbsolute = false;
  for (let index = arguments_.length - 1; index >= -1 && !resolvedAbsolute; index--) {
    const path = index >= 0 ? arguments_[index] : cwd();
    if (!path || path.length === 0) {
      continue;
    }
    resolvedPath = `${path}/${resolvedPath}`;
    resolvedAbsolute = isAbsolute(path);
  }
  resolvedPath = normalizeString(resolvedPath, !resolvedAbsolute);
  if (resolvedAbsolute && !isAbsolute(resolvedPath)) {
    return `/${resolvedPath}`;
  }
  return resolvedPath.length > 0 ? resolvedPath : ".";
};
function normalizeString(path, allowAboveRoot) {
  let res = "";
  let lastSegmentLength = 0;
  let lastSlash = -1;
  let dots = 0;
  let char = null;
  for (let index = 0; index <= path.length; ++index) {
    if (index < path.length) {
      char = path[index];
    } else if (char === "/") {
      break;
    } else {
      char = "/";
    }
    if (char === "/") {
      if (lastSlash === index - 1 || dots === 1) ; else if (dots === 2) {
        if (res.length < 2 || lastSegmentLength !== 2 || res[res.length - 1] !== "." || res[res.length - 2] !== ".") {
          if (res.length > 2) {
            const lastSlashIndex = res.lastIndexOf("/");
            if (lastSlashIndex === -1) {
              res = "";
              lastSegmentLength = 0;
            } else {
              res = res.slice(0, lastSlashIndex);
              lastSegmentLength = res.length - 1 - res.lastIndexOf("/");
            }
            lastSlash = index;
            dots = 0;
            continue;
          } else if (res.length > 0) {
            res = "";
            lastSegmentLength = 0;
            lastSlash = index;
            dots = 0;
            continue;
          }
        }
        if (allowAboveRoot) {
          res += res.length > 0 ? "/.." : "..";
          lastSegmentLength = 2;
        }
      } else {
        if (res.length > 0) {
          res += `/${path.slice(lastSlash + 1, index)}`;
        } else {
          res = path.slice(lastSlash + 1, index);
        }
        lastSegmentLength = index - lastSlash - 1;
      }
      lastSlash = index;
      dots = 0;
    } else if (char === "." && dots !== -1) {
      ++dots;
    } else {
      dots = -1;
    }
  }
  return res;
}
const isAbsolute = function(p) {
  return _IS_ABSOLUTE_RE.test(p);
};
const dirname = function(p) {
  const segments = normalizeWindowsPath(p).replace(/\/$/, "").split("/").slice(0, -1);
  if (segments.length === 1 && _DRIVE_LETTER_RE.test(segments[0])) {
    segments[0] += "/";
  }
  return segments.join("/") || (isAbsolute(p) ? "/" : ".");
};

function readAsset (id) {
  const serverDir = dirname(fileURLToPath(globalThis._importMeta_.url));
  return promises.readFile(resolve(serverDir, assets[id].path))
}

const publicAssetBases = {"/_nuxt/builds/meta/":{"maxAge":31536000},"/_nuxt/builds/":{"maxAge":1},"/_fonts/":{"maxAge":31536000},"/_nuxt/":{"maxAge":31536000}};

function isPublicAssetURL(id = '') {
  if (assets[id]) {
    return true
  }
  for (const base in publicAssetBases) {
    if (id.startsWith(base)) { return true }
  }
  return false
}

function getAsset (id) {
  return assets[id]
}

const METHODS = /* @__PURE__ */ new Set(["HEAD", "GET"]);
const EncodingMap = { gzip: ".gz", br: ".br" };
const _bpZuMG = eventHandler((event) => {
  if (event.method && !METHODS.has(event.method)) {
    return;
  }
  let id = decodePath(
    withLeadingSlash(withoutTrailingSlash(parseURL(event.path).pathname))
  );
  let asset;
  const encodingHeader = String(
    getRequestHeader(event, "accept-encoding") || ""
  );
  const encodings = [
    ...encodingHeader.split(",").map((e) => EncodingMap[e.trim()]).filter(Boolean).sort(),
    ""
  ];
  for (const encoding of encodings) {
    for (const _id of [id + encoding, joinURL(id, "index.html" + encoding)]) {
      const _asset = getAsset(_id);
      if (_asset) {
        asset = _asset;
        id = _id;
        break;
      }
    }
  }
  if (!asset) {
    if (isPublicAssetURL(id)) {
      removeResponseHeader(event, "Cache-Control");
      throw createError$1({ statusCode: 404 });
    }
    return;
  }
  if (asset.encoding !== void 0) {
    appendResponseHeader(event, "Vary", "Accept-Encoding");
  }
  const ifNotMatch = getRequestHeader(event, "if-none-match") === asset.etag;
  if (ifNotMatch) {
    setResponseStatus(event, 304, "Not Modified");
    return "";
  }
  const ifModifiedSinceH = getRequestHeader(event, "if-modified-since");
  const mtimeDate = new Date(asset.mtime);
  if (ifModifiedSinceH && asset.mtime && new Date(ifModifiedSinceH) >= mtimeDate) {
    setResponseStatus(event, 304, "Not Modified");
    return "";
  }
  if (asset.type && !getResponseHeader(event, "Content-Type")) {
    setResponseHeader(event, "Content-Type", asset.type);
  }
  if (asset.etag && !getResponseHeader(event, "ETag")) {
    setResponseHeader(event, "ETag", asset.etag);
  }
  if (asset.mtime && !getResponseHeader(event, "Last-Modified")) {
    setResponseHeader(event, "Last-Modified", mtimeDate.toUTCString());
  }
  if (asset.encoding && !getResponseHeader(event, "Content-Encoding")) {
    setResponseHeader(event, "Content-Encoding", asset.encoding);
  }
  if (asset.size > 0 && !getResponseHeader(event, "Content-Length")) {
    setResponseHeader(event, "Content-Length", asset.size);
  }
  return readAsset(id);
});

const _SxA8c9 = defineEventHandler(() => {});

const _lazy_Lu6n4E = () => import('../routes/api/_..._.mjs');
const _lazy_3jmpeN = () => import('../routes/docs.mjs');
const _lazy_BMfYsD = () => import('../routes/openapi.json.mjs');
const _lazy_f2wZqr = () => import('../routes/renderer.mjs');

const handlers = [
  { route: '', handler: _bpZuMG, lazy: false, middleware: true, method: undefined },
  { route: '/api/**', handler: _lazy_Lu6n4E, lazy: true, middleware: false, method: undefined },
  { route: '/docs', handler: _lazy_3jmpeN, lazy: true, middleware: false, method: undefined },
  { route: '/openapi.json', handler: _lazy_BMfYsD, lazy: true, middleware: false, method: undefined },
  { route: '/__nuxt_error', handler: _lazy_f2wZqr, lazy: true, middleware: false, method: undefined },
  { route: '/__nuxt_island/**', handler: _SxA8c9, lazy: false, middleware: false, method: undefined },
  { route: '/**', handler: _lazy_f2wZqr, lazy: true, middleware: false, method: undefined }
];

function createNitroApp() {
  const config = useRuntimeConfig();
  const hooks = createHooks();
  const captureError = (error, context = {}) => {
    const promise = hooks.callHookParallel("error", error, context).catch((error_) => {
      console.error("Error while capturing another error", error_);
    });
    if (context.event && isEvent(context.event)) {
      const errors = context.event.context.nitro?.errors;
      if (errors) {
        errors.push({ error, context });
      }
      if (context.event.waitUntil) {
        context.event.waitUntil(promise);
      }
    }
  };
  const h3App = createApp({
    debug: destr(false),
    onError: (error, event) => {
      captureError(error, { event, tags: ["request"] });
      return errorHandler(error, event);
    },
    onRequest: async (event) => {
      event.context.nitro = event.context.nitro || { errors: [] };
      const fetchContext = event.node.req?.__unenv__;
      if (fetchContext?._platform) {
        event.context = {
          _platform: fetchContext?._platform,
          // #3335
          ...fetchContext._platform,
          ...event.context
        };
      }
      if (!event.context.waitUntil && fetchContext?.waitUntil) {
        event.context.waitUntil = fetchContext.waitUntil;
      }
      event.fetch = (req, init) => fetchWithEvent(event, req, init, { fetch: localFetch });
      event.$fetch = (req, init) => fetchWithEvent(event, req, init, {
        fetch: $fetch
      });
      event.waitUntil = (promise) => {
        if (!event.context.nitro._waitUntilPromises) {
          event.context.nitro._waitUntilPromises = [];
        }
        event.context.nitro._waitUntilPromises.push(promise);
        if (event.context.waitUntil) {
          event.context.waitUntil(promise);
        }
      };
      event.captureError = (error, context) => {
        captureError(error, { event, ...context });
      };
      await nitroApp$1.hooks.callHook("request", event).catch((error) => {
        captureError(error, { event, tags: ["request"] });
      });
    },
    onBeforeResponse: async (event, response) => {
      await nitroApp$1.hooks.callHook("beforeResponse", event, response).catch((error) => {
        captureError(error, { event, tags: ["request", "response"] });
      });
    },
    onAfterResponse: async (event, response) => {
      await nitroApp$1.hooks.callHook("afterResponse", event, response).catch((error) => {
        captureError(error, { event, tags: ["request", "response"] });
      });
    }
  });
  const router = createRouter({
    preemptive: true
  });
  const nodeHandler = toNodeListener(h3App);
  const localCall = (aRequest) => b(
    nodeHandler,
    aRequest
  );
  const localFetch = (input, init) => {
    if (!input.toString().startsWith("/")) {
      return globalThis.fetch(input, init);
    }
    return C(
      nodeHandler,
      input,
      init
    ).then((response) => normalizeFetchResponse(response));
  };
  const $fetch = createFetch({
    fetch: localFetch,
    Headers: Headers$1,
    defaults: { baseURL: config.app.baseURL }
  });
  globalThis.$fetch = $fetch;
  h3App.use(createRouteRulesHandler({ localFetch }));
  for (const h of handlers) {
    let handler = h.lazy ? lazyEventHandler(h.handler) : h.handler;
    if (h.middleware || !h.route) {
      const middlewareBase = (config.app.baseURL + (h.route || "/")).replace(
        /\/+/g,
        "/"
      );
      h3App.use(middlewareBase, handler);
    } else {
      const routeRules = getRouteRulesForPath(
        h.route.replace(/:\w+|\*\*/g, "_")
      );
      if (routeRules.cache) {
        handler = cachedEventHandler(handler, {
          group: "nitro/routes",
          ...routeRules.cache
        });
      }
      router.use(h.route, handler, h.method);
    }
  }
  h3App.use(config.app.baseURL, router.handler);
  const app = {
    hooks,
    h3App,
    router,
    localCall,
    localFetch,
    captureError
  };
  return app;
}
function runNitroPlugins(nitroApp2) {
  for (const plugin of plugins) {
    try {
      plugin(nitroApp2);
    } catch (error) {
      nitroApp2.captureError(error, { tags: ["plugin"] });
      throw error;
    }
  }
}
const nitroApp$1 = createNitroApp();
function useNitroApp() {
  return nitroApp$1;
}
runNitroPlugins(nitroApp$1);

function defineRenderHandler(render) {
  const runtimeConfig = useRuntimeConfig();
  return eventHandler(async (event) => {
    const nitroApp = useNitroApp();
    const ctx = { event, render, response: void 0 };
    await nitroApp.hooks.callHook("render:before", ctx);
    if (!ctx.response) {
      if (event.path === `${runtimeConfig.app.baseURL}favicon.ico`) {
        setResponseHeader(event, "Content-Type", "image/x-icon");
        return send(
          event,
          "data:image/gif;base64,R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7"
        );
      }
      ctx.response = await ctx.render(event);
      if (!ctx.response) {
        const _currentStatus = getResponseStatus(event);
        setResponseStatus(event, _currentStatus === 200 ? 500 : _currentStatus);
        return send(
          event,
          "No response returned from render handler: " + event.path
        );
      }
    }
    await nitroApp.hooks.callHook("render:response", ctx.response, ctx);
    if (ctx.response.headers) {
      setResponseHeaders(event, ctx.response.headers);
    }
    if (ctx.response.statusCode || ctx.response.statusMessage) {
      setResponseStatus(
        event,
        ctx.response.statusCode,
        ctx.response.statusMessage
      );
    }
    return ctx.response.body;
  });
}

const debug = (...args) => {
};
function GracefulShutdown(server, opts) {
  opts = opts || {};
  const options = Object.assign(
    {
      signals: "SIGINT SIGTERM",
      timeout: 3e4,
      development: false,
      forceExit: true,
      onShutdown: (signal) => Promise.resolve(signal),
      preShutdown: (signal) => Promise.resolve(signal)
    },
    opts
  );
  let isShuttingDown = false;
  const connections = {};
  let connectionCounter = 0;
  const secureConnections = {};
  let secureConnectionCounter = 0;
  let failed = false;
  let finalRun = false;
  function onceFactory() {
    let called = false;
    return (emitter, events, callback) => {
      function call() {
        if (!called) {
          called = true;
          return Reflect.apply(callback, this, arguments);
        }
      }
      for (const e of events) {
        emitter.on(e, call);
      }
    };
  }
  const signals = options.signals.split(" ").map((s) => s.trim()).filter((s) => s.length > 0);
  const once = onceFactory();
  once(process, signals, (signal) => {
    debug("received shut down signal", signal);
    shutdown(signal).then(() => {
      if (options.forceExit) {
        process.exit(failed ? 1 : 0);
      }
    }).catch((error) => {
      debug("server shut down error occurred", error);
      process.exit(1);
    });
  });
  function isFunction(functionToCheck) {
    const getType = Object.prototype.toString.call(functionToCheck);
    return /^\[object\s([A-Za-z]+)?Function]$/.test(getType);
  }
  function destroy(socket, force = false) {
    if (socket._isIdle && isShuttingDown || force) {
      socket.destroy();
      if (socket.server instanceof http.Server) {
        delete connections[socket._connectionId];
      } else {
        delete secureConnections[socket._connectionId];
      }
    }
  }
  function destroyAllConnections(force = false) {
    debug("Destroy Connections : " + (force ? "forced close" : "close"));
    let counter = 0;
    let secureCounter = 0;
    for (const key of Object.keys(connections)) {
      const socket = connections[key];
      const serverResponse = socket._httpMessage;
      if (serverResponse && !force) {
        if (!serverResponse.headersSent) {
          serverResponse.setHeader("connection", "close");
        }
      } else {
        counter++;
        destroy(socket);
      }
    }
    debug("Connections destroyed : " + counter);
    debug("Connection Counter    : " + connectionCounter);
    for (const key of Object.keys(secureConnections)) {
      const socket = secureConnections[key];
      const serverResponse = socket._httpMessage;
      if (serverResponse && !force) {
        if (!serverResponse.headersSent) {
          serverResponse.setHeader("connection", "close");
        }
      } else {
        secureCounter++;
        destroy(socket);
      }
    }
    debug("Secure Connections destroyed : " + secureCounter);
    debug("Secure Connection Counter    : " + secureConnectionCounter);
  }
  server.on("request", (req, res) => {
    req.socket._isIdle = false;
    if (isShuttingDown && !res.headersSent) {
      res.setHeader("connection", "close");
    }
    res.on("finish", () => {
      req.socket._isIdle = true;
      destroy(req.socket);
    });
  });
  server.on("connection", (socket) => {
    if (isShuttingDown) {
      socket.destroy();
    } else {
      const id = connectionCounter++;
      socket._isIdle = true;
      socket._connectionId = id;
      connections[id] = socket;
      socket.once("close", () => {
        delete connections[socket._connectionId];
      });
    }
  });
  server.on("secureConnection", (socket) => {
    if (isShuttingDown) {
      socket.destroy();
    } else {
      const id = secureConnectionCounter++;
      socket._isIdle = true;
      socket._connectionId = id;
      secureConnections[id] = socket;
      socket.once("close", () => {
        delete secureConnections[socket._connectionId];
      });
    }
  });
  process.on("close", () => {
    debug("closed");
  });
  function shutdown(sig) {
    function cleanupHttp() {
      destroyAllConnections();
      debug("Close http server");
      return new Promise((resolve, reject) => {
        server.close((err) => {
          if (err) {
            return reject(err);
          }
          return resolve(true);
        });
      });
    }
    debug("shutdown signal - " + sig);
    if (options.development) {
      debug("DEV-Mode - immediate forceful shutdown");
      return process.exit(0);
    }
    function finalHandler() {
      if (!finalRun) {
        finalRun = true;
        if (options.finally && isFunction(options.finally)) {
          debug("executing finally()");
          options.finally();
        }
      }
      return Promise.resolve();
    }
    function waitForReadyToShutDown(totalNumInterval) {
      debug(`waitForReadyToShutDown... ${totalNumInterval}`);
      if (totalNumInterval === 0) {
        debug(
          `Could not close connections in time (${options.timeout}ms), will forcefully shut down`
        );
        return Promise.resolve(true);
      }
      const allConnectionsClosed = Object.keys(connections).length === 0 && Object.keys(secureConnections).length === 0;
      if (allConnectionsClosed) {
        debug("All connections closed. Continue to shutting down");
        return Promise.resolve(false);
      }
      debug("Schedule the next waitForReadyToShutdown");
      return new Promise((resolve) => {
        setTimeout(() => {
          resolve(waitForReadyToShutDown(totalNumInterval - 1));
        }, 250);
      });
    }
    if (isShuttingDown) {
      return Promise.resolve();
    }
    debug("shutting down");
    return options.preShutdown(sig).then(() => {
      isShuttingDown = true;
      cleanupHttp();
    }).then(() => {
      const pollIterations = options.timeout ? Math.round(options.timeout / 250) : 0;
      return waitForReadyToShutDown(pollIterations);
    }).then((force) => {
      debug("Do onShutdown now");
      if (force) {
        destroyAllConnections(force);
      }
      return options.onShutdown(sig);
    }).then(finalHandler).catch((error) => {
      const errString = typeof error === "string" ? error : JSON.stringify(error);
      debug(errString);
      failed = true;
      throw errString;
    });
  }
  function shutdownManual() {
    return shutdown("manual");
  }
  return shutdownManual;
}

function getGracefulShutdownConfig() {
  return {
    disabled: !!process.env.NITRO_SHUTDOWN_DISABLED,
    signals: (process.env.NITRO_SHUTDOWN_SIGNALS || "SIGTERM SIGINT").split(" ").map((s) => s.trim()),
    timeout: Number.parseInt(process.env.NITRO_SHUTDOWN_TIMEOUT || "", 10) || 3e4,
    forceExit: !process.env.NITRO_SHUTDOWN_NO_FORCE_EXIT
  };
}
function setupGracefulShutdown(listener, nitroApp) {
  const shutdownConfig = getGracefulShutdownConfig();
  if (shutdownConfig.disabled) {
    return;
  }
  GracefulShutdown(listener, {
    signals: shutdownConfig.signals.join(" "),
    timeout: shutdownConfig.timeout,
    forceExit: shutdownConfig.forceExit,
    onShutdown: async () => {
      await new Promise((resolve) => {
        const timeout = setTimeout(() => {
          console.warn("Graceful shutdown timeout, force exiting...");
          resolve();
        }, shutdownConfig.timeout);
        nitroApp.hooks.callHook("close").catch((error) => {
          console.error(error);
        }).finally(() => {
          clearTimeout(timeout);
          resolve();
        });
      });
    }
  });
}

const cert = process.env.NITRO_SSL_CERT;
const key = process.env.NITRO_SSL_KEY;
const nitroApp = useNitroApp();
const server = cert && key ? new Server({ key, cert }, toNodeListener(nitroApp.h3App)) : new Server$1(toNodeListener(nitroApp.h3App));
const port = destr(process.env.NITRO_PORT || process.env.PORT) || 3e3;
const host = process.env.NITRO_HOST || process.env.HOST;
const path = process.env.NITRO_UNIX_SOCKET;
const listener = server.listen(path ? { path } : { port, host }, (err) => {
  if (err) {
    console.error(err);
    process.exit(1);
  }
  const protocol = cert && key ? "https" : "http";
  const addressInfo = listener.address();
  if (typeof addressInfo === "string") {
    console.log(`Listening on unix socket ${addressInfo}`);
    return;
  }
  const baseURL = (useRuntimeConfig().app.baseURL || "").replace(/\/$/, "");
  const url = `${protocol}://${addressInfo.family === "IPv6" ? `[${addressInfo.address}]` : addressInfo.address}:${addressInfo.port}${baseURL}`;
  console.log(`Listening on ${url}`);
});
trapUnhandledNodeErrors();
setupGracefulShutdown(listener, nitroApp);
const nodeServer = {};

export { joinRelativeURL as a, getResponseStatus as b, defineRenderHandler as c, defineEventHandler as d, getQuery as e, createError$1 as f, getResponseStatusText as g, destr as h, getRouteRules as i, joinURL as j, useNitroApp as k, nodeServer as n, proxyRequest as p, sendRedirect as s, useRuntimeConfig as u };
//# sourceMappingURL=nitro.mjs.map
