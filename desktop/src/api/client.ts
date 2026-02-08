import { invoke } from '@tauri-apps/api/core'

const MAX_RETRIES = 3
const BASE_DELAY_MS = 1000

async function delay(ms: number): Promise<void> {
  return new Promise(resolve => { setTimeout(resolve, ms) })
}

async function withRetry<T>(fn: () => Promise<T>, retries = MAX_RETRIES): Promise<T> {
  let lastError: Error | undefined
  for (let i = 0; i < retries; i++) {
    try {
      return await fn()
    } catch (error) {
      lastError = error instanceof Error ? error : new Error(String(error))
      if (i < retries - 1) {
        await delay(Math.pow(2, i) * BASE_DELAY_MS)
      }
    }
  }
  throw lastError ?? new Error('Unknown error after retries')
}

// API client that wraps Tauri commands
export async function apiGet<T>(endpoint: string): Promise<T> {
  return withRetry(async () => {
    const response = await invoke<string>('api_get', { endpoint })
    return JSON.parse(response)
  })
}

export async function apiPost<T>(endpoint: string, body?: unknown): Promise<T> {
  return withRetry(async () => {
    const response = await invoke<string>('api_post', {
      endpoint,
      body: body ? JSON.stringify(body) : null
    })
    return JSON.parse(response)
  })
}

export async function apiPut<T>(endpoint: string, body?: unknown): Promise<T> {
  return withRetry(async () => {
    const response = await invoke<string>('api_put', {
      endpoint,
      body: body ? JSON.stringify(body) : null
    })
    return JSON.parse(response)
  })
}

export async function apiPatch<T>(endpoint: string, body?: unknown): Promise<T> {
  return withRetry(async () => {
    const response = await invoke<string>('api_patch', {
      endpoint,
      body: body ? JSON.stringify(body) : null
    })
    return JSON.parse(response)
  })
}

export async function apiDelete<T>(endpoint: string): Promise<T> {
  return withRetry(async () => {
    const response = await invoke<string>('api_delete', { endpoint })
    return JSON.parse(response)
  })
}
