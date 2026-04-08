/**
 * Authentication API service.
 * Communicates with POST /api/v1/auth/* endpoints.
 */

import apiClient from './apiClient';
import type {
  AuthResult,
  LoginRequest,
  RegisterRequest,
  RefreshRequest,
} from '../types/api';

export const authService = {
  /**
   * POST /auth/register
   * Register a new user with phone, name, and password.
   */
  async register(data: RegisterRequest): Promise<AuthResult> {
    const response = await apiClient.post<AuthResult>('/auth/register', data);
    return response.data;
  },

  /**
   * POST /auth/login
   * Authenticate with phone and password.
   */
  async login(data: LoginRequest): Promise<AuthResult> {
    const response = await apiClient.post<AuthResult>('/auth/login', data);
    return response.data;
  },

  /**
   * POST /auth/refresh
   * Refresh the access token using a refresh token.
   */
  async refresh(data: RefreshRequest): Promise<AuthResult> {
    const response = await apiClient.post<AuthResult>('/auth/refresh', data);
    return response.data;
  },

  /**
   * POST /auth/logout
   * Revoke the refresh token. Requires valid access token.
   */
  async logout(): Promise<void> {
    await apiClient.post('/auth/logout');
  },
} as const;
