import path from "node:path";
import { searchForWorkspaceRoot, defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

const subPath = process.env.SUB_PATH?.trim() || "/";
const normalizedBase = subPath === "/" ? "/" : `${subPath.replace(/^\/?/, "/").replace(/\/?$/, "")}/`;

export default defineConfig({
  base: normalizedBase,
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    host: "0.0.0.0",
    port: 4173,
    fs: {
      allow: [searchForWorkspaceRoot(process.cwd()), path.resolve(__dirname, "..")],
    },
  },
  preview: {
    host: "0.0.0.0",
    port: 4173,
  },
  test: {
    environment: "jsdom",
  },
});
