/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_BACKEND_URL: string;
  readonly VITE_BACKEND_SCOPE: string;
  readonly VITE_PUBLIC_APP_ID: string;
  readonly VITE_PUBLIC_AUTHORITY_URL: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
