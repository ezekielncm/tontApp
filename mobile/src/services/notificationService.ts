/**
 * Push notification service using expo-notifications.
 *
 * Responsibilities:
 * - Request push notification permission (Android only at MVP)
 * - Retrieve Expo push token (backed by FCM on Android)
 * - Send token to backend only when it differs from the stored value
 * - Configure foreground notification handler
 *
 * Uses AsyncStorage flag 'fcm_permission_asked' to ensure the permission
 * dialog is shown at most once.
 */

import * as Notifications from 'expo-notifications';
import * as Device from 'expo-device';
import AsyncStorage from '@react-native-async-storage/async-storage';
import { Platform } from 'react-native';
import apiClient from './apiClient';
import { useAuthStore } from '../store/authStore';

const PERMISSION_ASKED_KEY = 'fcm_permission_asked';

/**
 * Configure how notifications are displayed when the app is in the foreground.
 * Must be called once at app startup (e.g. in App.tsx).
 */
export function setupNotificationHandler(): void {
  Notifications.setNotificationHandler({
    handleNotification: async () => ({
      shouldShowBanner: true,
      shouldShowList: true,
      shouldPlaySound: true,
      shouldSetBadge: false,
    }),
  });
}

/**
 * Request push notification permission and register the device token.
 *
 * Flow:
 * 1. Check AsyncStorage flag — skip if permission was already asked.
 * 2. Request permission via expo-notifications.
 * 3. Mark the flag so we never prompt again.
 * 4. If granted, get the Expo push token and send it to the backend.
 * 5. If denied, the app continues normally (no blocking screen).
 */
export async function registerForPushNotifications(): Promise<void> {
  // Only real devices can receive push notifications
  if (!Device.isDevice) {
    return;
  }

  // Android only at MVP
  if (Platform.OS !== 'android') {
    return;
  }

  // Check if we already asked for permission
  const alreadyAsked = await AsyncStorage.getItem(PERMISSION_ASKED_KEY);
  if (alreadyAsked === 'true') {
    // Permission was already asked — still try to get the token
    // in case it was granted previously
    const { status } = await Notifications.getPermissionsAsync();
    if (status === 'granted') {
      await retrieveAndSendToken();
    }
    return;
  }

  // Request permission
  const { status: existingStatus } = await Notifications.getPermissionsAsync();
  let finalStatus = existingStatus;

  if (existingStatus !== 'granted') {
    const { status } = await Notifications.requestPermissionsAsync();
    finalStatus = status;
  }

  // Mark as asked regardless of the result
  await AsyncStorage.setItem(PERMISSION_ASKED_KEY, 'true');

  if (finalStatus !== 'granted') {
    // User denied — app continues normally
    return;
  }

  // Set up the Android notification channel
  await Notifications.setNotificationChannelAsync('default', {
    name: 'TontinesApp',
    importance: Notifications.AndroidImportance.HIGH,
    vibrationPattern: [0, 250, 250, 250],
    lightColor: '#1B5E20',
  });

  await retrieveAndSendToken();
}

/**
 * Retrieve the Expo push token and send it to the backend if it differs
 * from the currently stored value in the auth store.
 */
async function retrieveAndSendToken(): Promise<void> {
  try {
    const tokenData = await Notifications.getExpoPushTokenAsync();
    const token = tokenData.data;

    // Only send to backend if the token changed
    const currentToken = useAuthStore.getState().fcmToken;
    if (token && token !== currentToken) {
      await sendTokenToBackend(token);
      useAuthStore.getState().setFcmToken(token);
    }
  } catch {
    // Silently fail — notifications are complementary to SMS
  }
}

/**
 * Send the push token to the backend endpoint.
 */
export async function sendTokenToBackend(token: string): Promise<void> {
  await apiClient.post('/membres/moi/fcm-token', { token });
}
