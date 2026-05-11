import type { NavigationGuard } from 'vue-router'
export type MiddlewareKey = "admin-only" | "advanced-only" | "can-manage-household-only" | "can-manage-only" | "can-organize-only" | "group-only"
declare module 'nuxt/app' {
  interface PageMeta {
    middleware?: MiddlewareKey | NavigationGuard | Array<MiddlewareKey | NavigationGuard>
  }
}