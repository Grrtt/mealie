import type { ComputedRef, MaybeRef } from 'vue'

type ComponentProps<T> = T extends new(...args: any) => { $props: infer P } ? NonNullable<P>
  : T extends (props: infer P, ...args: any) => any ? P
  : {}

declare module 'nuxt/app' {
  interface NuxtLayouts {
    admin: ComponentProps<typeof import("/home/grrtt/dev/mealie/frontend/app/layouts/admin.vue").default>
    basic: ComponentProps<typeof import("/home/grrtt/dev/mealie/frontend/app/layouts/basic.vue").default>
    blank: ComponentProps<typeof import("/home/grrtt/dev/mealie/frontend/app/layouts/blank.vue").default>
    default: ComponentProps<typeof import("/home/grrtt/dev/mealie/frontend/app/layouts/default.vue").default>
    error: ComponentProps<typeof import("/home/grrtt/dev/mealie/frontend/app/layouts/error.vue").default>
  }
  export type LayoutKey = keyof NuxtLayouts extends never ? string : keyof NuxtLayouts
  interface PageMeta {
    layout?: MaybeRef<LayoutKey | false> | ComputedRef<LayoutKey | false> | {
      [K in LayoutKey]: {
        name?: MaybeRef<K | false> | ComputedRef<K | false>
        props?: NuxtLayouts[K]
      }
    }[LayoutKey]
  }
}