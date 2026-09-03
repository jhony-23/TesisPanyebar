import { httpClient } from './httpClient.js'

export const authService = {
  login: (credentials) => httpClient.post('/auth/login', credentials),
  me: (accessToken) => httpClient.get('/auth/me', { accessToken }),
}