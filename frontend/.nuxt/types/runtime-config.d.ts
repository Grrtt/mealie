import { RuntimeConfig as UserRuntimeConfig, PublicRuntimeConfig as UserPublicRuntimeConfig } from 'nuxt/schema'
  interface SharedRuntimeConfig {
   app: {
      buildId: string,

      baseURL: string,

      buildAssetsDir: string,

      cdnURL: string,
   },

   sessionPassword: string,

   apiUrl: string,

   nitro: {
      envPrefix: string,
   },
  }
  interface SharedPublicRuntimeConfig {
   AUTH_TOKEN: string,

   GLOBAL_MIDDLEWARE: string,

   SUB_PATH: string,

   useDark: boolean,

   themes: {
      dark: {
         primary: string,

         accent: string,

         secondary: string,

         success: string,

         info: string,

         warning: string,

         error: string,

         background: string,
      },

      light: {
         primary: string,

         accent: string,

         secondary: string,

         success: string,

         info: string,

         warning: string,

         error: string,
      },
   },

   i18n: {
      baseUrl: string,

      defaultLocale: string,

      defaultDirection: string,

      strategy: string,

      lazy: boolean,

      rootRedirect: any,

      routesNameSeparator: string,

      defaultLocaleRouteNameSuffix: string,

      skipSettingLocaleOnNavigate: boolean,

      differentDomains: boolean,

      trailingSlash: boolean,

      locales: Array<{

      }>,

      detectBrowserLanguage: {
         alwaysRedirect: boolean,

         cookieCrossOrigin: boolean,

         cookieDomain: any,

         cookieKey: string,

         cookieSecure: boolean,

         fallbackLocale: string,

         redirectOn: string,

         useCookie: boolean,
      },

      experimental: {
         localeDetector: string,

         switchLocalePathLinkSSR: boolean,

         autoImportTranslationFunctions: boolean,

         typedPages: boolean,

         typedOptionsAndMessages: boolean,

         generatedLocaleFilePathFormat: string,

         alternateLinkCanonicalQueries: boolean,

         hmr: boolean,
      },

      multiDomainLocales: boolean,

      domainLocales: {
         "af-ZA": {
            domain: string,
         },

         "ar-SA": {
            domain: string,
         },

         "bg-BG": {
            domain: string,
         },

         "ca-ES": {
            domain: string,
         },

         "cs-CZ": {
            domain: string,
         },

         "da-DK": {
            domain: string,
         },

         "de-DE": {
            domain: string,
         },

         "el-GR": {
            domain: string,
         },

         "en-GB": {
            domain: string,
         },

         "en-US": {
            domain: string,
         },

         "es-ES": {
            domain: string,
         },

         "et-EE": {
            domain: string,
         },

         "fi-FI": {
            domain: string,
         },

         "fr-BE": {
            domain: string,
         },

         "fr-CA": {
            domain: string,
         },

         "fr-FR": {
            domain: string,
         },

         "gl-ES": {
            domain: string,
         },

         "he-IL": {
            domain: string,
         },

         "hr-HR": {
            domain: string,
         },

         "hu-HU": {
            domain: string,
         },

         "is-IS": {
            domain: string,
         },

         "it-IT": {
            domain: string,
         },

         "ja-JP": {
            domain: string,
         },

         "ko-KR": {
            domain: string,
         },

         "lt-LT": {
            domain: string,
         },

         "lv-LV": {
            domain: string,
         },

         "nl-NL": {
            domain: string,
         },

         "no-NO": {
            domain: string,
         },

         "pl-PL": {
            domain: string,
         },

         "pt-BR": {
            domain: string,
         },

         "pt-PT": {
            domain: string,
         },

         "ro-RO": {
            domain: string,
         },

         "ru-RU": {
            domain: string,
         },

         "sk-SK": {
            domain: string,
         },

         "sl-SI": {
            domain: string,
         },

         "sr-SP": {
            domain: string,
         },

         "sv-SE": {
            domain: string,
         },

         "tr-TR": {
            domain: string,
         },

         "uk-UA": {
            domain: string,
         },

         "vi-VN": {
            domain: string,
         },

         "zh-CN": {
            domain: string,
         },

         "zh-TW": {
            domain: string,
         },
      },
   },
  }
declare module '@nuxt/schema' {
  interface RuntimeConfig extends UserRuntimeConfig {}
  interface PublicRuntimeConfig extends UserPublicRuntimeConfig {}
}
declare module 'nuxt/schema' {
  interface RuntimeConfig extends SharedRuntimeConfig {}
  interface PublicRuntimeConfig extends SharedPublicRuntimeConfig {}
}
declare module 'vue' {
        interface ComponentCustomProperties {
          $config: UserRuntimeConfig
        }
      }