import { invoke } from '@tauri-apps/api/core'

// API client that wraps Tauri commands
export async function apiGet<T>(endpoint: string): Promise<T> {
  const response = await invoke<string>('api_get', { endpoint })
  return JSON.parse(response)
}

export async function apiPost<T>(endpoint: string, body?: unknown): Promise<T> {
  const response = await invoke<string>('api_post', {
    endpoint,
    body: body ? JSON.stringify(body) : null
  })
  return JSON.parse(response)
}

export async function apiPut<T>(endpoint: string, body?: unknown): Promise<T> {
  const response = await invoke<string>('api_put', {
    endpoint,
    body: body ? JSON.stringify(body) : null
  })
  return JSON.parse(response)
}

export async function apiPatch<T>(endpoint: string, body?: unknown): Promise<T> {
  const response = await invoke<string>('api_patch', {
    endpoint,
    body: body ? JSON.stringify(body) : null
  })
  return JSON.parse(response)
}

export async function apiDelete<T>(endpoint: string): Promise<T> {
  const response = await invoke<string>('api_delete', { endpoint })
  return JSON.parse(response)
}
